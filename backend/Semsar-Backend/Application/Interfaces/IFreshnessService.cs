namespace Application.Interfaces;

public class FreshnessScore
{
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public double Score { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime NextUpdateDue { get; set; }
    public bool NeedsUpdate { get; set; }
    public List<string> StaleFields { get; set; } = new();
}

public interface IFreshnessService
{
    Task<FreshnessScore> CalculateFreshnessAsync(string entityType, int entityId, DateTime lastUpdated);
    Task<List<FreshnessScore>> GetStaleEntitiesAsync(string entityType, int maxCount = 50);
    Task RecordUpdateAsync(string entityType, int entityId);
    double ComputeFreshnessScore(DateTime lastUpdated, DateTime? nextUpdate = null);
}
