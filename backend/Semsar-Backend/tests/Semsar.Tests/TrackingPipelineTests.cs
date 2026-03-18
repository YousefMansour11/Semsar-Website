using Application.DTOs;
using Application.Interfaces;
using API.Controllers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Semsar.Tests;

/// <summary>
/// Creates isolated InMemory-backed AppDbContext + UnitOfWork per test.
/// </summary>
public class TrackingPipelineTests
{
    protected static (AppDbContext Ctx, IUnitOfWork Uow) CreateFresh()
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

    protected static Domain.Entities.Property MakeProperty(int id = 1, string code = "PROP-001")
    {
        return new Domain.Entities.Property
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
    }

    // ============================================================
    // LeadsController — Tracking Field Mapping
    // ============================================================
    public sealed class LeadsMapping : TrackingPipelineTests
    {
        [Fact]
        public async Task Maps_All_Tracking_Fields_From_DTO_To_Entity()
        {
            var (_, uow) = CreateFresh();
            var loggerMock = new Mock<ILogger<LeadsController>>();
            var controller = new LeadsController(Mock.Of<ILeadService>(), uow, loggerMock.Object, Mock.Of<INotificationService>(), Mock.Of<IConfiguration>());

            var dto = new LeadCreateDto
            {
                Name = "John Doe",
                Phone = "+201234567890",
                Message = "Interested",
                Source = "facebook",
                Medium = "social",
                Campaign = "summer_sale",
                Term = "buy_land",
                Content = "banner1",
                LandingPage = "/properties/123?utm_source=facebook",
                FirstVisitAt = DateTime.Parse("2025-06-15T12:00:00Z"),
                CurrentPage = "/contact",
                Referrer = "https://facebook.com/ad",
                UserAgent = "Mozilla/5.0 TestAgent",
                PageViews = 5,
                SessionDuration = 120,
                LastReferrer = "https://instagram.com",
                VisitHistory = "[{\"path\":\"/\"}]"
            };

            var result = await controller.Create(dto);

            result.Should().BeOfType<OkObjectResult>();

            var saved = await uow.Leads.Query().FirstOrDefaultAsync();
            saved.Should().NotBeNull();
            saved!.Source.Should().Be("facebook");
            saved.Medium.Should().Be("social");
            saved.Campaign.Should().Be("summer_sale");
            saved.Term.Should().Be("buy_land");
            saved.Content.Should().Be("banner1");
            saved.LandingPage.Should().Be("/properties/123?utm_source=facebook");
            saved.FirstVisitAt.Should().Be(DateTime.Parse("2025-06-15T12:00:00Z"));
            saved.CurrentPage.Should().Be("/contact");
            saved.Referrer.Should().Be("https://facebook.com/ad");
            saved.UserAgent.Should().Be("Mozilla/5.0 TestAgent");
            saved.PageViews.Should().Be(5);
            saved.SessionDuration.Should().Be(120);
            saved.LastReferrer.Should().Be("https://instagram.com");
            saved.VisitHistory.Should().Be("[{\"path\":\"/\"}]");
        }

        [Fact]
        public async Task Falls_Back_To_Direct_When_Source_Is_Null()
        {
            var (_, uow) = CreateFresh();
            var loggerMock = new Mock<ILogger<LeadsController>>();
            var controller = new LeadsController(Mock.Of<ILeadService>(), uow, loggerMock.Object, Mock.Of<INotificationService>(), Mock.Of<IConfiguration>());

            await controller.Create(new LeadCreateDto
            {
                Name = "John Doe",
                Phone = "+201234567890",
                Source = null
            });

            var saved = await uow.Leads.Query().FirstOrDefaultAsync();
            saved!.Source.Should().Be("direct");
        }

        [Fact]
        public async Task Falls_Back_To_Direct_When_Source_Is_Whitespace()
        {
            var (_, uow) = CreateFresh();
            var loggerMock = new Mock<ILogger<LeadsController>>();
            var controller = new LeadsController(Mock.Of<ILeadService>(), uow, loggerMock.Object, Mock.Of<INotificationService>(), Mock.Of<IConfiguration>());

            await controller.Create(new LeadCreateDto
            {
                Name = "John Doe",
                Phone = "+201234567890",
                Source = "   "
            });

            var saved = await uow.Leads.Query().FirstOrDefaultAsync();
            saved!.Source.Should().Be("direct");
        }

