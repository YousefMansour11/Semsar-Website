using Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class HybridCacheService : ICacheService, IDisposable
    {
        private readonly IMemoryCache _l1;
        private readonly IDistributedCache _l2;
        private readonly IConnectionMultiplexer? _redis;
        private readonly ILogger<HybridCacheService>? _logger;
        private readonly IAppMetrics? _metrics;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        private readonly ConcurrentDictionary<string, byte> _registry = new();
        private readonly ISubscriber? _subscriber;
        private readonly TimeSpan _l1DefaultTtl = TimeSpan.FromSeconds(15);
        private const string ChannelName = "__cache_invalidation__";

        public HybridCacheService(IMemoryCache l1, IDistributedCache l2, IConnectionMultiplexer? redis = null, ILogger<HybridCacheService>? logger = null, IAppMetrics? metrics = null)
        {
            _l1 = l1;
            _l2 = l2;
            _redis = redis;
            _logger = logger;
            _metrics = metrics;

            if (_redis != null)
            {
                _subscriber = _redis.GetSubscriber();
                _subscriber.Subscribe(RedisChannel.Literal(ChannelName), (channel, message) =>
                {
                    try
                    {
                        var prefix = message.ToString();
                        InvalidateLocalByPrefix(prefix);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Redis pub/sub invalidation handler failed");
                    }
                });
            }
        }

        public T? Get<T>(string key)
        {
            if (_l1.TryGetValue(key, out object? cached) && cached is T t)
            {
                _metrics?.Increment("cache.l1.hit");
                return t;
            }

            try
            {
                var json = _l2.GetString(key);
                if (string.IsNullOrEmpty(json))
                {
                    _metrics?.Increment("cache.miss");
                    return default;
                }

                var value = JsonSerializer.Deserialize<T>(json);
                if (value != null)
                {
                    _metrics?.Increment("cache.l2.hit");
                    _l1.Set(key, value, _l1DefaultTtl);
                }
                return value;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "L2 cache get failed for key {Key}", key);
                _metrics?.Increment("cache.error");
                return default;
            }
        }

        public void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null)
        {
            var ttl = absoluteExpiration ?? TimeSpan.FromMinutes(30);

            var json = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };

            try
            {
                _l2.SetString(key, json, options);
                _metrics?.Increment("cache.l2.set");
            }
            catch (Exception ex) { _logger?.LogWarning(ex, "L2 cache set failed for key {Key}", key); }

            _l1.Set(key, value, ttl);
            RegisterKey(key);
        }

        public void Remove(string key)
        {
            try { _l2.Remove(key); }
            catch (Exception ex) { _logger?.LogWarning(ex, "L2 cache remove failed for key {Key}", key); }

            _l1.Remove(key);
            _registry.TryRemove(key, out _);
        }

        public void RegisterKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            _registry.TryAdd(key, 0);
        }

        public void InvalidateByPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return;

            _metrics?.Increment("cache.invalidate");
            var count = InvalidateLocalByPrefix(prefix);
            _metrics?.Gauge("cache.invalidated_count", count);

            if (_subscriber != null)
            {
                try
                {
                    _subscriber.Publish(RedisChannel.Literal(ChannelName), prefix);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Redis pub/sub publish failed for prefix {Prefix}", prefix);
                }
            }
        }

        private int InvalidateLocalByPrefix(string prefix)
        {
            var count = 0;
            var snapshot = _registry.Keys.ToArray();
            foreach (var k in snapshot)
            {
                if (!string.IsNullOrEmpty(k) && k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    try { _l1.Remove(k); }
                    catch (Exception ex) { _logger?.LogWarning(ex, "L1 cache remove failed for key {Key}", k); }
                    try { _l2.Remove(k); }
                    catch (Exception ex) { _logger?.LogWarning(ex, "L2 cache remove failed for key {Key}", k); }
                    _registry.TryRemove(k, out _);
                    count++;
                }
            }
            return count;
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

        public void Dispose()
        {
            foreach (var sl in _locks.Values)
            {
                try { sl.Dispose(); } catch (Exception ex) { _logger?.LogWarning(ex, "Failed to dispose semaphore"); }
            }
            _locks.Clear();

            if (_subscriber != null)
            {
                try
                {
                    _subscriber.Unsubscribe(RedisChannel.Literal(ChannelName));
                    _subscriber.UnsubscribeAll();
                }
                catch (Exception ex) { _logger?.LogWarning(ex, "Failed to unsubscribe from Redis invalidation channel"); }
            }
        }
    }
}
