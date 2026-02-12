using Application.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services.Jobs
{
    public class SitemapGenerationJob
    {
        private readonly ILogger<SitemapGenerationJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public SitemapGenerationJob(ILogger<SitemapGenerationJob> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task RunAsync()
        {
            try
            {
                _logger.LogInformation("Sitemap pre-generation started");

                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var propertyCount = await uow.Properties.Query()
                    .CountAsync(p => !p.IsDeleted && !string.IsNullOrEmpty(p.Slug));
                var projectCount = await uow.Projects.Query()
                    .CountAsync(p => !p.IsDeleted && !string.IsNullOrEmpty(p.Slug));
                var unitCount = await uow.Units.Query()
                    .CountAsync(u => !u.IsDeleted && !string.IsNullOrEmpty(u.Slug));

                _logger.LogInformation(
                    "Sitemap pre-generation completed. Properties: {Props}, Projects: {Projects}, Units: {Units}",
                    propertyCount,
                    projectCount,
                    unitCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sitemap generation failed");
                throw;
            }
        }
    }
}
