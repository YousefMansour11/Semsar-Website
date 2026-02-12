using System.Text.Json;
using Application.Interfaces;
using StackExchange.Redis;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Services;

public class RedisAuthoritySignalStore : IAuthoritySignalService
{
    private readonly IDistributedCache _cache;
    private readonly IDatabase? _db;
    private readonly RedisSetIndex _index;
    private const string Prefix = "semsar:auth:";
    private const string IndexKey = "semsar:auth:index";
    private static readonly DistributedCacheEntryOptions Ttl = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30) };

    private static readonly Dictionary<string, double> EntityBaseScores = new(StringComparer.OrdinalIgnoreCase)
    {
        ["property"] = 45.0,
        ["project"] = 55.0,
        ["location"] = 60.0,
        ["unit"] = 35.0,
    };

    public RedisAuthoritySignalStore(IDistributedCache cache, IConnectionMultiplexer? muxer = null)
    {
        _cache = cache;
        _db = muxer?.GetDatabase();
        _index = new RedisSetIndex(cache, IndexKey, Ttl, muxer);
    }

    public async Task<AuthorityScoreResult> GetAuthorityScoreAsync(string url)
    {
        var json = await _cache.GetStringAsync(Prefix + url);
        if (json is not null)
            return JsonSerializer.Deserialize<AuthorityScoreResult>(json)!;

        var result = await ComputeAuthorityScoreAsync(url);
        await _cache.SetStringAsync(Prefix + url, JsonSerializer.Serialize(result), Ttl);
        await _index.AddAsync(url);
        return result;
    }

    public async Task RecordBacklinkAsync(string targetUrl, string sourceUrl)
    {
        if (_db != null)
        {
            var hashKey = new RedisKey(Prefix + targetUrl);
            await _db.HashIncrementAsync(hashKey, "backlinks");
            await _db.HashIncrementAsync(hashKey, "referringDomains");
            await _db.HashSetAsync(hashKey, "calculatedAt", DateTime.UtcNow.ToString("O"));
            await _index.AddAsync(targetUrl);
            return;
        }

        var score = await GetAuthorityScoreAsync(targetUrl);
        score.Backlinks++;
        score.ReferringDomains++;
        score.CalculatedAt = DateTime.UtcNow;
        await _cache.SetStringAsync(Prefix + targetUrl, JsonSerializer.Serialize(score), Ttl);
    }

    public async Task<double> CalculateEntityAuthorityAsync(string entityType, string slug)
    {
        var baseScore = EntityBaseScores.GetValueOrDefault(entityType, 40.0);
        var url = $"/{entityType}/{slug}";
        var score = await GetAuthorityScoreAsync(url);
        var authorityScore = (baseScore * 0.7) + (score.DomainAuthority * 0.3);
        return Math.Clamp(authorityScore, 0, 100);
    }

    public async Task<List<string>> GetTopAuthorityPagesAsync(int count = 10)
    {
        var urls = await _index.GetAllAsync();
        var scores = new List<(string Url, AuthorityScoreResult Score)>();
        foreach (var url in urls)
        {
            var json = await _cache.GetStringAsync(Prefix + url);
            if (json is not null)
                scores.Add((url, JsonSerializer.Deserialize<AuthorityScoreResult>(json)!));
        }

        return scores
            .OrderByDescending(s => s.Score.DomainAuthority)
            .Take(count)
            .Select(s => s.Url)
            .ToList();
    }

    private async Task<AuthorityScoreResult> ComputeAuthorityScoreAsync(string url)
    {
        var uri = new Uri(url.Contains("://") ? url : $"https://example.com{url}");
        var domainParts = uri.Host.Split('.');
        var domainLength = domainParts.Length >= 2 ? domainParts[^2].Length : 5;

        var domainAuthority = Math.Clamp(10 + domainLength * 5 + new Random().Next(-5, 15), 0, 100);
        var pageAuthority = Math.Clamp(domainAuthority * 0.8 + new Random().Next(-5, 10), 0, 100);
        var trustFlow = Math.Clamp(domainAuthority * 0.85 + new Random().Next(-3, 8), 0, 100);
        var citationFlow = Math.Clamp(domainAuthority * 0.75 + new Random().Next(-10, 10), 0, 100);
        var referringDomains = domainLength * 20 + new Random().Next(10, 200);
        var backlinks = referringDomains * 5 + new Random().Next(50, 500);

        return new AuthorityScoreResult
        {
            PageUrl = url,
            DomainAuthority = domainAuthority,
            PageAuthority = pageAuthority,
            TrustFlow = trustFlow,
            CitationFlow = citationFlow,
            ReferringDomains = Math.Max(1, referringDomains),
            Backlinks = Math.Max(1, backlinks),
            CalculatedAt = DateTime.UtcNow
        };
    }
}
