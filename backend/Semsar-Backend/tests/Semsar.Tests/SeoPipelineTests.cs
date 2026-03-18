using Application.Interfaces;
using Application.Services;
using Application.Settings;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Semsar.Tests;

public class SeoPipelineTests
{
    private static IOptions<AppSettings> CreateSettings()
    {
        return Options.Create(new AppSettings
        {
            BaseUrl = "https://example.com"
        });
    }

    [Fact]
    public void SeoContentGenerator_Generates_Title_For_Property()
    {
        var gen = new SeoContentGenerator();
        var result = gen.Generate(
            SeoEntityType.Property,
            "Modern Apartment", "شقة حديثة",
            "A beautiful modern apartment in New Cairo", "شقة جميلة في التجمع الخامس",
            "New Cairo", "Apartment", "Sale", 1500000, "EGP",
            new List<string> { "pool", "gym", "parking" });
        result.TitleEn.Should().NotBeNullOrWhiteSpace();
        result.TitleEn.Length.Should().BeLessThanOrEqualTo(60);
        result.TitleAr.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void SeoContentGenerator_Generates_FAQ_Items()
    {
        var gen = new SeoContentGenerator();
        var result = gen.Generate(
            SeoEntityType.Property,
            "Villa in North Coast", "فيلا في الساحل الشمالي",
            "Luxury villa with sea view", "فيلا فاخرة مع إطلالة على البحر",
            "North Coast", "Villa", "Sale", 5000000, "EGP", null);
        result.Faqs.Should().NotBeEmpty();
        result.Faqs.Should().HaveCountGreaterThanOrEqualTo(3);
        foreach (var faq in result.Faqs)
        {
            faq.QuestionEn.Should().NotBeNullOrWhiteSpace();
            faq.AnswerEn.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void SerpVariantGenerator_Selects_Best_Variant()
    {
        var gen = new SERPVariantGenerator();
        var request = new SerpVariantRequest
        {
            EntityType = SeoEntityType.Property,
            TitleEn = "Modern Apartment in New Cairo",
            DescriptionEn = "A beautiful modern apartment",
            Location = "New Cairo",
            PropertyType = "Apartment",
            ListingType = "Sale",
            Price = 1500000,
            Currency = "EGP"
        };

        var variants = gen.GenerateVariants(request);
        variants.Should().NotBeEmpty();

        var best = gen.SelectBestVariant(variants);
        best.Should().NotBeNull();
        best.PredictedCtrScore.Should().BeInRange(0, 100);
    }

    [Fact]
    public void SerpVariantGenerator_Generates_Multiple_Variants()
    {
        var gen = new SERPVariantGenerator();
        var request = new SerpVariantRequest
        {
            EntityType = SeoEntityType.Property,
            TitleEn = "Villa",
            DescriptionEn = "A luxury villa",
            Location = "New Cairo",
            PropertyType = "Villa",
            ListingType = "Sale",
            Price = 3000000,
            Currency = "EGP"
        };

        var variants = gen.GenerateVariants(request);
        variants.Should().HaveCountGreaterThanOrEqualTo(3);
        foreach (var v in variants)
        {
            v.TitleEn.Should().NotBeNullOrWhiteSpace();
            v.DescriptionEn.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void CanonicalService_Builds_Valid_Canonical()
    {
        var settings = CreateSettings();
        var service = new CanonicalService(settings);
        var result = service.BuildCanonical("property", "modern-apartment-in-new-cairo");
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("modern-apartment");
    }

    [Fact]
    public void CanonicalService_Builds_Hreflang_Tags()
    {
        var settings = CreateSettings();
        var service = new CanonicalService(settings);
        var tags = service.BuildHreflangTags("property", "en-slug", "ar-slug", null, null);
        tags.Should().NotBeEmpty();
        tags.Should().Contain(t => t.HrefLang == "en");
        tags.Should().Contain(t => t.HrefLang == "ar");
        tags.Should().Contain(t => t.HrefLang == "x-default");
    }

    [Fact]
    public void JsonLdService_Builds_Valid_PropertyJsonLd()
    {
        var service = new JsonLdService();
        var json = service.BuildPropertyJsonLd(
            "Modern Apartment", "A beautiful apartment", "SEO description",
            "/property/test", "CODE123", "New Cairo", "EGP",
            "Sale", 1500000, null, new List<string>(), "prop123");
        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("\"@context\"");
        json.Should().Contain("\"RealEstateListing\"");
        json.Should().Contain("Modern Apartment");
    }

    [Fact]
    public void JsonLdService_Builds_Valid_BreadcrumbJsonLd()
    {
        var service = new JsonLdService();
        var items = new List<(string, string)>
        {
            ("Home", "/"),
            ("Properties", "/properties/filter"),
            ("Apartment Details", "/property/test")
        };
        var json = service.BuildBreadcrumbJsonLd(items);
        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("\"BreadcrumbList\"");
        json.Should().Contain("Apartment Details");
    }

    [Fact]
    public void JsonLdService_Builds_Valid_FaqJsonLd()
    {
        var service = new JsonLdService();
        var faqs = new List<(string, string)>
        {
            ("Question 1?", "Answer 1"),
            ("Question 2?", "Answer 2")
        };
        var json = service.BuildFaqJsonLd(faqs);
        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("\"FAQPage\"");
        json.Should().Contain("Question 1?");
        json.Should().Contain("Answer 1");
    }

    [Fact]
    public void OgMetaService_Builds_Valid_Meta()
    {
        var service = new OgMetaService();
        var ogMeta = service.BuildPropertyOgMeta(
            "Test Title", "عنوان تجريبي",
            "Test Description", "وصف تجريبي",
            "/property/test", new List<string> { "https://example.com/img.jpg" });
        ogMeta.Should().NotBeNull();
        ogMeta.Title.Should().Be("Test Title");
        ogMeta.Url.Should().Be("/property/test");
    }

    [Fact]
    public void InternalLinkingService_Generates_Links()
    {
        var service = new InternalLinkingService();
        var links = service.GenerateLinks("New Cairo", "Apartment", "Sale", "test-slug", null);
        links.Should().NotBeEmpty();
        foreach (var group in links)
        {
            group.SectionTitle.Should().NotBeNullOrWhiteSpace();
            group.Links.Should().NotBeEmpty();
            foreach (var link in group.Links)
            {
                link.Url.Should().NotBeNullOrWhiteSpace();
                link.Text.Should().NotBeNullOrWhiteSpace();
            }
        }
    }

    [Fact]
    public void InternalLinkingService_Meets_Minimum_Requirement()
    {
        var service = new InternalLinkingService();
        var links = service.GenerateLinks("New Cairo", "Apartment", "Sale", "test-slug", null);
        var meetsMinimum = service.MeetsMinimumRequirement(links);
        meetsMinimum.Should().BeTrue();
    }

    [Fact]
    public void InternalLinkingService_Finds_Missing_Links()
    {
        var service = new InternalLinkingService();
        var missing = service.GetMissingLinks("New Cairo", "Apartment", "Sale", "test-slug");
        missing.Should().NotBeNull();
    }

    [Fact]
    public async Task SemanticDeduplication_Detects_Duplicates()
    {
        var service = new SemanticDeduplicationService();

        var result1 = await service.AnalyzePageAsync("/property/one", "Modern Apartment in New Cairo", "Beautiful apartment with pool");
        result1.IsDuplicate.Should().BeFalse();

        var result2 = await service.AnalyzePageAsync("/property/two", "Modern Apartment in New Cairo", "Beautiful apartment with pool");
        result2.IsDuplicate.Should().BeTrue();
        result2.SimilarUrls.Should().Contain("/property/one");
    }

    [Fact]
    public void SemanticDeduplication_Computes_Content_Hash()
    {
        var service = new SemanticDeduplicationService();
        var hash1 = service.ComputeContentHash("Title", "Description");
        var hash2 = service.ComputeContentHash("Title", "Description");
        var hash3 = service.ComputeContentHash("Different", "Content");

        hash1.Should().Be(hash2);
        hash1.Should().NotBe(hash3);
    }

    [Fact]
    public void SemanticDeduplication_Detects_Similar_Content()
    {
        var service = new SemanticDeduplicationService();
        var similar = service.IsContentSimilar(
            "modern apartment new cairo pool garden parking",
            "modern apartment new cairo pool garden view",
            0.7);
        similar.Should().BeTrue();

        var notSimilar = service.IsContentSimilar(
            "villa north coast sea view",
            "studio downtown cairo cheap rent",
            0.7);
        notSimilar.Should().BeFalse();
    }

    [Fact]
    public void EntityGraphService_Builds_And_Traverses_Graph()
    {
        var service = new EntityGraphService();
        var node1 = service.BuildEntityNode("location", "new-cairo", "New Cairo", "A district in Cairo");
        var node2 = service.BuildEntityNode("property", "prop-123", "Modern Apartment", "A nice apartment");

        service.AddRelationship(node1, "contains", node2);

        var graph = service.BuildKnowledgeGraph("location", "new-cairo");
        graph.Should().NotBeNull();
        graph.NodeCount.Should().BeGreaterThanOrEqualTo(2);
        graph.JsonLd.Should().NotBeNullOrWhiteSpace();
        graph.JsonLd.Should().Contain("New Cairo");
        graph.JsonLd.Should().Contain("Modern Apartment");
    }

    [Fact]
    public void EntityGraphService_Verifies_Integrity()
    {
        var service = new EntityGraphService();
        var loc = service.BuildEntityNode("location", "test-area", "Test Area");
        var prop = service.BuildEntityNode("property", "test-prop", "Test Property");
        service.AddRelationship(loc, "contains", prop);

        var valid = service.VerifyGraphIntegrity("location", "test-area");
        valid.Should().BeTrue();
    }

    [Fact]
    public void EntityGraphService_Gets_Related_Entities()
    {
        var service = new EntityGraphService();
        var loc = service.BuildEntityNode("location", "area1", "Area 1");
        var p1 = service.BuildEntityNode("property", "p1", "Property 1");
        var p2 = service.BuildEntityNode("property", "p2", "Property 2");
        service.AddRelationship(loc, "contains", p1);
        service.AddRelationship(loc, "contains", p2);

        var related = service.GetRelatedEntities("property", "p1");
        related.Should().NotBeEmpty();
        related.Should().Contain(r => r.EntityId == "area1");
    }

    [Fact]
    public void ClickBehaviorOptimization_Tracks_Clicks_And_CTR()
    {
        var service = new ClickBehaviorOptimizationService();
        service.RecordImpression("/property/test");
        service.RecordImpression("/property/test");
        service.RecordClick("/property/test");
        service.RecordClick("/property/test");

        var ctr = service.GetCurrentCtr("/property/test");
        ctr.Should().Be(100.0);
    }

    [Fact]
    public void ClickBehaviorOptimization_OptimizeTitle_Returns_Base_Unchanged()
    {
        var service = new ClickBehaviorOptimizationService();
        for (int i = 0; i < 30; i++)
            service.RecordImpression("/property/test");
        service.RecordClick("/property/test");

        var optimized = service.OptimizeTitle("Test Title", "/property/test");
        optimized.Should().Be("Test Title");
    }

    [Fact]
    public void ClickBehaviorOptimization_OptimizeDescription_Returns_Base_Unchanged()
    {
        var service = new ClickBehaviorOptimizationService();
        service.RecordImpression("/property/test");
        service.RecordClick("/property/test");

        var optimized = service.OptimizeDescription("A test description for the property.", "/property/test");
        optimized.Should().Be("A test description for the property.");
    }

    [Fact]
    public void ClickBehaviorOptimization_Gets_Top_Performing()
    {
        var service = new ClickBehaviorOptimizationService();
        service.RecordImpression("/page/1");
        service.RecordClick("/page/1");
        service.RecordImpression("/page/2");

        var top = service.GetTopPerformingUrls(10);
        top.Should().NotBeEmpty();
        top.First().PageUrl.Should().Be("/page/1");
    }

    [Fact]
    public async Task AuthoritySignalService_Computes_Scores()
    {
        var service = new AuthoritySignalService();
        var score = await service.GetAuthorityScoreAsync("/property/test");
        score.Should().NotBeNull();
        score.DomainAuthority.Should().BeInRange(0, 100);
        score.PageAuthority.Should().BeInRange(0, 100);
        score.Backlinks.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AuthoritySignalService_Calculates_Entity_Authority()
    {
        var service = new AuthoritySignalService();
        var authority = await service.CalculateEntityAuthorityAsync("property", "test-slug");
        authority.Should().BeInRange(0, 100);
    }

    [Fact]
    public void FreshnessService_Computes_Score()
    {
        var service = new FreshnessService();
        var score = service.ComputeFreshnessScore(DateTime.UtcNow.AddDays(-7));
        score.Should().BeInRange(0, 100);
        score.Should().BeLessThan(100);
    }

    [Fact]
    public async Task FreshnessService_Detects_Stale_Content()
    {
        var service = new FreshnessService();
        var freshResult = await service.CalculateFreshnessAsync("property", 1, DateTime.UtcNow.AddHours(-1));
        freshResult.Score.Should().BeGreaterThan(0);

        var staleResult = await service.CalculateFreshnessAsync("property", 2, DateTime.UtcNow.AddDays(-60));
        staleResult.Score.Should().BeLessThan(20);
        staleResult.NeedsUpdate.Should().BeTrue();
    }

    [Fact]
    public async Task RankingDataStore_Records_And_Retrieves()
    {
        var store = new RankingDataStore();
        await store.RecordRankingAsync(new RankingRecord
        {
            Keyword = "test keyword",
            PageUrl = "/property/test",
            Position = 5,
            SearchEngine = "google"
        });

        var latest = await store.GetLatestRankingAsync("test keyword", "/property/test");
        latest.Should().NotBeNull();
        latest!.Position.Should().Be(5);
    }

    [Fact]
    public async Task RankingDataStore_Gets_Trends()
    {
        var store = new RankingDataStore();
        await store.RecordRankingAsync(new RankingRecord { Keyword = "kw1", PageUrl = "/page/1", Position = 10, PreviousPosition = 15, CheckedAt = DateTime.UtcNow.AddDays(-1) });
        await store.RecordRankingAsync(new RankingRecord { Keyword = "kw1", PageUrl = "/page/1", Position = 8, PreviousPosition = 10, CheckedAt = DateTime.UtcNow });

        var trends = await store.GetAllTrendsAsync();
        trends.Should().NotBeEmpty();
        trends.First().Trend.Should().Be("up");
    }

    [Fact]
    public async Task IndexVelocityService_Tracks_Submissions()
    {
        var service = new IndexVelocityService();
        await service.RecordSubmissionAsync("/page/1");
        await service.RecordIndexingAsync("/page/1");

        var velocity = await service.GetCurrentVelocityAsync();
        velocity.Should().NotBeNull();
    }

    [Fact]
    public async Task IndexVelocityService_Detects_Needs_Indexing()
    {
        var service = new IndexVelocityService();
        await service.RecordSubmissionAsync("/page/unindexed");

        var needing = await service.GetUrlsNeedingIndexingAsync(10);
        needing.Should().Contain("/page/unindexed");
    }

    [Fact]
    public void CrawlBudgetOptimizer_Computes_Priorities()
    {
        var service = new CrawlBudgetOptimizer();
        var pages = new List<CrawlPriority>
        {
            new() { PageUrl = "/property/1", Importance = 0.9 },
            new() { PageUrl = "/property/2", Importance = 0.1 }
        };

        var prioritized = service.ComputeCrawlPriorities(pages);
        prioritized.Should().NotBeEmpty();
        prioritized.First().PriorityScore.Should().BeGreaterThanOrEqualTo(prioritized.Last().PriorityScore);
    }

    [Fact]
    public void CrawlBudgetOptimizer_Filters_Unimportant()
    {
        var service = new CrawlBudgetOptimizer();
        var pages = new List<CrawlPriority>
        {
            new() { PageUrl = "/important", Importance = 0.9, PriorityScore = 0.8 },
            new() { PageUrl = "/trash", Importance = 0.1, PriorityScore = 0.1 }
        };

        var filtered = service.FilterUnimportantPages(pages, 0.3);
        filtered.Should().Contain(p => p.PageUrl == "/important");
        filtered.Should().NotContain(p => p.PageUrl == "/trash");
    }

    [Fact]
    public async Task LocationSeoService_Generates_Data()
    {
        var service = new LocationSeoService();
        var data = await service.GenerateLocationSeoAsync("New Cairo", "Apartment");
        data.Should().NotBeNull();
        data.TitleEn.Should().NotBeNullOrWhiteSpace();
        data.DescriptionEn.Should().NotBeNullOrWhiteSpace();
        data.H1En.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void LocationSeoService_Calculates_Relevance()
    {
        var service = new LocationSeoService();
        var relevance = service.CalculateLocationRelevance("New Cairo", "apartment in new cairo");
        relevance.Should().BeGreaterThan(0);
    }

    [Fact]
    public void IndexControlService_Returns_Directives()
    {
        var service = new IndexControlService();
        var directive = service.GetIndexDirective("/property/test", "property", 0.8);
        directive.Should().NotBeNull();
        directive.ShouldIndex.Should().BeTrue();
        directive.RobotsTag.Should().Contain("index");
    }

    [Fact]
    public void IndexControlService_Blocks_Low_Quality()
    {
        var service = new IndexControlService();
        var directive = service.GetIndexDirective("/thin/content", "property", 0.1);
        directive.ShouldIndex.Should().BeFalse();
    }

    [Fact]
    public void IndexControlService_Assesses_Page_Quality()
    {
        var service = new IndexControlService();
        var quality = service.AssessPageQuality(
            "Good Title for SEO",
            "Good description with adequate length for SEO purposes and ranking well.",
            "Content with sufficient words to pass the thin content check and be considered valuable for search engine indexing and ranking.",
            300);
        quality.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public async Task TopicClusterService_Creates_Cluster()
    {
        var service = new TopicClusterService();
        var cluster = await service.CreateClusterAsync("New Cairo Properties", "/location/new-cairo", "new cairo real estate");
        cluster.Should().NotBeNull();
        cluster.ClusterId.Should().NotBeNullOrWhiteSpace();
        cluster.PillarPageUrl.Should().Be("/location/new-cairo");
    }

    [Fact]
    public async Task TopicClusterService_Adds_To_Cluster()
    {
        var service = new TopicClusterService();
        var cluster = await service.CreateClusterAsync("Test Cluster", "/pillar", "test keyword");
        await service.AddToClusterAsync(cluster.ClusterId, "/property/1", "keyword 1");
        await service.AddToClusterAsync(cluster.ClusterId, "/property/2", "keyword 2");

        var integrity = await service.VerifyClusterIntegrityAsync(cluster.ClusterId);
        integrity.Should().NotBeNull();
        integrity.ActualPages.Should().Be(3); // pillar + 2 added
    }

    [Fact]
    public async Task RankingFeedbackLoop_Generates_Recommendations()
    {
        var dataStore = new RankingDataStore();
        var seoGen = new SeoContentGenerator();
        var service = new RankingFeedbackLoopService(dataStore, seoGen);

        await dataStore.RecordRankingAsync(new RankingRecord
        {
            Keyword = "test kw",
            PageUrl = "/property/test",
            Position = 15,
            PreviousPosition = 5,
            CheckedAt = DateTime.UtcNow
        });

        var recs = await service.GenerateRecommendationsAsync("property", 1);
        recs.Should().NotBeNull();
    }

    [Fact]
    public void SeoUtils_Contains_Arabic()
    {
        SeoUtils.ContainsArabic("English text").Should().BeFalse();
        SeoUtils.ContainsArabic("نص عربي").Should().BeTrue();
        SeoUtils.ContainsArabic("Mixed نص عربي").Should().BeTrue();
    }

    [Fact]
    public void All_Services_Construct_Without_Error()
    {
        var settings = CreateSettings();
        var seoContent = new SeoContentGenerator();
        var serp = new SERPVariantGenerator();
        var canonical = new CanonicalService(settings);
        var jsonLd = new JsonLdService();
        var ogMeta = new OgMetaService();
        var internalLinks = new InternalLinkingService();
        var semanticDedup = new SemanticDeduplicationService();
        var entityGraph = new EntityGraphService();
        var clickBehavior = new ClickBehaviorOptimizationService();
        var authority = new AuthoritySignalService();
        var rankingStore = new RankingDataStore();
        var rankingFeedback = new RankingFeedbackLoopService(rankingStore, seoContent);
        var indexVelocity = new IndexVelocityService();
        var crawlBudget = new CrawlBudgetOptimizer();
        var freshness = new FreshnessService();
        var locationSeo = new LocationSeoService();
        var indexControl = new IndexControlService();
        var topicCluster = new TopicClusterService();

        seoContent.Should().NotBeNull();
        serp.Should().NotBeNull();
        canonical.Should().NotBeNull();
        jsonLd.Should().NotBeNull();
        ogMeta.Should().NotBeNull();
        internalLinks.Should().NotBeNull();
        semanticDedup.Should().NotBeNull();
        entityGraph.Should().NotBeNull();
        clickBehavior.Should().NotBeNull();
        authority.Should().NotBeNull();
        rankingStore.Should().NotBeNull();
        rankingFeedback.Should().NotBeNull();
        indexVelocity.Should().NotBeNull();
        crawlBudget.Should().NotBeNull();
        freshness.Should().NotBeNull();
        locationSeo.Should().NotBeNull();
        indexControl.Should().NotBeNull();
        topicCluster.Should().NotBeNull();
    }

    [Fact]
    public void SerpVariantGenerator_Is_Deterministic_Same_Input_Always_Same_Output()
    {
        var gen = new SERPVariantGenerator();
        var request = new SerpVariantRequest
        {
            EntityType = SeoEntityType.Property,
            TitleEn = "Modern Apartment in New Cairo",
            DescriptionEn = "A beautiful modern apartment",
            Location = "New Cairo",
            PropertyType = "Apartment",
            ListingType = "Sale",
            Price = 1500000,
            Currency = "EGP"
        };

        var variants1 = gen.GenerateVariants(request);
        var best1 = gen.SelectBestVariant(variants1);
        var variants2 = gen.GenerateVariants(request);
        var best2 = gen.SelectBestVariant(variants2);
        var variants3 = gen.GenerateVariants(request);
        var best3 = gen.SelectBestVariant(variants3);

        best1.VariantId.Should().Be(best2.VariantId);
        best2.VariantId.Should().Be(best3.VariantId);
        best1.TitleEn.Should().Be(best2.TitleEn);
        best1.DescriptionEn.Should().Be(best2.DescriptionEn);
    }

    [Fact]
    public void SeoValidationGate_Detects_Empty_Title()
    {
        var gate = new SeoValidationGate(null);
        var result = gate.ValidatePropertySeo(
            "", "A description", "https://example.com/page",
            "residential", "Cairo", null, null, null, null, "Sale", 1000000);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("SeoTitle"));
    }

    [Fact]
    public void SeoValidationGate_Passes_Valid_Input()
    {
        var gate = new SeoValidationGate(null);
        var result = gate.ValidatePropertySeo(
            "Test Property Title", "A valid description for testing purposes.",
            "https://example.com/property/test",
            "residential", "Cairo",
            "{\"@context\":\"https://schema.org\",\"@type\":\"FAQPage\",\"mainEntity\":[]}",
            "{\"@context\":\"https://schema.org\",\"@type\":\"BreadcrumbList\",\"itemListElement\":[{\"@type\":\"ListItem\",\"position\":1,\"name\":\"Home\",\"item\":\"/\"},{\"@type\":\"ListItem\",\"position\":2,\"name\":\"Properties\",\"item\":\"/properties\"}]}",
            "{\"nodes\":[]}",
            "[{\"sectionTitle\":\"Related\",\"links\":[{\"text\":\"Link 1\",\"url\":\"/link1\",\"type\":\"location\"},{\"text\":\"Link 2\",\"url\":\"/link2\",\"type\":\"guide\"},{\"text\":\"Link 3\",\"url\":\"/link3\",\"type\":\"filter\"}]}]",
            "Sale", 1000000);
        result.IsValid.Should().BeTrue();
    }
}
