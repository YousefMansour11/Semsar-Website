using Application.DTOs;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class UnitService : IUnitService
    {
        private readonly IUnitOfWork _uow;
        private readonly ISlugService _slugService;
        private readonly IContentMetaService _metaService;
        private readonly ICanonicalService _canonicalService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ICacheService? _cache;
        private readonly ILogger<UnitService>? _logger;
        private readonly IReservationRepository _reservations;
        private readonly IVideoUploadService _videoUpload;

        public UnitService(IUnitOfWork uow, ISlugService slugService, IContentMetaService metaService, ICanonicalService canonicalService, ICloudinaryService cloudinaryService, IReservationRepository reservations, IVideoUploadService videoUpload, ICacheService? cache = null, ILogger<UnitService>? logger = null)
        {
            _uow = uow;
            _slugService = slugService;
            _metaService = metaService;
            _canonicalService = canonicalService ?? throw new ArgumentNullException(nameof(canonicalService));
            _cloudinaryService = cloudinaryService ?? throw new ArgumentNullException(nameof(cloudinaryService));
            _cache = cache;
            _reservations = reservations ?? throw new ArgumentNullException(nameof(reservations));
            _videoUpload = videoUpload ?? throw new ArgumentNullException(nameof(videoUpload));
            _logger = logger;
        }

        public async Task<Unit> CreateAsync(CreateUnitDto dto)
        {
            var project = await _uow.Projects.GetByIdAsync(dto.ProjectId);
            if (project == null) throw new InvalidOperationException("Project not found");

            await using var dedupTx = await _uow.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
            var dup = await _uow.Units.Query()
                .AnyAsync(u => u.TitleEn == dto.TitleEn && u.ProjectId == dto.ProjectId && !u.IsDeleted);
            if (dup)
                throw new InvalidOperationException("A unit with the same name already exists in this project.");
            await dedupTx.CommitAsync();

            var candidate = new Unit
            {
                TitleEn = dto.TitleEn,
                TitleAr = dto.TitleAr,
                DescriptionEn = dto.DescriptionEn,
                DescriptionAr = dto.DescriptionAr,
                MinPrice = dto.ListingType == PropertyListingType.Rental ? null : dto.MinPrice,
                MaxPrice = dto.ListingType == PropertyListingType.Rental ? null : dto.MaxPrice,
                RentPerMonth = dto.RentPerMonth.GetValueOrDefault() > 0
                    ? dto.RentPerMonth
                    : dto.ListingType == PropertyListingType.Rental ? (dto.MinPrice ?? dto.MaxPrice)
                    : null,
                Location = dto.Location,
                LocationAr = dto.LocationAr,
                ProjectId = dto.ProjectId,
                MinArea = dto.MinArea,
                MaxArea = dto.MaxArea,
                IsFeatured = dto.IsFeatured,
                IsRecommended = dto.IsRecommended ?? false,
                DeliveryText = dto.DeliveryText,
                DeliveryTextAr = dto.DeliveryTextAr,
                ConstructionStatus = dto.ConstructionStatus,
                AvailabilityStatus = dto.AvailabilityStatus ?? "Available",
                OwnershipType = dto.OwnershipType,
                VirtualTourUrl = dto.VirtualTourUrl,
                HighlightsAr = dto.HighlightsAr,
                NearbyPlaces = dto.NearbyPlaces,
                NearbyPlacesAr = dto.NearbyPlacesAr,
                Features = dto.Features ?? new List<string>(),
                FeaturesAr = dto.FeaturesAr ?? new List<string>(),
                Bedrooms = dto.Bedrooms ?? 0,
                Bathrooms = dto.Bathrooms ?? 0,
                Floor = dto.Floor,
                IsFurnished = dto.IsFurnished ?? false,
                View = dto.View ?? PropertyView.Unknown,
                UnitNumber = dto.UnitNumber,
                BuildingNumber = dto.BuildingNumber,
                DeliveryDate = dto.DeliveryDate,
                FinishingType = dto.FinishingType,
                HasBalcony = dto.HasBalcony,
                HasParking = dto.HasParking,
                ListingType = dto.ListingType,
                PropertyType = dto.PropertyType,
                CreatedAt = DateTime.UtcNow
            };

            string locationPart = string.IsNullOrEmpty(candidate.Location) ? "LOC" : candidate.Location.Substring(0, Math.Min(3, candidate.Location.Length)).ToUpper();
            var typeString = candidate.PropertyType.ToString();
            string typePart = string.IsNullOrEmpty(typeString) ? "PT" : typeString.Substring(0, Math.Min(2, typeString.Length)).ToUpper();
            string prefix = $"{locationPart}-{typePart}";

            const int maxAttempts = 5;
            Unit? created = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                string suffix = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
                candidate.Code = $"{prefix}-{suffix}";

                try
                {
                    await _metaService.GenerateMeta("unit",
                        async (slug, meta) =>
                        {
                            candidate.Slug = slug;
                            candidate.SlugIsAuto = true;
                            candidate.SlugLanguage = meta.SlugLanguage;

                            if (string.IsNullOrWhiteSpace(candidate.SeoTitle)) candidate.SeoTitle = meta.SeoTitleEn;
                            if (string.IsNullOrWhiteSpace(candidate.SeoTitleAr)) candidate.SeoTitleAr = meta.SeoTitleAr;
                            if (string.IsNullOrWhiteSpace(candidate.SeoDescription)) candidate.SeoDescription = meta.SeoDescriptionEn;
                            if (string.IsNullOrWhiteSpace(candidate.SeoDescriptionAr)) candidate.SeoDescriptionAr = meta.SeoDescriptionAr;
                            if (string.IsNullOrWhiteSpace(candidate.SeoKeywords)) candidate.SeoKeywords = meta.SeoKeywordsEn;
                            if (string.IsNullOrWhiteSpace(candidate.SeoKeywordsAr)) candidate.SeoKeywordsAr = meta.SeoKeywordsAr;

                            candidate.CanonicalUrl = meta.CanonicalUrl;
                            candidate.MetaGeneratedAt = meta.MetaGeneratedAt;
                            candidate.MetaVersion = meta.MetaVersion;
                        },
                        async () => { await Task.CompletedTask; },
                        candidate.TitleEn,
                        candidate.TitleAr,
                        candidate.DescriptionEn,
                        candidate.DescriptionAr,
                        candidate.Location);

                    await _uow.Units.AddAsync(candidate);

                    var baseSlug = candidate.Slug ?? string.Empty;
                    var attempts = 4;
                    for (int a = 1; a <= attempts; a++)
                    {
                        var attemptSlug = baseSlug;
                        if (a > 1) attemptSlug = _slugService.NormalizeSlug(baseSlug + "-" + a.ToString());
                        candidate.Slug = attemptSlug;
                        candidate.CanonicalUrl = _canonicalService.BuildCanonical("unit", attemptSlug);
                        try
                        {
                            await _uow.CommitAsync();
                            created = candidate;
                            break;
                        }
                        catch (DbUpdateException dbEx)
                        {
                            _logger?.LogWarning(dbEx, "DbUpdateException when trying commit for unit slug {Slug} (attempt {Attempt}/{Attempts})", candidate.Slug, a, attempts);
                            if (a == attempts)
                            {
                                _logger?.LogError(dbEx, "Final failure creating unit with slug {Slug}", candidate.Slug);
                                throw;
                            }
                        }
                    }
                    if (created != null) break;
                }
                catch (DbUpdateException dbEx)
                {
                    _logger?.LogWarning(dbEx, "DbUpdateException during unit creation, retrying with new code");
                    _uow.DetachEntity(candidate);
                    continue;
                }
            }
            if (created == null) throw new SlugConflictException("Unable to assign unique slug for unit after multiple attempts");

            var tx = await _uow.BeginTransactionAsync();
            try
            {
                if (dto.Contact != null && !string.IsNullOrWhiteSpace(dto.Contact.Name) && !string.IsNullOrWhiteSpace(dto.Contact.Phone))
                {
                    var existing = await _uow.Contacts.Query()
                        .FirstOrDefaultAsync(c => c.Phone == dto.Contact.Phone);

                    if (existing != null)
                    {
                        created.ContactId = existing.Id;
                    }
                    else
                    {
                        var contact = new ContactInfo
                        {
                            Name = dto.Contact.Name,
                            Phone = dto.Contact.Phone,
                            Type = dto.Contact.Type
                        };
                        await _uow.Contacts.AddAsync(contact);
                        await _uow.CommitAsync();

                        created.ContactId = contact.Id;
                        await _uow.CommitAsync();
                    }
                }

                if (dto.Installments != null)
                {
                    foreach (var instDto in dto.Installments.Where(i => i.IsEnabled))
                    {
                        var installment = new UnitInstallmentPlan
                        {
                            UnitId = created.Id,
                            PaymentType = instDto.PaymentType,
                            DownPaymentPercent = instDto.PaymentType == PaymentType.Cash ? 100 : instDto.DownPaymentPercent,
                            DiscountPercent = instDto.DiscountPercent,
                            Years = instDto.PaymentType == PaymentType.Cash ? 0 : instDto.Years,
                            IsEnabled = true
                        };
                        await _uow.UnitInstallmentPlans.AddAsync(installment);
                    }
                    await _uow.CommitAsync();
                }

                if (dto.Variants != null)
                {
                    foreach (var v in dto.Variants)
                    {
                        var variant = new UnitVariant
                        {
                            UnitId = created.Id,
                            PublicKey = $"UV-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                            Name = v.Name,
                            NameAr = v.NameAr,
                            Size = v.Size,
                            Price = v.Price,
                            Currency = v.Currency ?? "EGP",
                            RentPerMonth = v.RentPerMonth,
                            Bedrooms = v.Bedrooms,
                            Bathrooms = v.Bathrooms,
                            Floor = v.Floor,
                            IsFurnished = v.IsFurnished,
                            View = !string.IsNullOrWhiteSpace(v.View) && Enum.TryParse<PropertyView>(v.View.Replace(" ", "").Replace("&", ""), true, out var parsedView) ? parsedView : PropertyView.Unknown,
                            UnitNumber = v.UnitNumber,
                            BuildingNumber = v.BuildingNumber,
                            DeliveryDate = v.DeliveryDate,
                            FinishingType = !string.IsNullOrWhiteSpace(v.FinishingType) && Enum.TryParse<Domain.Enums.FinishingType>(v.FinishingType, true, out var parsedFt) ? parsedFt : null,
                            HasBalcony = v.HasBalcony,
                            HasParking = v.HasParking,
                            FloorPlanUrl = v.FloorPlanUrl,
                            AvailabilityStatus = v.AvailabilityStatus ?? "Available",
                            SortOrder = v.SortOrder,
                            IsActive = v.IsActive,
                            IsFeatured = v.IsFeatured ?? false,
                            IsRecommended = v.IsRecommended ?? false,
                            DeliveryText = v.DeliveryText,
                            DeliveryTextAr = v.DeliveryTextAr
                        };
                        await _uow.UnitVariants.AddAsync(variant);
                    }
                    await _uow.CommitAsync();
                }

                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                try { await tx.RollbackAsync(); } catch (Exception rbEx) { _logger?.LogWarning(rbEx, "Rollback failed for unit {UnitId}", created.Id); }
                _logger?.LogError(ex, "Failed to create contact/installments for unit {UnitId}", created.Id);
                throw;
            }

            try { _cache?.InvalidateByPrefix(Application.Services.CacheKeys.PropertiesList); _cache?.InvalidateByPrefix(Application.Services.CacheKeys.ProjectsList); _cache?.InvalidateByPrefix(Application.Services.CacheKeys.UnitsListPrefix); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_"); } catch (Exception ex) { _logger?.LogWarning(ex, "Cache invalidation failed after unit creation"); }
            return created;
        }

        private async Task DeleteCloudinaryImageSafe(string publicId)
        {
            try { await _cloudinaryService.DeleteImageAsync(publicId); }
            catch (Exception ex) { _logger?.LogWarning(ex, "Failed to delete Cloudinary image {PublicId}", publicId); }
        }

        public async Task<Unit?> GetByIdAsync(int id)
        {
            return await _uow.Units.GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var tx = await _uow.BeginTransactionAsync();
            try
            {
                var unit = await _uow.Units.QueryTracked().IgnoreQueryFilters()
                    .Include(u => u.Images)
                    .Include(u => u.Videos)
                    .FirstOrDefaultAsync(u => u.Id == id);
                if (unit == null)
                {
                    await tx.RollbackAsync();
                    return false;
                }

                if (unit.Images != null)
                {
                    var deleteTasks = new List<Task>();
                    foreach (var img in unit.Images)
                    {
                        if (!string.IsNullOrWhiteSpace(img.PublicId))
                        {
                            deleteTasks.Add(DeleteCloudinaryImageSafe(img.PublicId));
                        }
                    }
                    await Task.WhenAll(deleteTasks);
                }

                if (unit.Videos != null)
                {
                    foreach (var video in unit.Videos)
                    {
                        if (!string.IsNullOrWhiteSpace(video.PublicId))
                        {
                            try { await _videoUpload.DeleteVideoAsync(video.PublicId); } catch { }
                        }
                    }
                }

                if (_reservations.Context != null)
                {
                    var ctx = _reservations.Context;
                    var codeRes = await ctx.Set<Domain.Entities.CodeReservation>()
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(cr => cr.EntityType == "unit"
                            && EF.Property<int?>(cr, "UnitId") == id);
                    if (codeRes != null)
                    {
                        ctx.Remove(codeRes);
                    }

                    var slugRes = await ctx.Set<Domain.Entities.SlugReservation>()
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(cr => cr.EntityType == "unit"
                            && EF.Property<int?>(cr, "UnitId") == id);
                    if (slugRes != null)
                    {
                        ctx.Remove(slugRes);
                    }
                }

                _uow.Units.Delete(unit);

                await _uow.CommitAsync();
                await tx.CommitAsync();
                try { _cache?.InvalidateByPrefix(Application.Services.CacheKeys.PropertiesList); _cache?.InvalidateByPrefix(Application.Services.CacheKeys.ProjectsList); _cache?.InvalidateByPrefix(Application.Services.CacheKeys.UnitsListPrefix); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_"); } catch (Exception ex) { _logger?.LogWarning(ex, "Cache invalidation failed after unit deletion"); }
                return true;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger?.LogError(ex, "Delete failed for unit {UnitId}", id);
                throw;
            }
        }

        public async Task<Unit> UpdateAsync(int id, CreatePropertyDto dto)
        {
            var unit = await _uow.Units.GetByIdAsync(id);
            if (unit == null) throw new ArgumentException("Unit not found");

            var tx = await _uow.BeginTransactionAsync();
            try
            {
                unit.TitleEn = dto.TitleEn;
                unit.TitleAr = dto.TitleAr;
                unit.DescriptionEn = dto.DescriptionEn;
                unit.DescriptionAr = dto.DescriptionAr;
                unit.MinPrice = dto.Price > 0 ? dto.Price : null;
                unit.MaxPrice = null;
                unit.Location = dto.Location;
                unit.LocationAr = dto.LocationAr;
                unit.MinArea = dto.Size > 0 ? dto.Size : null;
                unit.MaxArea = null;
                unit.IsFeatured = dto.IsFeatured;
                unit.Features = dto.Features ?? new List<string>();
                unit.FeaturesAr = dto.FeaturesAr ?? new List<string>();
                unit.ListingType = dto.ListingType;
                unit.PropertyType = dto.PropertyType;
                unit.UpdatedAt = DateTime.UtcNow;

                _uow.Units.Update(unit);
                await _uow.CommitAsync();
                await tx.CommitAsync();
                try { _cache?.InvalidateByPrefix(Application.Services.CacheKeys.PropertiesList); _cache?.InvalidateByPrefix(Application.Services.CacheKeys.ProjectsList); _cache?.InvalidateByPrefix(Application.Services.CacheKeys.UnitsListPrefix); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_"); } catch (Exception ex) { _logger?.LogWarning(ex, "Cache invalidation failed after unit update"); }
                return unit;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger?.LogError(ex, "Update unit failed for unit {Id}", id);
                throw;
            }
        }

        public async Task<Unit> PatchAsync(int id, PatchUnitDto dto)
        {
            var tx = await _uow.BeginTransactionAsync();
            try
            {
                var unit = await _uow.Units.QueryTracked()
                    .Include(u => u.Installments)
                    .Include(u => u.Contact)
                    .Include(u => u.Variants)
                    .FirstOrDefaultAsync(u => u.Id == id);
                if (unit == null) throw new KeyNotFoundException("Unit not found");

                // Duplicate check: prevent creating a duplicate when changing title+project
                if (dto.TitleEn != null || dto.ProjectId.HasValue)
                {
                    var dupTitleEn = dto.TitleEn ?? unit.TitleEn ?? string.Empty;
                    var dupProjectId = dto.ProjectId ?? unit.ProjectId;
                    var dup = await _uow.Units.Query()
                        .AnyAsync(u => u.TitleEn == dupTitleEn && u.ProjectId == dupProjectId && u.Id != id && !u.IsDeleted);
                    if (dup)
                        throw new InvalidOperationException("A unit with the same name already exists in this project.");
                }

                bool slugWasAuto = unit.SlugIsAuto || string.IsNullOrWhiteSpace(unit.Slug);
                bool titleChanged = false;

                // Apply partial updates — only fields that are explicitly provided
                if (dto.TitleEn != null) { titleChanged = dto.TitleEn != unit.TitleEn; unit.TitleEn = dto.TitleEn; }
                if (dto.ProjectId.HasValue) unit.ProjectId = dto.ProjectId.Value;
                if (dto.TitleAr != null) unit.TitleAr = dto.TitleAr;
                if (dto.DescriptionEn != null) unit.DescriptionEn = dto.DescriptionEn;
                if (dto.DescriptionAr != null) unit.DescriptionAr = dto.DescriptionAr;
                if (dto.MinPrice.HasValue) unit.MinPrice = dto.MinPrice.Value;
                if (dto.MaxPrice.HasValue) unit.MaxPrice = dto.MaxPrice.Value;
                if (dto.RentPerMonth.HasValue) unit.RentPerMonth = dto.RentPerMonth.Value;
                if (dto.Location != null) unit.Location = dto.Location;
                if (dto.LocationAr != null) unit.LocationAr = dto.LocationAr;
                if (dto.MinArea.HasValue) unit.MinArea = dto.MinArea.Value;
                if (dto.MaxArea.HasValue) unit.MaxArea = dto.MaxArea.Value;
                if (dto.IsFeatured.HasValue) unit.IsFeatured = dto.IsFeatured.Value;
                if (dto.PropertyType.HasValue) unit.PropertyType = dto.PropertyType.Value;
                if (dto.ListingType.HasValue) unit.ListingType = dto.ListingType.Value;
                if (dto.Features != null) unit.Features = dto.Features;
                if (dto.FeaturesAr != null) unit.FeaturesAr = dto.FeaturesAr;
                if (dto.Bedrooms.HasValue) unit.Bedrooms = dto.Bedrooms.Value;
                if (dto.Bathrooms.HasValue) unit.Bathrooms = dto.Bathrooms.Value;
                if (dto.Floor.HasValue) unit.Floor = dto.Floor.Value;
                if (dto.IsFurnished.HasValue) unit.IsFurnished = dto.IsFurnished.Value;
                if (dto.View.HasValue) unit.View = dto.View.Value;
                if (dto.UnitNumber != null) unit.UnitNumber = string.IsNullOrWhiteSpace(dto.UnitNumber) ? null : dto.UnitNumber;
                if (dto.BuildingNumber != null) unit.BuildingNumber = string.IsNullOrWhiteSpace(dto.BuildingNumber) ? null : dto.BuildingNumber;
                if (dto.DeliveryDate.HasValue) unit.DeliveryDate = dto.DeliveryDate;
                if (dto.FinishingType.HasValue) unit.FinishingType = dto.FinishingType.Value;
                if (dto.HasBalcony.HasValue) unit.HasBalcony = dto.HasBalcony.Value;
                if (dto.HasParking.HasValue) unit.HasParking = dto.HasParking.Value;
                if (dto.IsRecommended.HasValue) unit.IsRecommended = dto.IsRecommended.Value;
                if (dto.DeliveryText != null) unit.DeliveryText = dto.DeliveryText;
                if (dto.DeliveryTextAr != null) unit.DeliveryTextAr = dto.DeliveryTextAr;
                if (dto.ConstructionStatus.HasValue) unit.ConstructionStatus = dto.ConstructionStatus.Value;
                if (dto.AvailabilityStatus != null) unit.AvailabilityStatus = dto.AvailabilityStatus;
                if (dto.OwnershipType.HasValue) unit.OwnershipType = dto.OwnershipType.Value;
                if (dto.VirtualTourUrl != null) unit.VirtualTourUrl = dto.VirtualTourUrl;
                if (dto.HighlightsAr != null) unit.HighlightsAr = dto.HighlightsAr;
                if (dto.NearbyPlaces != null) unit.NearbyPlaces = dto.NearbyPlaces;
                if (dto.NearbyPlacesAr != null) unit.NearbyPlacesAr = dto.NearbyPlacesAr;

                if (dto.Contact != null)
                {
                    var existingContact = await _uow.Contacts.Query()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Phone == dto.Contact.Phone);

                    if (existingContact != null)
                    {
                        if (unit.Contact?.Id == existingContact.Id)
                        {
                            if (dto.Contact.Name != null) unit.Contact.Name = dto.Contact.Name;
                            unit.Contact.Type = dto.Contact.Type;
                        }
                        else
                        {
                            var tracked = await _uow.Contacts.QueryTracked()
                                .FirstAsync(c => c.Id == existingContact.Id);
                            unit.Contact = tracked;
                        }
                    }
                    else
                    {
                        var newContact = new ContactInfo
                        {
                            Name = dto.Contact.Name,
                            Phone = dto.Contact.Phone,
                            Type = dto.Contact.Type
                        };
                        await _uow.Contacts.AddAsync(newContact);
                        unit.Contact = newContact;
                    }
                }

                // Slug regeneration — build candidate in memory, do NOT commit yet
                if ((dto.SlugRegenerateRequested == true || titleChanged) && slugWasAuto && !string.IsNullOrWhiteSpace(unit.TitleEn))
                {
                    string? newSlug = null;
                    string? newCanonical = null;

                    await _metaService.GenerateMeta("unit",
                        async (s, m) =>
                        {
                            newSlug = s;
                            unit.SlugLanguage = m.SlugLanguage;

                            if (string.IsNullOrWhiteSpace(unit.SeoTitle)) unit.SeoTitle = m.SeoTitleEn;
                            if (string.IsNullOrWhiteSpace(unit.SeoTitleAr)) unit.SeoTitleAr = m.SeoTitleAr;
                            if (string.IsNullOrWhiteSpace(unit.SeoDescription)) unit.SeoDescription = m.SeoDescriptionEn;
                            if (string.IsNullOrWhiteSpace(unit.SeoDescriptionAr)) unit.SeoDescriptionAr = m.SeoDescriptionAr;
                            if (string.IsNullOrWhiteSpace(unit.SeoKeywords)) unit.SeoKeywords = m.SeoKeywordsEn;
                            if (string.IsNullOrWhiteSpace(unit.SeoKeywordsAr)) unit.SeoKeywordsAr = m.SeoKeywordsAr;
                            unit.CanonicalUrl ??= m.CanonicalUrl;
                        },
                        async () => { await Task.CompletedTask; },
                        unit.TitleEn,
                        unit.TitleAr,
                        unit.DescriptionEn,
                        unit.DescriptionAr,
                        unit.Location);

                    if (!string.IsNullOrWhiteSpace(newSlug))
                    {
                        var baseSlug = newSlug;
                        var attempts = 4;
                        for (int a = 1; a <= attempts; a++)
                        {
                            var attemptSlug = baseSlug;
                            if (a > 1) attemptSlug = _slugService.NormalizeSlug(baseSlug + "-" + a.ToString());

                            var exists = await _uow.Units.Query()
                                .AnyAsync(u => u.Slug == attemptSlug && u.Id != id && !u.IsDeleted);
                            if (!exists)
                            {
                                unit.Slug = attemptSlug;
                                unit.SlugIsAuto = true;
                                newCanonical = _canonicalService.BuildCanonical("unit", attemptSlug);
                                break;
                            }
                            if (a == attempts) throw new SlugConflictException("Unable to generate unique slug for unit");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(newCanonical))
                    {
                        unit.CanonicalUrl = newCanonical;
                    }
                }

                // Custom slug override (when explicitly provided)
                if (dto.Slug != null)
                {
                    if (string.IsNullOrWhiteSpace(dto.Slug))
                    {
                        unit.SlugIsAuto = true;
                    }
                    else
                    {
                        var slugExists = await _uow.Units.Query()
                            .AnyAsync(u => u.Slug == dto.Slug && u.Id != id && !u.IsDeleted);
                        if (slugExists)
                        {
                            var deduped = await DeduplicateSlugAsync(dto.Slug, id);
                            unit.Slug = deduped;
                            unit.SlugIsAuto = true;
                            unit.CanonicalUrl = _canonicalService.BuildCanonical("unit", deduped);
                        }
                        else
                        {
                            unit.Slug = dto.Slug;
                            unit.SlugIsAuto = false;
                            unit.CanonicalUrl = _canonicalService.BuildCanonical("unit", dto.Slug);
                        }
                    }
                }

                // SEO overrides (only when explicitly provided)
                if (dto.SeoTitle != null) unit.SeoTitle = string.IsNullOrWhiteSpace(dto.SeoTitle) ? null : HtmlSanitizer.Sanitize(dto.SeoTitle);
                if (dto.SeoDescription != null) unit.SeoDescription = string.IsNullOrWhiteSpace(dto.SeoDescription) ? null : HtmlSanitizer.Sanitize(dto.SeoDescription);
                if (dto.SeoKeywords != null) unit.SeoKeywords = string.IsNullOrWhiteSpace(dto.SeoKeywords) ? null : HtmlSanitizer.Sanitize(dto.SeoKeywords);
                if (dto.SeoTitleAr != null) unit.SeoTitleAr = string.IsNullOrWhiteSpace(dto.SeoTitleAr) ? null : HtmlSanitizer.Sanitize(dto.SeoTitleAr);
                if (dto.SeoDescriptionAr != null) unit.SeoDescriptionAr = string.IsNullOrWhiteSpace(dto.SeoDescriptionAr) ? null : HtmlSanitizer.Sanitize(dto.SeoDescriptionAr);
                if (dto.SeoKeywordsAr != null) unit.SeoKeywordsAr = string.IsNullOrWhiteSpace(dto.SeoKeywordsAr) ? null : HtmlSanitizer.Sanitize(dto.SeoKeywordsAr);

                // Installments — soft-delete existing tracked ones, then add new via repo
                if (dto.Installments != null)
                {
                    var existingInsts = unit.Installments?.Where(x => !x.IsDeleted).ToList() ?? new List<UnitInstallmentPlan>();
                    foreach (var inst in existingInsts)
                    {
                        inst.IsDeleted = true;
                    }

                    foreach (var instDto in dto.Installments)
                    {
                        var newInst = new UnitInstallmentPlan
                        {
                            UnitId = id,
                            PaymentType = instDto.PaymentType,
                            DownPaymentPercent = instDto.PaymentType == PaymentType.Cash ? 100 : instDto.DownPaymentPercent,
                            DiscountPercent = instDto.DiscountPercent,
                            Years = instDto.PaymentType == PaymentType.Cash ? 0 : instDto.Years,
                            IsEnabled = instDto.IsEnabled,
                            IsDeleted = false
                        };
                        await _uow.UnitInstallmentPlans.AddAsync(newInst);
                    }
                }

                // Variants — soft-delete existing tracked ones, then add new
                if (dto.Variants != null)
                {
                    var existingVariants = unit.Variants?.Where(x => !x.IsDeleted).ToList() ?? new List<UnitVariant>();
                    foreach (var v in existingVariants)
                    {
                        v.IsDeleted = true;
                    }

                    foreach (var v in dto.Variants)
                    {
                        var newVariant = new UnitVariant
                        {
                            UnitId = id,
                            PublicKey = $"UV-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                            Name = v.Name,
                            NameAr = v.NameAr,
                            Size = v.Size,
                            Price = v.Price,
                            Currency = v.Currency ?? "EGP",
                            RentPerMonth = v.RentPerMonth,
                            Bedrooms = v.Bedrooms,
                            Bathrooms = v.Bathrooms,
                            Floor = v.Floor,
                            IsFurnished = v.IsFurnished,
                            View = !string.IsNullOrWhiteSpace(v.View) && Enum.TryParse<PropertyView>(v.View.Replace(" ", "").Replace("&", ""), true, out var parsedView) ? parsedView : PropertyView.Unknown,
                            UnitNumber = v.UnitNumber,
                            BuildingNumber = v.BuildingNumber,
                            DeliveryDate = v.DeliveryDate,
                            FinishingType = !string.IsNullOrWhiteSpace(v.FinishingType) && Enum.TryParse<Domain.Enums.FinishingType>(v.FinishingType, true, out var parsedFt) ? parsedFt : null,
                            HasBalcony = v.HasBalcony,
                            HasParking = v.HasParking,
                            FloorPlanUrl = v.FloorPlanUrl,
                            AvailabilityStatus = v.AvailabilityStatus ?? "Available",
                            SortOrder = v.SortOrder,
                            IsActive = v.IsActive,
                            IsFeatured = v.IsFeatured ?? false,
                            IsRecommended = v.IsRecommended ?? false,
                            DeliveryText = v.DeliveryText,
                            DeliveryTextAr = v.DeliveryTextAr
                        };
                        await _uow.UnitVariants.AddAsync(newVariant);
                    }
                }

                unit.UpdatedAt = DateTime.UtcNow;

                // Single commit at the end — no intermediate commits
                await _uow.CommitAsync();
                await tx.CommitAsync();
                try { _cache?.InvalidateByPrefix(Application.Services.CacheKeys.PropertiesList); _cache?.InvalidateByPrefix(Application.Services.CacheKeys.ProjectsList); _cache?.InvalidateByPrefix(Application.Services.CacheKeys.UnitsListPrefix); _cache?.InvalidateByPrefix("properties_location_"); _cache?.InvalidateByPrefix("landing_"); } catch (Exception ex) { _logger?.LogWarning(ex, "Cache invalidation failed after unit patch"); }

                var reloaded = await _uow.Units.Query()
                    .Include(u => u.Images)
                    .Include(u => u.Installments)
                    .Include(u => u.Contact)
                    .AsSplitQuery()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == id);
                if (reloaded != null) return reloaded;
                return unit;
            }
            catch (KeyNotFoundException)
            {
                try { await tx.RollbackAsync(); } catch (Exception rbEx) { _logger?.LogWarning(rbEx, "Rollback failed for unit patch (KeyNotFoundException path)"); }
                throw;
            }
            catch (SlugConflictException)
            {
                try { await tx.RollbackAsync(); } catch (Exception rbEx) { _logger?.LogWarning(rbEx, "Rollback failed for unit patch (SlugConflictException path)"); }
                throw;
            }
            catch (Exception ex)
            {
                try { await tx.RollbackAsync(); } catch (Exception rbEx) { _logger?.LogWarning(rbEx, "Rollback failed for unit patch (general error path)"); }
                _logger?.LogError(ex, "Patch unit failed for unit {UnitId}. InnerException: {Inner}", id, ex.InnerException?.Message);
                throw;
            }
        }

        private async Task<string> DeduplicateSlugAsync(string baseSlug, int excludeId)
        {
            var maxAttempts = 10;
            for (int a = 1; a <= maxAttempts; a++)
            {
                var attemptSlug = a == 1 ? baseSlug : _slugService.NormalizeSlug(baseSlug + "-" + a.ToString());
                var exists = await _uow.Units.Query()
                    .AnyAsync(u => u.Slug == attemptSlug && u.Id != excludeId && !u.IsDeleted);
                if (!exists) return attemptSlug;
            }
            return _slugService.NormalizeSlug(baseSlug + "-" + Guid.NewGuid().ToString().Substring(0, 6));
        }
    }
}
