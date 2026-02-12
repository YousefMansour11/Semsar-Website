using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using StackExchange.Redis;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Services;

public class RedisSemanticProfileStore : ISemanticDeduplicationService
{
    private readonly IDistributedCache _cache;
    private readonly IDatabase? _db;
    private readonly RedisSetIndex _index;
    private const string Prefix = "semsar:sem:";
    private const string IndexKey = "semsar:sem:index";
    private const string CanonicalKey = "semsar:sem:canonical";
    private const string CanonicalLockPrefix = "semsar:sem:canonical:lock:";
    private static readonly DistributedCacheEntryOptions Ttl = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365) };

    public RedisSemanticProfileStore(IDistributedCache cache, IConnectionMultiplexer? muxer = null)
    {
        _cache = cache;
        _db = muxer?.GetDatabase();
        _index = new RedisSetIndex(cache, IndexKey, Ttl, muxer);
    }

    public async Task<DuplicateResult> AnalyzePageAsync(string url, string title, string description)
    {
        var hash = ComputeContentHash(title, description);
        var fullContent = title + " " + description;
        var result = new DuplicateResult
        {
            PageUrl = url,
            CanonicalUrl = url
        };

        var entries = await _index.GetAllAsync();
        var similar = new List<(string Url, string Content)>();

        foreach (var entry in entries)
        {
            var parts = entry.Split('|');
            if (parts.Length == 3)
            {
                var sim = IsContentSimilar(fullContent, parts[2], 0.8);
                if (sim)
                    similar.Add((parts[0], parts[2]));
            }
        }

        result.SimilarUrls = similar.Select(s => s.Url).ToList();
        result.SimilarityScore = similar.Count > 0 ? 0.9 : 0;
        result.IsDuplicate = similar.Count > 0;

        if (!entries.Any(e => e.StartsWith(url + "|")))
        {
            await _index.AddAsync($"{url}|{hash}|{fullContent}");
        }

        var profile = new SemanticProfile
        {
            Url = url,
            ContentHash = hash,
            Title = title,
            Description = description,
            AddedAt = DateTime.UtcNow,
            CanonicalUrl = result.IsDuplicate ? result.SimilarUrls.First() : url
        };
        await _cache.SetStringAsync(Prefix + url, JsonSerializer.Serialize(profile), Ttl);

        return result;
    }

    public async Task<string> ResolveCanonicalAsync(string url, string title, string description)
    {
        if (_db != null)
        {
            var lockKey = CanonicalLockPrefix + url;
            var lockToken = Guid.NewGuid().ToString();
            var acquired = await _db.LockTakeAsync(lockKey, lockToken, TimeSpan.FromSeconds(5));
            try
            {
                var canonicalMap = await GetCanonicalMapAsync();
                if (canonicalMap.TryGetValue(url, out var mapped))
                    return mapped;

                var analysis = await AnalyzePageAsync(url, title, description);
                var canonical = analysis.SimilarUrls.Count > 0 ? analysis.SimilarUrls.First() : url;
                canonicalMap[url] = canonical;
                await SaveCanonicalMapAsync(canonicalMap);
                return canonical;
            }
            finally
            {
                if (acquired)
                    await _db.LockReleaseAsync(lockKey, lockToken);
            }
        }

        var localMap = await GetCanonicalMapAsync();
        if (localMap.TryGetValue(url, out var localMapped))
            return localMapped;

        var localAnalysis = await AnalyzePageAsync(url, title, description);
        var localCanonical = localAnalysis.SimilarUrls.Count > 0 ? localAnalysis.SimilarUrls.First() : url;
        localMap[url] = localCanonical;
        await SaveCanonicalMapAsync(localMap);
        return localCanonical;
    }

    public async Task<List<DuplicateResult>> FindNearDuplicatesAsync(string title, string description, string? excludeUrl = null)
    {
        var hash = ComputeContentHash(title, description);
        var entries = await _index.GetAllAsync();
        var results = new List<DuplicateResult>();

        foreach (var entry in entries)
        {
            var parts = entry.Split('|');
            if (parts.Length < 2) continue;
            if (excludeUrl is not null && parts[0] == excludeUrl) continue;

            var content = parts.Length >= 3 ? parts[2] : parts[1].Replace("-", " ");
            if (IsContentSimilar(title + " " + description, content, 0.8))
            {
                results.Add(new DuplicateResult
                {
                    PageUrl = parts[0],
                    SimilarUrls = new List<string>(),
                    CanonicalUrl = parts[0],
                    SimilarityScore = 0.85,
                    IsDuplicate = true
                });
            }
        }

        return results;
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

    private async Task<Dictionary<string, string>> GetCanonicalMapAsync()
    {
        var json = await _cache.GetStringAsync(CanonicalKey);
        return json is not null
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>()
            : new Dictionary<string, string>();
    }

    private async Task SaveCanonicalMapAsync(Dictionary<string, string> map)
    {
        await _cache.SetStringAsync(CanonicalKey, JsonSerializer.Serialize(map), Ttl);
    }

    private static HashSet<string> GetWords(string text)
    {
        var separators = new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':', '-', '_', '(', ')' };
        return new HashSet<string>(
            (text ?? "").ToLowerInvariant().Split(separators, StringSplitOptions.RemoveEmptyEntries),
            StringComparer.OrdinalIgnoreCase
        );
    }

    private class SemanticProfile
    {
        public string Url { get; set; } = string.Empty;
        public string ContentHash { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; }
        public string CanonicalUrl { get; set; } = string.Empty;
    }
}
