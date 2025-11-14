namespace Application.Interfaces;

public class FeedbackAction
{
    public string ActionType { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int Priority { get; set; }
    public DateTime SuggestedAt { get; set; }
}

public class SeoRecommendation
{
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Field { get; set; } = string.Empty;
    public string CurrentValue { get; set; } = string.Empty;
    public string SuggestedValue { get; set; } = string.Empty;
    public double Impact { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public interface IRankingFeedbackLoopService
{
    Task ProcessFeedbackAsync();
    Task<List<FeedbackAction>> GetPendingActionsAsync(int count = 20);
    Task<List<SeoRecommendation>> GenerateRecommendationsAsync(string entityType, int entityId);
    Task RecordKeywordPositionChangeAsync(string keyword, string pageUrl, int oldPosition, int newPosition);
}
