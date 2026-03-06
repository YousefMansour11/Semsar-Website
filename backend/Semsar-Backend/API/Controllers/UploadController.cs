using System.Collections.Generic;
using System.IO;
using Application.DTOs;
using Application.Interfaces;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers
{
    [ApiController]
    [Route("api/upload")]
    [EnableRateLimiting("upload")]
    [Authorize(Roles = "Admin")]
    public class UploadController : ControllerBase
    {
        private readonly IImageService _imageService;
        private readonly ILogger<UploadController> _logger;

        public UploadController(IImageService imageService, ILogger<UploadController> logger)
        {
            _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private static readonly HashSet<string> AllowedImageMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp"
        };

        [HttpPost]
        [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromQuery] string folder = "properties")
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file provided or file is empty" });

            if (string.IsNullOrWhiteSpace(folder))
                folder = "properties";

            var validFolders = new[] { "properties", "projects" };
            if (!validFolders.Contains(folder, StringComparer.OrdinalIgnoreCase))
                return BadRequest(new { error = $"Invalid folder '{folder}'. Allowed: {string.Join(", ", validFolders)}" });

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext) || !AllowedImageExtensions.Contains(ext))
                return BadRequest(new { error = $"File extension '{ext}' is not allowed. Allowed: {string.Join(", ", AllowedImageExtensions)}" });

            var contentType = file.ContentType ?? string.Empty;
            if (!string.IsNullOrEmpty(contentType) && !AllowedImageMimeTypes.Contains(contentType))
                return BadRequest(new { error = $"Content type '{contentType}' is not allowed" });

            try
            {
                var result = await _imageService.UploadAsync(file, folder);

                return Ok(new UploadResponse
                {
                    Url = result.Url,
                    PublicId = result.PublicId,
                    Width = result.Width,
                    Height = result.Height,
                    Warnings = result.Warnings
                });
            }
            catch (ImageUploadException ex)
            {
                _logger.LogWarning(ex, "Image upload rejected: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error uploading file {FileName}", file.FileName);
                return StatusCode(500, new { error = "An unexpected error occurred during upload" });
            }
        }
    }

    public class UploadResponse
    {
        public string Url { get; init; } = null!;
        public string PublicId { get; init; } = null!;
        public int Width { get; init; }
        public int Height { get; init; }
        public List<string> Warnings { get; init; } = new();
    }
}
