using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/properties")]
    [EnableRateLimiting("fixed")]
    public class PropertiesFilterController : ControllerBase
    {
        private readonly IPropertyFilterService _filterService;
        private readonly ILogger<PropertiesFilterController> _logger;

        public PropertiesFilterController(IPropertyFilterService filterService, ILogger<PropertiesFilterController> logger)
        {
            _filterService = filterService;
            _logger = logger;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] int? locationId,
            [FromQuery] bool includeChildren = false,
            [FromQuery] int[]? locationIds = null,
            [FromQuery] bool? isFurnished = null,
            [FromQuery] bool? hasInstallment = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] double? minSize = null,
            [FromQuery] double? maxSize = null,
            [FromQuery] int? bedrooms = null,
            [FromQuery] int? bathrooms = null,
            [FromQuery] string? propertyType = null,
            [FromQuery] string? listingType = null,
            [FromQuery] string? features = null,
            [FromQuery] int? projectId = null,
            [FromQuery] string? keyword = null,
            [FromQuery] string sortBy = "newest",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var result = await _filterService.FilterPropertiesAsync(
                locationId, includeChildren, locationIds, isFurnished, hasInstallment,
                minPrice, maxPrice,
                minSize, maxSize,
                bedrooms, bathrooms,
                propertyType, listingType,
                features, projectId,
                keyword, sortBy,
                page, pageSize, ct);

            return Ok(result);
        }
    }
}