        [Fact]
        public async Task Normalizes_Tracking_Strings_Lowercase_Trim()
        {
            var (_, uow) = CreateFresh();
            var loggerMock = new Mock<ILogger<LeadsController>>();
            var controller = new LeadsController(Mock.Of<ILeadService>(), uow, loggerMock.Object, Mock.Of<INotificationService>(), Mock.Of<IConfiguration>());

            await controller.Create(new LeadCreateDto
            {
                Name = "John Doe",
                Phone = "+201234567890",
                Source = "  FACEBOOK  ",
                Medium = "  CPC  ",
                Campaign = "  Summer_Sale  ",
                Term = "  Buy_Land  ",
                Content = "  Banner1  "
            });

            var saved = await uow.Leads.Query().FirstOrDefaultAsync();
            saved!.Source.Should().Be("facebook");
            saved.Medium.Should().Be("cpc");
            saved.Campaign.Should().Be("summer_sale");
            saved.Term.Should().Be("buy_land");
            saved.Content.Should().Be("banner1");
        }

        [Fact]
        public async Task Sets_IsPaid_True_When_Medium_Is_CPC()
        {
            var (_, uow) = CreateFresh();
            var loggerMock = new Mock<ILogger<LeadsController>>();
            var controller = new LeadsController(Mock.Of<ILeadService>(), uow, loggerMock.Object, Mock.Of<INotificationService>(), Mock.Of<IConfiguration>());

            await controller.Create(new LeadCreateDto
            {
                Name = "JD",
                Phone = "+20",
                Source = "google",
                Medium = "cpc"
            });

            var saved = await uow.Leads.Query().FirstOrDefaultAsync();
            saved!.IsPaid.Should().BeTrue();
        }

        [Fact]
        public async Task Sets_IsPaid_False_When_Medium_Is_Not_CPC()
        {
            var (_, uow) = CreateFresh();
            var loggerMock = new Mock<ILogger<LeadsController>>();
            var controller = new LeadsController(Mock.Of<ILeadService>(), uow, loggerMock.Object, Mock.Of<INotificationService>(), Mock.Of<IConfiguration>());

            await controller.Create(new LeadCreateDto
            {
                Name = "JD",
                Phone = "+20",
                Source = "facebook",
                Medium = "social"
            });

            var saved = await uow.Leads.Query().FirstOrDefaultAsync();
            saved!.IsPaid.Should().BeFalse();
        }

        [Fact]
        public async Task Sets_Null_Tracking_Strings_To_Null()
        {
            var (_, uow) = CreateFresh();
            var loggerMock = new Mock<ILogger<LeadsController>>();
            var controller = new LeadsController(Mock.Of<ILeadService>(), uow, loggerMock.Object, Mock.Of<INotificationService>(), Mock.Of<IConfiguration>());

            await controller.Create(new LeadCreateDto
            {
                Name = "JD",
                Phone = "+20",
                Source = "google",
                Medium = null,
                Campaign = null,
                Term = null,
                Content = null
            });

            var saved = await uow.Leads.Query().FirstOrDefaultAsync();
            saved!.Source.Should().Be("google");
            saved.Medium.Should().BeNull();
            saved.Campaign.Should().BeNull();
            saved.Term.Should().BeNull();
            saved.Content.Should().BeNull();
        }
    }

    // ============================================================
    // BookingsController — Tracking Field Mapping
    // ============================================================
    public sealed class BookingsMapping : TrackingPipelineTests
    {
        [Fact]
        public async Task Maps_All_Tracking_Fields_To_BookingRequest()
        {
            var (_, uow) = CreateFresh();
            await uow.Properties.AddAsync(MakeProperty(1, "PROP-001"));
            await uow.CommitAsync();

            var controller = new BookingsController(uow, Mock.Of<INotificationService>(), Mock.Of<IConfiguration>(), Mock.Of<ILogger<BookingsController>>());

            var dto = new BookingSubmitDto
            {
                PropertyId = 1,
                Name = "Jane Doe",
                Phone = "+201111111111",
                Source = "instagram",
                Medium = "social",
                Campaign = "story_ad",
                Term = "villa",
                Content = "story_1",
                LandingPage = "/properties/villa-x",
                FirstVisitAt = DateTime.Parse("2025-06-15T12:00:00Z"),
                CurrentPage = "/booking",
                Referrer = "https://instagram.com",
                UserAgent = "Mozilla/5.0",
                PageViews = 3,
                SessionDuration = 90,
                LastReferrer = "https://facebook.com",
                VisitHistory = "[{\"path\":\"/\"}]"
            };

            var result = await controller.Create(dto);

            result.Should().BeOfType<OkObjectResult>();

            var saved = await uow.Bookings.Query().FirstOrDefaultAsync();
            saved.Should().NotBeNull();
            saved!.Source.Should().Be("instagram");
            saved.Medium.Should().Be("social");
            saved.Campaign.Should().Be("story_ad");
            saved.Term.Should().Be("villa");
            saved.Content.Should().Be("story_1");
            saved.LandingPage.Should().Be("/properties/villa-x");
            saved.FirstVisitAt.Should().Be(DateTime.Parse("2025-06-15T12:00:00Z"));
            saved.CurrentPage.Should().Be("/booking");
            saved.Referrer.Should().Be("https://instagram.com");
            saved.UserAgent.Should().Be("Mozilla/5.0");
            saved.PageViews.Should().Be(3);
            saved.SessionDuration.Should().Be(90);
            saved.LastReferrer.Should().Be("https://facebook.com");
            saved.VisitHistory.Should().Be("[{\"path\":\"/\"}]");
        }

