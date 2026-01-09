using System.Collections.Concurrent;
using Application.Interfaces;

namespace Application.Services;

public class AuthoritySignalService : IAuthoritySignalService
{
    private readonly ConcurrentDictionary<string, AuthorityScoreResult> _cache = new();

    private static readonly Dictionary<string, double> EntityBaseScores = new(StringComparer.OrdinalIgnoreCase)
    {
        ["property"] = 45.0,
        ["project"] = 55.0,
        ["location"] = 60.0,
        ["unit"] = 35.0,
        ["developer"] = 70.0,
        ["area"] = 50.0,
    };

    public Task<AuthorityScoreResult> GetAuthorityScoreAsync(string url)
    {
        if (_cache.TryGetValue(url, out var cached))
            return Task.FromResult(cached);

        var result = ComputeAuthorityScore(url);
        _cache.TryAdd(url, result);
        return Task.FromResult(result);
    }

    public async Task RecordBacklinkAsync(string targetUrl, string sourceUrl)
    {
        var score = await GetAuthorityScoreAsync(targetUrl);
        score.Backlinks++;
        score.ReferringDomains++;
        score.CalculatedAt = DateTime.UtcNow;
        _cache[targetUrl] = score;
    }

    public Task<double> CalculateEntityAuthorityAsync(string entityType, string slug)
    {
        var typeBase = EntityBaseScores.GetValueOrDefault(entityType, 40.0);
        var slugScore = ComputeSlugAuthority(slug);
        var combined = (typeBase * 0.7) + (slugScore * 0.3);
        return Task.FromResult(Math.Clamp(combined, 0, 100));
    }

    public Task<List<string>> GetTopAuthorityPagesAsync(int count = 10)
    {
        var pages = _cache
            .OrderByDescending(kv => kv.Value.DomainAuthority)
            .Take(count)
            .Select(kv => kv.Key)
            .ToList();
        return Task.FromResult(pages);
    }

    private AuthorityScoreResult ComputeAuthorityScore(string url)
    {
        var uri = url.Contains("://")
            ? new Uri(url)
            : new Uri($"https://example.com{url}");

        var path = uri.AbsolutePath.Trim('/');

        var domainLength = uri.Host.Split('.').Length >= 2
            ? uri.Host.Split('.')[^2].Length
            : 5;

        var pathDepth = path.Split('/').Length;
        var hasHyphens = path.Contains('-');
        var pathLength = path.Length;

        var domainAuthority = Math.Clamp(
            20 + domainLength * 4
                - (pathDepth * 2)
                + (hasHyphens ? 5 : 0)
                + Math.Min(pathLength / 5, 15),
            0, 100);

        var pageAuthority = Math.Clamp(
            domainAuthority * 0.8
                + (pathDepth > 1 ? 5 : 0)
                - (pathDepth > 4 ? 10 : 0)
                + (hasHyphens ? 3 : 0),
            0, 100);

        var trustFlow = Math.Clamp(
            domainAuthority * 0.85
                + (path.EndsWith("property", StringComparison.OrdinalIgnoreCase) ? 10 : 0)
                + (path.EndsWith("project", StringComparison.OrdinalIgnoreCase) ? 5 : 0),
            0, 100);

        var citationFlow = Math.Clamp(
            domainAuthority * 0.7
                + (path.Contains("location", StringComparison.OrdinalIgnoreCase) ? 15 : 0)
                + (path.Contains("area", StringComparison.OrdinalIgnoreCase) ? 10 : 0),
            0, 100);

        var referringDomains = Math.Max(1, domainLength * 15 + pathDepth * 5);
        var backlinks = Math.Max(1, referringDomains * 3 + pathLength);

        return new AuthorityScoreResult
        {
            PageUrl = url,
            DomainAuthority = Math.Round((double)domainAuthority, 1),
            PageAuthority = Math.Round((double)pageAuthority, 1),
            TrustFlow = Math.Round((double)trustFlow, 1),
            CitationFlow = Math.Round((double)citationFlow, 1),
            ReferringDomains = referringDomains,
            Backlinks = backlinks,
            CalculatedAt = DateTime.UtcNow
        };
    }

    private static double ComputeSlugAuthority(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return 20;
        var score = Math.Min(slug.Length * 2, 40);
        score += slug.Contains('-') ? 10 : 0;
        score += slug.Any(char.IsDigit) ? 5 : 0;
        return Math.Clamp(score, 0, 100);
    }
}
