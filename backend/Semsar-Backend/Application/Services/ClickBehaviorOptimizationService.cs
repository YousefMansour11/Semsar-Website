using System.Collections.Concurrent;
using Application.Interfaces;

namespace Application.Services;

public class ClickBehaviorOptimizationService : IClickBehaviorOptimizationService
{
    private readonly ConcurrentDictionary<string, ClickMetrics> _metrics = new();
    private readonly ConcurrentDictionary<string, int> _impressions = new();

    private class ClickMetrics
    {
        public int Clicks { get; set; }
        public int Impressions { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public void RecordClick(string pageUrl, string? sessionId = null)
    {
        var metrics = _metrics.GetOrAdd(pageUrl, _ => new ClickMetrics());
        lock (metrics)
        {
            metrics.Clicks++;
            metrics.LastUpdated = DateTime.UtcNow;
        }
    }

    public void RecordImpression(string pageUrl)
    {
        var metrics = _metrics.GetOrAdd(pageUrl, _ => new ClickMetrics());
        lock (metrics)
        {
            metrics.Impressions++;
            metrics.LastUpdated = DateTime.UtcNow;
        }
        _impressions.AddOrUpdate(pageUrl, 1, (_, v) => v + 1);
    }

    public double GetCurrentCtr(string pageUrl)
    {
        if (_metrics.TryGetValue(pageUrl, out var metrics))
        {
            return metrics.Impressions > 0
                ? (double)metrics.Clicks / metrics.Impressions * 100
                : 0;
        }
        return 0;
    }

    public List<ClickBehaviorRecord> GetTopPerformingUrls(int count = 20)
    {
        return _metrics
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

    public string OptimizeTitle(string baseTitle, string pageUrl)
    {
        return baseTitle;
    }

    public string OptimizeDescription(string baseDescription, string pageUrl)
    {
        return baseDescription;
    }
}
