namespace Application.Interfaces;

public class IndexDirective
{
    public string PageUrl { get; set; } = string.Empty;
    public bool ShouldIndex { get; set; } = true;
    public bool ShouldFollow { get; set; } = true;
    public string? CanonicalUrl { get; set; }
    public string? RobotsTag { get; set; }
    public string? ContentLanguage { get; set; }
    public double PageQuality { get; set; }
}

public interface IIndexControlService
{
    IndexDirective GetIndexDirective(string pageUrl, string entityType, double qualityScore = 0.5);
    string BuildRobotsTag(IndexDirective directive);
    bool ShouldBlockFromSitemap(string pageUrl, string entityType);
    List<string> GetNoindexPatterns();
    double AssessPageQuality(string title, string description, string content, int wordCount);
}
