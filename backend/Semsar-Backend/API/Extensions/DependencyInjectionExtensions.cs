using Application.Interfaces;
using Application.Mapping;
using Application.Services;
using Application.Validators;
using AutoMapper;
using FluentValidation;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using StackExchange.Redis;

namespace API.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection ConfigureApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // AutoMapper
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

        // Validators
        services.AddValidatorsFromAssemblyContaining<CreatePropertyValidator>();

        // Caching — Hybrid L1 (MemoryCache) + L2 (Redis) for multi-replica
        services.AddMemoryCache();
        var redisConn = configuration.GetConnectionString("Redis");
        var hasRedis = !string.IsNullOrWhiteSpace(redisConn);
        if (hasRedis)
        {
            try
            {
                services.AddSingleton(ConnectionMultiplexer.Connect(redisConn!));
                services.AddStackExchangeRedisCache(opt => opt.Configuration = redisConn);
                services.AddSingleton<ICacheService, HybridCacheService>();
            }
            catch (Exception redisEx)
            {
                try
                {
                    var sp = services.BuildServiceProvider();
                    var loggerFactory = sp.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
                    var logger = loggerFactory?.CreateLogger("Semsar.Redis");
                    logger?.LogWarning(redisEx, "Redis connection failed — falling back to in-memory cache");
                }
                catch { }
                services.AddSingleton<ICacheService, MemoryCacheService>();
            }
        }
        else
        {
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }

        // Metrics
        services.AddSingleton<IAppMetrics, AppMetrics>();
        services.AddSingleton<ISeoTelemetry, SeoTelemetry>();

        // Concurrency
        services.AddScoped<IConcurrencyValidator, ConcurrencyValidator>();

        // Core services with retry policies
        services.AddScoped<INotificationService, ResilientNotificationService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IInstallmentQueryService, InstallmentQueryService>();
        services.AddScoped<IPropertyQueryService>(sp =>
        {
            var uow = sp.GetRequiredService<IUnitOfWork>();
            var installmentQuery = sp.GetRequiredService<IInstallmentQueryService>();
            var jsonLd = sp.GetRequiredService<IJsonLdService>();
            var canonical = sp.GetRequiredService<ICanonicalService>();
            var search = sp.GetRequiredService<ISearchService>();
            var seoGen = sp.GetRequiredService<ISeoContentGenerator>();
            var internalLinks = sp.GetRequiredService<IInternalLinkingService>();
            var serp = sp.GetRequiredService<ISERPVariantGenerator>();
            var entityGraph = sp.GetRequiredService<IEntityGraphService>();
            var semanticDedup = sp.GetRequiredService<ISemanticDeduplicationService>();
            var seoValidationGate = sp.GetRequiredService<ISeoValidationGate>();
            var publicIdService = sp.GetRequiredService<IPublicIdService>();
            var cloud = sp.GetService<Application.Interfaces.ICloudinaryService>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<PropertyQueryService>>();
            return new PropertyQueryService(uow, installmentQuery, jsonLd, canonical, search, seoGen, internalLinks, serp, entityGraph, semanticDedup, seoValidationGate, publicIdService, cloud, logger);
        });
        services.AddScoped<IProjectQueryService>(sp =>
        {
            var uow = sp.GetRequiredService<IUnitOfWork>();
            var jsonLd = sp.GetRequiredService<IJsonLdService>();
            var canonical = sp.GetRequiredService<ICanonicalService>();
            var seoGen = sp.GetRequiredService<ISeoContentGenerator>();
            var serp = sp.GetRequiredService<ISERPVariantGenerator>();
            var entityGraph = sp.GetRequiredService<IEntityGraphService>();
            var internalLinks = sp.GetRequiredService<IInternalLinkingService>();
            var clickBehavior = sp.GetRequiredService<IClickBehaviorOptimizationService>();
            var publicIdService = sp.GetRequiredService<IPublicIdService>();
            var cloud = sp.GetService<Application.Interfaces.ICloudinaryService>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<ProjectQueryService>>();
            return new ProjectQueryService(uow, jsonLd, canonical, seoGen, serp, entityGraph, internalLinks, clickBehavior, publicIdService, cloud, logger);
        });
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IPropertyCommandService, PropertyCommandService>();
        services.AddScoped<DashboardService>();
        // Authentication service registration - ensure the concrete implementation is available for DI
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ICloudinaryService, ResilientCloudinaryService>();
        services.AddScoped<ILandRequestService, LandRequestService>();
        services.AddScoped<IImageUploadService, ImageUploadService>();
        // Application service registrations
        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<IUnitService, UnitService>();

        // Video services
        services.AddScoped<IVideoUploadService, Infrastructure.Services.CloudinaryVideoUploadService>();
        services.AddScoped<IVideoService, VideoService>();
        services.AddScoped<IVideoLibraryService, VideoLibraryService>();

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IRepository<Domain.Entities.LandRequest>, LandRequestRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();

        // SEO / Slug / Canonical / JSON-LD
        services.AddSingleton<ISlugService, SlugService>();
        services.AddSingleton<ISeoService, SeoService>();
        services.AddSingleton<ICanonicalService, CanonicalService>();
        services.AddSingleton<IContentMetaService, ContentMetaService>();
        services.AddSingleton<IJsonLdService, JsonLdService>();
        services.AddSingleton<IOgMetaService, OgMetaService>();

        // SEO content generation
        services.AddSingleton<ISeoContentGenerator, SeoContentGenerator>();
        services.AddSingleton<IInternalLinkingService, InternalLinkingService>();
        services.AddSingleton<ISeoValidationGate, SeoValidationGate>();

        // Ranking intelligence system (12 modules) — Redis-backed for multi-replica persistence
        if (hasRedis)
        {
            services.AddSingleton<IRankingDataStore, RedisRankingDataStore>();
            services.AddSingleton<IClickBehaviorOptimizationService, RedisClickBehaviorStore>();
            services.AddSingleton<IAuthoritySignalService, RedisAuthoritySignalStore>();
            services.AddSingleton<ISemanticDeduplicationService, RedisSemanticProfileStore>();
            services.AddSingleton<IIndexVelocityService, RedisIndexVelocityStore>();
            services.AddSingleton<IEntityGraphService, RedisEntityGraphStore>();
        }
        else
        {
            services.AddSingleton<IRankingDataStore, RankingDataStore>();
            services.AddSingleton<IClickBehaviorOptimizationService, ClickBehaviorOptimizationService>();
            services.AddSingleton<IAuthoritySignalService, AuthoritySignalService>();
            services.AddSingleton<ISemanticDeduplicationService, SemanticDeduplicationService>();
            services.AddSingleton<IIndexVelocityService, IndexVelocityService>();
            services.AddSingleton<IEntityGraphService, EntityGraphService>();
        }
        services.AddSingleton<ISERPVariantGenerator, SERPVariantGenerator>();
        services.AddSingleton<IRankingFeedbackLoopService, RankingFeedbackLoopService>();
        services.AddSingleton<ICrawlBudgetOptimizer, CrawlBudgetOptimizer>();
        services.AddSingleton<IFreshnessService, FreshnessService>();
        services.AddSingleton<ILocationSeoService, LocationSeoService>();
        services.AddSingleton<ITopicClusterService, TopicClusterService>();
        services.AddSingleton<IIndexControlService, IndexControlService>();

        // Mapping layer
        services.AddSingleton<IUnitMapper, UnitMapper>();

        // Query services
        services.AddScoped<IUnitQueryService>(sp =>
        {
            var uow = sp.GetRequiredService<IUnitOfWork>();
            var jsonLd = sp.GetRequiredService<IJsonLdService>();
            var canonical = sp.GetRequiredService<ICanonicalService>();
            var seoGen = sp.GetRequiredService<ISeoContentGenerator>();
            var serp = sp.GetRequiredService<ISERPVariantGenerator>();
            var entityGraph = sp.GetRequiredService<IEntityGraphService>();
            var internalLinks = sp.GetRequiredService<IInternalLinkingService>();
            var clickBehavior = sp.GetRequiredService<IClickBehaviorOptimizationService>();
            var publicIdService = sp.GetRequiredService<IPublicIdService>();
            var cloud = sp.GetService<Application.Interfaces.ICloudinaryService>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<UnitQueryService>>();
            return new UnitQueryService(uow, jsonLd, canonical, seoGen, serp, entityGraph, internalLinks, clickBehavior, publicIdService, cloud, logger);
        });
        services.AddScoped<IPropertyFilterService, PropertyFilterService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IProjectService, ProjectService>();
        // Search service
        services.AddScoped<ISearchService, SearchService>();
        // JWT service
        services.AddScoped<Application.Interfaces.IJwtService, Infrastructure.Auth.JwtService>();

        // HttpContext accessor
        services.AddHttpContextAccessor();

        // Public ID service (UUIDv7 + prefixed entity keys)
        services.AddSingleton<IPublicIdService, PublicIdService>();
        services.AddSingleton<PublicKeyGenerationInterceptor>();

        // Cloudinary Configuration
        services.Configure<CloudinarySettings>(configuration.GetSection("Cloudinary"));
        services.AddScoped<IImageService, CloudinaryService>();

        // AppSettings
        services.Configure<Application.Settings.AppSettings>(configuration.GetSection("AppSettings"));

        return services;
    }
}
