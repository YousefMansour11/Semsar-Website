using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/locations")]
    [EnableRateLimiting("fixed")]
    public class LocationsController : ControllerBase
    {
        private readonly ILocationService _locationService;
        private readonly ILogger<LocationsController> _logger;

        public LocationsController(ILocationService locationService, ILogger<LocationsController> logger)
        {
            _locationService = locationService;
            _logger = logger;
        }

        [HttpGet("tree")]
        public async Task<IActionResult> GetTree(CancellationToken ct = default)
        {
            var tree = await _locationService.GetTreeAsync(ct);
            return Ok(tree);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int maxResults = 15, CancellationToken ct = default)
        {
            var results = await _locationService.SearchAsync(q, maxResults, ct);
            return Ok(results);
        }

        [HttpGet("{id:int}/descendants")]
        public async Task<IActionResult> GetDescendants(int id, CancellationToken ct = default)
        {
            var ids = await _locationService.GetDescendantIdsAsync(id, ct);
            return Ok(ids);
        }
    }
}
