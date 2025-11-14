namespace Application.Interfaces;

public class DuplicateResult
{
    public string PageUrl { get; set; } = string.Empty;
    public List<string> SimilarUrls { get; set; } = new();
    public string CanonicalUrl { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
    public bool IsDuplicate { get; set; }
}

public interface ISemanticDeduplicationService
{
    Task<DuplicateResult> AnalyzePageAsync(string url, string title, string description);
    Task<string> ResolveCanonicalAsync(string url, string title, string description);
    Task<List<DuplicateResult>> FindNearDuplicatesAsync(string title, string description, string? excludeUrl = null);
    string ComputeContentHash(string title, string description);
    bool IsContentSimilar(string content1, string content2, double threshold = 0.85);
}
