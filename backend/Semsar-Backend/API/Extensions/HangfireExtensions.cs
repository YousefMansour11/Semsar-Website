using API.Auth;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Infrastructure.Services.Jobs;

namespace API.Extensions;

public static class HangfireExtensions
{
    public static IServiceCollection ConfigureHangfire(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            return services;

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.FromSeconds(15),
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }));

        services.AddHangfireServer(options =>
        {
            options.Queues = new[] { "critical", "default", "email" };
            options.WorkerCount = Environment.ProcessorCount * 2;
            options.ServerName = $"semsar-{Environment.MachineName}";
        });

        services.AddScoped<CleanupOrphanedUploadsJob>();
        services.AddScoped<SitemapGenerationJob>();
        services.AddScoped<SeoRecalculationJob>();
        services.AddScoped<ReservationCleanupJob>();

        return services;
    }

    public static IApplicationBuilder UseHangfireDashboard(this IApplicationBuilder app, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            return app;

        // IgnoreAntiforgeryToken is required for Hangfire's internal POST-based job operations
        // (retry, delete, trigger). The dashboard is protected by HangfireDashboardAuthFilter
        // which requires the Admin role — CSRF is mitigated by the auth check on every request.
        app.UseHangfireDashboard("/jobs", new DashboardOptions
        {
            Authorization = new[] { new HangfireDashboardAuthFilter() },
            StatsPollingInterval = 5000,
            DashboardTitle = "Semsar Jobs",
            IgnoreAntiforgeryToken = true
        });

        return app;
    }

    public static void RegisterRecurringJobs()
    {
        RecurringJob.AddOrUpdate<CleanupOrphanedUploadsJob>(
            "cleanup-orphaned-uploads",
            job => job.RunAsync(),
            "*/5 * * * *");

        RecurringJob.AddOrUpdate<SitemapGenerationJob>(
            "generate-sitemap",
            job => job.RunAsync(),
            "0 */6 * * *");

        RecurringJob.AddOrUpdate<SeoRecalculationJob>(
            "recalculate-seo",
            job => job.RunAsync(),
            "0 3 * * *");

        RecurringJob.AddOrUpdate<ReservationCleanupJob>(
            "cleanup-reservations",
            job => job.RunAsync(),
            "0 4 * * *");
    }
}
