using System.Diagnostics;
using Application.Interfaces;

namespace Infrastructure.Services;

public interface ISeoTelemetry
{
    Task<T> TrackAsync<T>(string serviceName, string operation, Func<Task<T>> func);
    Task TrackAsync(string serviceName, string operation, Func<Task> func);
    void RecordCacheHit(string serviceName);
    void RecordCacheMiss(string serviceName);
    void RecordDedupDecision(string serviceName, bool isDuplicate);
    void RecordCtrSelection(string variantName);
    void RecordSitemapGeneration(double durationMs);
    void RecordGeoPageGeneration(double durationMs);
    void RecordRankingFeedback(string actionType);
}

public class SeoTelemetry : ISeoTelemetry
{
    private readonly IAppMetrics _metrics;
    private static readonly Random _jitter = new();

    public SeoTelemetry(IAppMetrics metrics)
    {
        _metrics = metrics;
    }

    public async Task<T> TrackAsync<T>(string serviceName, string operation, Func<Task<T>> func)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            _metrics.Increment($"seo.{serviceName}.{operation}.count");
            var result = await func();
            sw.Stop();
            _metrics.Observe($"seo.{serviceName}.{operation}.latency_ms", sw.Elapsed.TotalMilliseconds);
            return result;
        }
        catch
        {
            _metrics.Increment($"seo.{serviceName}.{operation}.failure");
            throw;
        }
    }

    public async Task TrackAsync(string serviceName, string operation, Func<Task> func)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            _metrics.Increment($"seo.{serviceName}.{operation}.count");
            await func();
            sw.Stop();
            _metrics.Observe($"seo.{serviceName}.{operation}.latency_ms", sw.Elapsed.TotalMilliseconds);
        }
        catch
        {
            _metrics.Increment($"seo.{serviceName}.{operation}.failure");
            throw;
        }
    }

    public void RecordCacheHit(string serviceName)
    {
        _metrics.Increment($"seo.{serviceName}.cache_hit");
    }

    public void RecordCacheMiss(string serviceName)
    {
        _metrics.Increment($"seo.{serviceName}.cache_miss");
    }

    public void RecordDedupDecision(string serviceName, bool isDuplicate)
    {
        _metrics.Increment($"seo.{serviceName}.dedup.{(isDuplicate ? "duplicate" : "unique")}");
    }

    public void RecordCtrSelection(string variantName)
    {
        _metrics.Increment($"seo.serp.variant_selected.{variantName}");
    }

    public void RecordSitemapGeneration(double durationMs)
    {
        _metrics.Observe("seo.sitemap.generation_ms", durationMs);
    }

    public void RecordGeoPageGeneration(double durationMs)
    {
        _metrics.Observe("seo.geo_page.generation_ms", durationMs);
    }

    public void RecordRankingFeedback(string actionType)
    {
        _metrics.Increment($"seo.feedback_loop.{actionType}");
    }
}
