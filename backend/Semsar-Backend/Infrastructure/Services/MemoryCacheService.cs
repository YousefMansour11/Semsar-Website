using Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        private readonly ConcurrentDictionary<string, byte> _registry = new();
        private readonly ILogger<MemoryCacheService>? _logger;
        private const int RegistryWarnThreshold = 10000;

        public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService>? logger = null)
        {
            _cache = cache;
            _logger = logger;
        }

        public T? Get<T>(string key)
        {
            if (_cache.TryGetValue(key, out object? val) && val is T t) return t;
            return default;
        }

        public void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null)
        {
            var opts = new MemoryCacheEntryOptions();
            if (absoluteExpiration.HasValue)
                opts.AbsoluteExpirationRelativeToNow = absoluteExpiration;

            opts.RegisterPostEvictionCallback((k, v, reason, state) =>
            {
                try
                {
                    if (k is string keyStr)
                    {
                        _registry.TryRemove(keyStr, out _);
                    }
                }
                catch (Exception ex)
                {
                    try { _logger?.LogWarning(ex, "Eviction callback failed for cache key {Key}", k); } catch (Exception logEx) { _logger?.LogError(logEx, "Failed to log eviction callback error"); }
                }
            });

            _cache.Set(key, value, opts);
            try { RegisterKey(key); }
            catch (Exception ex) { _logger?.LogWarning(ex, "Failed to register cache key {Key}", key); }
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
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
            var snapshot = _registry.Keys.ToArray();
            var toRemove = new List<string>();
            foreach (var k in snapshot)
            {
                if (!string.IsNullOrEmpty(k) && k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) toRemove.Add(k);
            }
            try
            {
                if (snapshot.Length > RegistryWarnThreshold)
                {
                    _logger?.LogWarning("Cache registry size is large ({Size}) while invalidating prefix {Prefix}", snapshot.Length, prefix);
                }
            }
            catch (Exception ex) { _logger?.LogWarning(ex, "Failed to log cache registry size warning"); }
            foreach (var k in toRemove)
            {
                try { _cache.Remove(k); }
                catch (Exception ex) { _logger?.LogWarning(ex, "Failed to remove cache key {Key}", k); }
                finally { _registry.TryRemove(k, out _); }
            }
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null)
        {
            if (_cache.TryGetValue(key, out object? existing) && existing is T cached)
                return cached;

            SemaphoreSlim? semaphore = null;
            try
            {
                semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
                await semaphore.WaitAsync();

                // Double-check after acquiring lock
                if (_cache.TryGetValue(key, out object? recheck) && recheck is T rechecked)
                    return rechecked;

                var value = await factory();
                Set(key, value, absoluteExpiration);
                return value;
            }
            finally
            {
                semaphore?.Release();
            }
        }
    }
}
