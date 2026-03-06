using Application.Interfaces;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/videos")]
    [EnableRateLimiting("fixed")]
    public class VideosController : ControllerBase
    {
        private readonly IVideoLibraryService _libraryService;
        private readonly ILogger<VideosController> _logger;
        private readonly CloudinarySettings _cloudinarySettings;

        public VideosController(IVideoLibraryService libraryService, ILogger<VideosController> logger, IOptions<CloudinarySettings> cloudinarySettings)
        {
            _libraryService = libraryService ?? throw new ArgumentNullException(nameof(libraryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cloudinarySettings = cloudinarySettings?.Value ?? throw new ArgumentNullException(nameof(cloudinarySettings));
        }

        /// <summary>
        /// Generate a signed upload token for direct browser-to-Cloudinary video upload.
        /// If publicId is provided, uses it for content-based dedup (overwrite=false).
        /// The browser uploads directly to Cloudinary's API, then calls /confirm to register.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("upload-signature")]
        public IActionResult GetUploadSignature([FromQuery] string folder = "properties", [FromQuery] string? publicId = null)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var apiKey = _cloudinarySettings.ApiKey;
            var apiSecret = _cloudinarySettings.ApiSecret;
            var cloudName = _cloudinarySettings.CloudName;

            // Build sorted param string for Cloudinary signature (alphabetical order)
            var prefix = $"folder={folder}&timestamp={timestamp}";
            if (!string.IsNullOrWhiteSpace(publicId))
                prefix = $"folder={folder}&overwrite=false&public_id={publicId}&timestamp={timestamp}";

            var toSign = $"{prefix}{apiSecret}";
            var signature = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(toSign))).ToLowerInvariant();

            return Ok(new
            {
                signature,
                timestamp,
                apiKey,
                cloudName,
                folder,
                publicId = !string.IsNullOrWhiteSpace(publicId) ? publicId : null,
                overwrite = !string.IsNullOrWhiteSpace(publicId) ? (bool?)false : null
            });
        }

        /// <summary>
        /// Get all unique videos in the library (deduplicated by Cloudinary PublicId).
        /// Used by the admin dashboard to select existing videos instead of re-uploading.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("library")]
        public async Task<IActionResult> GetLibrary()
        {
            try
            {
                var items = await _libraryService.GetLibraryAsync();
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve video library");
                return StatusCode(500, new { message = "Failed to retrieve video library" });
            }
        }

        /// <summary>
        /// Get unique videos scoped to a project's units (deduplicated by Cloudinary PublicId).
        /// Used by the admin dashboard when adding videos to units within a project.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("library/project/{projectId:int}")]
        public async Task<IActionResult> GetLibraryByProject(int projectId)
        {
            try
            {
                var items = await _libraryService.GetLibraryByProjectAsync(projectId);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve video library for project {ProjectId}", projectId);
                return StatusCode(500, new { message = "Failed to retrieve video library for project" });
            }
        }

        /// <summary>
        /// Attach an existing library video to a property.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("attach/property/{propertyId:int}")]
        public async Task<IActionResult> AttachToProperty(int propertyId, [FromBody] AttachVideoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.PublicId))
                return BadRequest(new { message = "PublicId is required" });

            try
            {
                var result = await _libraryService.AttachLibraryVideoToPropertyAsync(propertyId, request.PublicId);
                return Ok(new { message = "Video attached successfully", files = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to attach video to property {PropertyId}", propertyId);
                return StatusCode(500, new { message = "Failed to attach video" });
            }
        }

        /// <summary>
        /// Attach an existing library video to a unit.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("attach/unit/{unitId:int}")]
        public async Task<IActionResult> AttachToUnit(int unitId, [FromBody] AttachVideoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.PublicId))
                return BadRequest(new { message = "PublicId is required" });

            try
            {
                var result = await _libraryService.AttachLibraryVideoToUnitAsync(unitId, request.PublicId);
                return Ok(new { message = "Video attached successfully", files = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to attach video to unit {UnitId}", unitId);
                return StatusCode(500, new { message = "Failed to attach video" });
            }
        }

        /// <summary>
        /// Attach an existing library video to a project.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("attach/project/{projectId:int}")]
        public async Task<IActionResult> AttachToProject(int projectId, [FromBody] AttachVideoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.PublicId))
                return BadRequest(new { message = "PublicId is required" });

            try
            {
                var result = await _libraryService.AttachLibraryVideoToProjectAsync(projectId, request.PublicId);
                return Ok(new { message = "Video attached successfully", files = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to attach video to project {ProjectId}", projectId);
                return StatusCode(500, new { message = "Failed to attach video" });
            }
        }
    }

    public class AttachVideoRequest
    {
        public string PublicId { get; set; } = string.Empty;
    }
}