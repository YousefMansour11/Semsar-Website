using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
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
    public class UnitsController : ControllerBase
    {
        private readonly Application.Interfaces.IUnitQueryService _queryService;
        private readonly Application.Interfaces.IUnitService _unitService;
        private readonly IUnitOfWork _uow;
        private readonly Application.Interfaces.ICacheService _cache;
        private readonly Application.Interfaces.ICloudinaryService _cloudinary;
        private readonly Application.Interfaces.IImageUploadService? _imageUploadService;
        private readonly Application.Interfaces.IVideoUploadService _videoUploadService;
        private readonly Application.Interfaces.IVideoService _videoService;
        private readonly Microsoft.Extensions.Logging.ILogger<UnitsController>? _logger;

        public UnitsController(
            Application.Interfaces.IUnitQueryService queryService,
            Application.Interfaces.IUnitService unitService,
            IUnitOfWork uow,
            Application.Interfaces.ICacheService cache,
            Application.Interfaces.ICloudinaryService cloudinary,
            Application.Interfaces.IImageUploadService? imageUploadService = null,
            Application.Interfaces.IVideoUploadService videoUploadService = null!,
            Application.Interfaces.IVideoService videoService = null!,
            Microsoft.Extensions.Logging.ILogger<UnitsController>? logger = null)
        {
            _queryService = queryService;
            _unitService = unitService;
            _uow = uow;
            _cache = cache;
            _cloudinary = cloudinary;
            _imageUploadService = imageUploadService;
            _videoUploadService = videoUploadService;
            _videoService = videoService;
            _logger = logger;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUnitDto? dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var unit = await _unitService.CreateAsync(dto);
                _cache?.InvalidateByPrefix(CacheKeys.UnitsListPrefix);
                _cache?.InvalidateByPrefix("projects_"); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_");
                return CreatedAtAction(nameof(GetByIdAdmin), new { id = unit.Id }, new
                {
                    Id = unit.Id,
                    TitleEn = unit.TitleEn,
                    TitleAr = unit.TitleAr,
                    MinPrice = unit.MinPrice,
                    MaxPrice = unit.MaxPrice,
                    Code = unit.Code,
                    Location = unit.Location,
                    Slug = unit.Slug,
                    SeoTitle = unit.SeoTitle,
                    SeoDescription = unit.SeoDescription,
                    SeoTitleAr = unit.SeoTitleAr,
                    SeoDescriptionAr = unit.SeoDescriptionAr,
                    SeoKeywords = unit.SeoKeywords,
                    SeoKeywordsAr = unit.SeoKeywordsAr,
                    CanonicalUrl = unit.CanonicalUrl
                });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (SlugConflictException)
            {
                return Conflict(new { message = "Slug conflict - please retry" });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Create unit failed for project {ProjectId}", dto.ProjectId);
                return StatusCode(500, new { message = "Failed to create unit" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(int? projectId, int page = 1, int pageSize = 20)
        {
            var pageNum = Math.Max(1, page);
            var pageSizeNum = Math.Clamp(pageSize, 1, 100);

            var cacheKey = $"units_list_pid_{projectId?.ToString() ?? "all"}_page_{pageNum}_size_{pageSizeNum}";
            var cached = _cache.Get<IEnumerable<object>>(cacheKey);
            if (cached != null)
                return Ok(cached);

            var (data, total) = await _queryService.GetPublicCardsPagedAsync(projectId, pageNum, pageSizeNum);
            var result = new { total, page = pageNum, pageSize = pageSizeNum, data };
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(3));
            _cache.RegisterKey(cacheKey);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("search/code")]
        public async Task<IActionResult> SearchByCode([FromQuery] string code)
        {
            var unit = await _queryService.GetByCodeAsync(code);
            if (unit == null) return NotFound();

            var dto = await _queryService.GetByIdAsync(unit.Id);
            if (dto != null) return Ok(dto);

            return Ok(new
            {
                unit.Id,
                unit.Code,
                unit.TitleEn,
                unit.MinPrice,
                unit.MaxPrice,
                unit.Location,
                unit.PropertyType,
                unit.ListingType
            });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var dto = await _queryService.GetPublicByIdAsync(id);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        [HttpGet("public-key/{publicKey}")]
        public async Task<IActionResult> GetByPublicKey(string publicKey)
        {
            var dto = await _queryService.GetPublicByPublicKeyAsync(publicKey);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/{id:int}")]
        public async Task<IActionResult> GetByIdAdmin(int id)
        {
            var dto = await _queryService.GetByIdAsync(id);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var dto = await _queryService.GetBySlugAsync(slug);
            if (dto != null) return Ok(dto);

            if (int.TryParse(slug, out var id))
            {
                dto = await _queryService.GetPublicByIdAsync(id);
                if (dto != null) return Ok(dto);
            }

            return NotFound();
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id:int}")]
        public async Task<IActionResult> Patch(int id, [FromBody] PatchUnitDto? dto)
        {
            if (dto == null || !ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var updated = await _unitService.PatchAsync(id, dto);
                _cache?.InvalidateByPrefix(CacheKeys.UnitsListPrefix);
                _cache?.InvalidateByPrefix("projects_"); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_");
                return Ok(new
                {
                    Id = updated.Id,
                    TitleEn = updated.TitleEn,
                    MinPrice = updated.MinPrice,
                    MaxPrice = updated.MaxPrice,
                    Code = updated.Code,
                    Location = updated.Location,
                    Slug = updated.Slug,
                    CanonicalUrl = updated.CanonicalUrl
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ioex) { return Conflict(new { message = ioex.Message }); }
            catch (SlugConflictException) { return Conflict(new { message = "Slug conflict - please retry" }); }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Patch unit failed for {UnitId}. InnerException: {Inner}", id, ex.InnerException?.Message);
                return StatusCode(500, new { message = "Failed to update unit", detail = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _unitService.DeleteAsync(id);
            if (!deleted) return NotFound();

            _cache?.InvalidateByPrefix(CacheKeys.UnitsListPrefix);
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
            var envMax = Environment.GetEnvironmentVariable("MAX_UPLOAD_FILE_BYTES");
            if (!string.IsNullOrEmpty(envMax) && long.TryParse(envMax, out var parsedMax)) maxBytes = Math.Max(1024, parsedMax);

            foreach (var file in images)
            {
                if (file == null || file.Length == 0) continue;
                if (file.Length > maxBytes)
                {
                    return BadRequest(new { message = $"File {file.FileName} exceeds maximum size of {maxBytes} bytes" });
                }

                var contentType = file.ContentType ?? string.Empty;
                if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

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
                    try { stream.Dispose(); } catch (Exception disposeEx) { _logger?.LogWarning(disposeEx, "Failed to dispose stream for unit {UnitId}", id); }
                    _logger?.LogWarning(ex, "Upload validation failed for unit {UnitId} file {File}", id, file.FileName);
                    continue;
                }
            }

            if (!uploadList.Any()) return BadRequest(new { message = "No valid image files provided" });

            List<Application.DTOs.CloudinaryUploadResult> results;
            try
            {
                var paramList = uploadList.Select(x => (x.Stream, x.FileName)).ToList();
                if (_imageUploadService == null)
                {
                    foreach (var s in uploadList) try { s.Stream.Dispose(); } catch (Exception ex) { _logger?.LogDebug(ex, "Failed to dispose upload stream for unit {UnitId}", id); }
                    _logger?.LogError("ImageUploadService not available for unit {UnitId}", id);
                    return StatusCode(500, new { message = "Image upload service not available" });
                }

                results = await _imageUploadService.UploadImagesAsync(paramList);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ImageUploadService exception for unit {UnitId}", id);
                foreach (var s in uploadList) try { s.Stream.Dispose(); } catch (Exception disposeEx) { _logger?.LogDebug(disposeEx, "Failed to dispose upload stream for unit {UnitId}", id); }
                return StatusCode(502, new { message = "Image upload failed", detail = ex.Message });
            }

            foreach (var s in uploadList) try { s.Stream.Dispose(); } catch (Exception disposeEx) { _logger?.LogDebug(disposeEx, "Failed to dispose upload stream for unit {UnitId}", id); }

            var failed = results.FirstOrDefault(r => !r.Success);
            if (failed != null)
            {
                _logger?.LogError("Cloudinary reported failed upload for unit {UnitId} File={File} Error={Error}", id, failed.PublicId ?? "unknown", failed.ErrorMessage);
                return BadRequest(new { message = "Image upload failed", detail = failed.ErrorMessage });
            }

            var urls = results.Where(r => r.Success && !string.IsNullOrWhiteSpace(r.Url)).Select(r => r.Url!).ToList();
            if (!urls.Any()) return StatusCode(500, new { message = "No images uploaded" });

            var unit = await _uow.Units.QueryTracked().FirstOrDefaultAsync(u => u.Id == id);
            if (unit == null) return NotFound();
            unit.Images ??= new List<Domain.Entities.UnitImage>();
            int idx = unit.Images.Count + 1;
            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                var url = r.Url ?? urls.ElementAtOrDefault(i) ?? string.Empty;
                unit.Images.Add(new Domain.Entities.UnitImage { UnitId = id, Url = url, SortOrder = idx++, PublicId = r.PublicId });
            }

            _uow.Units.Update(unit);
            try
            {
                await _uow.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "DB persist failed after Cloudinary upload for unit {UnitId}", id);
                foreach (var r in results.Where(rr => rr.Success && !string.IsNullOrWhiteSpace(rr.PublicId)))
                {
                    try { await _cloudinary.DeleteImageAsync(r.PublicId!); } catch (Exception delEx) { _logger?.LogWarning(delEx, "Failed to delete Cloudinary image {PublicId} after DB persist failure", r.PublicId); }
                }
                return StatusCode(500, new { message = "Failed to persist image URLs" });
            }

            // Capture assigned IDs after commit
            var addedImageIds = unit.Images
                .OrderByDescending(i => i.Id)
                .Take(results.Count)
                .Select(i => new { i.Id, i.Url })
                .Reverse()
                .ToList();

            try
            {
                _cache?.InvalidateByPrefix("projects_");
                _cache?.InvalidateByPrefix(CacheKeys.UnitsListPrefix);
                _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Cache invalidation failed after uploading images for unit {UnitId}", id);
            }

            return Ok(new { message = "Images uploaded", files = addedImageIds });
        }

        // ADMIN: Delete an image from a unit
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}/images/{imageId:int}")]
        public async Task<IActionResult> DeleteImage(int id, int imageId)
        {
            var unit = await _uow.Units.QueryTracked()
                .Include(u => u.Images)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (unit == null) return NotFound(new { message = "Unit not found" });

            var img = unit.Images?.FirstOrDefault(i => i.Id == imageId);
            if (img == null) return NotFound(new { message = "Image not found" });

            if (!string.IsNullOrWhiteSpace(img.PublicId))
            {
                try { await _cloudinary.DeleteImageAsync(img.PublicId); }
                catch (Exception ex) { _logger?.LogWarning(ex, "Failed to delete Cloudinary image {PublicId}", img.PublicId); }
            }

            unit.Images?.Remove(img);
            _uow.Units.Update(unit);
            await _uow.CommitAsync();

            _cache?.InvalidateByPrefix("projects_"); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_");
            _cache?.InvalidateByPrefix(CacheKeys.UnitsListPrefix);
            return NoContent();
        }

        // ADMIN: Replace an image on a unit
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
                _logger?.LogError(ex, "Replace image upload failed for unit {UnitId} image {ImageId}", id, imageId);
                return StatusCode(502, new { message = "Image upload failed", detail = ex.Message });
            }

            var result = results?.FirstOrDefault();
            if (result == null || !result.Success)
                return StatusCode(502, new { message = "Image upload failed", detail = result?.ErrorMessage ?? "Unknown" });

            var unit = await _uow.Units.QueryTracked()
                .Include(u => u.Images)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (unit == null)
            {
                if (!string.IsNullOrWhiteSpace(result.PublicId))
                    try { await _cloudinary.DeleteImageAsync(result.PublicId); } catch (Exception delEx) { _logger?.LogWarning(delEx, "Failed to delete Cloudinary image {PublicId} after unit not found", result.PublicId); }
                return NotFound(new { message = "Unit not found" });
            }

            var img = unit.Images?.FirstOrDefault(i => i.Id == imageId);
            if (img == null)
            {
                if (!string.IsNullOrWhiteSpace(result.PublicId))
                    try { await _cloudinary.DeleteImageAsync(result.PublicId); } catch (Exception delEx) { _logger?.LogWarning(delEx, "Failed to delete Cloudinary image {PublicId} after image not found", result.PublicId); }
                return NotFound(new { message = "Image not found" });
            }

            if (!string.IsNullOrWhiteSpace(img.PublicId))
            {
                try { await _cloudinary.DeleteImageAsync(img.PublicId); }
                catch (Exception ex) { _logger?.LogWarning(ex, "Failed to delete old Cloudinary image {PublicId}", img.PublicId); }
            }

            img.Url = result.Url!;
            img.PublicId = result.PublicId;
            await _uow.CommitAsync();

            _cache?.InvalidateByPrefix("projects_"); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_");
            _cache?.InvalidateByPrefix(CacheKeys.UnitsListPrefix);
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
                    var result = await _videoUploadService.UploadVideoAsync(stream, file.FileName, $"units/{id}");
                    uploadResults.Add((result, file.FileName));
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Video upload failed for unit {UnitId} file {File}", id, file.FileName);
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
                var added = await _videoService.AddUnitVideosAsync(id, fileData);
                _cache?.InvalidateByPrefix(CacheKeys.UnitsListPrefix);
                _cache?.InvalidateByPrefix("projects_"); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_");
                return Ok(new { message = "Videos uploaded successfully", files = added.Select(v => new { v.Id, v.Url, v.PublicId }).ToList() });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Unit not found" });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to persist video URLs for unit {UnitId}", id);
                return StatusCode(500, new { message = "Failed to persist video URLs" });
            }
        }

        // ADMIN: Confirm a direct-browser-to-Cloudinary upload and attach to unit
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

                var added = await _videoService.AddUnitVideosAsync(id, fileData);
                _cache?.InvalidateByPrefix(CacheKeys.UnitsListPrefix);
                _cache?.InvalidateByPrefix("projects_"); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_");
                return Ok(new { message = "Video attached successfully", files = added.Select(v => new { v.Id, v.Url, v.PublicId }).ToList() });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Unit not found" });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to confirm video for unit {UnitId}", id);
                return StatusCode(500, new { message = "Failed to confirm video" });
            }
        }

        // ADMIN: Delete a video from a unit
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}/videos/{videoId:int}")]
        public async Task<IActionResult> DeleteVideo(int id, int videoId)
        {
            try
            {
                var deleted = await _videoService.RemoveUnitVideoAsync(id, videoId);
                if (!deleted) return NotFound(new { message = "Unit or video not found" });
                _cache?.InvalidateByPrefix(CacheKeys.UnitsListPrefix);
                _cache?.InvalidateByPrefix("projects_"); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to delete video {VideoId} from unit {UnitId}", videoId, id);
                return StatusCode(500, new { message = "Failed to delete video" });
            }
        }

        // PUBLIC: Get variants for a unit
        [HttpGet("{unitId:int}/variants")]
        public async Task<IActionResult> GetVariants(int unitId)
        {
            var variants = await _uow.UnitVariants.Query()
                .Where(v => v.UnitId == unitId && !v.IsDeleted && v.IsActive)
                .OrderBy(v => v.SortOrder)
                .Select(v => new UnitVariantDto
                {
                    Id = v.Id,
                    PublicKey = v.PublicKey,
                    Name = v.Name,
                    Size = v.Size,
                    Price = v.Price,
                    Currency = v.Currency,
                    RentPerMonth = v.RentPerMonth,
                    Bedrooms = v.Bedrooms,
                    Bathrooms = v.Bathrooms,
                    Floor = v.Floor,
                    IsFurnished = v.IsFurnished,
                    View = v.View.ToString(),
                    UnitNumber = v.UnitNumber,
                    BuildingNumber = v.BuildingNumber,
                    DeliveryDate = v.DeliveryDate,
                    FinishingType = v.FinishingType.HasValue ? v.FinishingType.ToString() : null,
                    HasBalcony = v.HasBalcony,
                    HasParking = v.HasParking,
                    FloorPlanUrl = v.FloorPlanUrl,
                    AvailabilityStatus = v.AvailabilityStatus,
                    SortOrder = v.SortOrder,
                    IsActive = v.IsActive,
                    IsFeatured = v.IsFeatured,
                    IsRecommended = v.IsRecommended,
                    DeliveryText = v.DeliveryText,
                    DeliveryTextAr = v.DeliveryTextAr
                })
                .ToListAsync();

            return Ok(variants);
        }

        // PUBLIC: Calculate financing for a specific variant + installment plan
        [HttpGet("{unitId:int}/financing")]
        public async Task<IActionResult> GetFinancing(int unitId, [FromQuery] int variantId, [FromQuery] int planId)
        {
            var variant = await _uow.UnitVariants.Query()
                .FirstOrDefaultAsync(v => v.Id == variantId && v.UnitId == unitId && !v.IsDeleted && v.IsActive);
            if (variant == null)
                return NotFound(new { message = "Variant not found" });

            var plan = await _uow.UnitInstallmentPlans.Query()
                .FirstOrDefaultAsync(p => p.Id == planId && p.UnitId == unitId && !p.IsDeleted && p.IsEnabled);
            if (plan == null)
                return NotFound(new { message = "Installment plan not found" });

            if (plan.PaymentType == PaymentType.Cash)
            {
                return Ok(new FinancingResultDto
                {
                    VariantPrice = variant.Price,
                    Currency = variant.Currency ?? "EGP",
                    DownPaymentPercent = 100,
                    Years = 0,
                    DownPaymentAmount = variant.Price,
                    RemainingAmount = 0,
                    MonthlyInstallment = 0
                });
            }

            var downPaymentAmount = variant.Price * plan.DownPaymentPercent / 100m;
            var remainingAmount = variant.Price - downPaymentAmount;
            var monthlyInstallment = plan.Years > 0 ? remainingAmount / (plan.Years * 12) : 0;

            return Ok(new FinancingResultDto
            {
                VariantPrice = variant.Price,
                Currency = variant.Currency ?? "EGP",
                DownPaymentPercent = plan.DownPaymentPercent,
                Years = plan.Years,
                DownPaymentAmount = Math.Round(downPaymentAmount, 2),
                RemainingAmount = Math.Round(remainingAmount, 2),
                MonthlyInstallment = Math.Round(monthlyInstallment, 2)
            });
        }

        // ADMIN: Get all variants for a unit (including inactive)
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/{unitId:int}/variants")]
        public async Task<IActionResult> GetVariantsAdmin(int unitId)
        {
            var variants = await _uow.UnitVariants.Query()
                .IgnoreQueryFilters()
                .Where(v => v.UnitId == unitId && !v.IsDeleted)
                .OrderBy(v => v.SortOrder)
                .Select(v => new UnitVariantDto
                {
                    Id = v.Id,
                    PublicKey = v.PublicKey,
                    Name = v.Name,
                    Size = v.Size,
                    Price = v.Price,
                    Currency = v.Currency,
                    RentPerMonth = v.RentPerMonth,
                    Bedrooms = v.Bedrooms,
                    Bathrooms = v.Bathrooms,
                    Floor = v.Floor,
                    IsFurnished = v.IsFurnished,
                    View = v.View.ToString(),
                    UnitNumber = v.UnitNumber,
                    BuildingNumber = v.BuildingNumber,
                    DeliveryDate = v.DeliveryDate,
                    FinishingType = v.FinishingType.HasValue ? v.FinishingType.ToString() : null,
                    HasBalcony = v.HasBalcony,
                    HasParking = v.HasParking,
                    FloorPlanUrl = v.FloorPlanUrl,
                    AvailabilityStatus = v.AvailabilityStatus,
                    SortOrder = v.SortOrder,
                    IsActive = v.IsActive,
                    IsFeatured = v.IsFeatured,
                    IsRecommended = v.IsRecommended,
                    DeliveryText = v.DeliveryText,
                    DeliveryTextAr = v.DeliveryTextAr
                })
                .ToListAsync();

            return Ok(variants);
        }

        // ADMIN: Create a variant for a unit
        [Authorize(Roles = "Admin")]
        [HttpPost("admin/{unitId:int}/variants")]
        public async Task<IActionResult> CreateVariant(int unitId, [FromBody] CreateUnitVariantDto? dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Variant name is required" });

            var unit = await _uow.Units.Query()
                .FirstOrDefaultAsync(u => u.Id == unitId && !u.IsDeleted);
            if (unit == null) return NotFound(new { message = "Unit not found" });

            var variant = new UnitVariant
            {
                UnitId = unitId,
                PublicKey = $"UV-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                Name = dto.Name,
                Size = dto.Size,
                Price = dto.Price,
                Currency = dto.Currency ?? "EGP",
                RentPerMonth = dto.RentPerMonth,
                Bedrooms = dto.Bedrooms,
                Bathrooms = dto.Bathrooms,
                Floor = dto.Floor,
                IsFurnished = dto.IsFurnished,
                View = !string.IsNullOrWhiteSpace(dto.View) && Enum.TryParse<Domain.Enums.PropertyView>(dto.View.Replace(" ", "").Replace("&", ""), true, out var parsedView) ? parsedView : Domain.Enums.PropertyView.Unknown,
                UnitNumber = dto.UnitNumber,
                BuildingNumber = dto.BuildingNumber,
                DeliveryDate = dto.DeliveryDate,
                FinishingType = !string.IsNullOrWhiteSpace(dto.FinishingType) && Enum.TryParse<Domain.Enums.FinishingType>(dto.FinishingType, true, out var parsedFt) ? parsedFt : null,
                HasBalcony = dto.HasBalcony,
                HasParking = dto.HasParking,
                FloorPlanUrl = dto.FloorPlanUrl,
                AvailabilityStatus = dto.AvailabilityStatus ?? "Available",
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive,
                IsFeatured = dto.IsFeatured ?? false,
                IsRecommended = dto.IsRecommended ?? false,
                DeliveryText = dto.DeliveryText,
                DeliveryTextAr = dto.DeliveryTextAr
            };
            await _uow.UnitVariants.AddAsync(variant);
            await _uow.CommitAsync();

            _cache?.InvalidateByPrefix(CacheKeys.UnitsListPrefix);
            _cache?.InvalidateByPrefix("projects_"); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_");

            return CreatedAtAction(nameof(GetVariantsAdmin), new { unitId }, new UnitVariantDto
            {
                Id = variant.Id,
                PublicKey = variant.PublicKey,
                Name = variant.Name,
                Size = variant.Size,
                Price = variant.Price,
                Currency = variant.Currency,
                RentPerMonth = variant.RentPerMonth,
                Bedrooms = variant.Bedrooms,
                Bathrooms = variant.Bathrooms,
                Floor = variant.Floor,
                IsFurnished = variant.IsFurnished,
                View = variant.View.ToString(),
                UnitNumber = variant.UnitNumber,
                BuildingNumber = variant.BuildingNumber,
                DeliveryDate = variant.DeliveryDate,
                FinishingType = variant.FinishingType?.ToString(),
                HasBalcony = variant.HasBalcony,
                HasParking = variant.HasParking,
                FloorPlanUrl = variant.FloorPlanUrl,
                AvailabilityStatus = variant.AvailabilityStatus,
                SortOrder = variant.SortOrder,
                IsActive = variant.IsActive,
                IsFeatured = variant.IsFeatured,
                IsRecommended = variant.IsRecommended,
                DeliveryText = variant.DeliveryText,
                DeliveryTextAr = variant.DeliveryTextAr
            });
        }

        // ADMIN: Update a variant
        [Authorize(Roles = "Admin")]
        [HttpPut("admin/{unitId:int}/variants/{variantId:int}")]
        public async Task<IActionResult> UpdateVariant(int unitId, int variantId, [FromBody] UpdateUnitVariantDto? dto)
        {
            if (dto == null) return BadRequest(new { message = "Request body is required" });

            var variant = await _uow.UnitVariants.QueryTracked()
                .FirstOrDefaultAsync(v => v.Id == variantId && v.UnitId == unitId && !v.IsDeleted);
            if (variant == null) return NotFound(new { message = "Variant not found" });

            if (dto.Name != null) variant.Name = dto.Name;
            if (dto.Size.HasValue) variant.Size = dto.Size.Value;
            if (dto.Price.HasValue) variant.Price = dto.Price.Value;
            if (dto.Currency != null) variant.Currency = dto.Currency;
            if (dto.RentPerMonth.HasValue) variant.RentPerMonth = dto.RentPerMonth;
            if (dto.Bedrooms.HasValue) variant.Bedrooms = dto.Bedrooms.Value;
            if (dto.Bathrooms.HasValue) variant.Bathrooms = dto.Bathrooms.Value;
            if (dto.Floor.HasValue) variant.Floor = dto.Floor;
            if (dto.IsFurnished.HasValue) variant.IsFurnished = dto.IsFurnished.Value;
            if (dto.View != null) variant.View = Enum.TryParse<Domain.Enums.PropertyView>(dto.View.Replace(" ", "").Replace("&", ""), true, out var parsedView) ? parsedView : Domain.Enums.PropertyView.Unknown;
            if (dto.UnitNumber != null) variant.UnitNumber = string.IsNullOrWhiteSpace(dto.UnitNumber) ? null : dto.UnitNumber;
            if (dto.BuildingNumber != null) variant.BuildingNumber = string.IsNullOrWhiteSpace(dto.BuildingNumber) ? null : dto.BuildingNumber;
            if (dto.DeliveryDate.HasValue) variant.DeliveryDate = dto.DeliveryDate;
            if (dto.FinishingType != null) variant.FinishingType = Enum.TryParse<Domain.Enums.FinishingType>(dto.FinishingType, true, out var parsedFt) ? parsedFt : null;
            if (dto.HasBalcony.HasValue) variant.HasBalcony = dto.HasBalcony.Value;
            if (dto.HasParking.HasValue) variant.HasParking = dto.HasParking.Value;
            if (dto.FloorPlanUrl != null) variant.FloorPlanUrl = string.IsNullOrWhiteSpace(dto.FloorPlanUrl) ? null : dto.FloorPlanUrl;
            if (dto.AvailabilityStatus != null) variant.AvailabilityStatus = string.IsNullOrWhiteSpace(dto.AvailabilityStatus) ? "Available" : dto.AvailabilityStatus;
            if (dto.SortOrder.HasValue) variant.SortOrder = dto.SortOrder.Value;
            if (dto.IsActive.HasValue) variant.IsActive = dto.IsActive.Value;
            if (dto.IsFeatured.HasValue) variant.IsFeatured = dto.IsFeatured.Value;
            if (dto.IsRecommended.HasValue) variant.IsRecommended = dto.IsRecommended.Value;
            if (dto.DeliveryText != null) variant.DeliveryText = string.IsNullOrWhiteSpace(dto.DeliveryText) ? null : dto.DeliveryText;
            if (dto.DeliveryTextAr != null) variant.DeliveryTextAr = string.IsNullOrWhiteSpace(dto.DeliveryTextAr) ? null : dto.DeliveryTextAr;

            variant.UpdatedAt = DateTime.UtcNow;
            await _uow.CommitAsync();

            _cache?.InvalidateByPrefix(CacheKeys.UnitsListPrefix);
            _cache?.InvalidateByPrefix("projects_"); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_");

            return Ok(new UnitVariantDto
            {
                Id = variant.Id,
                PublicKey = variant.PublicKey,
                Name = variant.Name,
                Size = variant.Size,
                Price = variant.Price,
                Currency = variant.Currency,
                RentPerMonth = variant.RentPerMonth,
                Bedrooms = variant.Bedrooms,
                Bathrooms = variant.Bathrooms,
                Floor = variant.Floor,
                IsFurnished = variant.IsFurnished,
                View = variant.View.ToString(),
                UnitNumber = variant.UnitNumber,
                BuildingNumber = variant.BuildingNumber,
                DeliveryDate = variant.DeliveryDate,
                FinishingType = variant.FinishingType?.ToString(),
                HasBalcony = variant.HasBalcony,
                HasParking = variant.HasParking,
                FloorPlanUrl = variant.FloorPlanUrl,
                AvailabilityStatus = variant.AvailabilityStatus,
                SortOrder = variant.SortOrder,
                IsActive = variant.IsActive,
                IsFeatured = variant.IsFeatured,
                IsRecommended = variant.IsRecommended,
                DeliveryText = variant.DeliveryText,
                DeliveryTextAr = variant.DeliveryTextAr
            });
        }

        // ADMIN: Delete (soft-delete) a variant
        [Authorize(Roles = "Admin")]
        [HttpDelete("admin/{unitId:int}/variants/{variantId:int}")]
        public async Task<IActionResult> DeleteVariant(int unitId, int variantId)
        {
            var variant = await _uow.UnitVariants.QueryTracked()
                .FirstOrDefaultAsync(v => v.Id == variantId && v.UnitId == unitId && !v.IsDeleted);
            if (variant == null) return NotFound(new { message = "Variant not found" });

            variant.IsDeleted = true;
            variant.UpdatedAt = DateTime.UtcNow;
            await _uow.CommitAsync();

            _cache?.InvalidateByPrefix(CacheKeys.UnitsListPrefix);
            _cache?.InvalidateByPrefix("projects_"); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_");

            return NoContent();
        }
    }
}
