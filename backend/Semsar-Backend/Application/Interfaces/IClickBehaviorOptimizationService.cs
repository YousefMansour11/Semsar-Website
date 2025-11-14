namespace Application.Interfaces;

public class ClickBehaviorRecord
{
    public string PageUrl { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public int Impressions { get; set; }
    public int Clicks { get; set; }
    public double Ctr { get; set; }
    public double AvgPosition { get; set; }
    public DateTime RecordedAt { get; set; }
}

public interface IClickBehaviorOptimizationService
{
    void RecordClick(string pageUrl, string? sessionId = null);
    void RecordImpression(string pageUrl);
    double GetCurrentCtr(string pageUrl);
    List<ClickBehaviorRecord> GetTopPerformingUrls(int count = 20);
    string OptimizeTitle(string baseTitle, string pageUrl);
    string OptimizeDescription(string baseDescription, string pageUrl);
}
