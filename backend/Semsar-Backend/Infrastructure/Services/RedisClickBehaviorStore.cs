using System.Text.Json;
using Application.Interfaces;
using StackExchange.Redis;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Services;

public class RedisClickBehaviorStore : IClickBehaviorOptimizationService
{
    private readonly IDistributedCache _cache;
    private readonly IDatabase? _db;
    private readonly RedisSetIndex _index;
    private const string Prefix = "semsar:click:";
    private const string IndexKey = "semsar:click:index";
    private static readonly DistributedCacheEntryOptions Ttl = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365) };

    public RedisClickBehaviorStore(IDistributedCache cache, IConnectionMultiplexer? muxer = null)
    {
        _cache = cache;
        _db = muxer?.GetDatabase();
        _index = new RedisSetIndex(cache, IndexKey, Ttl, muxer);
    }

    public void RecordClick(string pageUrl, string? sessionId = null)
    {
        Task.Run(async () => await RecordClickAsync(pageUrl));
    }

    public void RecordImpression(string pageUrl)
    {
        Task.Run(async () => await RecordImpressionAsync(pageUrl));
    }

    public double GetCurrentCtr(string pageUrl)
    {
        var metrics = GetMetricsAsync(pageUrl).GetAwaiter().GetResult();
        return metrics is null || metrics.Impressions == 0
            ? 0
            : (double)metrics.Clicks / metrics.Impressions * 100;
    }

    public List<ClickBehaviorRecord> GetTopPerformingUrls(int count = 20)
    {
        var records = GetAllMetricsAsync().GetAwaiter().GetResult();
        return records
            .Where(kv => kv.Value.Impressions > 0)
            .OrderByDescending(kv => (double)kv.Value.Clicks / kv.Value.Impressions)
            .Take(count)
            .Select(kv => new ClickBehaviorRecord
            {
                PageUrl = kv.Key,
                Clicks = kv.Value.Clicks,
                Impressions = kv.Value.Impressions,
                Ctr = (double)kv.Value.Clicks / kv.Value.Impressions * 100,
                RecordedAt = kv.Value.LastUpdated
            })
            .ToList();
    }

    public string OptimizeTitle(string baseTitle, string pageUrl) => baseTitle;
    public string OptimizeDescription(string baseDescription, string pageUrl) => baseDescription;

    private async Task RecordClickAsync(string pageUrl)
    {
        if (_db != null)
        {
            var hashKey = new RedisKey(Prefix + pageUrl);
            await _db.HashIncrementAsync(hashKey, "clicks");
            await _db.HashSetAsync(hashKey, "lastUpdated", DateTime.UtcNow.ToString("O"));
            await _index.AddAsync(pageUrl);
            return;
        }

        var metrics = await GetMetricsAsync(pageUrl) ?? new ClickMetricsStore();
        metrics.Clicks++;
        metrics.LastUpdated = DateTime.UtcNow;
        await SaveMetricsAsync(pageUrl, metrics);
    }

    private async Task RecordImpressionAsync(string pageUrl)
    {
        if (_db != null)
        {
            var hashKey = new RedisKey(Prefix + pageUrl);
            await _db.HashIncrementAsync(hashKey, "impressions");
            await _db.HashSetAsync(hashKey, "lastUpdated", DateTime.UtcNow.ToString("O"));
            await _index.AddAsync(pageUrl);
            return;
        }

        var metrics = await GetMetricsAsync(pageUrl) ?? new ClickMetricsStore();
        metrics.Impressions++;
        metrics.LastUpdated = DateTime.UtcNow;
        await SaveMetricsAsync(pageUrl, metrics);
    }

    private async Task<ClickMetricsStore?> GetMetricsAsync(string pageUrl)
    {
        if (_db != null)
        {
            var hashKey = new RedisKey(Prefix + pageUrl);
            var entries = await _db.HashGetAllAsync(hashKey);
            if (entries.Length == 0) return null;
            var dict = entries.ToDictionary(e => (string)e.Name!, e => (string?)e.Value);
            return new ClickMetricsStore
            {
                Clicks = dict.TryGetValue("clicks", out var c) && int.TryParse(c, out var ci) ? ci : 0,
                Impressions = dict.TryGetValue("impressions", out var im) && int.TryParse(im, out var ii) ? ii : 0,
                LastUpdated = dict.TryGetValue("lastUpdated", out var lu) && DateTime.TryParse(lu, out var dt) ? dt : DateTime.UtcNow
            };
        }

        var json = await _cache.GetStringAsync(Prefix + pageUrl);
        return json is not null ? JsonSerializer.Deserialize<ClickMetricsStore>(json) : null;
    }

    private async Task SaveMetricsAsync(string pageUrl, ClickMetricsStore metrics)
    {
        if (_db != null)
        {
            var hashKey = new RedisKey(Prefix + pageUrl);
            await _db.HashSetAsync(hashKey, [
                new HashEntry("clicks", metrics.Clicks),
                new HashEntry("impressions", metrics.Impressions),
                new HashEntry("lastUpdated", metrics.LastUpdated.ToString("O"))
            ]);
            await _index.AddAsync(pageUrl);
            return;
        }

        await _cache.SetStringAsync(Prefix + pageUrl, JsonSerializer.Serialize(metrics), Ttl);
        await _index.AddAsync(pageUrl);
    }

    private async Task<Dictionary<string, ClickMetricsStore>> GetAllMetricsAsync()
    {
        var urls = await _index.GetAllAsync();
        var result = new Dictionary<string, ClickMetricsStore>();
        foreach (var url in urls)
        {
            var metrics = await GetMetricsAsync(url);
            if (metrics is not null)
                result[url] = metrics;
        }
        return result;
    }

    private class ClickMetricsStore
    {
        public int Clicks { get; set; }
        public int Impressions { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
