using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class SeoFeedbackBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SeoFeedbackBackgroundService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    public SeoFeedbackBackgroundService(IServiceScopeFactory scopeFactory, ILogger<SeoFeedbackBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SeoFeedbackBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, stoppingToken);
                await ProcessFeedbackCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SEO feedback cycle failed");
            }
        }

        _logger.LogInformation("SeoFeedbackBackgroundService stopped");
    }

    private async Task ProcessFeedbackCycleAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();

        // Core feedback loop
        var feedbackLoop = scope.ServiceProvider.GetRequiredService<IRankingFeedbackLoopService>();
        var freshness = scope.ServiceProvider.GetRequiredService<IFreshnessService>();
        var rankingStore = scope.ServiceProvider.GetRequiredService<IRankingDataStore>();

        await feedbackLoop.ProcessFeedbackAsync();

        // Freshness tracking
        var staleProperties = await freshness.GetStaleEntitiesAsync("property", 50);
        var staleProjects = await freshness.GetStaleEntitiesAsync("project", 20);

        // Index velocity tracking
        var indexVelocity = scope.ServiceProvider.GetRequiredService<IIndexVelocityService>();
        var velocity = await indexVelocity.GetCurrentVelocityAsync();
        var needsIndexing = await indexVelocity.GetUrlsNeedingIndexingAsync(20);

        // Authority signal refresh
        var authority = scope.ServiceProvider.GetRequiredService<IAuthoritySignalService>();
        var topPages = await authority.GetTopAuthorityPagesAsync(10);

        // Topic cluster integrity
        var topicClusters = scope.ServiceProvider.GetRequiredService<ITopicClusterService>();
        var allClusters = await topicClusters.GetAllClustersAsync();
        foreach (var cluster in allClusters)
        {
            var integrity = await topicClusters.VerifyClusterIntegrityAsync(cluster.ClusterId);
            if (!integrity.IsValid)
            {
                _logger.LogWarning("Topic cluster {ClusterId} integrity check failed: {Gaps}",
                    cluster.ClusterId, string.Join(", ", integrity.Gaps));
            }
        }

        // Ranking feedback
        var pendingActions = await feedbackLoop.GetPendingActionsAsync();
        var staleCount = staleProperties.Count + staleProjects.Count;

        _logger.LogInformation(
            "SEO feedback cycle completed. Actions: {ActionCount}, Stale: {StaleCount}, " +
            "Index velocity: {Velocity:P}, Top pages tracked: {TopPages}, Clusters: {ClusterCount}",
            pendingActions.Count, staleCount,
            velocity.CurrentVelocity,
            topPages.Count, allClusters.Count);
    }
}
