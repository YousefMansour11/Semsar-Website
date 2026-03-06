using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Domain.Enums;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("fixed")]
    public class PropertiesController : ControllerBase
    {
        private readonly IPropertyQueryService _queryHandler;
        private readonly IPropertyService _commandHandler;
        private readonly IUnitQueryService _unitQueryService;
        private readonly ILogger<PropertiesController> _logger;
        private readonly ICacheService _cache;
        private readonly IUnitOfWork _uow;
        private readonly ISlugService _slugService;
        private readonly IImageUploadService _imageUploadService;
        private readonly IVideoUploadService _videoUploadService;
        private readonly IVideoService _videoService;
        private readonly IClickBehaviorOptimizationService _clickBehavior;

        public PropertiesController(
            IPropertyQueryService queryHandler,
            IPropertyService commandHandler,
            IUnitQueryService unitQueryService,
            ILogger<PropertiesController> logger,
            ICacheService cache,
            IUnitOfWork uow,
            ISlugService slugService,
            IImageUploadService imageUploadService,
            IVideoUploadService videoUploadService,
            IVideoService videoService,
            IClickBehaviorOptimizationService clickBehavior)
        {
            _queryHandler = queryHandler;
            _commandHandler = commandHandler;
            _unitQueryService = unitQueryService;
            _logger = logger;
            _cache = cache;
            _uow = uow;
            _slugService = slugService;
            _imageUploadService = imageUploadService;
            _videoUploadService = videoUploadService;
            _videoService = videoService;
            _clickBehavior = clickBehavior;
        }

        // ADMIN: Create property
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePropertyDto? dto)
        {
            if ((dto == null || !ModelState.IsValid) && Request.HasFormContentType && Request.Form.ContainsKey("payload"))
            {
                try
                {
                    var json = Request.Form["payload"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(json))
                    {
                        dto = System.Text.Json.JsonSerializer.Deserialize<CreatePropertyDto>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (dto != null)
                            TryValidateModel(dto);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to deserialize multipart payload for create");
                }
            }

            if (!ModelState.IsValid || dto == null)
                return BadRequest(ModelState);

            try
            {
                var prop = await _commandHandler.CreateAsync(dto);
                _cache.InvalidateByPrefix("properties_"); _cache.InvalidateByPrefix("properties_location_"); _cache.InvalidateByPrefix("landing_");
                return CreatedAtAction(nameof(Get), new { id = prop.Id }, new PropertyCreatedResponse
                {
                    Id = prop.Id,
                    TitleEn = prop.TitleEn ?? string.Empty,
                    TitleAr = prop.TitleAr ?? string.Empty,
                    Price = prop.Price,
                    Code = prop.Code,
                    Location = prop.Location ?? string.Empty,
                    Slug = prop.Slug,
                    SeoTitle = prop.SeoTitle ?? string.Empty,
                    SeoDescription = prop.SeoDescription ?? string.Empty,
                    SeoTitleAr = prop.SeoTitleAr ?? string.Empty,
                    SeoDescriptionAr = prop.SeoDescriptionAr ?? string.Empty,
                    SeoKeywords = prop.SeoKeywords ?? string.Empty,
                    SeoKeywordsAr = prop.SeoKeywordsAr ?? string.Empty,
                    CanonicalUrl = prop.CanonicalUrl ?? string.Empty
                });
            }
            catch (ArgumentException aex)
            {
                return BadRequest(new { message = aex.Message });
            }
            catch (InvalidOperationException ioex)
            {
                return Conflict(new { message = ioex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create property failed");
                return StatusCode(500, new { message = "Failed to create property" });
            }
        }

        // PUBLIC: Get properties (lightweight list with pagination)
        [HttpGet]
        public async Task<IActionResult> GetAll(int page = 1, int pageSize = 20, string? listingType = null)
        {
            var pageNum = Math.Max(1, page);
            var pageSizeNum = Math.Clamp(pageSize, 1, 100);

            var cacheKey = $"properties_list_page_{pageNum}_size_{pageSizeNum}_listing_{listingType ?? ""}";
            var cachedObj = _cache.Get<object>(cacheKey);
            if (cachedObj != null)
                return Ok(cachedObj);

            var items = await _queryHandler.GetLatestCardsAsync(pageNum, pageSizeNum, listingType: listingType);
            var result = new { data = items, page = pageNum, pageSize = pageSizeNum };
            _cache.Set(cacheKey, result, TimeSpan.FromSeconds(30));
            _cache.RegisterKey(cacheKey);
            return Ok(result);
        }

        // PUBLIC: Get properties with filters
        [HttpGet("filter")]
        public async Task<IActionResult> GetFilter(
            decimal? minPrice, decimal? maxPrice, string? location, string? propertyType,
            string? listingType, int? projectId, string? locations, string? types,
            bool? isFeatured, bool? hasInstallment, double? minSize, double? maxSize,
            int page = 1, int pageSize = 12,
            string sortBy = "createdAt", string sortOrder = "desc")
        {
            var cacheKey = $"properties_page_{page}_size_{pageSize}_loc_{location ?? ""}_min_{minPrice?.ToString() ?? ""}_max_{maxPrice?.ToString() ?? ""}_type_{propertyType ?? ""}_listing_{listingType ?? ""}_isFeatured_{isFeatured?.ToString() ?? ""}_hasInstallment_{hasInstallment?.ToString() ?? ""}_minSize_{minSize?.ToString() ?? ""}_maxSize_{maxSize?.ToString() ?? ""}_sort_{sortBy}_{sortOrder}";

            var cachedResult = _cache.Get<object>(cacheKey);
            if (cachedResult != null)
                return Ok(cachedResult);

            var (data, total, pageOut, pageSizeOut, totalPages) = await _queryHandler.GetPublicAsync(
                minPrice, maxPrice, location, propertyType, listingType, locations, types,
                isFeatured, hasInstallment, minSize, maxSize, page, pageSize, sortBy, sortOrder);

            var list = data.Select(p => new PropertyListDto
            {
                Id = p.Id,
                ProjectId = p.ProjectId,
                PublicKey = p.PublicKey,
                Title = p.TitleEn ?? string.Empty,
                TitleAr = p.TitleAr ?? string.Empty,
                Type = p.PropertyType ?? string.Empty,
                Price = p.Price,
                RentPerMonth = p.RentPerMonth,
                Location = p.Location ?? string.Empty,
                LocationAr = p.LocationAr,
                PropertyCode = p.Code ?? p.Id.ToString(),
                Image = p.Images?.FirstOrDefault() ?? string.Empty,
                Images = p.Images ?? new List<string>(),
                Size = p.Size,
                Status = p.ListingType ?? string.Empty,
                Description = p.DescriptionEn ?? string.Empty,
                DescriptionAr = p.DescriptionAr ?? string.Empty,
                Currency = p.Currency ?? "EGP",
                Features = p.Features ?? new List<string>(),
                FeaturesAr = p.FeaturesAr ?? new List<string>(),
                ListingType = p.ListingType ?? string.Empty,
                IsFeatured = p.IsFeatured,
                SortOrder = p.SortOrder,
                Installment = p.Installments?.FirstOrDefault()
            }).ToList();

            var result = new { data = list, total, page = pageOut, pageSize = pageSizeOut, totalPages };
            _cache.Set(cacheKey, result, TimeSpan.FromSeconds(30));
            _cache.RegisterKey(cacheKey);
            return Ok(result);
        }

        // PUBLIC: Get filter metadata (locations, property types for dropdowns)
        [HttpGet("filter/metadata")]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<IActionResult> GetFilterMetadata()
        {
            var cacheKey = "properties_filter_metadata";
            var cached = _cache.Get<object>(cacheKey);
            if (cached != null)
                return Ok(cached);

            var distinctLocations = await _uow.Properties.Query()
                .Where(p => !string.IsNullOrEmpty(p.Location))
                .Select(p => p.Location.Trim())
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var distinctLocationsAr = await _uow.Properties.Query()
                .Where(p => !string.IsNullOrEmpty(p.LocationAr))
                .Select(p => p.LocationAr!.Trim())
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var distinctPropertyTypes = await _uow.Properties.Query()
                .Select(p => p.PropertyType)
                .Distinct()
                .ToListAsync();

            var distinctListingTypes = await _uow.Properties.Query()
                .Select(p => p.ListingType)
                .Distinct()
                .ToListAsync();

            var propertyTypes = distinctPropertyTypes
                .Select(e => new { value = e.ToString(), name = e.ToString() })
                .ToList();

            var listingTypes = distinctListingTypes
                .Select(e => new { value = e.ToString(), name = e.ToString() })
                .ToList();

            var result = new
            {
                locations = distinctLocations,
                locationsAr = distinctLocationsAr,
                propertyTypes,
                listingTypes
            };

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            _cache.RegisterKey(cacheKey);
            return Ok(result);
        }

        // ADMIN: Search by code (MUST come before {id} to avoid routing conflict)
        [Authorize(Roles = "Admin")]
        [HttpGet("search/code")]
        public async Task<IActionResult> SearchByCode([FromQuery] string code)
        {
            var dto = await _queryHandler.GetAdminByCodeAsync(code);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        // PUBLIC: Get property by slug (falls back to ID if numeric, then to Units)
        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var dto = await _queryHandler.GetPublicBySlugAsync(slug);
            if (dto != null)
            {
                try { _logger.LogInformation("Property view: {Id} at {Time}", dto.Id, DateTime.UtcNow); }
                catch (Exception ex) { _logger.LogDebug(ex, "Analytics logging failed for property view"); }
                _clickBehavior.RecordImpression($"/property/{slug}");
                return Ok(dto);
            }

            if (int.TryParse(slug, out var id))
            {
                dto = await _queryHandler.GetPublicByIdAsync(id);
                if (dto != null) return Ok(dto);
            }

            var unitDto = await _unitQueryService.GetBySlugAsync(slug);
            if (unitDto != null) return Ok(unitDto);

            return NotFound();
        }

        // PUBLIC: Get property by ID
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var dto = await _queryHandler.GetPublicByIdAsync(id);
            if (dto == null) return NotFound();
            try { await _commandHandler.IncrementViewCountAsync(id); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to increment view count for property {Id}", id); }
            return Ok(dto);
        }

        // PUBLIC: Get property by public key
        [HttpGet("public-key/{publicKey}")]
        public async Task<IActionResult> GetByPublicKey(string publicKey)
        {
            var dto = await _queryHandler.GetPublicByPublicKeyAsync(publicKey);
            if (dto == null) return NotFound();
            try { _logger.LogInformation("Property view (public-key): {PublicKey} at {Time}", publicKey, DateTime.UtcNow); }
            catch (Exception ex) { _logger.LogDebug(ex, "Analytics logging failed for property public-key view"); }
            _clickBehavior.RecordImpression($"/property/{dto.Slug}");
            return Ok(dto);
        }

        [HttpGet("location/{location}")]
        public async Task<IActionResult> ByLocation(string location, int page = 1, int pageSize = 12)
        {
            if (string.IsNullOrWhiteSpace(location))
                return BadRequest(new { message = "Location is required" });

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var cacheKey = $"properties_location_{location}_page_{page}_size_{pageSize}";
            var cachedResult = _cache.Get<object>(cacheKey);
            if (cachedResult != null)
                return Ok(cachedResult);

            var (data, total, p, ps, totalPages, seoTitle, seoDescription) = await _queryHandler.GetByLocationAsync(location, page, pageSize);

            var list = data.Select(pdto => new PropertyListDto
            {
                Id = pdto.Id,
                ProjectId = pdto.ProjectId,
                PublicKey = pdto.PublicKey,
                Title = pdto.TitleEn ?? string.Empty,
                Type = pdto.PropertyType ?? string.Empty,
                Price = pdto.Price,
                RentPerMonth = pdto.RentPerMonth,
                Location = pdto.Location ?? string.Empty,
                LocationAr = pdto.LocationAr,
                PropertyCode = pdto.Id.ToString(),
                Image = pdto.Images?.FirstOrDefault() ?? string.Empty,
                Images = pdto.Images ?? new List<string>(),
                Size = pdto.Size,
                Status = pdto.ListingType ?? string.Empty,
                Description = pdto.DescriptionEn ?? string.Empty,
                Features = pdto.Features ?? new List<string>(),
                ListingType = pdto.ListingType ?? string.Empty,
                IsFeatured = pdto.IsFeatured,
                SortOrder = pdto.SortOrder,
                Installment = pdto.Installments?.FirstOrDefault()
            }).ToList();

            var result = new { data = list, total, page = p, pageSize = ps, totalPages, seoTitle, seoDescription };
            _cache.Set(cacheKey, result, TimeSpan.FromSeconds(30));
            _cache.RegisterKey(cacheKey);
            return Ok(result);
        }

        // ADMIN: Get property by ID with admin details
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/{id}")]
        public async Task<IActionResult> GetAdmin(int id)
        {
            var dto = await _queryHandler.GetAdminByIdAsync(id);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        // ADMIN: Update property
        [Authorize(Roles = "Admin")]
        [HttpPatch("{id:int}")]
        public async Task<IActionResult> Patch(int id, [FromBody] PatchPropertyDto? dto)
        {
            if (dto == null || !ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var updated = await _commandHandler.PatchAsync(id, dto);
                _cache.InvalidateByPrefix("properties_"); _cache.InvalidateByPrefix("properties_location_"); _cache.InvalidateByPrefix("landing_");
                return Ok(new PropertyUpdatedResponse
                {
                    Id = updated.Id,
                    TitleEn = updated.TitleEn ?? string.Empty,
                    TitleAr = updated.TitleAr ?? string.Empty,
                    Price = updated.Price,
                    Code = updated.Code,
                    Location = updated.Location ?? string.Empty,
                    Slug = updated.Slug,
                    SeoTitle = updated.SeoTitle ?? string.Empty,
                    SeoDescription = updated.SeoDescription ?? string.Empty,
                    SeoTitleAr = updated.SeoTitleAr ?? string.Empty,
                    SeoDescriptionAr = updated.SeoDescriptionAr ?? string.Empty,
                    SeoKeywords = updated.SeoKeywords ?? string.Empty,
                    SeoKeywordsAr = updated.SeoKeywordsAr ?? string.Empty,
                    CanonicalUrl = updated.CanonicalUrl ?? string.Empty
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ioex) { return Conflict(new { message = ioex.Message }); }
            catch (ArgumentException aex) { return BadRequest(new { message = aex.Message }); }
            catch (SlugConflictException) { return Conflict(new { message = "Slug conflict - please retry" }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update property failed for {PropertyId}. InnerException: {Inner}", id, ex.InnerException?.Message);
                return StatusCode(500, new { message = "Failed to update property", detail = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // ADMIN: Delete property
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _commandHandler.DeleteAsync(id);
            if (!deleted) return NotFound();
            _cache.InvalidateByPrefix("properties_"); _cache.InvalidateByPrefix("properties_location_"); _cache.InvalidateByPrefix("landing_");
            return NoContent();
        }

        // ADMIN: Upload images
        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/images")]
        public async Task<IActionResult> UploadImages(int id, [FromForm] List<IFormFile> images)
        {
            if (images == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            if (!images.Any())
                return BadRequest(new { message = "No images were provided" });

            var uploadList = new List<(System.IO.Stream Stream, string FileName, string? PublicId)>();
            long maxBytes = 5 * 1024 * 1024;
            var env = Environment.GetEnvironmentVariable("MAX_UPLOAD_FILE_BYTES");
            if (!string.IsNullOrEmpty(env) && long.TryParse(env, out var parsed)) maxBytes = Math.Max(1024, parsed);

            // Validate total upload size and file count
            var maxFileCount = 20;
            if (images.Count > maxFileCount)
            {
                return BadRequest(new { message = $"Maximum {maxFileCount} files allowed per upload" });
            }

            long totalSize = 0;
            var maxTotalBytes = 100L * 1024 * 1024; // 100MB total limit
            foreach (var file in images)
            {
                if (file != null) totalSize += file.Length;
            }
            if (totalSize > maxTotalBytes)
            {
                return BadRequest(new { message = $"Total upload size exceeds maximum allowed size of {maxTotalBytes / (1024 * 1024)}MB" });
            }

            foreach (var file in images)
            {
                if (file == null || file.Length == 0) continue;
                if (file.Length > maxBytes)
                {
                    _logger?.LogWarning("Upload rejected: file too large. PropertyId={PropertyId} FileName={FileName} Length={Length}", id, file.FileName, file.Length);
                    return BadRequest(new { message = $"File {file.FileName} exceeds maximum allowed size of {maxBytes} bytes" });
                }

                var contentType = file.ContentType ?? string.Empty;
                if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.LogWarning("Upload rejected: invalid content-type. PropertyId={PropertyId} FileName={FileName} ContentType={ContentType}", id, file.FileName, contentType);
                    continue;
                }

                var stream = file.OpenReadStream();
                try
                {
                    if (!Application.Services.ImageValidation.ValidateImageHeader(stream, contentType, file.FileName))
                    {
                        _logger?.LogWarning("Upload rejected: failed header validation. PropertyId={PropertyId} FileName={FileName}", id, file.FileName);
                        stream.Dispose();
                        continue;
                    }
                    if (stream.CanSeek) stream.Position = 0;
                    uploadList.Add((stream, file.FileName, null));
                }
                catch (Exception ex)
                {
                    try { stream.Dispose(); } catch (Exception disposeEx) { _logger?.LogWarning(disposeEx, "Failed to dispose stream for property {PropertyId}", id); }
                    _logger?.LogWarning(ex, "Upload validation failed for property {PropertyId} file {File}", id, file.FileName);
                    continue;
                }
            }

            if (!uploadList.Any()) return BadRequest(new { message = "No valid image files provided" });

            List<CloudinaryUploadResult>? results = null;
            try
            {
                var paramList = uploadList.Select(x => (x.Stream, x.FileName)).ToList();
                results = await _imageUploadService.UploadImagesAsync(paramList);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ImageUploadService exception for property {PropertyId}", id);
                return StatusCode(502, new { message = "Image upload failed", detail = ex.Message });
            }
            finally
            {
                foreach (var s in uploadList) try { s.Stream.Dispose(); } catch (Exception disposeEx) { _logger?.LogDebug(disposeEx, "Failed to dispose upload stream"); }
            }

            var failed = results.FirstOrDefault(r => !r.Success);
            if (failed != null)
            {
                _logger?.LogError("ImageUploadService reported failed upload for property {PropertyId} File={File} Error={Error}", id, failed.PublicId ?? "unknown", failed.ErrorMessage);
                return BadRequest(new { message = "Image upload failed", detail = failed.ErrorMessage });
            }

            var fileData = results.Where(r => r.Success && !string.IsNullOrWhiteSpace(r.Url)).Select(r => (Url: r.Url!, PublicId: r.PublicId)).ToList();
            if (!fileData.Any()) return StatusCode(500, new { message = "No images uploaded" });

            List<(int Id, string Url, string? PublicId)> addedImages;
            try
            {
                addedImages = await _commandHandler.AddImagesAsync(id, fileData);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "DB persist failed after image upload for property {PropertyId}", id);
                await CleanupOrphanedUploadsAsync(results.Where(r => r.Success && !string.IsNullOrWhiteSpace(r.PublicId)), id);
                return StatusCode(500, new { message = "Failed to persist image URLs" });
            }

            _cache.InvalidateByPrefix("properties_"); _cache.InvalidateByPrefix("properties_location_"); _cache.InvalidateByPrefix("landing_");
            return Ok(new { message = "Images uploaded successfully", files = addedImages.Select(f => new { f.Id, f.Url }).ToList() });
        }

        // ADMIN: Delete an image from a property
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}/images/{imageId:int}")]
        public async Task<IActionResult> DeleteImage(int id, int imageId)
        {
            var deleted = await _commandHandler.RemoveImageAsync(id, imageId);
            if (!deleted) return NotFound(new { message = "Property or image not found" });
            _cache.InvalidateByPrefix("properties_"); _cache.InvalidateByPrefix("properties_location_"); _cache.InvalidateByPrefix("landing_");
            return NoContent();
        }

        // ADMIN: Replace an image on a property (upload new, delete old)
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

            List<CloudinaryUploadResult>? results = null;
            await using var stream = image.OpenReadStream();
            try
            {
                results = await _imageUploadService.UploadImagesAsync(new[] { (stream, image.FileName) });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Replace image upload failed for property {PropertyId} image {ImageId}", id, imageId);
                return StatusCode(502, new { message = "Image upload failed", detail = ex.Message });
            }

            var result = results?.FirstOrDefault();
            if (result == null || !result.Success)
            {
                var err = result?.ErrorMessage ?? "Unknown error";
                return StatusCode(502, new { message = "Image upload failed", detail = err });
            }

            try
            {
                await _commandHandler.ReplaceImageAsync(id, imageId, result.Url!, result.PublicId);
            }
            catch (KeyNotFoundException)
            {
                // Upload succeeded but property/image doesn't exist — clean up orphan
                if (!string.IsNullOrWhiteSpace(result.PublicId))
                    try { await _imageUploadService.DeleteImageAsync(result.PublicId); } catch (Exception delEx) { _logger?.LogWarning(delEx, "Failed to delete orphaned Cloudinary image after replace failure"); }
                return NotFound(new { message = "Property or image not found" });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Replace image DB persist failed for property {PropertyId} image {ImageId}", id, imageId);
                return StatusCode(500, new { message = "Failed to replace image" });
            }

            _cache.InvalidateByPrefix("properties_"); _cache.InvalidateByPrefix("properties_location_"); _cache.InvalidateByPrefix("landing_");
            return Ok(new { message = "Image replaced", url = result.Url });
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
                    var result = await _videoUploadService.UploadVideoAsync(stream, file.FileName, $"properties/{id}");
                    uploadResults.Add((result, file.FileName));
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Video upload failed for property {PropertyId} file {File}", id, file.FileName);
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
                var added = await _videoService.AddPropertyVideosAsync(id, fileData);
                _cache.InvalidateByPrefix("properties_"); _cache.InvalidateByPrefix("properties_location_"); _cache.InvalidateByPrefix("landing_");
                return Ok(new { message = "Videos uploaded successfully", files = added.Select(v => new { v.Id, v.Url, v.PublicId }).ToList() });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Property not found" });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to persist video URLs for property {PropertyId}", id);
                return StatusCode(500, new { message = "Failed to persist video URLs" });
            }
        }

        // ADMIN: Confirm a direct-browser-to-Cloudinary upload and attach to property
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

                var added = await _videoService.AddPropertyVideosAsync(id, fileData);
                _cache.InvalidateByPrefix("properties_"); _cache.InvalidateByPrefix("properties_location_"); _cache.InvalidateByPrefix("landing_");
                return Ok(new { message = "Video attached successfully", files = added.Select(v => new { v.Id, v.Url, v.PublicId }).ToList() });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Property not found" });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to confirm video for property {PropertyId}", id);
                return StatusCode(500, new { message = "Failed to confirm video" });
            }
        }

        private async Task CleanupOrphanedUploadsAsync(IEnumerable<CloudinaryUploadResult> failedUploads, int propertyId)
        {
            foreach (var r in failedUploads)
            {
                try
                {
                    var deleted = await _imageUploadService.DeleteImageAsync(r.PublicId!);
                    if (!deleted)
                    {
                        _logger?.LogWarning("Failed to delete orphaned Cloudinary asset {PublicId} for property {PropertyId}", r.PublicId, propertyId);
                        await EnqueueOrphanedUploadAsync(r.PublicId!, null);
                    }
                }
                catch (Exception delEx)
                {
                    _logger?.LogWarning(delEx, "Exception deleting orphaned Cloudinary asset {PublicId} for property {PropertyId}", r.PublicId, propertyId);
                    await EnqueueOrphanedUploadAsync(r.PublicId!, delEx.Message);
                }
            }
        }

        private async Task EnqueueOrphanedUploadAsync(string publicId, string? errorMessage)
        {
            IDbContextTransaction? tx = null;
            try
            {
                tx = await _uow.BeginTransactionAsync();
                await _uow.OrphanedUploads.AddAsync(new Domain.Entities.OrphanedUpload
                {
                    PublicId = publicId,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Pending",
                    ErrorMessage = errorMessage
                });
                await _uow.CommitAsync();
                await tx.CommitAsync();
            }
            catch (Exception exTx)
            {
                _logger?.LogWarning(exTx, "Failed to enqueue orphaned upload PublicId={PublicId}", publicId);
                if (tx != null)
                {
                    try { await tx.RollbackAsync(); } catch (Exception rbEx) { _logger?.LogWarning(rbEx, "Rollback failed PublicId={PublicId}", publicId); }
                }
            }
        }

        // ADMIN: Delete a video from a property
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}/videos/{videoId:int}")]
        public async Task<IActionResult> DeleteVideo(int id, int videoId)
        {
            try
            {
                var deleted = await _videoService.RemovePropertyVideoAsync(id, videoId);
                if (!deleted) return NotFound(new { message = "Property or video not found" });
                _cache.InvalidateByPrefix("properties_"); _cache.InvalidateByPrefix("properties_location_"); _cache.InvalidateByPrefix("landing_");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to delete video {VideoId} from property {PropertyId}", videoId, id);
                return StatusCode(500, new { message = "Failed to delete video" });
            }
        }
    }
}
