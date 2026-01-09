using Application.Interfaces;

namespace Application.Services;

public class RankingFeedbackLoopService : IRankingFeedbackLoopService
{
    private readonly IRankingDataStore _rankingDataStore;
    private readonly ISeoContentGenerator _seoContentGenerator;

    public RankingFeedbackLoopService(IRankingDataStore rankingDataStore, ISeoContentGenerator seoContentGenerator)
    {
        _rankingDataStore = rankingDataStore;
        _seoContentGenerator = seoContentGenerator;
    }

    public async Task ProcessFeedbackAsync()
    {
        var trends = await _rankingDataStore.GetAllTrendsAsync();

        foreach (var trend in trends.Where(t => t.Trend == "down" && t.PositionChange > 3))
        {
            var latest = trend.History.LastOrDefault();
            if (latest == null) continue;

            if (latest.Ctr < 2.0)
            {
                var actions = await GenerateRecommendationsAsync("property", 0);
            }
        }
    }

    public Task<List<FeedbackAction>> GetPendingActionsAsync(int count = 20)
    {
        var actions = new List<FeedbackAction>
        {
            new()
            {
                ActionType = "refresh_content",
                TargetEntity = "stale_pages",
                Reason = "Automatic freshness check required for pages older than 30 days",
                Priority = 3,
                SuggestedAt = DateTime.UtcNow
            },
            new()
            {
                ActionType = "update_titles",
                TargetEntity = "low_ctr_pages",
                Reason = "Pages with CTR below 2% need title optimization",
                Priority = 5,
                SuggestedAt = DateTime.UtcNow
            }
        };

        return Task.FromResult(actions);
    }

    public async Task<List<SeoRecommendation>> GenerateRecommendationsAsync(string entityType, int entityId)
    {
        var recommendations = new List<SeoRecommendation>();

        var trends = await _rankingDataStore.GetAllTrendsAsync();
        var keyword = trends.FirstOrDefault()?.Keyword ?? "property";

        recommendations.Add(new SeoRecommendation
        {
            EntityType = entityType,
            EntityId = entityId,
            Field = "title",
            SuggestedValue = $"Updated: {keyword} - Premium Selection",
            Impact = 0.7,
            Reason = "Title may be underperforming based on ranking trends"
        });

        return recommendations;
    }

    public async Task RecordKeywordPositionChangeAsync(string keyword, string pageUrl, int oldPosition, int newPosition)
    {
        var record = new RankingRecord
        {
            Keyword = keyword,
            PageUrl = pageUrl,
            Position = newPosition,
            PreviousPosition = oldPosition,
            BestPosition = Math.Min(oldPosition, newPosition),
            SearchEngine = "google",
            CheckedAt = DateTime.UtcNow
        };

        await _rankingDataStore.RecordRankingAsync(record);
    }
}
