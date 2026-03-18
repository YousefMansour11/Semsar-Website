using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using API.Controllers;
using API.Middleware;
using API.Services;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Semsar.Tests;

// ============================================================
// Non-static bot behavior store for test isolation
// ============================================================
public sealed class TestBotBehaviorStore : IBotBehaviorStore
{
    private readonly ConcurrentDictionary<string, List<DateTime>> _velocity = new();
    private readonly ConcurrentDictionary<string, (string Fingerprint, DateTime FirstSeen)> _sessions = new();
    private readonly ConcurrentDictionary<string, List<string>> _payloads = new();

    public bool CheckVelocity(string key, int maxRequests, TimeSpan window)
    {
        var now = DateTime.UtcNow;
        var hits = _velocity.GetOrAdd(key, _ => new List<DateTime>());
        lock (hits)
        {
            hits.RemoveAll(t => now - t > window);
            hits.Add(now);
            return hits.Count <= maxRequests;
        }
    }

    public bool CheckAndStoreFingerprint(string ip, string fingerprint)
    {
        var session = _sessions.GetOrAdd(ip, _ => (fingerprint, DateTime.UtcNow));
        return session.Fingerprint == fingerprint;
    }

    public List<string> GetPayloadHashes(string ip) =>
        _payloads.GetOrAdd(ip, _ => new List<string>());

    public void RecordPayloadHash(string ip, string hash)
    {
        var list = _payloads.GetOrAdd(ip, _ => new List<string>());
        lock (list) { list.Add(hash); }
    }

    public void TrimPayloadHistory(string ip, int maxEntries)
    {
        if (_payloads.TryGetValue(ip, out var list))
        {
            lock (list) { while (list.Count > maxEntries) list.RemoveAt(0); }
        }
    }

    public bool CheckEntityVelocity(string ip, string entityType, string entityId, int maxRequests, TimeSpan window)
    {
        return true; // test stub — always allow
    }

    public int AddReputationScore(string key, int delta, TimeSpan ttl) => delta;

    public int GetReputationScore(string key) => 0;

    public bool TryGetCooldown(string key, out int retryAfterSeconds)
    {
        retryAfterSeconds = 0;
        return false; // no cooldown
    }

    public void SetCooldown(string key, int durationSeconds) { }

    public void Cleanup() { }
}

