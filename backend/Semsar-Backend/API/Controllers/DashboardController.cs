using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/stats")]
    [EnableRateLimiting("fixed")]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _service;
        private readonly ILogger<DashboardController>? _logger;

        public DashboardController(DashboardService service, ILogger<DashboardController>? logger = null)
        {
            _service = service;
            _logger = logger;
        }

        // =========================
        // ADMIN: Dashboard stats
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var stats = await _service.GetStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to retrieve dashboard stats");
                return StatusCode(500, new { message = "Failed to retrieve dashboard stats" });
            }
        }
    }
}