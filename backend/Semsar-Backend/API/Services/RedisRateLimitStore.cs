using System.Collections.Concurrent;
using StackExchange.Redis;

namespace API.Services;

public class RedisRateLimitStore : IDistributedRateLimitStore, IDisposable
{
    private readonly IDatabase _db;
    private readonly ConnectionMultiplexer _muxer;
    private readonly ILogger<RedisRateLimitStore> _logger;
    private readonly TimeSpan _defaultTtl;
    private static readonly ConcurrentDictionary<string, List<DateTime>> _fallback = new();

    // Lua script for atomic sliding-window increment + count
    private const string SlidingWindowScript = @"
        local key = KEYS[1]
        local now = tonumber(ARGV[1])
        local window = tonumber(ARGV[2])
        local maxRequests = tonumber(ARGV[3])
        local ttl = tonumber(ARGV[4])

        -- Remove entries outside the window
        redis.call('ZREMRANGEBYSCORE', key, 0, now - window)

        -- Count current entries in window
        local count = redis.call('ZCARD', key)

        if count >= maxRequests then
            return 0  -- rejected
        end

        -- Add current timestamp
        redis.call('ZADD', key, now, now .. ':' .. redis.call('INCR', key .. ':seq'))
        -- Set TTL on the key
        redis.call('PEXPIRE', key, ttl)

        return 1  -- allowed
    ";

    public RedisRateLimitStore(ConnectionMultiplexer muxer, ILogger<RedisRateLimitStore> logger)
    {
        _muxer = muxer;
        _db = muxer.GetDatabase();
        _logger = logger;
        _defaultTtl = TimeSpan.FromMinutes(10);
    }

    public async Task<bool> CheckAndIncrementAsync(string key, int maxRequests, TimeSpan window, CancellationToken ct = default)
    {
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var windowMs = window.TotalMilliseconds;
            var ttlMs = Math.Max(windowMs + 60_000, _defaultTtl.TotalMilliseconds);

            var result = await _db.ScriptEvaluateAsync(
                SlidingWindowScript,
                [new RedisKey($"ratelimit:{key}")],
                [now, windowMs, maxRequests, ttlMs]
            );

            return (int)result == 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis rate limit check failed for key={Key}, using in-memory fallback", key);
            return CheckAndIncrementFallback(key, maxRequests, window);
        }
    }

    private static bool CheckAndIncrementFallback(string key, int maxRequests, TimeSpan window)
    {
        var now = DateTime.UtcNow;
        var timestamps = _fallback.GetOrAdd(key, _ => new List<DateTime>());

        lock (timestamps)
        {
            var cutoff = now - window;
            timestamps.RemoveAll(t => t < cutoff);

            if (timestamps.Count >= maxRequests)
                return false;

            timestamps.Add(now);
            return true;
        }
    }

    public async Task<long> GetCurrentCountAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var vals = await _db.SortedSetRangeByScoreAsync(
                new RedisKey($"ratelimit:{key}"),
                now - 60_000,
                now,
                Exclude.None,
                Order.Ascending,
                0,
                0
            );
            return vals.Length;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis rate limit count failed for key={Key}", key);
            return 0;
        }
    }

    public async Task ResetAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _db.KeyDeleteAsync(new RedisKey($"ratelimit:{key}"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis rate limit reset failed for key={Key}", key);
        }
    }

    public void Dispose()
    {
    }
}
