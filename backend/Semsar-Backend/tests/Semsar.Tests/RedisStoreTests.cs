using Application.Interfaces;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace Semsar.Tests;

public class InMemoryDistributedCache : IDistributedCache
{
    private readonly Dictionary<string, byte[]> _storage = new();

    public byte[]? Get(string key) =>
        _storage.TryGetValue(key, out var v) ? v : null;

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
        Task.FromResult<byte[]?>(_storage.TryGetValue(key, out var v) ? v : null);

    public void Set(string key, byte[] value, DistributedCacheEntryOptions? options = null) =>
        _storage[key] = value;

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions? options = null, CancellationToken token = default)
    {
        _storage[key] = value;
        return Task.CompletedTask;
    }

    public void Refresh(string key) { }
    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
    public void Remove(string key) => _storage.Remove(key);
    public Task RemoveAsync(string key, CancellationToken token = default) { _storage.Remove(key); return Task.CompletedTask; }

    public string? GetString(string key) =>
        _storage.TryGetValue(key, out var v) ? Encoding.UTF8.GetString(v) : null;

    public Task<string?> GetStringAsync(string key, CancellationToken token = default) =>
        Task.FromResult<string?>(_storage.TryGetValue(key, out var v) ? Encoding.UTF8.GetString(v) : null);

    public void SetString(string key, string value, DistributedCacheEntryOptions? options = null) =>
        _storage[key] = Encoding.UTF8.GetBytes(value);

    public Task SetStringAsync(string key, string value, DistributedCacheEntryOptions? options = null, CancellationToken token = default)
    {
        _storage[key] = Encoding.UTF8.GetBytes(value);
        return Task.CompletedTask;
    }
}

public class RedisStoreTests
{
    [Fact]
    public async Task RedisRankingDataStore_Records_And_Retrieves()
    {
        var cache = new InMemoryDistributedCache();
        var store = new RedisRankingDataStore(cache);

        await store.RecordRankingAsync(new RankingRecord { Keyword = "test kw", PageUrl = "/page/1", Position = 5 });

        var latest = await store.GetLatestRankingAsync("test kw", "/page/1");
        latest.Should().NotBeNull();
        latest!.Keyword.Should().Be("test kw");
        latest.Position.Should().Be(5);
    }

    [Fact]
    public async Task RedisRankingDataStore_Gets_Trends()
    {
        var cache = new InMemoryDistributedCache();
        var store = new RedisRankingDataStore(cache);

        await store.RecordRankingAsync(new RankingRecord { Keyword = "kw1", PageUrl = "/page/1", Position = 10, CheckedAt = DateTime.UtcNow.AddDays(-1) });
        await store.RecordRankingAsync(new RankingRecord { Keyword = "kw1", PageUrl = "/page/1", Position = 7, CheckedAt = DateTime.UtcNow });

        var trends = await store.GetAllTrendsAsync();
        trends.Should().NotBeEmpty();
        trends.First().Trend.Should().Be("up");
    }

    [Fact]
    public async Task RedisRankingDataStore_Keywords_In_Range()
    {
        var cache = new InMemoryDistributedCache();
        var store = new RedisRankingDataStore(cache);

        await store.RecordRankingAsync(new RankingRecord { Keyword = "kw1", PageUrl = "/page/1", Position = 3, CheckedAt = DateTime.UtcNow });
        await store.RecordRankingAsync(new RankingRecord { Keyword = "kw2", PageUrl = "/page/2", Position = 15, CheckedAt = DateTime.UtcNow });

        var inRange = await store.GetKeywordsInPositionRangeAsync(1, 5);
        inRange.Should().Contain("kw1");
        inRange.Should().NotContain("kw2");
    }

    [Fact]
    public async Task RedisAuthoritySignalStore_Computes_Score()
    {
        var cache = new InMemoryDistributedCache();
        var store = new RedisAuthoritySignalStore(cache);

        var score = await store.GetAuthorityScoreAsync("/property/test");
        score.Should().NotBeNull();
        score.DomainAuthority.Should().BeInRange(0, 100);
        score.PageUrl.Should().Be("/property/test");
    }

    [Fact]
    public async Task RedisAuthoritySignalStore_Calculates_Entity_Authority()
    {
        var cache = new InMemoryDistributedCache();
        var store = new RedisAuthoritySignalStore(cache);

        var authority = await store.CalculateEntityAuthorityAsync("property", "test-slug");
        authority.Should().BeInRange(0, 100);
    }

    [Fact]
    public async Task RedisSemanticProfileStore_Detects_Duplicates()
    {
        var cache = new InMemoryDistributedCache();
        var store = new RedisSemanticProfileStore(cache);

        var result1 = await store.AnalyzePageAsync("/page/1", "Same Title Content", "Same Description Content");
        result1.IsDuplicate.Should().BeFalse();

        var result2 = await store.AnalyzePageAsync("/page/2", "Same Title Content", "Same Description Content");
        result2.IsDuplicate.Should().BeTrue();
        result2.SimilarUrls.Should().Contain("/page/1");
    }

    [Fact]
    public async Task RedisSemanticProfileStore_Resolves_Canonical()
    {
        var cache = new InMemoryDistributedCache();
        var store = new RedisSemanticProfileStore(cache);

        await store.AnalyzePageAsync("/page/original", "Unique Title A", "Unique Desc A");
        var canonical = await store.ResolveCanonicalAsync("/page/duplicate", "Unique Title A", "Unique Desc A");
        canonical.Should().Be("/page/original");
    }

    [Fact]
    public void RedisEntityGraphStore_Builds_Graph()
    {
        var cache = new InMemoryDistributedCache();
        var store = new RedisEntityGraphStore(cache);

        var loc = store.BuildEntityNode("location", "new-cairo", "New Cairo", "A district");
        var prop = store.BuildEntityNode("property", "prop-1", "Property One");
        store.AddRelationship(loc, "contains", prop);

        var graph = store.BuildKnowledgeGraph("location", "new-cairo");
        graph.Should().NotBeNull();
        graph.NodeCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void RedisEntityGraphStore_Verifies_Integrity()
    {
        var cache = new InMemoryDistributedCache();
        var store = new RedisEntityGraphStore(cache);

        var loc = store.BuildEntityNode("location", "area", "Area");
        var prop = store.BuildEntityNode("property", "p1", "Property 1");
        store.AddRelationship(loc, "contains", prop);

        var valid = store.VerifyGraphIntegrity("location", "area");
        valid.Should().BeTrue();
    }

    [Fact]
    public async Task RedisIndexVelocityStore_Tracks_Velocity()
    {
        var cache = new InMemoryDistributedCache();
        var store = new RedisIndexVelocityStore(cache);

        await store.RecordSubmissionAsync("/page/1");
        await store.RecordIndexingAsync("/page/1");

        var velocity = await store.GetCurrentVelocityAsync();
        velocity.Should().NotBeNull();
    }

    [Fact]
    public async Task RedisIndexVelocityStore_Detects_Needs_Indexing()
    {
        var cache = new InMemoryDistributedCache();
        var store = new RedisIndexVelocityStore(cache);

        await store.RecordSubmissionAsync("/page/unindexed");
        var needing = await store.GetUrlsNeedingIndexingAsync(10);
        needing.Should().Contain("/page/unindexed");
    }
}
