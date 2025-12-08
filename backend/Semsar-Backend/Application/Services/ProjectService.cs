using Application.DTOs;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IUnitOfWork _uow;
        private readonly ISlugService _slugService;
        private readonly IContentMetaService _metaService;
        private readonly ICanonicalService _canonicalService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ICacheService? _cache;
        private readonly IReservationRepository _reservations;
        private readonly IVideoUploadService _videoUpload;
        private readonly ILogger<ProjectService>? _logger;
        public ProjectService(IUnitOfWork uow, ISlugService slugService, IContentMetaService metaService, ICanonicalService canonicalService, ICloudinaryService cloudinaryService, IReservationRepository reservations, IVideoUploadService videoUpload, ICacheService? cache = null, ILogger<ProjectService>? logger = null)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _slugService = slugService ?? throw new ArgumentNullException(nameof(slugService));
            _metaService = metaService ?? throw new ArgumentNullException(nameof(metaService));
            _canonicalService = canonicalService ?? throw new ArgumentNullException(nameof(canonicalService));
            _cloudinaryService = cloudinaryService ?? throw new ArgumentNullException(nameof(cloudinaryService));
            _cache = cache;
            _reservations = reservations ?? throw new ArgumentNullException(nameof(reservations));
            _videoUpload = videoUpload ?? throw new ArgumentNullException(nameof(videoUpload));
            _logger = logger;
        }

        public async Task<(int Id, string Name, string Location)> CreateAsync(ProjectDto dto)
        {
            await using var dedupTx = await _uow.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
            var dup = await _uow.Projects.Query()
                .AnyAsync(p => p.NameEn == dto.NameEn && (p.Location == dto.Location || (p.LocationAr != null && dto.LocationAr != null && p.LocationAr == dto.LocationAr)) && p.Developer == dto.Developer && !p.IsDeleted);
            if (dup)
                throw new InvalidOperationException("A project with the same name, location, and developer already exists.");
            await dedupTx.CommitAsync();

            var project = new Domain.Entities.Project
            {
                NameEn = dto.NameEn,
                NameAr = dto.NameAr,
                DescriptionEn = dto.DescriptionEn,
                DescriptionAr = dto.DescriptionAr,
                Location = dto.Location,
                LocationAr = dto.LocationAr,
                Developer = dto.Developer,
                Image = dto.Image,
                Highlights = dto.Highlights ?? new System.Collections.Generic.List<string>(),
                HighlightsAr = dto.HighlightsAr,
                StartingPrice = dto.StartingPrice,
                NearbyPlaces = dto.NearbyPlaces,
                NearbyPlacesAr = dto.NearbyPlacesAr,
                PropertyTypes = dto.PropertyTypes,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                TotalArea = dto.TotalArea,
                OwnershipType = dto.OwnershipType,
                UnitCount = dto.UnitCount,
                IsRecommended = dto.IsRecommended,
                DeliveryText = dto.DeliveryText,
                DeliveryTextAr = dto.DeliveryTextAr,
                ConstructionStatus = dto.ConstructionStatus != null ? Enum.Parse<Domain.Enums.ConstructionStatus>(dto.ConstructionStatus) : null,
                AvailabilityStatus = dto.AvailabilityStatus ?? "Available",
                ViewCount = dto.ViewCount,
                InquiryCount = dto.InquiryCount,
                FavoriteCount = dto.FavoriteCount,
                VirtualTourUrl = dto.VirtualTourUrl
            };

            var tx = await _uow.BeginTransactionAsync();
            try
            {
                var meta = await _metaService.GenerateAsync("project", project.NameEn, project.NameAr, project.DescriptionEn, project.DescriptionAr, project.Location);
                if (string.IsNullOrWhiteSpace(meta.BaseSlug)) throw new InvalidOperationException("Slug generation returned empty base slug");

                var baseSlug = meta.BaseSlug;
                Domain.Entities.SlugReservation? slugRes = null;
                string finalSlug = baseSlug;
                for (int i = 0; i < 6; i++)
                {
                    var attemptSlug = i == 0 ? baseSlug : _slugService.NormalizeSlug(baseSlug + "-" + i.ToString());
                    var slugExists = await _uow.Projects.Query().AnyAsync(p => p.Slug == attemptSlug && !p.IsDeleted);
                    if (slugExists) continue;
                    slugRes = await _reservations.TryCreateSlugReservationAsync("project", attemptSlug);
                    if (slugRes != null)
                    {
                        finalSlug = attemptSlug;
                        break;
                    }
                }
                if (slugRes == null) throw new SlugConflictException("Unable to reserve unique slug for project");

                project.Slug = finalSlug;
                project.SlugIsAuto = true;
                project.SlugLanguage = meta.SlugLanguage;
                project.SeoTitle = project.SeoTitle ?? meta.SeoTitleEn;
                project.SeoTitleAr = project.SeoTitleAr ?? meta.SeoTitleAr;
                project.SeoDescription = project.SeoDescription ?? meta.SeoDescriptionEn;
                project.SeoDescriptionAr = project.SeoDescriptionAr ?? meta.SeoDescriptionAr;
                project.SeoKeywords = project.SeoKeywords ?? meta.SeoKeywordsEn;
                project.SeoKeywordsAr = project.SeoKeywordsAr ?? meta.SeoKeywordsAr;
                project.CanonicalUrl = _canonicalService.BuildCanonical("projects", project.Slug);

                slugRes.Project = project;

                await _uow.Projects.AddAsync(project);
                await _uow.CommitAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                try { await tx.RollbackAsync(); } catch (Exception rbEx) { _logger?.LogWarning(rbEx, "Rollback failed for project creation"); }
                try { await _reservations.CleanupPendingReservationsAsync(); } catch (Exception cleanupEx) { _logger?.LogWarning(cleanupEx, "Failed to cleanup pending reservations after project creation failure"); }
                _logger?.LogError(ex, "Create project failed");
                throw;
            }

            try { _cache?.InvalidateByPrefix(Application.Services.CacheKeys.ProjectDetailsPrefix); _cache?.InvalidateByPrefix(Application.Services.CacheKeys.ProjectsList); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_"); } catch (Exception ex) { _logger?.LogWarning(ex, "Cache invalidation failed after creating project"); }

            return (project.Id, project.NameEn, project.Location);
        }

        private async Task DeleteCloudinaryImageSafe(string publicId)
        {
            try { await _cloudinaryService.DeleteImageAsync(publicId); }
            catch (Exception ex) { _logger?.LogWarning(ex, "Failed to delete Cloudinary image {PublicId}", publicId); }
        }

        public async Task PatchAsync(int id, UpdateProjectDto dto)
        {
            var proj = await _uow.Projects.QueryTracked()
                .FirstOrDefaultAsync(p => p.Id == id);
            if (proj == null) throw new KeyNotFoundException("Project not found");

            // Apply partial updates — only fields that are explicitly provided
            if (!string.IsNullOrWhiteSpace(dto.NameEn)) proj.NameEn = dto.NameEn;
            if (!string.IsNullOrWhiteSpace(dto.NameAr)) proj.NameAr = dto.NameAr;
            if (!string.IsNullOrWhiteSpace(dto.DescriptionEn)) proj.DescriptionEn = dto.DescriptionEn;
            if (!string.IsNullOrWhiteSpace(dto.DescriptionAr)) proj.DescriptionAr = dto.DescriptionAr;
            if (!string.IsNullOrWhiteSpace(dto.Location)) proj.Location = dto.Location;
            if (dto.LocationAr != null) proj.LocationAr = string.IsNullOrWhiteSpace(dto.LocationAr) ? null : dto.LocationAr;
            if (!string.IsNullOrWhiteSpace(dto.Developer)) proj.Developer = dto.Developer;
            if (dto.Image != null)
            {
                var oldImage = proj.Image;
                proj.Image = string.IsNullOrWhiteSpace(dto.Image) ? null : dto.Image;
                if (proj.Image == null && !string.IsNullOrWhiteSpace(oldImage))
                {
                    var publicId = ExtractCloudinaryPublicId(oldImage);
                    if (!string.IsNullOrWhiteSpace(publicId))
                    {
                        try { await _cloudinaryService.DeleteImageAsync(publicId); }
                        catch (Exception ex) { _logger?.LogWarning(ex, "Failed to delete old project image from Cloudinary: {PublicId}", publicId); }
                    }
                }
            }
            if (dto.Highlights != null) proj.Highlights = dto.Highlights;
            if (dto.HighlightsAr != null) proj.HighlightsAr = dto.HighlightsAr;
            if (dto.StartingPrice.HasValue) proj.StartingPrice = dto.StartingPrice.Value;
            if (dto.NearbyPlaces != null) proj.NearbyPlaces = dto.NearbyPlaces;
            if (dto.NearbyPlacesAr != null) proj.NearbyPlacesAr = dto.NearbyPlacesAr;
            if (dto.PropertyTypes != null) proj.PropertyTypes = dto.PropertyTypes;
            if (dto.Latitude.HasValue) proj.Latitude = dto.Latitude.Value;
            if (dto.Longitude.HasValue) proj.Longitude = dto.Longitude.Value;
            if (dto.TotalArea.HasValue) proj.TotalArea = dto.TotalArea.Value;
            if (dto.OwnershipType.HasValue) proj.OwnershipType = dto.OwnershipType.Value;
            if (dto.UnitCount.HasValue) proj.UnitCount = dto.UnitCount.Value;
            if (dto.IsRecommended.HasValue) proj.IsRecommended = dto.IsRecommended.Value;
            if (dto.DeliveryText != null) proj.DeliveryText = dto.DeliveryText;
            if (dto.DeliveryTextAr != null) proj.DeliveryTextAr = dto.DeliveryTextAr;
            if (dto.ConstructionStatus.HasValue) proj.ConstructionStatus = dto.ConstructionStatus.Value;
            if (dto.AvailabilityStatus != null) proj.AvailabilityStatus = dto.AvailabilityStatus;
            if (dto.VirtualTourUrl != null) proj.VirtualTourUrl = dto.VirtualTourUrl;

            // Slug override (when explicitly provided)
            if (dto.Slug != null)
            {
                proj.Slug = string.IsNullOrWhiteSpace(dto.Slug) ? string.Empty : dto.Slug;
                proj.SlugIsAuto = string.IsNullOrWhiteSpace(dto.Slug);
            }

            // SEO overrides (when explicitly provided)
            if (dto.SeoTitle != null) proj.SeoTitle = string.IsNullOrWhiteSpace(dto.SeoTitle) ? null : dto.SeoTitle;
            if (dto.SeoDescription != null) proj.SeoDescription = string.IsNullOrWhiteSpace(dto.SeoDescription) ? null : dto.SeoDescription;
            if (dto.SeoKeywords != null) proj.SeoKeywords = string.IsNullOrWhiteSpace(dto.SeoKeywords) ? null : dto.SeoKeywords;
            if (dto.SeoTitleAr != null) proj.SeoTitleAr = string.IsNullOrWhiteSpace(dto.SeoTitleAr) ? null : dto.SeoTitleAr;
            if (dto.SeoDescriptionAr != null) proj.SeoDescriptionAr = string.IsNullOrWhiteSpace(dto.SeoDescriptionAr) ? null : dto.SeoDescriptionAr;
            if (dto.SeoKeywordsAr != null) proj.SeoKeywordsAr = string.IsNullOrWhiteSpace(dto.SeoKeywordsAr) ? null : dto.SeoKeywordsAr;
            if (dto.CanonicalUrl != null) proj.CanonicalUrl = string.IsNullOrWhiteSpace(dto.CanonicalUrl) ? string.Empty : dto.CanonicalUrl;

            // Auto-fill SEO fields from provided content if they are empty
            if (string.IsNullOrWhiteSpace(proj.SeoTitle) && !string.IsNullOrWhiteSpace(proj.NameEn)) proj.SeoTitle = $"{proj.NameEn} in {proj.Location}";
            if (string.IsNullOrWhiteSpace(proj.SeoDescription) && !string.IsNullOrWhiteSpace(proj.DescriptionEn)) proj.SeoDescription = proj.DescriptionEn.Length <= 150 ? proj.DescriptionEn : proj.DescriptionEn.Substring(0, 150);
            if (string.IsNullOrWhiteSpace(proj.SeoTitleAr) && !string.IsNullOrWhiteSpace(proj.NameAr)) proj.SeoTitleAr = proj.NameAr;
            if (string.IsNullOrWhiteSpace(proj.SeoDescriptionAr) && !string.IsNullOrWhiteSpace(proj.DescriptionAr)) proj.SeoDescriptionAr = proj.DescriptionAr.Length <= 150 ? proj.DescriptionAr : proj.DescriptionAr.Substring(0, 150);
            if (string.IsNullOrWhiteSpace(proj.CanonicalUrl) && !string.IsNullOrWhiteSpace(proj.Slug)) proj.CanonicalUrl = _canonicalService.BuildCanonical("projects", proj.Slug);

            proj.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _uow.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Patch project failed for project {ProjectId}. InnerException: {Inner}", id, ex.InnerException?.Message);
                throw;
            }

            try { _cache?.InvalidateByPrefix(Application.Services.CacheKeys.ProjectDetailsPrefix); _cache?.InvalidateByPrefix(Application.Services.CacheKeys.ProjectsList); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_"); } catch (Exception ex) { _logger?.LogWarning(ex, "Cache invalidation failed after project patch"); }
        }

        public async Task DeleteAsync(int id)
        {
            var tx = await _uow.BeginTransactionAsync();
            try
            {
                var proj = await _uow.Projects.QueryTracked().IgnoreQueryFilters()
                    .AsSplitQuery()
                    .Include(p => p.Videos)
                    .FirstOrDefaultAsync(p => p.Id == id);
                if (proj == null)
                {
                    await tx.RollbackAsync();
                    throw new KeyNotFoundException("Project not found");
                }

                if (proj.Videos != null)
                {
                    foreach (var video in proj.Videos)
                    {
                        if (!string.IsNullOrWhiteSpace(video.PublicId))
                        {
                            try { await _videoUpload.DeleteVideoAsync(video.PublicId); } catch { }
                        }
                    }
                }

                int skip = 0;
                const int pageSize = 50;
                bool hasMore;
                do
                {
                    var unitPage = await _uow.Units.Query().AsNoTracking().IgnoreQueryFilters()
                        .Where(u => u.ProjectId == id)
                        .Skip(skip).Take(pageSize)
                        .Select(u => new
                        {
                            ImagePublicIds = u.Images!.Where(i => i.PublicId != null && i.PublicId != "").Select(i => i.PublicId!).ToList(),
                            VideoPublicIds = u.Videos!.Where(v => v.PublicId != null && v.PublicId != "").Select(v => v.PublicId!).ToList()
                        })
                        .ToListAsync();

                    var deleteTasks = new List<Task>();
                    foreach (var batch in unitPage)
                    {
                        foreach (var imgId in batch.ImagePublicIds)
                            deleteTasks.Add(DeleteCloudinaryImageSafe(imgId!));
                        foreach (var vidId in batch.VideoPublicIds)
                            deleteTasks.Add(_videoUpload.DeleteVideoAsync(vidId!));
                    }
                    await Task.WhenAll(deleteTasks);

                    skip += pageSize;
                    hasMore = unitPage.Count == pageSize;
                } while (hasMore);

                if (!string.IsNullOrWhiteSpace(proj.Image))
                {
                    var publicId = ExtractCloudinaryPublicId(proj.Image);
                    if (!string.IsNullOrWhiteSpace(publicId))
                    {
                        await DeleteCloudinaryImageSafe(publicId);
                    }
                }

                if (_reservations.Context != null)
                {
                    var ctx = _reservations.Context;
                    var codeRes = await ctx.Set<Domain.Entities.CodeReservation>()
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(cr => cr.EntityType == "project"
                            && EF.Property<int?>(cr, "ProjectId") == id);
                    if (codeRes != null) ctx.Remove(codeRes);

                    var slugRes = await ctx.Set<Domain.Entities.SlugReservation>()
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(cr => cr.EntityType == "project"
                            && EF.Property<int?>(cr, "ProjectId") == id);
                    if (slugRes != null) ctx.Remove(slugRes);
                }

                _uow.Projects.Delete(proj);

                await _uow.CommitAsync();
                await tx.CommitAsync();
                try { _cache?.InvalidateByPrefix(Application.Services.CacheKeys.ProjectDetailsPrefix); _cache?.InvalidateByPrefix(Application.Services.CacheKeys.ProjectsList); _cache?.InvalidateByPrefix("properties_"); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_"); } catch (Exception ex) { _logger?.LogWarning(ex, "Cache invalidation failed after project deletion"); }
            }
            catch (Exception ex)
            {
                try { await tx.RollbackAsync(); } catch (Exception rbEx) { _logger?.LogWarning(rbEx, "Rollback failed for project deletion"); }
                _logger?.LogError(ex, "Delete project failed for {ProjectId}", id);
                throw;
            }
        }

        private static string? ExtractCloudinaryPublicId(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;

            var segments = uri.Segments;
            for (int i = 0; i < segments.Length - 1; i++)
            {
                if (segments[i] == "upload/")
                {
                    var startIdx = i + 2;
                    if (startIdx >= segments.Length) return null;

                    var publicIdWithFormat = string.Concat(segments[startIdx..]);
                    var lastDot = publicIdWithFormat.LastIndexOf('.');
                    return lastDot > 0 ? publicIdWithFormat[..lastDot] : publicIdWithFormat;
                }
            }
            return null;
        }
    }
}