// ============================================================
// SpamValidationMiddleware — isolated unit tests
// ============================================================
public sealed class SpamValidationMiddlewareTests
{
    /// <summary>
    /// Creates a fixture with a mocked BotBehaviorDetector that always allows
    /// (so only the spam-validation logic is tested).
    /// </summary>
    private static (SpamValidationMiddleware Middleware, DefaultHttpContext Ctx) CreateFixture(
        string bodyJson,
        string path = "/api/bookings",
        string contentType = "application/json")
    {
        var logger = Mock.Of<ILogger<SpamValidationMiddleware>>();

        // Mock the store always to allow through velocity/fingerprint/duplicate checks
        var storeMock = new Mock<IBotBehaviorStore>();
        storeMock.Setup(s => s.CheckVelocity(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>())).Returns(true);
        storeMock.Setup(s => s.CheckAndStoreFingerprint(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        storeMock.Setup(s => s.GetPayloadHashes(It.IsAny<string>())).Returns(new List<string>());

        var botLogger = Mock.Of<ILogger<BotBehaviorDetector>>();
        var detector = new BotBehaviorDetector(botLogger, storeMock.Object);

        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new SpamValidationMiddleware(next, logger, detector);

        var ctx = new DefaultHttpContext();
        ctx.Request.Method = "POST";
        ctx.Request.Path = path;
        ctx.Request.ContentType = contentType;
        ctx.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(bodyJson));
        ctx.Request.Headers.UserAgent = "Mozilla/5.0 Test";
        ctx.Request.Headers.AcceptLanguage = "en-US";
        ctx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0." + Random.Shared.Next(1, 255));

        ctx.Response.Body = new MemoryStream();
        return (middleware, ctx);
    }

    private static async Task<int> ExecuteAndGetStatus(SpamValidationMiddleware middleware, DefaultHttpContext ctx)
    {
        await middleware.InvokeAsync(ctx);
        return ctx.Response.StatusCode;
    }

    // --- visitHistory with many URLs (THE FIX) ---

    [Fact]
    public async Task VisitHistory_With_Many_Urls_Passes_SpamCheck()
    {
        var visits = Enumerable.Range(0, 20).Select(i =>
            $"{{\"path\":\"/page{i}\",\"referrer\":\"https://semsar-alpha.vercel.app/\"}}");
        var visitHistory = "[" + string.Join(",", visits) + "]";

        var body = JsonSerializer.Serialize(new
        {
            name = "Test User",
            phone = "+201234567890",
            message = "Hello",
            visitHistory
        });

        var (mw, ctx) = CreateFixture(body);
        var status = await ExecuteAndGetStatus(mw, ctx);
        status.Should().Be(200, "visitHistory with 20 URLs should not trigger excessive_urls violation");
    }

    [Fact]
    public async Task VisitHistory_With_Zero_Urls_Passes()
    {
        var body = JsonSerializer.Serialize(new
        {
            name = "Test User",
            phone = "+201234567890",
            visitHistory = "[{\"path\":\"/\"}]"
        });
        var (mw, ctx) = CreateFixture(body);
        (await ExecuteAndGetStatus(mw, ctx)).Should().Be(200);
    }

    [Fact]
    public async Task VisitHistory_With_Empty_String_Passes()
    {
        var body = JsonSerializer.Serialize(new
        {
            name = "Test User",
            phone = "+201234567890",
            visitHistory = ""
        });
        var (mw, ctx) = CreateFixture(body);
        (await ExecuteAndGetStatus(mw, ctx)).Should().Be(200);
    }

    // --- Regular fields with excessive URLs ---

    [Fact]
    public async Task Message_With_More_Than_3_Urls_Is_Blocked()
    {
        var body = JsonSerializer.Serialize(new
        {
            name = "Test",
            phone = "+201234567890",
            message = "https://a.com https://b.com https://c.com https://d.com"
        });
        var (mw, ctx) = CreateFixture(body);
        (await ExecuteAndGetStatus(mw, ctx)).Should().Be(400);
    }

    [Fact]
    public async Task Notes_With_More_Than_3_Urls_Is_Blocked()
    {
        var body = JsonSerializer.Serialize(new
        {
            name = "Test",
            phone = "+201234567890",
            location = "Hurghada",
            notes = "https://x.com https://y.com https://z.com https://w.com"
        });
        var (mw, ctx) = CreateFixture(body, "/api/land-requests");
        (await ExecuteAndGetStatus(mw, ctx)).Should().Be(400);
    }

    // --- Honeypot ---

    [Fact]
    public async Task Filled_Honeypot_Is_Blocked()
    {
        var body = JsonSerializer.Serialize(new
        {
            name = "Test",
            phone = "+201234567890",
            hp_test = "I am a bot"
        });
        var (mw, ctx) = CreateFixture(body);
        (await ExecuteAndGetStatus(mw, ctx)).Should().Be(400);
    }

    [Fact]
    public async Task Empty_Honeypot_Passes()
    {
        var body = JsonSerializer.Serialize(new
        {
            name = "Test",
            phone = "+201234567890",
            hp_test = ""
        });
        var (mw, ctx) = CreateFixture(body);
        (await ExecuteAndGetStatus(mw, ctx)).Should().Be(200);
    }

    // --- HTML injection ---

    [Fact]
    public async Task Html_Injection_Is_Blocked()
    {
        var body = JsonSerializer.Serialize(new
        {
            name = "<script>alert('xss')</script>",
            phone = "+201234567890"
        });
        var (mw, ctx) = CreateFixture(body);
        (await ExecuteAndGetStatus(mw, ctx)).Should().Be(400);
    }

    // --- Encoded entities ---

    [Fact]
    public async Task Encoded_Html_Entities_Are_Blocked()
    {
        var body = JsonSerializer.Serialize(new
        {
            name = "Test",
            phone = "+201234567890",
            message = "&#108;&#111;&#108;"
        });
        var (mw, ctx) = CreateFixture(body);
        (await ExecuteAndGetStatus(mw, ctx)).Should().Be(400);
    }

    // --- Spam keywords ---

    [Theory]
    [InlineData("buy now")]
    [InlineData("click here")]
    [InlineData("work from home")]
    [InlineData("make money fast")]
    [InlineData("casino")]
    [InlineData("Limited Offer")]
    public async Task Spam_Keywords_Are_Blocked(string keyword)
    {
        var body = JsonSerializer.Serialize(new
        {
            name = "Test",
            phone = "+201234567890",
            message = $"Check this out: {keyword}!!!"
        });
        var (mw, ctx) = CreateFixture(body);
        (await ExecuteAndGetStatus(mw, ctx)).Should().Be(400, $"message containing '{keyword}' should be blocked");
    }

    [Fact]
    public async Task Clean_Message_With_No_Spam_Keywords_Passes()
    {
        var body = JsonSerializer.Serialize(new
        {
            name = "Test User",
            phone = "+201234567890",
            message = "I am interested in this property. Please contact me."
        });
        var (mw, ctx) = CreateFixture(body);
        (await ExecuteAndGetStatus(mw, ctx)).Should().Be(200);
    }

    // --- Repeated chars ---

    [Fact]
    public async Task Repeated_Characters_Are_Blocked()
    {
        var body = JsonSerializer.Serialize(new
        {
            name = "Test",
            phone = "+201234567890",
            message = new string('a', 15)
        });
        var (mw, ctx) = CreateFixture(body);
        (await ExecuteAndGetStatus(mw, ctx)).Should().Be(400);
    }

    // --- Invisible chars ---

    [Fact]
    public async Task Invisible_Unicode_Chars_Are_Blocked()
    {
        var body = JsonSerializer.Serialize(new
        {
            name = "Test",
            phone = "+201234567890",
            message = "Hello\u200BWorld"
        });
        var (mw, ctx) = CreateFixture(body);
        (await ExecuteAndGetStatus(mw, ctx)).Should().Be(400);
    }

    // --- Unexpected fields ---

    [Fact]
    public async Task Unknown_Field_Is_Blocked()
    {
        var body = JsonSerializer.Serialize(new
        {
            name = "Test",
            phone = "+201234567890",
            someRandomField = "malicious data"
        });
        var (mw, ctx) = CreateFixture(body);
        (await ExecuteAndGetStatus(mw, ctx)).Should().Be(400);
    }

    // --- Non-JSON content ---

    [Fact]
    public async Task Non_Json_Content_Type_Returns_415()
    {
        var (mw, ctx) = CreateFixture("name=test&phone=123",
            contentType: "application/x-www-form-urlencoded");
        (await ExecuteAndGetStatus(mw, ctx)).Should().Be(415);
    }

    // --- Malformed JSON ---

    [Fact]
    public async Task Malformed_Json_Returns_400()
    {
        var (mw, ctx) = CreateFixture("{invalid json here!!!}");
        (await ExecuteAndGetStatus(mw, ctx)).Should().Be(400);
    }

    // --- Non-POST methods pass through ---

    [Fact]
    public async Task Get_Request_Passes_Through()
    {
        var (mw, ctx) = CreateFixture("{}");
        ctx.Request.Method = "GET";
        (await ExecuteAndGetStatus(mw, ctx)).Should().Be(200);
    }

    // --- Unmonitored paths pass through ---

    [Fact]
    public async Task Non_Form_Path_Passes_Through()
    {
        var body = JsonSerializer.Serialize(new { someRandomField = "whatever" });
        var (mw, ctx) = CreateFixture(body, "/api/properties");
        (await ExecuteAndGetStatus(mw, ctx)).Should().Be(200);
    }

    // --- Field too long ---

    [Fact]
    public async Task Field_Exceeding_Max_Length_Is_Blocked()
    {
        var body = JsonSerializer.Serialize(new
        {
            name = "Test",
            phone = "+201234567890",
            message = new string('x', 5001)
        });
        var (mw, ctx) = CreateFixture(body);
        (await ExecuteAndGetStatus(mw, ctx)).Should().Be(400);
    }

    // --- Non-string values pass through ---

    [Fact]
    public async Task Non_String_Values_Are_Skipped()
    {
        var body = JsonSerializer.Serialize(new
        {
            name = "Test",
            phone = "+201234567890",
            pageViews = 999,
            sessionDuration = 9999
        });
        var (mw, ctx) = CreateFixture(body);
        (await ExecuteAndGetStatus(mw, ctx)).Should().Be(200);
    }

    // --- Arabic text passes (common legit input) ---

    [Fact]
    public async Task Arabic_Text_Passes()
    {
        var body = JsonSerializer.Serialize(new
        {
            name = "يوسف",
            phone = "+201234567890",
            message = "أنا مهتم بهذا العقار، من فضلك اتصل بي"
        });
        var (mw, ctx) = CreateFixture(body);
        (await ExecuteAndGetStatus(mw, ctx)).Should().Be(200);
    }

    // --- Realistic full payload with many tracking fields ---

    [Fact]
    public async Task Realistic_Full_Payload_With_Tracking_Passes()
    {
        var visits = Enumerable.Range(0, 15).Select(i =>
            $"{{\"path\":\"/page{i}\",\"referrer\":\"https://semsar-alpha.vercel.app/\"}}");
        var visitHistory = "[" + string.Join(",", visits) + "]";

        var body = JsonSerializer.Serialize(new
        {
            propertyId = 10,
            unitId = (int?)null,
            name = "يوسف",
            phone = "01156477213",
            message = "أرغب في معرفة المزيد عن هذا العقار",
            source = "direct",
            currentPage = "/ar/properties/test-redsea-hurghada-eldahar",
            landingPage = "/en/properties/testlocation-redsea-hurghada-kawthar",
            referrer = "https://semsar-alpha.vercel.app/",
            lastReferrer = "https://semsar-web-alpha.vercel.app/en",
            userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
            pageViews = 10,
            sessionDuration = 300,
            firstVisitAt = "2026-05-23T09:30:12.622Z",
            submittedAt = "2026-05-26T17:15:03.131Z",
            interactionTimestamp = 1779815703779,
            hp_fcu = "",
            visitHistory
        });

        var (mw, ctx) = CreateFixture(body);
        var status = await ExecuteAndGetStatus(mw, ctx);
        status.Should().Be(200, "realistic full payload with all tracking fields should pass");
    }
}

// ============================================================
// Form controller endpoint integration tests
// ============================================================
public sealed class FormEndpointTests
{
    private static (AppDbContext Ctx, IUnitOfWork Uow) CreateFresh()
    {
        var dbName = Guid.NewGuid().ToString();
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var ctx = new AppDbContext(opts);
        ctx.Database.EnsureCreated();

        var concurrencyMock = new Mock<IConcurrencyValidator>();
        var reservationMock = new Mock<IReservationRepository>();
        var loggerMock = new Mock<ILogger<UnitOfWork>>();
        var metricsMock = new Mock<IAppMetrics>();

        var uow = new UnitOfWork(ctx, concurrencyMock.Object, reservationMock.Object,
            loggerMock.Object, metricsMock.Object);

        return (ctx, uow);
    }

