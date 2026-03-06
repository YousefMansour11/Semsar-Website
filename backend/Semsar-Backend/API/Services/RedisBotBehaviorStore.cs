using System.Collections.Concurrent;
using StackExchange.Redis;

namespace API.Services;

public class RedisBotBehaviorStore : IBotBehaviorStore
{
    private readonly IDatabase _db;
    private readonly ConnectionMultiplexer _muxer;
    private readonly ILogger<RedisBotBehaviorStore> _logger;
    private static readonly ConcurrentDictionary<string, RedisFallbackTracker> _fallback = new();
    private static DateTime _lastFallbackCleanup = DateTime.UtcNow;
    private static readonly TimeSpan FallbackCleanupInterval = TimeSpan.FromMinutes(10);
    private const int MaxFallbackEntries = 5_000;

    private const string VelocityScript = @"
        local key = KEYS[1]
        local now = tonumber(ARGV[1])
        local window = tonumber(ARGV[2])
        local maxRequests = tonumber(ARGV[3])

        redis.call('ZREMRANGEBYSCORE', key, 0, now - window)
        local count = redis.call('ZCARD', key)

        if count >= maxRequests then
            return 0
        end

        redis.call('ZADD', key, now, now .. ':' .. redis.call('INCR', key .. ':seq'))
        redis.call('PEXPIRE', key, window + 60000)
        return 1
    ";

    private const string ReputationScript = @"
        local key = KEYS[1]
        local delta = tonumber(ARGV[1])
        local ttlMs = tonumber(ARGV[2])

        local existing = redis.call('GET', key)
        local current = 0
        if existing then
            current = tonumber(existing)
        end

        local newScore = current + delta
        if newScore < 0 then newScore = 0 end
        if newScore > 100 then newScore = 100 end

        redis.call('SET', key, newScore, 'PX', ttlMs)
        return newScore
    ";

    private const string CooldownCheckScript = @"
        local key = KEYS[1]
        local now = tonumber(ARGV[1])

        local expires = redis.call('GET', key)
        if not expires then
            return -1
        end

        local remaining = tonumber(expires) - now
        if remaining <= 0 then
            redis.call('DEL', key)
            return -1
        end

        return math.ceil(remaining / 1000)
    ";

    private const int VelocityTtlMs = 90_000;
    private const int FingerprintTtlSec = 21_600;
    private const int PayloadTtlSec = 7_200;

    public RedisBotBehaviorStore(ConnectionMultiplexer muxer, ILogger<RedisBotBehaviorStore> logger)
    {
        _muxer = muxer;
        _db = muxer.GetDatabase();
        _logger = logger;
    }

