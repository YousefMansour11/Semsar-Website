using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("fixed")]
    public class ProjectsController : ControllerBase
    {
        private readonly Application.Interfaces.IProjectQueryService _queryService;
        private readonly Application.Interfaces.IProjectService _projectService;
        private readonly IUnitOfWork _uow;
        private readonly Application.Interfaces.ISlugService _slugService;
        private readonly IPublicIdService _publicIdService;
        private readonly Microsoft.Extensions.Logging.ILogger<ProjectsController> _logger;
        private readonly Application.Interfaces.ICacheService _cache;
        private readonly Application.Interfaces.ICloudinaryService _cloudinary;
        private readonly Application.Interfaces.IImageUploadService? _imageUploadService;
        private readonly Application.Interfaces.IVideoUploadService _videoUploadService;
        private readonly Application.Interfaces.IVideoService _videoService;

        public ProjectsController(
            Application.Interfaces.IProjectQueryService queryService,
            Application.Interfaces.IProjectService projectService,
            IUnitOfWork uow,
            Application.Interfaces.ISlugService slugService,
            IPublicIdService publicIdService,
            Microsoft.Extensions.Logging.ILogger<ProjectsController> logger,
            Application.Interfaces.ICacheService cache,
            Application.Interfaces.ICloudinaryService cloudinary,
            Application.Interfaces.IImageUploadService? imageUploadService = null,
            Application.Interfaces.IVideoUploadService videoUploadService = null!,
            Application.Interfaces.IVideoService videoService = null!)
        {
            _queryService = queryService;
            _projectService = projectService;
            _uow = uow;
            _slugService = slugService;
            _publicIdService = publicIdService;
            _logger = logger;
            _cache = cache;
            _cloudinary = cloudinary;
            _imageUploadService = imageUploadService;
            _videoUploadService = videoUploadService;
            _videoService = videoService;
        }

        [HttpGet]
        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<IActionResult> GetAll(int page = 1, int pageSize = 20)
        {
            int pageNum = Math.Max(1, page);
            int pageSizeNum = Math.Clamp(pageSize, 1, 100);

            var q = _uow.Projects.Query().AsNoTracking()
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.CreatedAt);

            var total = await q.CountAsync();
            var data = await q.Skip((pageNum - 1) * pageSizeNum)
                .Take(pageSizeNum)
                .Select(p => new ProjectCardDto
                {
                    Id = p.Id,
                    PublicKey = p.PublicKey,
                    NameEn = p.NameEn,
                    NameAr = p.NameAr,
                    Location = p.Location,
                    LocationAr = p.LocationAr,
                    Developer = p.Developer,
                    Image = p.Image,
                    Slug = p.Slug,
                    StartingPrice = p.StartingPrice,
                    PropertyTypes = p.PropertyTypes,
                    TotalArea = p.TotalArea,
                    UnitCount = p.UnitCount,
                    Highlights = p.Highlights,
                    HighlightsAr = p.HighlightsAr
                }).ToListAsync();

            return Ok(new { total, page = pageNum, pageSize = pageSizeNum, data });
        }

        [HttpGet("{id:int}")]
        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<IActionResult> Get(int id)
        {
            var project = await _queryService.GetBySlugOrIdAsync(id.ToString());
            if (project == null) return NotFound();

            try
            {
                _logger.LogInformation("Project view: {Id} at {Time}", id, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Analytics logging failed for project view");
            }

            return Ok(project);
        }

        // PUBLIC: Get project by public key
        [HttpGet("public-key/{publicKey}")]
        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<IActionResult> GetByPublicKey(string publicKey)
        {
            var project = await _queryService.GetByPublicKeyAsync(publicKey);
            if (project == null) return NotFound();
            return Ok(project);
        }

        [HttpGet("slug/{slug}")]
        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var project = await _queryService.GetBySlugOrIdAsync(slug);
            if (project == null) return NotFound();
            return Ok(project);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProjectDto? dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var created = await _projectService.CreateAsync(dto);
                _cache?.InvalidateByPrefix("projects_"); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_");
                return Ok(new { Id = created.Id, Name = created.Name, Location = created.Location });
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Duplicate slug or resource already exists" });
            }
            catch (Application.Services.SlugConflictException)
            {
                return Conflict(new { message = "Slug conflict - please retry" });
            }
            catch (InvalidOperationException ioex)
            {
                return Conflict(new { message = ioex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, [FromBody] UpdateProjectDto? dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _projectService.PatchAsync(id, dto);
                _cache?.InvalidateByPrefix("projects_"); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_");
                return Ok(new { message = "Project updated" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ioex) { return Conflict(new { message = ioex.Message }); }
            catch (SlugConflictException) { return Conflict(new { message = "Slug conflict - please retry" }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error patching project {ProjectId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _projectService.DeleteAsync(id);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            _cache?.InvalidateByPrefix("projects_"); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_");

            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/images")]
        public async Task<IActionResult> UploadImages(int id, [FromForm] List<Microsoft.AspNetCore.Http.IFormFile> images)
        {
            if (images == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            if (!images.Any())
                return BadRequest(new { message = "No images were provided" });

            var uploadList = new List<(System.IO.Stream Stream, string FileName, string? PublicId)>();
            long maxBytes = 5 * 1024 * 1024;
            var env = System.Environment.GetEnvironmentVariable("MAX_UPLOAD_FILE_BYTES");
            if (!string.IsNullOrEmpty(env) && long.TryParse(env, out var parsed)) maxBytes = Math.Max(1024, parsed);

            var maxFileCount = 20;
            if (images.Count > maxFileCount)
                return BadRequest(new { message = $"Maximum {maxFileCount} files allowed per upload" });

            foreach (var file in images)
            {
                if (file == null || file.Length == 0) continue;
                if (file.Length > maxBytes)
                    return BadRequest(new { message = $"File {file.FileName} exceeds maximum allowed size of {maxBytes} bytes" });

                var contentType = file.ContentType ?? string.Empty;
                if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    continue;

                var stream = file.OpenReadStream();
                try
                {
                    if (!Application.Services.ImageValidation.ValidateImageHeader(stream, contentType, file.FileName))
                    {
                        stream.Dispose();
                        continue;
                    }
                    if (stream.CanSeek) stream.Position = 0;
                    uploadList.Add((stream, file.FileName, null));
                }
                catch (Exception ex)
                {
                    try { stream.Dispose(); } catch (Exception disposeEx) { _logger?.LogWarning(disposeEx, "Failed to dispose stream for project {ProjectId}", id); }
                    _logger?.LogWarning(ex, "Upload validation failed for project {ProjectId} file {File}", id, file.FileName);
                    continue;
                }
            }

            if (!uploadList.Any()) return BadRequest(new { message = "No valid image files provided" });

            List<CloudinaryUploadResult>? results = null;
            try
            {
                var paramList = uploadList.Select(x => (x.Stream, x.FileName)).ToList();
                if (_imageUploadService == null)
                {
                    foreach (var s in uploadList) try { s.Stream.Dispose(); } catch (Exception disposeEx) { _logger?.LogWarning(disposeEx, "Failed to dispose upload stream"); }
                    return StatusCode(500, new { message = "Image upload service not available" });
                }

                results = await _imageUploadService.UploadImagesAsync(paramList);
            }
            catch (Exception ex)
            {
                return StatusCode(502, new { message = "Image upload failed", detail = ex.Message });
            }
            finally
            {
                foreach (var s in uploadList) try { s.Stream.Dispose(); } catch (Exception disposeEx) { _logger?.LogWarning(disposeEx, "Failed to dispose upload stream in finally"); }
            }

            var failed = results.FirstOrDefault(r => !r.Success);
            if (failed != null)
                return BadRequest(new { message = "Image upload failed", detail = failed.ErrorMessage });

            var fileData = results.Where(r => r.Success && !string.IsNullOrWhiteSpace(r.Url)).Select(r => (Url: r.Url!, PublicId: r.PublicId)).ToList();
            if (!fileData.Any()) return StatusCode(500, new { message = "No images uploaded" });

            var project = await _uow.Projects.QueryTracked()
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return NotFound();

            project.Images ??= new List<Domain.Entities.ProjectImage>();
            int idx = project.Images.Count + 1;
            foreach (var (url, publicId) in fileData)
            {
                project.Images.Add(new Domain.Entities.ProjectImage { ProjectId = id, Url = url, SortOrder = idx++, PublicId = publicId });
            }

            project.Image = fileData.First().Url;

            try
            {
                await _uow.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "DB persist failed after image upload for project {ProjectId}", id);
                foreach (var r in results.Where(rr => rr.Success && !string.IsNullOrWhiteSpace(rr.PublicId)))
                {
                    try { await _cloudinary.DeleteImageAsync(r.PublicId!); } catch (Exception delEx) { _logger?.LogWarning(delEx, "Failed to delete Cloudinary image {PublicId} after DB persist failure", r.PublicId); }
                }
                return StatusCode(500, new { message = "Failed to persist image URLs" });
            }

            var addedImages = project.Images
                .OrderByDescending(i => i.Id)
                .Take(fileData.Count)
                .Select(i => new { i.Id, i.Url })
                .Reverse()
                .ToList();

            try { _cache?.InvalidateByPrefix("projects_"); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_"); } catch (Exception cacheEx) { _logger?.LogWarning(cacheEx, "Cache invalidation failed for cache prefixes"); }

            return Ok(new { message = "Images uploaded successfully", files = addedImages });
        }

        // ADMIN: Delete an image from a project
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}/images/{imageId:int}")]
        public async Task<IActionResult> DeleteImage(int id, int imageId)
        {
            var project = await _uow.Projects.QueryTracked()
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return NotFound(new { message = "Project not found" });

            var img = project.Images?.FirstOrDefault(i => i.Id == imageId);
            if (img == null) return NotFound(new { message = "Image not found" });

            if (!string.IsNullOrWhiteSpace(img.PublicId))
            {
                try { await _cloudinary.DeleteImageAsync(img.PublicId); }
                catch (Exception ex) { _logger?.LogWarning(ex, "Failed to delete Cloudinary image {PublicId}", img.PublicId); }
            }

            project.Images?.Remove(img);
            project.UpdatedAt = DateTime.UtcNow;

            if (project.Image == img.Url)
            {
                project.Image = project.Images?.FirstOrDefault()?.Url;
            }

            await _uow.CommitAsync();

            try { _cache?.InvalidateByPrefix("projects_"); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_"); } catch (Exception cacheEx) { _logger?.LogWarning(cacheEx, "Cache invalidation failed for cache prefixes"); }
            return NoContent();
        }

        // ADMIN: Replace an image on a project
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}/images/{imageId:int}")]
        public async Task<IActionResult> ReplaceImage(int id, int imageId, IFormFile? image)
        {
            if (image == null || image.Length == 0)
                return BadRequest(new { message = "Image file is required" });

            if (image.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "File exceeds maximum allowed size of 5MB" });

            var contentType = image.ContentType ?? string.Empty;
            if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "File must be an image" });

            if (_imageUploadService == null)
                return StatusCode(500, new { message = "Image upload service not available" });

            List<CloudinaryUploadResult>? results;
            await using var stream = image.OpenReadStream();
            try
            {
                results = await _imageUploadService.UploadImagesAsync(new[] { (stream, image.FileName) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Replace image upload failed for project {ProjectId} image {ImageId}", id, imageId);
                return StatusCode(502, new { message = "Image upload failed", detail = ex.Message });
            }

            var result = results?.FirstOrDefault();
            if (result == null || !result.Success)
                return StatusCode(502, new { message = "Image upload failed", detail = result?.ErrorMessage ?? "Unknown" });

            var project = await _uow.Projects.QueryTracked()
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
            {
                if (!string.IsNullOrWhiteSpace(result.PublicId))
                    try { await _cloudinary.DeleteImageAsync(result.PublicId); } catch (Exception delEx) { _logger?.LogWarning(delEx, "Failed to delete Cloudinary image {PublicId}", result.PublicId); }
                return NotFound(new { message = "Project not found" });
            }

            var img = project.Images?.FirstOrDefault(i => i.Id == imageId);
            if (img == null)
            {
                if (!string.IsNullOrWhiteSpace(result.PublicId))
                    try { await _cloudinary.DeleteImageAsync(result.PublicId); } catch (Exception delEx) { _logger?.LogWarning(delEx, "Failed to delete Cloudinary image {PublicId}", result.PublicId); }
                return NotFound(new { message = "Image not found" });
            }

            if (!string.IsNullOrWhiteSpace(img.PublicId))
            {
                try { await _cloudinary.DeleteImageAsync(img.PublicId); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete old Cloudinary image {PublicId}", img.PublicId); }
            }

            img.Url = result.Url!;
            img.PublicId = result.PublicId;

            if (project.Image == img.Url) project.Image = result.Url;

            project.UpdatedAt = DateTime.UtcNow;
            await _uow.CommitAsync();

            _cache?.InvalidateByPrefix("projects_"); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_");
            return Ok(new { message = "Image replaced", url = result.Url, imageId = img.Id });
        }

        // ADMIN: Upload videos
        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/videos")]
        public async Task<IActionResult> UploadVideos(int id, [FromForm] List<IFormFile> videos)
        {
            if (videos == null || !videos.Any())
                return BadRequest(new { message = "No video files provided" });

            var uploadResults = new List<(CloudinaryUploadResult Result, string FileName)>();

            foreach (var file in videos)
            {
                if (file == null || file.Length == 0) continue;

                if (file.Length > 150 * 1024 * 1024)
                    return BadRequest(new { message = $"File {file.FileName} exceeds maximum allowed size of 150MB" });

                await using var stream = file.OpenReadStream();
                try
                {
                    var result = await _videoUploadService.UploadVideoAsync(stream, file.FileName, $"projects/{id}");
                    uploadResults.Add((result, file.FileName));
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Video upload failed for project {ProjectId} file {File}", id, file.FileName);
                    return StatusCode(502, new { message = $"Video upload failed for {file.FileName}", detail = ex.Message });
                }
            }

            var fileData = uploadResults
                .Where(r => r.Result.Success && !string.IsNullOrWhiteSpace(r.Result.Url))
                .Select(r => (Url: r.Result.Url!, PublicId: r.Result.PublicId, ThumbnailUrl: r.Result.ThumbnailUrl, FileName: (string?)r.FileName))
                .ToList();

            if (!fileData.Any())
                return StatusCode(500, new { message = "No videos uploaded" });

            try
            {
                var added = await _videoService.AddProjectVideosAsync(id, fileData);
                _cache?.InvalidateByPrefix("projects_"); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_");
                return Ok(new { message = "Videos uploaded successfully", files = added.Select(v => new { v.Id, v.Url, v.PublicId }).ToList() });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Project not found" });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to persist video URLs for project {ProjectId}", id);
                return StatusCode(500, new { message = "Failed to persist video URLs" });
            }
        }

        // ADMIN: Confirm a direct-browser-to-Cloudinary upload and attach to project
        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/videos/confirm")]
        public async Task<IActionResult> ConfirmVideoUpload(int id, [FromBody] ConfirmVideoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Url) || string.IsNullOrWhiteSpace(request?.PublicId))
                return BadRequest(new { message = "Url and PublicId are required" });

            try
            {
                var thumbnailUrl = request.ThumbnailUrl;
                if (string.IsNullOrWhiteSpace(thumbnailUrl) && request.Url.Contains("res.cloudinary.com"))
                {
                    thumbnailUrl = request.Url.Replace("/upload/", "/upload/so_2.0,q_auto:good,w_640,f_jpg/");
                }

                var fileData = new List<(string Url, string? PublicId, string? ThumbnailUrl, string? FileName)>
                {
                    (request.Url, request.PublicId, thumbnailUrl, request.FileName)
                };

                var added = await _videoService.AddProjectVideosAsync(id, fileData);
                _cache?.InvalidateByPrefix("projects_"); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_");
                return Ok(new { message = "Video attached successfully", files = added.Select(v => new { v.Id, v.Url, v.PublicId }).ToList() });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Project not found" });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to confirm video for project {ProjectId}", id);
                return StatusCode(500, new { message = "Failed to confirm video" });
            }
        }

        // ADMIN: Delete a video from a project
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}/videos/{videoId:int}")]
        public async Task<IActionResult> DeleteVideo(int id, int videoId)
        {
            try
            {
                var deleted = await _videoService.RemoveProjectVideoAsync(id, videoId);
                if (!deleted) return NotFound(new { message = "Project or video not found" });
                _cache?.InvalidateByPrefix("projects_"); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to delete video {VideoId} from project {ProjectId}", videoId, id);
                return StatusCode(500, new { message = "Failed to delete video" });
            }
        }
    }
}
