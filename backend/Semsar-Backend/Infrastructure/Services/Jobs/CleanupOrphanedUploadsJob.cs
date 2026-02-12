using Domain.Entities;
using Hangfire;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Services.Jobs
{
    public class CleanupOrphanedUploadsJob
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<CleanupOrphanedUploadsJob> _logger;
        private static readonly AsyncRetryPolicy _deleteRetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100));
        private static readonly AsyncRetryPolicy _dbRetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(2, _ => TimeSpan.FromSeconds(1));

        public CleanupOrphanedUploadsJob(IServiceProvider sp, ILogger<CleanupOrphanedUploadsJob> logger)
        {
            _sp = sp;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 2)]
        public async Task RunAsync()
        {
            using var scope = _sp.CreateScope();
            var imageSvc = scope.ServiceProvider.GetService(typeof(Application.Interfaces.IImageUploadService)) as Application.Interfaces.IImageUploadService;
            var ctx = scope.ServiceProvider.GetService(typeof(AppDbContext)) as AppDbContext;
            if (imageSvc == null || ctx == null) return;

            var pending = await _dbRetryPolicy.ExecuteAsync(async () =>
                await ctx.Set<OrphanedUpload>()
                    .Where(x => x.Status == "Pending")
                    .OrderBy(x => x.Id)
                    .Take(50)
                    .ToListAsync());

            if (pending.Count == 0) return;

            foreach (var item in pending)
                item.Status = "Processing";
            await _dbRetryPolicy.ExecuteAsync(async () => await ctx.SaveChangesAsync());

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
                    _logger.LogWarning(ex, "Failed to process orphaned upload {Id} after retries", item.Id);
                    item.Status = "Failed";
                    item.ErrorMessage = ex.Message;
                }
            }

            await _dbRetryPolicy.ExecuteAsync(async () => await ctx.SaveChangesAsync());
        }
    }
}
