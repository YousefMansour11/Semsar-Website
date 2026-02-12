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
    public class ReservationCleanupJob
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<ReservationCleanupJob> _logger;
        private static readonly AsyncRetryPolicy _dbRetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(2, _ => TimeSpan.FromSeconds(1));

        public ReservationCleanupJob(IServiceProvider sp, ILogger<ReservationCleanupJob> logger)
        {
            _sp = sp;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 1)]
        public async Task RunAsync()
        {
            using var scope = _sp.CreateScope();
            var ctx = scope.ServiceProvider.GetService(typeof(AppDbContext)) as AppDbContext;
            if (ctx == null) return;

            try
            {
                var cutoff = DateTime.UtcNow.AddDays(-7);
                var staleSlugs = await _dbRetryPolicy.ExecuteAsync(async () =>
                    await ctx.Set<Domain.Entities.SlugReservation>()
                        .Where(s => s.CreatedAt < cutoff && s.EntityId == null)
                        .ToListAsync());

                if (staleSlugs.Count > 0)
                {
                    ctx.Set<Domain.Entities.SlugReservation>().RemoveRange(staleSlugs);
                    await _dbRetryPolicy.ExecuteAsync(async () => await ctx.SaveChangesAsync());
                    _logger.LogInformation("Cleaned {Count} stale slug reservations", staleSlugs.Count);
                }

                var staleCodes = await _dbRetryPolicy.ExecuteAsync(async () =>
                    await ctx.Set<Domain.Entities.CodeReservation>()
                        .Where(c => c.CreatedAt < cutoff && c.EntityId == null)
                        .ToListAsync());

                if (staleCodes.Count > 0)
                {
                    ctx.Set<Domain.Entities.CodeReservation>().RemoveRange(staleCodes);
                    await _dbRetryPolicy.ExecuteAsync(async () => await ctx.SaveChangesAsync());
                    _logger.LogInformation("Cleaned {Count} stale code reservations", staleCodes.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reservation cleanup failed");
                throw;
            }
        }
    }
}
