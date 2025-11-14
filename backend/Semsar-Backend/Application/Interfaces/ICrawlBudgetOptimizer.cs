namespace Application.Interfaces;

public class CrawlPriority
{
    public string PageUrl { get; set; } = string.Empty;
    public double PriorityScore { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ChangeFrequency { get; set; } = "weekly";
    public double Importance { get; set; }
}

public interface ICrawlBudgetOptimizer
{
    List<CrawlPriority> ComputeCrawlPriorities(List<CrawlPriority> allPages);
    string SuggestChangeFrequency(string pageType, DateTime lastModified, double popularity);
    double CalculateImportanceScore(string pageType, int viewCount, int backlinks, double freshness);
    List<CrawlPriority> FilterUnimportantPages(List<CrawlPriority> pages, double threshold = 0.3);
}