    public bool CheckVelocity(string key, int maxRequests, TimeSpan window)
    {
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var windowMs = window.TotalMilliseconds;

            var result = (int)_db.ScriptEvaluate(
                VelocityScript,
                [new RedisKey($"bot:velocity:{key}")],
                [now, windowMs, maxRequests]
            );

            return result == 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis velocity check failed for key={Key}, using in-memory fallback", key);
            return CheckVelocityFallback(key, maxRequests, window);
        }
    }

    private static bool CheckVelocityFallback(string key, int maxRequests, TimeSpan window)
    {
        TrimFallback();
        var now = DateTimeOffset.UtcNow;
        var tracker = _fallback.GetOrAdd(key, _ => new RedisFallbackTracker());

        lock (tracker)
        {
            var cutoff = now - window;
            tracker.Timestamps.RemoveAll(t => t < cutoff);

            if (tracker.Timestamps.Count >= maxRequests)
                return false;

            tracker.Timestamps.Add(now);
            return true;
        }
    }

    private static void TrimFallback()
    {
        if (_fallback.Count < MaxFallbackEntries) return;
        var now = DateTime.UtcNow;
        if (now - _lastFallbackCleanup < FallbackCleanupInterval) return;
        _lastFallbackCleanup = now;

        var keys = _fallback.Keys.ToList();
        var half = keys.Count / 2;
        for (int i = 0; i < half && _fallback.Count > MaxFallbackEntries / 2; i++)
        {
            _fallback.TryRemove(keys[i], out _);
        }
    }

    public List<string> GetPayloadHashes(string ip)
    {
        try
        {
            var key = new RedisKey($"bot:payloads:{ip}");
            var vals = _db.ListRange(key);
            return vals.Select(v => (string?)v).Where(x => x != null).Cast<string>().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis payload get failed for IP={IP}, returning empty", ip);
            return [];
        }
    }

    public void RecordPayloadHash(string ip, string hash)
    {
        try
        {
            var key = new RedisKey($"bot:payloads:{ip}");
            _db.ListLeftPush(key, hash);
            _db.KeyExpire(key, TimeSpan.FromSeconds(PayloadTtlSec));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis payload record failed for IP={IP}", ip);
        }
    }

    public void TrimPayloadHistory(string ip, int maxEntries)
    {
        try
        {
            var key = new RedisKey($"bot:payloads:{ip}");
            _db.ListTrim(key, 0, maxEntries - 1);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis payload trim failed for IP={IP}", ip);
        }
    }

    public bool CheckAndStoreFingerprint(string ip, string fingerprint)
    {
        try
        {
            var key = new RedisKey($"bot:fingerprint:{ip}");
            var existing = _db.StringGet(key);
            if (!existing.HasValue)
            {
                _db.StringSet(key, fingerprint, TimeSpan.FromSeconds(FingerprintTtlSec));
                return true;
            }
            return existing.ToString() == fingerprint;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis fingerprint check failed for IP={IP}, using in-memory fallback", ip);
            return CheckFingerprintFallback(ip, fingerprint);
        }
    }

    private static readonly ConcurrentDictionary<string, string> _fingerprintFallback = new();

    private static bool CheckFingerprintFallback(string ip, string fingerprint)
    {
        var existing = _fingerprintFallback.GetOrAdd(ip, fingerprint);
        return existing == fingerprint;
    }

    public bool CheckEntityVelocity(string ip, string entityType, string entityId, int maxRequests, TimeSpan window)
    {
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var windowMs = window.TotalMilliseconds;
            var key = new RedisKey($"bot:ev:{ip}:{entityType}:{entityId}");

            var result = (int)_db.ScriptEvaluate(
                VelocityScript,
                [key],
                [now, windowMs, maxRequests]
            );

            return result == 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis entity velocity check failed for IP={IP} Type={Type} Id={Id}, using fallback",
                ip, entityType, entityId);
            return CheckEntityVelocityFallback(ip, entityType, entityId, maxRequests, window);
        }
    }

    private static readonly ConcurrentDictionary<string, List<DateTime>> _entityVelocityFallback = new();

    private static bool CheckEntityVelocityFallback(string ip, string entityType, string entityId, int maxRequests, TimeSpan window)
    {
        var key = $"{ip}:{entityType}:{entityId}";
        var now = DateTime.UtcNow;
        var hits = _entityVelocityFallback.GetOrAdd(key, _ => new List<DateTime>());

        lock (hits)
        {
            hits.RemoveAll(t => now - t > window);
            if (hits.Count >= maxRequests)
                return false;
            hits.Add(now);
            return true;
        }
    }

    public int AddReputationScore(string key, int delta, TimeSpan ttl)
    {
        try
        {
            var ttlMs = ttl.TotalMilliseconds;
            var redisKey = new RedisKey($"bot:rep:{key}");

            var result = (int)_db.ScriptEvaluate(
                ReputationScript,
                [redisKey],
                [delta, ttlMs]
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis reputation score failed for key={Key}, using in-memory fallback", key);
            return AddReputationFallback(key, delta, ttl);
        }
    }

    private static readonly ConcurrentDictionary<string, int> _repFallback = new();

    private static int AddReputationFallback(string key, int delta, TimeSpan ttl)
    {
        var newScore = _repFallback.AddOrUpdate(key, Math.Clamp(delta, 0, 100), (_, existing) =>
        {
            var sum = existing + delta;
            return Math.Clamp(sum, 0, 100);
        });
        return newScore;
    }

    public int GetReputationScore(string key)
    {
        try
        {
            var redisKey = new RedisKey($"bot:rep:{key}");
            var val = _db.StringGet(redisKey);
            if (!val.HasValue) return 0;
            return (int)val;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis reputation get failed for key={Key}", key);
            return _repFallback.TryGetValue(key, out var score) ? score : 0;
        }
    }

    public bool TryGetCooldown(string key, out int retryAfterSeconds)
    {
        retryAfterSeconds = 0;
        try
        {
            var redisKey = new RedisKey($"bot:cooldown:{key}");
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var result = (int)_db.ScriptEvaluate(
                CooldownCheckScript,
                [redisKey],
                [now]
            );

            if (result < 0) return false;
            retryAfterSeconds = result;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cooldown check failed for key={Key}, using in-memory fallback", key);
            return TryGetCooldownFallback(key, out retryAfterSeconds);
        }
    }

    private static readonly ConcurrentDictionary<string, DateTime> _cooldownFallback = new();

    private static bool TryGetCooldownFallback(string key, out int retryAfterSeconds)
    {
        retryAfterSeconds = 0;
        if (!_cooldownFallback.TryGetValue(key, out var expires))
            return false;

        var remaining = (expires - DateTime.UtcNow).TotalSeconds;
        if (remaining <= 0)
        {
            _cooldownFallback.TryRemove(key, out _);
            return false;
        }

        retryAfterSeconds = (int)Math.Ceiling(remaining);
        return true;
    }

    public void SetCooldown(string key, int durationSeconds)
    {
        try
        {
            var redisKey = new RedisKey($"bot:cooldown:{key}");
            var expires = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (durationSeconds * 1000);
            _db.StringSet(redisKey, expires, TimeSpan.FromSeconds(durationSeconds));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cooldown set failed for key={Key}, using in-memory fallback", key);
            _cooldownFallback[key] = DateTime.UtcNow.AddSeconds(durationSeconds);
        }
    }

    public void Cleanup()
    {
        _repFallback.Clear();
        _entityVelocityFallback.Clear();
        _fallback.Clear();
        _fingerprintFallback.Clear();
        _cooldownFallback.Clear();
    }
}

internal class RedisFallbackTracker
{
    public List<DateTimeOffset> Timestamps { get; set; } = new();
}
