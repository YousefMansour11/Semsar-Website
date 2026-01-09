using Application.Interfaces;

namespace Application.Services;

public class CrawlBudgetOptimizer : ICrawlBudgetOptimizer
{
    public List<CrawlPriority> ComputeCrawlPriorities(List<CrawlPriority> allPages)
    {
        if (allPages.Count == 0) return allPages;

        var maxScore = allPages.Max(p => p.Importance);
        if (maxScore <= 0) maxScore = 1;

        foreach (var page in allPages)
        {
            var normalizedImportance = page.Importance / maxScore;
            page.PriorityScore = normalizedImportance * 100;
        }

        return allPages.OrderByDescending(p => p.PriorityScore).ToList();
    }

    public string SuggestChangeFrequency(string pageType, DateTime lastModified, double popularity)
    {
        var age = (DateTime.UtcNow - lastModified).TotalDays;
        var type = pageType.ToLowerInvariant();

        if (type == "property" || type == "unit")
        {
            if (age < 7) return "daily";
            if (age < 30) return "weekly";
            return "monthly";
        }

        if (type == "project" || type == "location")
        {
            if (age < 14) return "daily";
            if (age < 60) return "weekly";
            return "monthly";
        }

        if (popularity > 100) return "daily";
        if (popularity > 10) return "weekly";

        return "monthly";
    }

    public double CalculateImportanceScore(string pageType, int viewCount, int backlinks, double freshness)
    {
        double typeWeight = pageType.ToLowerInvariant() switch
        {
            "property" => 0.8,
            "project" => 0.7,
            "location" => 0.9,
            "unit" => 0.6,
            "guide" => 0.5,
            _ => 0.4
        };

        var viewScore = Math.Log10(Math.Max(1, viewCount)) * 0.1;
        var backlinkScore = Math.Log10(Math.Max(1, backlinks)) * 0.15;
        var freshnessScore = freshness * 0.25;

        return Math.Clamp(typeWeight + viewScore + backlinkScore + freshnessScore, 0, 1);
    }

    public List<CrawlPriority> FilterUnimportantPages(List<CrawlPriority> pages, double threshold = 0.3)
    {
        return pages.Where(p => p.Importance >= threshold).ToList();
    }
}
