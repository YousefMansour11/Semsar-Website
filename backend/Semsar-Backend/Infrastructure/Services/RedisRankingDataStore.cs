using System.Text.Json;
using Application.Interfaces;
using StackExchange.Redis;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Services;

public class RedisRankingDataStore : IRankingDataStore
{
    private readonly IDistributedCache _cache;
    private readonly RedisSetIndex _index;
    private const string Prefix = "semsar:rank:";
    private const string IndexKey = "semsar:rank:index";
    private static readonly DistributedCacheEntryOptions Ttl = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(90) };

    public RedisRankingDataStore(IDistributedCache cache, IConnectionMultiplexer? muxer = null)
    {
        _cache = cache;
        _index = new RedisSetIndex(cache, IndexKey, Ttl, muxer);
    }

    public async Task RecordRankingAsync(RankingRecord record)
    {
        record.CheckedAt = DateTime.UtcNow;
        var id = $"{Prefix}{record.Keyword}:{record.PageUrl}:{record.CheckedAt:yyyyMMddHHmmssfff}";
        var json = JsonSerializer.Serialize(record);
        await _cache.SetStringAsync(id, json, Ttl);
        await _index.AddAsync(id);
    }

    public async Task<RankingRecord?> GetLatestRankingAsync(string keyword, string pageUrl)
    {
        var allIds = await _index.GetAllAsync();
        var matchingKeys = allIds
            .Where(k => k.StartsWith($"{Prefix}{keyword}:{pageUrl}:", StringComparison.Ordinal))
            .OrderByDescending(k => k)
            .ToList();

        if (matchingKeys.Count == 0) return null;

        var json = await _cache.GetStringAsync(matchingKeys[0]);
        return json is not null ? JsonSerializer.Deserialize<RankingRecord>(json) : null;
    }

    public async Task<List<RankingTrend>> GetAllTrendsAsync()
    {
        var allIds = await _index.GetAllAsync();
        var records = new List<RankingRecord>();
        foreach (var id in allIds)
        {
            var json = await _cache.GetStringAsync(id);
            if (json is not null)
                records.Add(JsonSerializer.Deserialize<RankingRecord>(json)!);
        }

        return records
            .GroupBy(r => new { r.Keyword, r.PageUrl })
            .Select(g =>
            {
                var history = g.OrderBy(r => r.CheckedAt).ToList();
                var latest = history.LastOrDefault();
                var previous = history.Count > 1 ? history[^2] : null;
                return new RankingTrend
                {
                    Keyword = g.Key.Keyword,
                    History = history,
                    CurrentPosition = latest?.Position ?? 0,
                    PositionChange = previous != null && latest != null
                        ? previous.Position - latest.Position : 0,
                    Trend = previous != null && latest != null
                        ? latest.Position < previous.Position ? "up"
                        : latest.Position > previous.Position ? "down" : "stable"
                        : "unknown"
                };
            })
            .ToList();
    }

    public async Task<List<RankingRecord>> GetRankingsForPageAsync(string pageUrl, int days = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var allIds = await _index.GetAllAsync();
        var records = new List<RankingRecord>();
        foreach (var id in allIds)
        {
            var json = await _cache.GetStringAsync(id);
            if (json is not null)
            {
                var record = JsonSerializer.Deserialize<RankingRecord>(json)!;
                if (record.PageUrl == pageUrl && record.CheckedAt >= cutoff)
                    records.Add(record);
            }
        }
        return records.OrderByDescending(r => r.CheckedAt).ToList();
    }

    public async Task<List<string>> GetKeywordsInPositionRangeAsync(int minPosition, int maxPosition)
    {
        var allIds = await _index.GetAllAsync();
        var keywords = new HashSet<string>();
        foreach (var id in allIds)
        {
            var json = await _cache.GetStringAsync(id);
            if (json is not null)
            {
                var record = JsonSerializer.Deserialize<RankingRecord>(json)!;
                if (record.Position >= minPosition && record.Position <= maxPosition)
                    keywords.Add(record.Keyword);
            }
        }
        return keywords.ToList();
    }
}
