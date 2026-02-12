using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class ConcurrencyValidator : IConcurrencyValidator
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ConcurrencyValidator>? _logger;
        private readonly Application.Interfaces.IAppMetrics? _metrics;

        public ConcurrencyValidator(AppDbContext context, ILogger<ConcurrencyValidator>? logger = null, Application.Interfaces.IAppMetrics? metrics = null)
        {
            _context = context;
            _logger = logger;
            _metrics = metrics;
        }

        public async Task ValidateNoExternalModificationAsync(IEnumerable<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry> entries)
        {
            var list = entries.Where(e => e.Properties.Any(p => p.Metadata.Name == "RowVersion")).ToList();
            if (!list.Any()) return;

            _logger?.LogDebug("Deferring RowVersion validation for {Count} entities to SaveChangesAsync (EF handles this internally)", list.Count);

            await Task.CompletedTask;
        }

        public async Task<bool> IsHealthyAsync(System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                await _context.Database.CanConnectAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Database health check failed");
                return false;
            }
        }

        public Task LogMetricsAsync()
        {
            try
            {
                var snap = _metrics?.Snapshot();
                if (snap != null)
                {
                    foreach (var kv in snap)
                    {
                        _logger?.LogDebug("Metric snapshot: {Key}={Value}", kv.Key, kv.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to log metrics from ConcurrencyValidator");
            }
            return Task.CompletedTask;
        }
    }
}