        [Fact]
        public async Task Booking_Fallback_To_Direct()
        {
            var (_, uow) = CreateFresh();
            await uow.Properties.AddAsync(MakeProperty(1, "PROP-001"));
            await uow.CommitAsync();

            var controller = new BookingsController(uow, Mock.Of<INotificationService>(), Mock.Of<IConfiguration>(), Mock.Of<ILogger<BookingsController>>());
            await controller.Create(new BookingSubmitDto
            {
                PropertyId = 1,
                Name = "User",
                Phone = "+20",
                Source = null
            });

            var saved = await uow.Bookings.Query().FirstOrDefaultAsync();
            saved!.Source.Should().Be("direct");
        }

        [Fact]
        public async Task Booking_Normalizes_Strings()
        {
            var (_, uow) = CreateFresh();
            await uow.Properties.AddAsync(MakeProperty(1, "PROP-001"));
            await uow.CommitAsync();

            var controller = new BookingsController(uow, Mock.Of<INotificationService>(), Mock.Of<IConfiguration>(), Mock.Of<ILogger<BookingsController>>());
            await controller.Create(new BookingSubmitDto
            {
                PropertyId = 1,
                Name = "User",
                Phone = "+20",
                Source = "  TWITTER  ",
                Medium = "  SOCIAL  "
            });

            var saved = await uow.Bookings.Query().FirstOrDefaultAsync();
            saved!.Source.Should().Be("twitter");
            saved.Medium.Should().Be("social");
        }

        [Fact]
        public async Task Booking_Creates_Lead_With_Same_Normalized_Source()
        {
            var (_, uow) = CreateFresh();
            await uow.Properties.AddAsync(MakeProperty(1, "PROP-001"));
            await uow.CommitAsync();

            var controller = new BookingsController(uow, Mock.Of<INotificationService>(), Mock.Of<IConfiguration>(), Mock.Of<ILogger<BookingsController>>());
            await controller.Create(new BookingSubmitDto
            {
                PropertyId = 1,
                Name = "User",
                Phone = "+201234567890",
                Source = "  TikTok  "
            });

            var lead = await uow.Leads.Query().FirstOrDefaultAsync();
            lead.Should().NotBeNull();
            lead!.Source.Should().Be("tiktok");
        }
    }

    // ============================================================
    // LandRequestsController — Tracking Field Mapping
    // ============================================================
    public sealed class LandRequestsMapping : TrackingPipelineTests
    {
        [Fact]
        public async Task Maps_All_Tracking_Fields_To_LandRequest()
        {
            var (_, uow) = CreateFresh();
            var controller = new LandRequestsController(uow, Mock.Of<INotificationService>(), Mock.Of<IConfiguration>(), Mock.Of<ILogger<LandRequestsController>>());

            var dto = new CreateLandRequestDto
            {
                Name = "Land Seeker",
                Phone = "+201222222222",
                Location = "Hurghada",
                Source = "email",
                Medium = "email",
                Campaign = "land_campaign",
                Term = "beachfront",
                Content = "newsletter_1",
                LandingPage = "/land-request",
                FirstVisitAt = DateTime.Parse("2025-06-15T12:00:00Z"),
                CurrentPage = "/land-form",
                Referrer = "https://mail.google.com",
                UserAgent = "Mozilla/5.0",
                PageViews = 7,
                SessionDuration = 300,
                LastReferrer = "https://facebook.com",
                VisitHistory = "[{\"path\":\"/landing\"}]"
            };

            var result = await controller.Create(dto);

            result.Should().BeOfType<CreatedResult>();

            var saved = await uow.LandRequests.Query().FirstOrDefaultAsync();
            saved.Should().NotBeNull();
            saved!.Source.Should().Be("email");
            saved.Medium.Should().Be("email");
            saved.Campaign.Should().Be("land_campaign");
            saved.Term.Should().Be("beachfront");
            saved.Content.Should().Be("newsletter_1");
            saved.LandingPage.Should().Be("/land-request");
            saved.FirstVisitAt.Should().Be(DateTime.Parse("2025-06-15T12:00:00Z"));
            saved.CurrentPage.Should().Be("/land-form");
            saved.Referrer.Should().Be("https://mail.google.com");
            saved.UserAgent.Should().Be("Mozilla/5.0");
            saved.PageViews.Should().Be(7);
            saved.SessionDuration.Should().Be(300);
            saved.LastReferrer.Should().Be("https://facebook.com");
            saved.VisitHistory.Should().Be("[{\"path\":\"/landing\"}]");
        }

