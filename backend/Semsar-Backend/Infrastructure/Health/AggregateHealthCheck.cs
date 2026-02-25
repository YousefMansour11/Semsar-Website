using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Infrastructure.Health;

public class AggregateHealthCheck : IHealthCheck
{
    private readonly IEnumerable<IAppHealthCheck> _checks;

    public AggregateHealthCheck(IEnumerable<IAppHealthCheck> checks)
    {
        _checks = checks;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var failed = new Dictionary<string, object>();
        var healthy = true;

        foreach (var check in _checks)
        {
            var (isHealthy, description) = await check.CheckAsync(cancellationToken);
            if (!isHealthy)
            {
                healthy = false;
                failed[check.Name] = description ?? "unhealthy";
            }
        }

        if (healthy) return HealthCheckResult.Healthy("All checks passed");
        return HealthCheckResult.Unhealthy("One or more checks failed", data: failed);
    }
}
