using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Application.Interfaces;

namespace API.Controllers
{
    [ApiController]
    [Route("api/seo")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("fixed")]
    public class SeoController : ControllerBase
    {
        private readonly IContentMetaService _metaService;

        public SeoController(IContentMetaService metaService)
        {
            _metaService = metaService;
        }

        public class MetaPreviewRequest
        {
            public string EntityType { get; set; } = string.Empty;
            public string? TitleEn { get; set; }
            public string? TitleAr { get; set; }
            public string? DescriptionEn { get; set; }
            public string? DescriptionAr { get; set; }
            public string? Location { get; set; }
        }

        [HttpPost("preview")]
        public async Task<IActionResult> Preview([FromBody] MetaPreviewRequest req)
        {
            if (req == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(req.EntityType))
                return BadRequest(new { message = "EntityType is required" });

            var meta = await _metaService.GenerateAsync(
                req.EntityType,
                req.TitleEn,
                req.TitleAr,
                req.DescriptionEn,
                req.DescriptionAr,
                req.Location
            );

            return Ok(meta);
        }
    }
}
