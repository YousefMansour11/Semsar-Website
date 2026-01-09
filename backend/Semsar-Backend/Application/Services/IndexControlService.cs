using Application.Interfaces;

namespace Application.Services;

public class IndexControlService : IIndexControlService
{
    private static readonly HashSet<string> NoindexPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/",
        "/admin/",
        "/swagger/",
        "/auth/",
        "/seo/",
        "/healthz",
        "/readyz",
        "/metrics/",
        "/jobs",
        "/filter?page=1"
    };

    public IndexDirective GetIndexDirective(string pageUrl, string entityType, double qualityScore = 0.5)
    {
        var directive = new IndexDirective
        {
            PageUrl = pageUrl,
            ShouldIndex = true,
            ShouldFollow = true,
            PageQuality = qualityScore
        };

        if (qualityScore < 0.3)
        {
            directive.ShouldIndex = false;
        }

        if (NoindexPatterns.Any(p => pageUrl.Contains(p, StringComparison.OrdinalIgnoreCase)))
        {
            directive.ShouldIndex = false;
        }

        if (pageUrl.Contains("/filter", StringComparison.OrdinalIgnoreCase) && pageUrl.Contains("page="))
        {
            if (pageUrl.Contains("page=1"))
                directive.ShouldIndex = true;
            else
                directive.ShouldIndex = false;
        }

        if (string.IsNullOrWhiteSpace(entityType))
        {
            directive.ShouldFollow = false;
        }

        directive.RobotsTag = BuildRobotsTag(directive);
        return directive;
    }

    public string BuildRobotsTag(IndexDirective directive)
    {
        var parts = new List<string>();

        if (!directive.ShouldIndex && !directive.ShouldFollow)
            parts.Add("none");
        else if (!directive.ShouldIndex)
            parts.Add("noindex");
        else if (!directive.ShouldFollow)
            parts.Add("nofollow");

        if (!string.IsNullOrWhiteSpace(directive.ContentLanguage))
            parts.Add($"content-language: {directive.ContentLanguage}");

        return parts.Count > 0 ? string.Join(", ", parts) : "index, follow";
    }

    public bool ShouldBlockFromSitemap(string pageUrl, string entityType)
    {
        if (NoindexPatterns.Any(p => pageUrl.Contains(p, StringComparison.OrdinalIgnoreCase)))
            return true;

        if (pageUrl.Contains("/filter", StringComparison.OrdinalIgnoreCase) && pageUrl.Contains("page=") && !pageUrl.Contains("page=1"))
            return true;

        return false;
    }

    public List<string> GetNoindexPatterns()
    {
        return NoindexPatterns.ToList();
    }

    public double AssessPageQuality(string title, string description, string content, int wordCount)
    {
        double score = 0.5;

        if (string.IsNullOrWhiteSpace(title))
            score -= 0.2;
        else if (title.Length >= 30 && title.Length <= 60)
            score += 0.15;

        if (string.IsNullOrWhiteSpace(description))
            score -= 0.15;
        else if (description.Length >= 120 && description.Length <= 160)
            score += 0.1;

        if (wordCount < 100)
            score -= 0.3;
        else if (wordCount >= 300)
            score += 0.2;
        else if (wordCount >= 500)
            score += 0.3;

        return Math.Clamp(score, 0, 1);
    }
}
