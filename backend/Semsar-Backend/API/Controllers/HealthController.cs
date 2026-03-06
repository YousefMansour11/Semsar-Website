using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Application.Interfaces;
using Infrastructure.Health;
using System.Threading;
using System.Collections.Generic;

namespace API.Controllers
{
    [ApiController]
    [Route("api/health/detailed")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("fixed")]
    public class HealthController : ControllerBase
    {
        private readonly IEnumerable<IAppHealthCheck> _checks;
        private readonly Application.Interfaces.IAppMetrics _metrics;
        private readonly Microsoft.Extensions.Logging.ILogger<HealthController> _logger;
        public HealthController(IEnumerable<IAppHealthCheck> checks, Application.Interfaces.IAppMetrics metrics, Microsoft.Extensions.Logging.ILogger<HealthController> logger)
        {
            _checks = checks;
            _metrics = metrics;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var results = new Dictionary<string, object?>();
            var overallHealthy = true;
            foreach (var chk in _checks)
            {
                try
                {
                    var (healthy, desc) = await chk.CheckAsync(cancellationToken);
                    results[chk.Name] = new { Healthy = healthy, Description = desc };
                    if (!healthy)
                    {
                        overallHealthy = false;
                        _logger.LogWarning("Health check {Name} failed: {Description}", chk.Name, desc);
                    }
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Health check {Name} threw an exception", chk.Name);
                    results[chk.Name] = new { Healthy = false, Description = ex.Message };
                    overallHealthy = false;
                }
            }

            var payload = new
            {
                Status = overallHealthy ? "Healthy" : "Unhealthy",
                Checks = results,
                Metrics = _metrics.Snapshot()
            };
            return overallHealthy ? Ok(payload) : StatusCode(503, payload);
        }
    }
}
