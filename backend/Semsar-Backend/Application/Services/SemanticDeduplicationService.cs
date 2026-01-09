using System.Security.Cryptography;
using System.Text;
using Application.Interfaces;

namespace Application.Services;

public class SemanticDeduplicationService : ISemanticDeduplicationService
{
    private readonly List<(string Url, string ContentHash, string FullContent, DateTime AddedAt)> _knownContent = new();
    private readonly object _lock = new();

    public Task<DuplicateResult> AnalyzePageAsync(string url, string title, string description)
    {
        var hash = ComputeContentHash(title, description);
        var content = title + " " + description;
        var result = new DuplicateResult
        {
            PageUrl = url,
            CanonicalUrl = url
        };

        lock (_lock)
        {
            var duplicates = _knownContent
                .Where(k => IsContentSimilar(content, k.FullContent, 0.8))
                .ToList();

            result.SimilarUrls = duplicates.Select(d => d.Url).ToList();
            result.SimilarityScore = duplicates.Count > 0 ? 0.9 : 0;
            result.IsDuplicate = duplicates.Count > 0;

            if (!_knownContent.Any(k => k.Url == url))
            {
                _knownContent.Add((url, hash, content, DateTime.UtcNow));
            }
        }

        return Task.FromResult(result);
    }

    public async Task<string> ResolveCanonicalAsync(string url, string title, string description)
    {
        var analysis = await AnalyzePageAsync(url, title, description);
        return analysis.SimilarUrls.Count > 0 ? analysis.SimilarUrls.First() : url;
    }

    public Task<List<DuplicateResult>> FindNearDuplicatesAsync(string title, string description, string? excludeUrl = null)
    {
        var content = title + " " + description;
        var results = new List<DuplicateResult>();

        lock (_lock)
        {
            var similar = _knownContent
                .Where(k => excludeUrl == null || k.Url != excludeUrl)
                .Where(k => IsContentSimilar(content, k.FullContent, 0.8))
                .ToList();

            foreach (var s in similar)
            {
                results.Add(new DuplicateResult
                {
                    PageUrl = s.Url,
                    SimilarUrls = new List<string>(),
                    CanonicalUrl = s.Url,
                    SimilarityScore = 0.85,
                    IsDuplicate = true
                });
            }
        }

        return Task.FromResult(results);
    }

    public string ComputeContentHash(string title, string description)
    {
        var input = $"{title}|{description}".ToLowerInvariant().Trim();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public bool IsContentSimilar(string content1, string content2, double threshold = 0.85)
    {
        var words1 = GetWords(content1);
        var words2 = GetWords(content2);

        if (words1.Count == 0 || words2.Count == 0) return false;

        var intersection = words1.Intersect(words2, StringComparer.OrdinalIgnoreCase).Count();
        var union = words1.Union(words2, StringComparer.OrdinalIgnoreCase).Count();

        var jaccard = union > 0 ? (double)intersection / union : 0;
        return jaccard >= threshold;
    }

    private static HashSet<string> GetWords(string text)
    {
        var separators = new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':', '-', '_', '(', ')' };
        return new HashSet<string>(
            (text ?? "").ToLowerInvariant().Split(separators, StringSplitOptions.RemoveEmptyEntries),
            StringComparer.OrdinalIgnoreCase
        );
    }
}