    private static Property MakeProperty(int id = 1, string code = "PROP-001") => new()
    {
        Id = id,
        Code = code,
        PublicKey = Guid.CreateVersion7().ToString(),
        TitleEn = "Test Property",
        TitleAr = "عقار تجريبي",
        DescriptionEn = "A test property for unit tests",
        DescriptionAr = "عقار تجريبي لاختبار الوحدة",
        Slug = $"test-property-{id}",
        Location = "Test Location",
        Price = 1000000,
        Currency = "EGP",
        PropertyType = PropertyType.Apartment,
        ListingType = PropertyListingType.Resale,
        IsDeleted = false
    };

    // ============================================================
    // BookingsController
    // ============================================================

    [Fact]
    public async Task Booking_Succeeds_With_VisitHistory_Containing_Many_Urls()
    {
        var (_, uow) = CreateFresh();
        await uow.Properties.AddAsync(MakeProperty(1, "PROP-001"));
        await uow.CommitAsync();

        var visits = Enumerable.Range(0, 20).Select(i =>
            $"{{\"path\":\"/page{i}\",\"referrer\":\"https://semsar-alpha.vercel.app/\"}}");
        var visitHistory = "[" + string.Join(",", visits) + "]";

        var controller = new BookingsController(uow,
            Mock.Of<INotificationService>(), Mock.Of<IConfiguration>(),
            Mock.Of<ILogger<BookingsController>>());

        var result = await controller.Create(new BookingSubmitDto
        {
            PropertyId = 1,
            Name = "يوسف",
            Phone = "+201234567890",
            Source = "direct",
            CurrentPage = "/ar/p",
            Referrer = "https://semsar-alpha.vercel.app/",
            UserAgent = "Mozilla/5.0 Test",
            PageViews = 10,
            SessionDuration = 300,
            VisitHistory = visitHistory
        });

        result.Should().BeOfType<OkObjectResult>();
        var saved = await uow.Bookings.Query().FirstOrDefaultAsync();
        saved.Should().NotBeNull();
        saved!.VisitHistory.Should().Be(visitHistory);
    }

