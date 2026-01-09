using System.Collections.Concurrent;
using Application.Interfaces;

namespace Application.Services;

public class IndexVelocityService : IIndexVelocityService
{
    private readonly ConcurrentQueue<string> _submissions = new();
    private readonly ConcurrentQueue<string> _indexed = new();
    private readonly ConcurrentDictionary<string, DateTime> _urlTracking = new();

    public Task RecordSubmissionAsync(string url)
    {
        _submissions.Enqueue(url);
        _urlTracking.TryAdd(url, DateTime.UtcNow);
        return Task.CompletedTask;
    }

    public Task RecordIndexingAsync(string url)
    {
        _indexed.Enqueue(url);
        return Task.CompletedTask;
    }

    public Task<IndexVelocityResult> GetCurrentVelocityAsync()
    {
        var today = DateTime.UtcNow.Date;
        var submissionsToday = _submissions.Count(s =>
        {
            _urlTracking.TryGetValue(s, out var dt);
            return dt.Date == today;
        });
        var indexedToday = _indexed.Count;

        var result = new IndexVelocityResult
        {
            CurrentVelocity = indexedToday > 0 ? (double)indexedToday / Math.Max(1, submissionsToday) : 0,
            TargetVelocity = 0.8,
            PagesIndexedToday = indexedToday,
            PagesSubmittedToday = submissionsToday,
            UrlsToPrioritize = _urlTracking
                .Where(kv => kv.Value.Date == today)
                .OrderBy(kv => kv.Value)
                .Take(10)
                .Select(kv => kv.Key)
                .ToList()
        };

        return Task.FromResult(result);
    }

    public Task<List<string>> GetUrlsNeedingIndexingAsync(int maxCount = 50)
    {
        var urls = _submissions
            .Where(s => !_indexed.Contains(s))
            .Distinct()
            .Take(maxCount)
            .ToList();

        return Task.FromResult(urls);
    }

    public Task<bool> ShouldSubmitToIndexNowAsync()
    {
        var indexedToday = _indexed.Count;
        var submittedToday = _submissions.Count;

        if (submittedToday == 0) return Task.FromResult(false);

        var ratio = (double)indexedToday / submittedToday;
        return Task.FromResult(ratio < 0.5 && submittedToday > 10);
    }
}