        [Fact]
        public async Task LandRequest_Fallback_To_Direct()
        {
            var (_, uow) = CreateFresh();
            var controller = new LandRequestsController(uow, Mock.Of<INotificationService>(), Mock.Of<IConfiguration>(), Mock.Of<ILogger<LandRequestsController>>());
            await controller.Create(new CreateLandRequestDto
            {
                Name = "User",
                Phone = "+20",
                Location = "Hurghada",
                Source = null
            });

            var saved = await uow.LandRequests.Query().FirstOrDefaultAsync();
            saved!.Source.Should().Be("direct");
        }

        [Fact]
        public async Task LandRequest_Normalizes_Strings()
        {
            var (_, uow) = CreateFresh();
            var controller = new LandRequestsController(uow, Mock.Of<INotificationService>(), Mock.Of<IConfiguration>(), Mock.Of<ILogger<LandRequestsController>>());
            await controller.Create(new CreateLandRequestDto
            {
                Name = "User",
                Phone = "+20",
                Location = "Hurghada",
                Source = "  TIKTOK  ",
                Medium = "  CPC  "
            });

            var saved = await uow.LandRequests.Query().FirstOrDefaultAsync();
            saved!.Source.Should().Be("tiktok");
            saved.Medium.Should().Be("cpc");
        }

        [Fact]
        public async Task LandRequest_Creates_Lead_With_Same_Normalized_Source()
        {
            var (_, uow) = CreateFresh();
            var controller = new LandRequestsController(uow, Mock.Of<INotificationService>(), Mock.Of<IConfiguration>(), Mock.Of<ILogger<LandRequestsController>>());
            await controller.Create(new CreateLandRequestDto
            {
                Name = "User",
                Phone = "+201234567890",
                Location = "Hurghada",
                Source = "  REFERRAL  "
            });

            var lead = await uow.Leads.Query().FirstOrDefaultAsync();
            lead.Should().NotBeNull();
            lead!.Source.Should().Be("referral");
        }
    }

    // ============================================================
    // Data Integrity — Cross-Controller Consistency
    // ============================================================
    public sealed class DataIntegrity : TrackingPipelineTests
    {
        [Fact]
        public async Task Lead_Stores_All_Tracking_Fields_Without_Nulls_For_Optional()
        {
            var (_, uow) = CreateFresh();
            var loggerMock = new Mock<ILogger<LeadsController>>();
            var controller = new LeadsController(Mock.Of<ILeadService>(), uow, loggerMock.Object, Mock.Of<INotificationService>(), Mock.Of<IConfiguration>());

            await controller.Create(new LeadCreateDto
            {
                Name = "Data Check",
                Phone = "+201000000000",
                Source = "bing",
                Medium = "cpc",
                Campaign = "search_ad",
                LandingPage = "/",
                FirstVisitAt = DateTime.UtcNow,
                CurrentPage = "/contact",
                Referrer = "https://bing.com",
                UserAgent = "Mozilla/5.0",
                PageViews = 1,
                SessionDuration = 10,
            });

            var saved = await uow.Leads.Query().FirstOrDefaultAsync();
            saved.Should().NotBeNull();
            saved!.Source.Should().Be("bing");
            saved.Medium.Should().Be("cpc");
            saved.Campaign.Should().Be("search_ad");
            saved.LandingPage.Should().Be("/");
            saved.FirstVisitAt.Should().NotBeNull();
            saved.CurrentPage.Should().Be("/contact");
            saved.Referrer.Should().Be("https://bing.com");
            saved.UserAgent.Should().Be("Mozilla/5.0");
            saved.PageViews.Should().Be(1);
            saved.SessionDuration.Should().Be(10);
            saved.IsPaid.Should().BeTrue();
        }

        [Fact]
        public async Task Multiple_Leads_Each_Have_Own_Tracking_Data()
        {
            var (_, uow) = CreateFresh();
            var loggerMock = new Mock<ILogger<LeadsController>>();
            var controller = new LeadsController(Mock.Of<ILeadService>(), uow, loggerMock.Object, Mock.Of<INotificationService>(), Mock.Of<IConfiguration>());

            await controller.Create(new LeadCreateDto
            {
                Name = "Lead A",
                Phone = "+201000000001",
                Source = "facebook",
                Medium = "social",
                Campaign = "camp_a"
            });
            await controller.Create(new LeadCreateDto
            {
                Name = "Lead B",
                Phone = "+201000000002",
                Source = "google",
                Medium = "cpc",
                Campaign = "camp_b"
            });

            var all = await uow.Leads.Query().ToListAsync();
            all.Should().HaveCount(2);
            all[0].Campaign.Should().Be("camp_a");
            all[1].Campaign.Should().Be("camp_b");
        }
    }
}
