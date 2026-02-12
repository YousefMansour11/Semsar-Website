using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;

namespace Infrastructure.Services
{
    public class UploadCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<UploadCleanupBackgroundService> _logger;
        private static readonly AsyncRetryPolicy _deleteRetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100));
        private static readonly AsyncRetryPolicy _dbRetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(2, _ => TimeSpan.FromSeconds(1));

        public UploadCleanupBackgroundService(IServiceProvider sp, ILogger<UploadCleanupBackgroundService> logger)
        {
            _sp = sp;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var intervalEnv = Environment.GetEnvironmentVariable("UPLOAD_CLEANUP_INTERVAL_SECONDS");
            var interval = 300; // default 5 minutes
            if (!string.IsNullOrEmpty(intervalEnv) && int.TryParse(intervalEnv, out var parsed)) interval = Math.Max(60, parsed);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessQueueAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Upload cleanup background iteration failed");
                }

                await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
            }
        }

        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            using var scope = _sp.CreateScope();
            var imageSvc = scope.ServiceProvider.GetService(typeof(Application.Interfaces.IImageUploadService)) as Application.Interfaces.IImageUploadService;
            var ctx = scope.ServiceProvider.GetService(typeof(Infrastructure.Data.AppDbContext)) as Infrastructure.Data.AppDbContext;
            if (imageSvc == null || ctx == null) return;

            try
            {
                var pending = await _dbRetryPolicy.ExecuteAsync(async ct =>
                    await ctx.Set<Domain.Entities.OrphanedUpload>()
                        .Where(x => x.Status == "Pending")
                        .OrderBy(x => x.Id)
                        .Take(50)
                        .ToListAsync(ct), cancellationToken);

                if (pending.Count == 0) return;

                foreach (var item in pending)
                {
                    item.Status = "Processing";
                }
                await _dbRetryPolicy.ExecuteAsync(async ct =>
                    await ctx.SaveChangesAsync(ct), cancellationToken);

                foreach (var item in pending)
                {
                    try
                    {
                        var ok = await _deleteRetryPolicy.ExecuteAsync(async () =>
                            await imageSvc.DeleteImageAsync(item.PublicId));
                        item.Status = ok ? "Done" : "Failed";
                        item.ErrorMessage = ok ? null : "Delete failed";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process orphaned upload {Id} PublicId={PublicId} after retries", item.Id, item.PublicId);
                        item.Status = "Failed";
                        item.ErrorMessage = ex.Message;
                    }
                }

                // Batch save all status updates in a single call
                await _dbRetryPolicy.ExecuteAsync(async ct =>
                    await ctx.SaveChangesAsync(ct), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run orphaned upload cleanup");
            }
        }
    }
}
