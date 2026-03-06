using API.Middleware;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace API.Services
{
    public class IdempotencyCleanupService : BackgroundService
    {
        private readonly IIdempotencyStore _store;
        private readonly ILogger<IdempotencyCleanupService> _logger;
        private readonly TimeSpan _retention;
        private readonly TimeSpan _interval;

        public IdempotencyCleanupService(IIdempotencyStore store, ILogger<IdempotencyCleanupService> logger, TimeSpan retention)
        {
            _store = store;
            _logger = logger;
            _retention = retention;
            _interval = TimeSpan.FromHours(1);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _store.CleanupAsync(_retention);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Idempotency cleanup failed");
                }
                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
