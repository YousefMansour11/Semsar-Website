using Application.Interfaces;

namespace Application.Services;

public class FreshnessService : IFreshnessService
{
    private static readonly TimeSpan PropertyStaleAfter = TimeSpan.FromDays(30);
    private static readonly TimeSpan ProjectStaleAfter = TimeSpan.FromDays(45);
    private static readonly TimeSpan UnitStaleAfter = TimeSpan.FromDays(30);
    private static readonly TimeSpan LocationStaleAfter = TimeSpan.FromDays(60);

    public Task<FreshnessScore> CalculateFreshnessAsync(string entityType, int entityId, DateTime lastUpdated)
    {
        var staleAfter = entityType.ToLowerInvariant() switch
        {
            "property" => PropertyStaleAfter,
            "project" => ProjectStaleAfter,
            "unit" => UnitStaleAfter,
            "location" => LocationStaleAfter,
            _ => TimeSpan.FromDays(30)
        };

        var age = DateTime.UtcNow - lastUpdated;
        var score = ComputeFreshnessScore(lastUpdated);
        var needsUpdate = age > staleAfter;
        var nextUpdate = lastUpdated.Add(staleAfter);

        var staleFields = new List<string>();
        if (needsUpdate)
        {
            staleFields.Add("title");
            staleFields.Add("description");
            staleFields.Add("seo_metadata");
        }

        var result = new FreshnessScore
        {
            EntityType = entityType,
            EntityId = entityId,
            Score = score,
            LastUpdated = lastUpdated,
            NextUpdateDue = nextUpdate,
            NeedsUpdate = needsUpdate,
            StaleFields = staleFields
        };

        return Task.FromResult(result);
    }

    public Task<List<FreshnessScore>> GetStaleEntitiesAsync(string entityType, int maxCount = 50)
    {
        return Task.FromResult(new List<FreshnessScore>());
    }

    public Task RecordUpdateAsync(string entityType, int entityId)
    {
        return Task.CompletedTask;
    }

    public double ComputeFreshnessScore(DateTime lastUpdated, DateTime? nextUpdate = null)
    {
        var staleAfter = nextUpdate ?? lastUpdated.AddDays(30);
        var totalLifetime = (staleAfter - lastUpdated).TotalDays;
        var remaining = (staleAfter - DateTime.UtcNow).TotalDays;

        if (totalLifetime <= 0) return 0;
        if (remaining <= 0) return 0;
        if (remaining >= totalLifetime) return 1.0;

        return Math.Clamp(remaining / totalLifetime, 0, 1);
    }
}