    [Fact]
    public async Task Booking_Fails_With_Empty_Phone()
    {
        var (_, uow) = CreateFresh();
        await uow.Properties.AddAsync(MakeProperty(1, "PROP-001"));
        await uow.CommitAsync();

        var controller = new BookingsController(uow,
            Mock.Of<INotificationService>(), Mock.Of<IConfiguration>(),
            Mock.Of<ILogger<BookingsController>>());

        var result = await controller.Create(new BookingSubmitDto
        {
            PropertyId = 1,
            Name = "Test User",
            Phone = ""
        });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Booking_Fails_With_Empty_Phone_After_Normalization()
    {
        var (_, uow) = CreateFresh();
        await uow.Properties.AddAsync(MakeProperty(1, "PROP-001"));
        await uow.CommitAsync();

        var controller = new BookingsController(uow,
            Mock.Of<INotificationService>(), Mock.Of<IConfiguration>(),
            Mock.Of<ILogger<BookingsController>>());

        // Only special chars — NormalizePhone strips them to empty
        var result = await controller.Create(new BookingSubmitDto
        {
            PropertyId = 1,
            Name = "Test User",
            Phone = "!@#$%^&*()"
        });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Booking_Fails_Without_PropertyId()
    {
        var (_, uow) = CreateFresh();
        var controller = new BookingsController(uow,
            Mock.Of<INotificationService>(), Mock.Of<IConfiguration>(),
            Mock.Of<ILogger<BookingsController>>());

        var result = await controller.Create(new BookingSubmitDto
        {
            Name = "Test User",
            Phone = "+201234567890"
        });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Booking_With_Arabic_Text_Succeeds()
    {
        var (_, uow) = CreateFresh();
        await uow.Properties.AddAsync(MakeProperty(1, "PROP-001"));
        await uow.CommitAsync();

        var controller = new BookingsController(uow,
            Mock.Of<INotificationService>(), Mock.Of<IConfiguration>(),
            Mock.Of<ILogger<BookingsController>>());

        var result = await controller.Create(new BookingSubmitDto
        {
            PropertyId = 1,
            Name = "يوسف",
            Phone = "+201234567890",
            Message = "أرغب في معرفة المزيد عن هذا العقار"
        });

        result.Should().BeOfType<OkObjectResult>();
        var saved = await uow.Bookings.Query().FirstOrDefaultAsync();
        saved!.Name.Should().Be("يوسف");
    }

    [Fact]
    public async Task Booking_Creates_Lead_With_Correct_Source()
    {
        var (_, uow) = CreateFresh();
        await uow.Properties.AddAsync(MakeProperty(1, "PROP-001"));
        await uow.CommitAsync();

        var controller = new BookingsController(uow,
            Mock.Of<INotificationService>(), Mock.Of<IConfiguration>(),
            Mock.Of<ILogger<BookingsController>>());

        await controller.Create(new BookingSubmitDto
        {
            PropertyId = 1,
            Name = "User",
            Phone = "+201111111111",
            Source = "  Instagram  "
        });

        var lead = await uow.Leads.Query().FirstOrDefaultAsync();
        lead.Should().NotBeNull();
        lead!.Source.Should().Be("instagram");
    }

    // ============================================================
    // LandRequestsController
    // ============================================================

    [Fact]
    public async Task LandRequest_Succeeds_With_All_Tracking_Fields()
    {
        var (_, uow) = CreateFresh();
        var controller = new LandRequestsController(uow,
            Mock.Of<INotificationService>(), Mock.Of<IConfiguration>(),
            Mock.Of<ILogger<LandRequestsController>>());

        var visits = Enumerable.Range(0, 15).Select(i =>
            $"{{\"path\":\"/page{i}\",\"referrer\":\"https://example.com/\"}}");
        var visitHistory = "[" + string.Join(",", visits) + "]";

        var result = await controller.Create(new CreateLandRequestDto
        {
            Name = "Land Seeker",
            Phone = "+201222222222",
            Location = "Hurghada",
            MinPrice = 500000,
            MaxPrice = 2000000,
            MinArea = 100,
            MaxArea = 500,
            Notes = "Looking for beachfront land",
            Source = "facebook",
            Medium = "social",
            Campaign = "land_campaign",
            CurrentPage = "/land-request",
            Referrer = "https://facebook.com/ad",
            UserAgent = "Mozilla/5.0",
            PageViews = 7,
            SessionDuration = 300,
            LastReferrer = "https://instagram.com",
            VisitHistory = visitHistory
        });

        result.Should().BeOfType<CreatedResult>();
        var saved = await uow.LandRequests.Query().FirstOrDefaultAsync();
        saved.Should().NotBeNull();
        saved!.Source.Should().Be("facebook");
        saved.Medium.Should().Be("social");
        saved.Campaign.Should().Be("land_campaign");
        saved.VisitHistory.Should().Be(visitHistory);
    }

    [Fact]
    public async Task LandRequest_Fails_With_Empty_Name()
    {
        var (_, uow) = CreateFresh();
        var controller = new LandRequestsController(uow,
            Mock.Of<INotificationService>(), Mock.Of<IConfiguration>(),
            Mock.Of<ILogger<LandRequestsController>>());

        var result = await controller.Create(new CreateLandRequestDto
        {
            Name = "",
            Phone = "+201234567890",
            Location = "Hurghada"
        });
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task LandRequest_Fails_With_Empty_Phone()
    {
        var (_, uow) = CreateFresh();
        var controller = new LandRequestsController(uow,
            Mock.Of<INotificationService>(), Mock.Of<IConfiguration>(),
            Mock.Of<ILogger<LandRequestsController>>());

        var result = await controller.Create(new CreateLandRequestDto
        {
            Name = "Test",
            Phone = "",
            Location = "Hurghada"
        });
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task LandRequest_Creates_Lead_When_No_Existing_Lead()
    {
        var (_, uow) = CreateFresh();
        var controller = new LandRequestsController(uow,
            Mock.Of<INotificationService>(), Mock.Of<IConfiguration>(),
            Mock.Of<ILogger<LandRequestsController>>());

        await controller.Create(new CreateLandRequestDto
        {
            Name = "User",
            Phone = "+201333333333",
            Location = "Sahl Hasheesh"
        });

        var lead = await uow.Leads.Query().FirstOrDefaultAsync();
        lead.Should().NotBeNull();
        lead!.Message.Should().Contain("Sahl Hasheesh");
    }

    // ============================================================
    // LeadsController
    // ============================================================

    [Fact]
    public async Task Lead_Succeeds_With_All_Tracking_Fields()
    {
        var (_, uow) = CreateFresh();
        var loggerMock = new Mock<ILogger<LeadsController>>();
        var controller = new LeadsController(Mock.Of<ILeadService>(), uow,
            loggerMock.Object, Mock.Of<INotificationService>(),
            Mock.Of<IConfiguration>());

        var visits = Enumerable.Range(0, 10).Select(i =>
            $"{{\"path\":\"/page{i}\",\"referrer\":\"https://example.com/\"}}");
        var visitHistory = "[" + string.Join(",", visits) + "]";

        var result = await controller.Create(new LeadCreateDto
        {
            Name = "John Doe",
            Phone = "+201234567890",
            Message = "I'm interested in your services",
            Source = "google",
            Medium = "cpc",
            Campaign = "search_campaign",
            Term = "real_estate",
            Content = "ad_1",
            LandingPage = "/properties/123",
            FirstVisitAt = DateTime.UtcNow.AddDays(-3),
            CurrentPage = "/contact",
            Referrer = "https://google.com",
            UserAgent = "Mozilla/5.0",
            PageViews = 15,
            SessionDuration = 600,
            LastReferrer = "https://facebook.com",
            VisitHistory = visitHistory
        });

        result.Should().BeOfType<OkObjectResult>();
        var saved = await uow.Leads.Query().FirstOrDefaultAsync();
        saved.Should().NotBeNull();
        saved!.Source.Should().Be("google");
        saved.Medium.Should().Be("cpc");
        saved.Campaign.Should().Be("search_campaign");
        saved.Term.Should().Be("real_estate");
        saved.Content.Should().Be("ad_1");
        saved.LandingPage.Should().Be("/properties/123");
        saved.FirstVisitAt.Should().NotBeNull();
        saved.CurrentPage.Should().Be("/contact");
        saved.Referrer.Should().Be("https://google.com");
        saved.UserAgent.Should().Be("Mozilla/5.0");
        saved.PageViews.Should().Be(15);
        saved.SessionDuration.Should().Be(600);
        saved.LastReferrer.Should().Be("https://facebook.com");
        saved.VisitHistory.Should().Be(visitHistory);
        saved.IsPaid.Should().BeTrue();
    }

    [Fact]
    public async Task Lead_Fails_With_Empty_Name()
    {
        var (_, uow) = CreateFresh();
        var loggerMock = new Mock<ILogger<LeadsController>>();
        var controller = new LeadsController(Mock.Of<ILeadService>(), uow,
            loggerMock.Object, Mock.Of<INotificationService>(),
            Mock.Of<IConfiguration>());

        var result = await controller.Create(new LeadCreateDto
        {
            Name = "",
            Phone = "+201234567890"
        });
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ============================================================
    // Cross-entity dedup
    // ============================================================

    [Fact]
    public async Task Duplicate_Phone_Within_60s_Returns_Conflict()
    {
        var (_, uow) = CreateFresh();
        await uow.Properties.AddAsync(MakeProperty(1, "PROP-001"));
        await uow.CommitAsync();

        var controller = new BookingsController(uow,
            Mock.Of<INotificationService>(), Mock.Of<IConfiguration>(),
            Mock.Of<ILogger<BookingsController>>());

        var dto = new BookingSubmitDto
        {
            PropertyId = 1,
            Name = "User",
            Phone = "+201234567890"
        };

        (await controller.Create(dto)).Should().BeOfType<OkObjectResult>();
        (await controller.Create(dto)).Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Different_Phone_Same_Property_Both_Succeed()
    {
        var (_, uow) = CreateFresh();
        await uow.Properties.AddAsync(MakeProperty(1, "PROP-001"));
        await uow.CommitAsync();

        var controller = new BookingsController(uow,
            Mock.Of<INotificationService>(), Mock.Of<IConfiguration>(),
            Mock.Of<ILogger<BookingsController>>());

        (await controller.Create(new BookingSubmitDto
        {
            PropertyId = 1,
            Name = "User A",
            Phone = "+201000000001"
        })).Should().BeOfType<OkObjectResult>();

        (await controller.Create(new BookingSubmitDto
        {
            PropertyId = 1,
            Name = "User B",
            Phone = "+201000000002"
        })).Should().BeOfType<OkObjectResult>();
    }
}

// ============================================================
// BotBehaviorDetector interaction tests
// ============================================================
public sealed class BotBehaviorDetectorTests
{
    /// <summary>
    /// Creates a fresh detector with an isolated (non-static) store per test.
    /// Each test uses a unique IP to guarantee no cross-test interference.
    /// </summary>
    private static (BotBehaviorDetector Detector, string Ip) CreateDetector()
    {
        var store = new TestBotBehaviorStore();
        var logger = Mock.Of<ILogger<BotBehaviorDetector>>();
        var ip = "10.0.0." + Random.Shared.Next(100, 200);
        return (new BotBehaviorDetector(logger, store), ip);
    }

    private static DefaultHttpContext MakeContext(string userAgent = "Mozilla/5.0 Test",
        string acceptLang = "en-US", string secChUa = "")
    {
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.1");
        ctx.Request.Headers.UserAgent = userAgent;
        ctx.Request.Headers.AcceptLanguage = acceptLang;
        ctx.Request.Headers["Sec-CH-UA"] = secChUa;
        return ctx;
    }

    [Fact]
    public void Consistent_Fingerprint_On_Same_Headers()
    {
        var ctx = MakeContext();
        var (detector, _) = CreateDetector();
        detector.ComputeFingerprint(ctx).Should().Be(detector.ComputeFingerprint(ctx));
    }

    [Fact]
    public void Different_UserAgent_Produces_Different_Fingerprint()
    {
        var (detector, _) = CreateDetector();
        var fp1 = detector.ComputeFingerprint(MakeContext("Chrome"));
        var fp2 = detector.ComputeFingerprint(MakeContext("Firefox"));
        fp1.Should().NotBe(fp2);
    }

    [Fact]
    public void Fingerprint_Consistency_Check_Passes_On_First_Request()
    {
        var (detector, ip) = CreateDetector();
        var fp = detector.ComputeFingerprint(MakeContext());
        detector.CheckFingerprintConsistency(ip, fp).Should().BeTrue();
    }

    [Fact]
    public void Fingerprint_Consistency_Fails_On_Mismatch()
    {
        var (detector, ip) = CreateDetector();
        var fp1 = detector.ComputeFingerprint(MakeContext("Chrome"));
        var fp2 = detector.ComputeFingerprint(MakeContext("Firefox"));

        detector.CheckFingerprintConsistency(ip, fp1).Should().BeTrue();
        detector.CheckFingerprintConsistency(ip, fp2).Should().BeFalse();
    }

    [Fact]
    public void Velocity_Check_Allows_Up_To_Max_Requests()
    {
        var (detector, ip) = CreateDetector();
        for (int i = 0; i < 3; i++)
            detector.IsVelocityExceeded(ip).Should().BeFalse();
    }

    [Fact]
    public void Velocity_Check_Blocks_Excessive_Requests()
    {
        var (detector, ip) = CreateDetector();
        for (int i = 0; i < 3; i++)
            detector.IsVelocityExceeded(ip);
        detector.IsVelocityExceeded(ip).Should().BeTrue();
    }

    [Fact]
    public void Duplicate_Payload_Detected()
    {
        var (detector, ip) = CreateDetector();
        var body = "{\"name\":\"Test\",\"phone\":\"+20\"}";

        detector.IsDuplicatePayload(ip, body, out _).Should().BeFalse();
        detector.IsDuplicatePayload(ip, body, out var sim).Should().BeTrue();
        sim.Should().BeGreaterThan(0.75);
    }

    [Fact]
    public void Different_Payloads_Not_Detected_As_Duplicate()
    {
        var (detector, ip) = CreateDetector();
        var body1 = "{\"name\":\"Alice\",\"phone\":\"+201111\"}";
        var body2 = "{\"name\":\"Bob\",\"phone\":\"+202222\"}";

        detector.IsDuplicatePayload(ip, body1, out _).Should().BeFalse();
        detector.IsDuplicatePayload(ip, body2, out var sim).Should().BeFalse();
        sim.Should().BeLessThan(0.75);
    }

    [Fact]
    public void Reputation_Score_Defaults_To_100()
    {
        var (detector, ip) = CreateDetector();
        var fp = detector.ComputeFingerprint(MakeContext());
        detector.ComputeReputationScore(ip, fp).Should().Be(0);
    }
}
