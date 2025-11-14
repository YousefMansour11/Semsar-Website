namespace Application.Interfaces;

public class RankingRecord
{
    public string Keyword { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
    public int Position { get; set; }
    public int PreviousPosition { get; set; }
    public int BestPosition { get; set; }
    public string SearchEngine { get; set; } = "google";
    public DateTime CheckedAt { get; set; }
    public int Impressions { get; set; }
    public int Clicks { get; set; }
    public double Ctr { get; set; }
}

public class RankingTrend
{
    public string Keyword { get; set; } = string.Empty;
    public List<RankingRecord> History { get; set; } = new();
    public int CurrentPosition { get; set; }
    public int PositionChange { get; set; }
    public string Trend { get; set; } = "stable";
}

public interface IRankingDataStore
{
    Task RecordRankingAsync(RankingRecord record);
    Task<RankingRecord?> GetLatestRankingAsync(string keyword, string pageUrl);
    Task<List<RankingTrend>> GetAllTrendsAsync();
    Task<List<RankingRecord>> GetRankingsForPageAsync(string pageUrl, int days = 30);
    Task<List<string>> GetKeywordsInPositionRangeAsync(int minPosition, int maxPosition);
}
