using System.Collections.Concurrent;
using Application.Interfaces;

namespace Application.Services;

public class RankingDataStore : IRankingDataStore
{
    private readonly ConcurrentBag<RankingRecord> _records = new();

    public Task RecordRankingAsync(RankingRecord record)
    {
        record.CheckedAt = DateTime.UtcNow;
        _records.Add(record);
        return Task.CompletedTask;
    }

    public Task<RankingRecord?> GetLatestRankingAsync(string keyword, string pageUrl)
    {
        var latest = _records
            .Where(r => r.Keyword == keyword && r.PageUrl == pageUrl)
            .OrderByDescending(r => r.CheckedAt)
            .FirstOrDefault();

        return Task.FromResult(latest);
    }

    public Task<List<RankingTrend>> GetAllTrendsAsync()
    {
        var trends = _records
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
                        ? previous.Position - latest.Position
                        : 0,
                    Trend = previous != null && latest != null
                        ? latest.Position < previous.Position ? "up" :
                          latest.Position > previous.Position ? "down" : "stable"
                        : "unknown"
                };
            })
            .ToList();

        return Task.FromResult(trends);
    }

    public Task<List<RankingRecord>> GetRankingsForPageAsync(string pageUrl, int days = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var records = _records
            .Where(r => r.PageUrl == pageUrl && r.CheckedAt >= cutoff)
            .OrderByDescending(r => r.CheckedAt)
            .ToList();

        return Task.FromResult(records);
    }

    public Task<List<string>> GetKeywordsInPositionRangeAsync(int minPosition, int maxPosition)
    {
        var keywords = _records
            .GroupBy(r => r.Keyword)
            .Where(g =>
            {
                var latest = g.OrderByDescending(r => r.CheckedAt).FirstOrDefault();
                return latest != null && latest.Position >= minPosition && latest.Position <= maxPosition;
            })
            .Select(g => g.Key)
            .Distinct()
            .ToList();

        return Task.FromResult(keywords);
    }
}
