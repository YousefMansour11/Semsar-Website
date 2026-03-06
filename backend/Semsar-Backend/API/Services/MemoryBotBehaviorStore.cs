using System.Collections.Concurrent;

namespace API.Services;

public class MemoryBotBehaviorStore : IBotBehaviorStore
{
    private static readonly ConcurrentDictionary<string, List<DateTime>> _velocity = new();
    private static readonly ConcurrentDictionary<string, SessionRecord> _sessions = new();
    private static readonly ConcurrentDictionary<string, List<string>> _payloads = new();
    private static readonly ConcurrentDictionary<string, List<DateTime>> _entityVelocity = new();
    private static readonly ConcurrentDictionary<string, int> _reputationScores = new();
    private static readonly ConcurrentDictionary<string, DateTime> _cooldowns = new();

    private static DateTime _lastCleanup = DateTime.UtcNow;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(6);

    private static readonly TimeSpan _defaultRepTtl = TimeSpan.FromMinutes(30);

    public bool CheckVelocity(string key, int maxRequests, TimeSpan window)
    {
        var now = DateTime.UtcNow;
        var hits = _velocity.GetOrAdd(key, _ => new List<DateTime>());

        lock (hits)
        {
            hits.RemoveAll(t => now - t > window);
            hits.Add(now);
            return hits.Count <= maxRequests;
        }
    }

    public bool CheckAndStoreFingerprint(string ip, string fingerprint)
    {
        var session = _sessions.GetOrAdd(ip, _ => new SessionRecord());
        lock (session)
        {
            if (session.Fingerprint == null)
            {
                session.Fingerprint = fingerprint;
                session.FirstSeen = DateTime.UtcNow;
                return true;
            }

            return session.Fingerprint == fingerprint;
        }
    }

    public List<string> GetPayloadHashes(string ip)
    {
        return _payloads.GetOrAdd(ip, _ => new List<string>());
    }

    public void RecordPayloadHash(string ip, string hash)
    {
        var list = _payloads.GetOrAdd(ip, _ => new List<string>());
        lock (list)
        {
            list.Add(hash);
        }
    }

    public void TrimPayloadHistory(string ip, int maxEntries)
    {
        if (_payloads.TryGetValue(ip, out var list))
        {
            lock (list)
            {
                while (list.Count > maxEntries)
                    list.RemoveAt(0);
            }
        }
    }

    public bool CheckEntityVelocity(string ip, string entityType, string entityId, int maxRequests, TimeSpan window)
    {
        var key = $"{ip}:{entityType}:{entityId}";
        var now = DateTime.UtcNow;
        var hits = _entityVelocity.GetOrAdd(key, _ => new List<DateTime>());

        lock (hits)
        {
            hits.RemoveAll(t => now - t > window);
            hits.Add(now);
            return hits.Count <= maxRequests;
        }
    }

    public int AddReputationScore(string key, int delta, TimeSpan ttl)
    {
        var newScore = _reputationScores.AddOrUpdate(key, delta, (_, existing) => existing + delta);
        return Math.Clamp(newScore, 0, 100);
    }

    public int GetReputationScore(string key)
    {
        return _reputationScores.TryGetValue(key, out var score) ? score : 0;
    }

    public bool TryGetCooldown(string key, out int retryAfterSeconds)
    {
        retryAfterSeconds = 0;
        if (!_cooldowns.TryGetValue(key, out var expires))
            return false;

        var remaining = (expires - DateTime.UtcNow).TotalSeconds;
        if (remaining <= 0)
        {
            _cooldowns.TryRemove(key, out _);
            return false;
        }

        retryAfterSeconds = (int)Math.Ceiling(remaining);
        return true;
    }

    public void SetCooldown(string key, int durationSeconds)
    {
        _cooldowns[key] = DateTime.UtcNow.AddSeconds(durationSeconds);
    }

    public void Cleanup()
    {
        var now = DateTime.UtcNow;
        if (now - _lastCleanup < CleanupInterval)
            return;
        _lastCleanup = now;

        var staleSessions = _sessions
            .Where(kvp => now - kvp.Value.FirstSeen > SessionTtl)
            .Select(kvp => kvp.Key)
            .ToList();
        foreach (var key in staleSessions)
            _sessions.TryRemove(key, out _);

        var stalePayloads = _payloads
            .Where(kvp =>
            {
                lock (kvp.Value)
                {
                    return kvp.Value.Count == 0;
                }
            })
            .Select(kvp => kvp.Key)
            .ToList();
        foreach (var key in stalePayloads)
            _payloads.TryRemove(key, out _);

        var staleVelocity = _velocity
            .Where(kvp =>
            {
                lock (kvp.Value)
                {
                    kvp.Value.RemoveAll(t => now - t > TimeSpan.FromSeconds(30));
                    return kvp.Value.Count == 0;
                }
            })
            .Select(kvp => kvp.Key)
            .ToList();
        foreach (var key in staleVelocity)
            _velocity.TryRemove(key, out _);

        var staleEntityVelocity = _entityVelocity
            .Where(kvp =>
            {
                lock (kvp.Value)
                {
                    kvp.Value.RemoveAll(t => now - t > TimeSpan.FromMinutes(30));
                    return kvp.Value.Count == 0;
                }
            })
            .Select(kvp => kvp.Key)
            .ToList();
        foreach (var key in staleEntityVelocity)
            _entityVelocity.TryRemove(key, out _);

        var staleRep = _reputationScores
            .Where(kvp => !_cooldowns.ContainsKey(kvp.Key))
            .Select(kvp => kvp.Key)
            .ToList();
        foreach (var key in staleRep)
            _reputationScores.TryRemove(key, out _);

        var staleCooldowns = _cooldowns
            .Where(kvp => kvp.Value <= now)
            .Select(kvp => kvp.Key)
            .ToList();
        foreach (var key in staleCooldowns)
            _cooldowns.TryRemove(key, out _);
    }

    private class SessionRecord
    {
        public string? Fingerprint { get; set; }
        public DateTime FirstSeen { get; set; }
    }
}
