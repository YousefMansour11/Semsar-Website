using System.Text.Json;
using Application.Interfaces;
using StackExchange.Redis;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Services;

public class RedisIndexVelocityStore : IIndexVelocityService
{
    private readonly IDistributedCache _cache;
    private readonly IDatabase? _db;
    private readonly RedisSetIndex _index;
    private const string PrefixSub = "semsar:vel:sub:";
    private const string PrefixIdx = "semsar:vel:idx:";
    private const string IndexKey = "semsar:vel:index";
    private static readonly DistributedCacheEntryOptions Ttl = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30) };

    public RedisIndexVelocityStore(IDistributedCache cache, IConnectionMultiplexer? muxer = null)
    {
        _cache = cache;
        _db = muxer?.GetDatabase();
        _index = new RedisSetIndex(cache, IndexKey, Ttl, muxer);
    }

    public async Task RecordSubmissionAsync(string url)
    {
        var entry = new VelocityEntry { Url = url, Timestamp = DateTime.UtcNow };
        await _cache.SetStringAsync(PrefixSub + url, JsonSerializer.Serialize(entry), Ttl);
        await _index.AddAsync(JsonSerializer.Serialize(entry));
    }

    public async Task RecordIndexingAsync(string url)
    {
        if (_db != null)
        {
            var now = DateTime.UtcNow.ToString("O");
            var subKey = new RedisKey(PrefixSub + url);
            var idxKey = new RedisKey(PrefixIdx + url);
            var ttlSec = (long)(Ttl.AbsoluteExpirationRelativeToNow?.TotalSeconds ?? 2592000);

            var script = @"
                local subKey = KEYS[1]
                local idxKey = KEYS[2]
                local now = ARGV[1]
                local ttlSec = tonumber(ARGV[2])

                local existing = redis.call('GET', subKey)
                if existing then
                    local entry = cjson.decode(existing)
                    entry['indexedAt'] = now
                    redis.call('SETEX', subKey, ttlSec, cjson.encode(entry))
                else
                    local entry = { Url = KEYS[3], Timestamp = now, IndexedAt = now }
                    redis.call('SETEX', subKey, ttlSec, cjson.encode(entry))
                    redis.call('SADD', '" + IndexKey + @"', cjson.encode(entry))
                end
                redis.call('SETEX', idxKey, ttlSec, now)
            ";

            await _db.ScriptEvaluateAsync(script, [subKey, idxKey, url], [now, ttlSec]);
            return;
        }

        var existingJson = await _cache.GetStringAsync(PrefixSub + url);
        if (existingJson is not null)
        {
            var existing = JsonSerializer.Deserialize<VelocityEntry>(existingJson)!;
            existing.IndexedAt = DateTime.UtcNow;
            await _cache.SetStringAsync(PrefixSub + url, JsonSerializer.Serialize(existing), Ttl);
        }
        else
        {
            var entry = new VelocityEntry { Url = url, Timestamp = DateTime.UtcNow, IndexedAt = DateTime.UtcNow };
            await _cache.SetStringAsync(PrefixSub + url, JsonSerializer.Serialize(entry), Ttl);
            await _index.AddAsync(JsonSerializer.Serialize(entry));
        }
        await _cache.SetStringAsync(PrefixIdx + url, DateTime.UtcNow.ToString("O"), Ttl);
    }

    public async Task<IndexVelocityResult> GetCurrentVelocityAsync()
    {
        var entries = await GetAllEntriesAsync();
        var today = DateTime.UtcNow.Date;

        var submissionsToday = entries.Count(e => e.Timestamp.Date == today);
        var indexedToday = entries.Count(e => e.IndexedAt.HasValue && e.IndexedAt.Value.Date == today);

        return new IndexVelocityResult
        {
            CurrentVelocity = indexedToday > 0 ? (double)indexedToday / Math.Max(1, submissionsToday) : 0,
            TargetVelocity = 0.8,
            PagesIndexedToday = indexedToday,
            PagesSubmittedToday = submissionsToday,
            UrlsToPrioritize = entries
                .Where(e => e.Timestamp.Date == today)
                .OrderBy(e => e.Timestamp)
                .Take(10)
                .Select(e => e.Url)
                .ToList()
        };
    }

    public async Task<List<string>> GetUrlsNeedingIndexingAsync(int maxCount = 50)
    {
        var entries = await GetAllEntriesAsync();
        return entries
            .Where(e => !e.IndexedAt.HasValue)
            .Select(e => e.Url)
            .Distinct()
            .Take(maxCount)
            .ToList();
    }

    public async Task<bool> ShouldSubmitToIndexNowAsync()
    {
        var result = await GetCurrentVelocityAsync();
        if (result.PagesSubmittedToday == 0) return false;
        var ratio = result.PagesIndexedToday / (double)result.PagesSubmittedToday;
        return ratio < 0.5 && result.PagesSubmittedToday > 10;
    }

    private async Task<List<VelocityEntry>> GetAllEntriesAsync()
    {
        var members = await _index.GetAllAsync();
        var entries = new List<VelocityEntry>();
        foreach (var m in members)
        {
            var entry = JsonSerializer.Deserialize<VelocityEntry>(m);
            if (entry != null) entries.Add(entry);
        }
        return entries;
    }

    private class VelocityEntry
    {
        public string Url { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public DateTime? IndexedAt { get; set; }

        public override bool Equals(object? obj) =>
            obj is VelocityEntry other && Url == other.Url;
        public override int GetHashCode() => Url.GetHashCode(StringComparison.Ordinal);
    }
}
