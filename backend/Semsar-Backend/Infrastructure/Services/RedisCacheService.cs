using Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer? _redis;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        private readonly ILogger<RedisCacheService>? _logger;

        public RedisCacheService(IDistributedCache cache, IConnectionMultiplexer? redis = null, ILogger<RedisCacheService>? logger = null)
        {
            _cache = cache;
            _redis = redis;
            _logger = logger;
        }

        public T? Get<T>(string key)
        {
            try
            {
                var json = _cache.GetString(key);
                if (string.IsNullOrEmpty(json)) return default;
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Redis cache get failed for key {Key}", key);
                return default;
            }
        }

        public void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromMinutes(30)
            };

            var json = JsonSerializer.Serialize(value);
            _cache.SetString(key, json, options);

            if (_redis != null)
            {
                try
                {
                    var db = _redis.GetDatabase();
                    var setKey = "__cache_keys__";
                    db.SetAdd(setKey, key);
                    db.KeyExpire(setKey, TimeSpan.FromDays(7));

                    var prefix = ExtractPrefix(key);
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        var prefixSetKey = $"__prefix_set__{prefix}";
                        db.SetAdd(prefixSetKey, key);
                        db.KeyExpire(prefixSetKey, TimeSpan.FromDays(7));
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to register cache key {Key} in Redis", key);
                }
            }
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
            if (_redis != null)
            {
                try
                {
                    var db = _redis.GetDatabase();
                    var setKey = "__cache_keys__";
                    db.SetRemove(setKey, key);

                    var prefix = ExtractPrefix(key);
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        var prefixSetKey = $"__prefix_set__{prefix}";
                        db.SetRemove(prefixSetKey, key);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to remove from prefix set in Redis for key {Key}", key);
                }
            }
        }

        public void RegisterKey(string key)
        {
            if (_redis == null) return;
            try
            {
                var db = _redis.GetDatabase();
                var setKey = "__cache_keys__";
                db.SetAdd(setKey, key);
                db.KeyExpire(setKey, TimeSpan.FromDays(7));
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to register cache key {Key} in Redis", key);
            }
        }

        public void InvalidateByPrefix(string prefix)
        {
            if (_redis == null) return;
            try
            {
                var db = _redis.GetDatabase();
                var setKey = "__cache_keys__";
                _logger?.LogWarning("InvalidateByPrefix scans entire key set (O(N)) for prefix {Prefix}", prefix);
                var members = db.SetMembers(setKey);

                var toRemove = members
                    .Where(m => m.HasValue && m.ToString().StartsWith(prefix, StringComparison.Ordinal))
                    .ToArray();

                if (toRemove.Length > 0)
                {
                    var keys = toRemove.Select(m => (RedisKey)m.ToString()).ToArray();
                    db.KeyDelete(keys);
                    db.SetRemove(setKey, toRemove);
                }

                var prefixSetKey = $"__prefix_set__{prefix}";
                if (db.KeyExists(prefixSetKey))
                {
                    var prefixMembers = db.SetMembers(prefixSetKey);
                    if (prefixMembers.Length > 0)
                    {
                        var prefixKeys = prefixMembers.Select(m => (RedisKey)m.ToString()).ToArray();
                        db.KeyDelete(prefixKeys);
                    }
                    db.KeyDelete(prefixSetKey);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Redis InvalidateByPrefix failed for prefix {Prefix}", prefix);
            }
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null)
        {
            var cached = Get<T>(key);
            if (cached != null) return cached;

            SemaphoreSlim? semaphore = null;
            try
            {
                semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
                await semaphore.WaitAsync();

                cached = Get<T>(key);
                if (cached != null) return cached;

                var value = await factory();
                Set(key, value, absoluteExpiration);
                return value;
            }
            finally
            {
                semaphore?.Release();
                if (semaphore?.CurrentCount == 1)
                    _locks.TryRemove(key, out _);
            }
        }

        private static string ExtractPrefix(string key)
        {
            var underscoreIndex = key.IndexOf('_');
            if (underscoreIndex > 0)
            {
                var secondUnderscore = key.IndexOf('_', underscoreIndex + 1);
                if (secondUnderscore > 0)
                {
                    return key.Substring(0, secondUnderscore + 1);
                }
                return key.Substring(0, underscoreIndex + 1);
            }
            return string.Empty;
        }
    }
}
