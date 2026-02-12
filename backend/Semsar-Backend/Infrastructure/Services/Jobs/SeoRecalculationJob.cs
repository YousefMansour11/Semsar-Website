using Application.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Services.Jobs
{
    public class SeoRecalculationJob
    {
        private readonly IRankingFeedbackLoopService _feedbackLoop;
        private readonly IFreshnessService _freshness;
        private readonly ILogger<SeoRecalculationJob> _logger;

        public SeoRecalculationJob(
            IRankingFeedbackLoopService feedbackLoop,
            IFreshnessService freshness,
            ILogger<SeoRecalculationJob> logger)
        {
            _feedbackLoop = feedbackLoop;
            _freshness = freshness;
            _logger = logger;
        }

        public async Task RunAsync()
        {
            try
            {
                _logger.LogInformation("SEO recalculation started");

                await _feedbackLoop.ProcessFeedbackAsync();
                var staleProperties = await _freshness.GetStaleEntitiesAsync("property", 50);
                var staleProjects = await _freshness.GetStaleEntitiesAsync("project", 20);

                _logger.LogInformation(
                    "SEO recalculation completed. {StaleProps} stale properties, {StaleProjects} stale projects found",
                    staleProperties.Count,
                    staleProjects.Count);

                foreach (var prop in staleProperties)
                {
                    _logger.LogDebug("Stale property {EntityId} has score {Score}", prop.EntityId, prop.Score);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SEO recalculation failed");
                throw;
            }
        }
    }
}
