using System.Collections.Concurrent;

namespace API.Services;

public class MemoryRateLimitStore : IDistributedRateLimitStore
{
    private static readonly ConcurrentDictionary<string, SlidingWindow> _stores = new();
    private static readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
    private static DateTime _lastCleanup = DateTime.UtcNow;

    public Task<bool> CheckAndIncrementAsync(string key, int maxRequests, TimeSpan window, CancellationToken ct = default)
    {
        PerformCleanup();

        var sw = _stores.GetOrAdd(key, _ => new SlidingWindow());
        lock (sw)
        {
            var now = DateTime.UtcNow;
            sw.Timestamps.RemoveAll(ts => now - ts > window);

            if (sw.Timestamps.Count >= maxRequests)
                return Task.FromResult(false);

            sw.Timestamps.Add(now);
            return Task.FromResult(true);
        }
    }

    public Task<long> GetCurrentCountAsync(string key, CancellationToken ct = default)
    {
        if (_stores.TryGetValue(key, out var sw))
        {
            lock (sw)
            {
                var now = DateTime.UtcNow;
                sw.Timestamps.RemoveAll(ts => now - ts > TimeSpan.FromMinutes(1));
                return Task.FromResult((long)sw.Timestamps.Count);
            }
        }
        return Task.FromResult(0L);
    }

    public Task ResetAsync(string key, CancellationToken ct = default)
    {
        _stores.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    private static void PerformCleanup()
    {
        var now = DateTime.UtcNow;
        if (now - _lastCleanup < _cleanupInterval)
            return;
        _lastCleanup = now;

        var staleKeys = _stores
            .Where(kvp =>
            {
                lock (kvp.Value)
                {
                    return kvp.Value.Timestamps.Count == 0 ||
                           now - kvp.Value.Timestamps[^1] > TimeSpan.FromHours(1);
                }
            })
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in staleKeys)
        {
            _stores.TryRemove(key, out _);
        }
    }

    private class SlidingWindow
    {
        public List<DateTime> Timestamps { get; set; } = new();
    }
}
