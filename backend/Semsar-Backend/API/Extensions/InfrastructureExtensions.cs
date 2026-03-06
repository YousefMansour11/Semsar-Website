using API.Middleware;
using API.Services;
using Infrastructure.Health;
using Infrastructure.Middlewares;
using Infrastructure.Services;

namespace API.Extensions;

public static class IdempotencyExtensions
{
    public static IServiceCollection ConfigureIdempotency(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConn = configuration.GetConnectionString("Redis");
        var idempotencyRetentionHours = double.TryParse(configuration["Idempotency:RetentionHours"], out var ih) ? ih : 24;

        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();
        }
        else
        {
            services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
            services.AddHostedService<IdempotencyCleanupService>(sp =>
            {
                var store = sp.GetRequiredService<IIdempotencyStore>();
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<IdempotencyCleanupService>>();
                return new IdempotencyCleanupService(store, logger, TimeSpan.FromHours(idempotencyRetentionHours));
            });
        }

        return services;
    }
}

public static class HealthCheckExtensions
{
    public static IServiceCollection ConfigureHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IInfrastructureHealthCheck, InfrastructureHealthCheck>();
        services.AddHealthChecks().AddCheck<AggregateHealthCheck>("aggregate");
        services.AddScoped<IAppHealthCheck, DbContextHealthCheck>();
        services.AddScoped<IAppHealthCheck, TransactionSystemHealthCheck>();
        services.AddScoped<IAppHealthCheck, DependencyScopeHealthCheck>();

        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddScoped<IAppHealthCheck>(sp => new RedisHealthCheck(redisConn));
        }

        return services;
    }
}

public static class BackgroundServiceExtensions
{
    public static IServiceCollection ConfigureBackgroundServices(this IServiceCollection services, IWebHostEnvironment environment)
    {
        // Only register UploadCleanupBackgroundService in Development or Production. Testing environment
        // has been removed; we only consider Development environment for local startup.
        if (environment.IsDevelopment())
        {
            services.AddHostedService<UploadCleanupBackgroundService>();
        }

        // SEO feedback loop runs in all environments (6-hour cycle)
        services.AddHostedService<SeoFeedbackBackgroundService>();
        return services;
    }
}
