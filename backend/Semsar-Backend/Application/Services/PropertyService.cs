using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class PropertyService : IPropertyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ISlugService _slugService;
        private readonly IContentMetaService _metaService;
        private readonly ICanonicalService _canonicalService;
        private readonly IJsonLdService _jsonLdService;
        private readonly ILogger<PropertyService>? _logger;
        private readonly IReservationRepository _reservations;
        private readonly ILocationService _locationService;
        private readonly IVideoUploadService _videoUploadService;
        private const int CodeGenerationMaxAttempts = 3;

        public PropertyService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService, ISlugService slugService, IContentMetaService metaService, ICanonicalService canonicalService, IJsonLdService jsonLdService, IReservationRepository reservations, ILocationService locationService, IVideoUploadService videoUploadService, ILogger<PropertyService>? logger = null)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _cloudinaryService = cloudinaryService ?? throw new ArgumentNullException(nameof(cloudinaryService));
            _slugService = slugService ?? throw new ArgumentNullException(nameof(slugService));
            _metaService = metaService ?? throw new ArgumentNullException(nameof(metaService));
            _canonicalService = canonicalService ?? throw new ArgumentNullException(nameof(canonicalService));
            _jsonLdService = jsonLdService ?? throw new ArgumentNullException(nameof(jsonLdService));
            _logger = logger;
            _reservations = reservations ?? throw new ArgumentNullException(nameof(reservations));
            _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
            _videoUploadService = videoUploadService ?? throw new ArgumentNullException(nameof(videoUploadService));
        }

        public async Task<Property> CreateAsync(CreatePropertyDto dto)
        {
            ContactInfo? contact = null;

            if (dto.Contact != null)
            {
                var existingContact = await _unitOfWork.Contacts.Query()
                    .FirstOrDefaultAsync(c => c.Phone == dto.Contact.Phone);

                if (existingContact != null)
                {
                    contact = existingContact;
                }
                else
                {
                    contact = new ContactInfo
                    {
                        Name = dto.Contact.Name,
                        Phone = dto.Contact.Phone,
                        Type = dto.Contact.Type
                    };
                }
            }

            var titleEnCheck = (dto.TitleEn ?? string.Empty).Trim();
            var locationCheck = (dto.Location ?? string.Empty).Trim();

            Property? createdProperty = null;
            Property? softDeleted = null;

            var metaForSlug = await _metaService.GenerateAsync("property", dto.TitleEn ?? string.Empty, dto.TitleAr ?? string.Empty, dto.DescriptionEn ?? string.Empty, dto.DescriptionAr ?? string.Empty, dto.Location ?? string.Empty);
            if (string.IsNullOrWhiteSpace(metaForSlug.BaseSlug)) throw new InvalidOperationException("Slug generation returned empty base slug");

            var baseSlugPre = metaForSlug.BaseSlug;
            Domain.Entities.SlugReservation? slugResPre = null;
            var finalSlugPre = baseSlugPre;
            var slugTxPre = await _unitOfWork.BeginTransactionAsync();
            try
            {
                for (int i = 0; i < 6; i++)
                {
                    var attemptSlug = i == 0 ? baseSlugPre : _slugService.NormalizeSlug(baseSlugPre + "-" + i.ToString());
                    slugResPre = await _reservations.TryCreateSlugReservationAsync("property", attemptSlug);
                    if (slugResPre != null)
                    {
                        finalSlugPre = attemptSlug;
                        break;
                    }
                }
                if (slugResPre == null)
                {
                    await slugTxPre.RollbackAsync();
                    throw new SlugConflictException("Unable to reserve unique slug for property");
                }
                await slugTxPre.CommitAsync();
                await _reservations.Context!.SaveChangesAsync();
            }
            catch (SlugConflictException) { throw; }
            catch (Exception slugEx)
            {
                await slugTxPre.RollbackAsync();
                _logger?.LogWarning(slugEx, "Slug reservation transaction failed (pre)");
                throw new SlugConflictException("Unable to reserve unique slug for property due to concurrency issue");
            }

            var tx = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var activeDup = await _unitOfWork.Properties.QueryTracked()
                    .FirstOrDefaultAsync(p =>
                        ((p.TitleEn ?? string.Empty).Trim() == titleEnCheck.Trim())
                        && ((p.Location ?? string.Empty).Trim() == locationCheck.Trim())
                        && !p.IsDeleted);
                if (activeDup != null)
                {
                    try { await tx.RollbackAsync(); } catch { }
                    throw new InvalidOperationException("A property with the same name and location already exists.");
                }

                softDeleted = await _unitOfWork.Properties.Query().IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p =>
                        ((p.TitleEn ?? string.Empty).Trim() == titleEnCheck.Trim())
                        && ((p.Location ?? string.Empty).Trim() == locationCheck.Trim())
                        && p.IsDeleted);

                if (softDeleted != null)
                {
                    _unitOfWork.Properties.Delete(softDeleted);
                }

                var candidate = new Property
                {
                    TitleEn = dto.TitleEn ?? string.Empty,
                    TitleAr = dto.TitleAr ?? string.Empty,
                    DescriptionEn = dto.DescriptionEn ?? string.Empty,
                    DescriptionAr = dto.DescriptionAr ?? string.Empty,
                    Price = dto.ListingType == PropertyListingType.Rental ? 0 : dto.Price,
                    RentPerMonth = dto.RentPerMonth.GetValueOrDefault() > 0
                        ? dto.RentPerMonth
                        : dto.ListingType == PropertyListingType.Rental ? dto.Price
                        : null,
                    Location = dto.Location ?? string.Empty,
                    LocationAr = dto.LocationAr,
                    LocationId = null,
                    Size = dto.Size,
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
                    TotalFloors = dto.TotalFloors,
                    IsFurnished = dto.IsFurnished ?? false,
                    View = dto.View ?? PropertyView.Unknown,
                    ListingType = dto.ListingType,
                    PropertyType = dto.PropertyType,
                    CreatedAt = DateTime.UtcNow
                };

                var userProvidedLocationAr = dto.LocationAr;
                if (dto.GovernorateId.HasValue || dto.CityId.HasValue || dto.AreaId.HasValue)
                {
                    var resolved = await _locationService.ResolveLocationAsync(dto.GovernorateId, dto.CityId, dto.AreaId, default);
                    if (resolved != null)
                    {
                        candidate.Location = resolved.LocationString;
                        candidate.LocationAr = resolved.LocationStringAr;
                        candidate.LocationId = resolved.DeepestId;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(dto.Location))
                {
                    var resolved = await _locationService.ResolveOrCreateFromStringAsync(dto.Location, default);
                    if (resolved != null)
                    {
                        candidate.Location = resolved.LocationString;
                        candidate.LocationAr = resolved.LocationStringAr;
                        candidate.LocationId = resolved.DeepestId;
                    }
                }
                if (!string.IsNullOrWhiteSpace(userProvidedLocationAr))
                {
                    candidate.LocationAr = userProvidedLocationAr;
                }

                candidate.Code = await GeneratePropertyCodeCandidateAsync(candidate);

                if (contact != null)
                {
                    if (contact.Id == 0)
                    {
                        await _unitOfWork.Contacts.AddAsync(contact);
                        candidate.Contact = contact;
                    }
                    else
                    {
                        candidate.ContactId = contact.Id;
                    }
                }

                if (dto.Installments != null)
                {
                    var enabled = dto.Installments.Where(i => i.IsEnabled).ToList();
                    if (enabled.Count > 0)
                    {
                        candidate.Installments = new List<Domain.Entities.PropertyInstallmentPlan>();
                        foreach (var instDto in enabled)
                        {
                            candidate.Installments.Add(new Domain.Entities.PropertyInstallmentPlan
                            {
                                PaymentType = instDto.PaymentType,
                                DownPaymentPercent = instDto.PaymentType == PaymentType.Cash ? 100 : instDto.DownPaymentPercent,
                                Years = instDto.PaymentType == PaymentType.Cash ? 0 : instDto.Years,
                                DiscountPercent = instDto.DiscountPercent,
                                IsEnabled = true,
                                IsDeleted = false
                            });
                        }
                    }
                }

                var meta = metaForSlug;
                var slugRes = slugResPre;
                var finalSlug = finalSlugPre;

                candidate.Slug = finalSlug;
                candidate.SlugIsAuto = true;
                candidate.SlugLanguage = meta.SlugLanguage;
                candidate.SeoTitle = HtmlSanitizer.Sanitize(candidate.SeoTitle ?? meta.SeoTitleEn);
                candidate.SeoTitleAr = HtmlSanitizer.Sanitize(candidate.SeoTitleAr ?? meta.SeoTitleAr);
                candidate.SeoDescription = HtmlSanitizer.Sanitize(candidate.SeoDescription ?? meta.SeoDescriptionEn);
                candidate.SeoDescriptionAr = HtmlSanitizer.Sanitize(candidate.SeoDescriptionAr ?? meta.SeoDescriptionAr);
                candidate.SeoKeywords = HtmlSanitizer.Sanitize(candidate.SeoKeywords ?? meta.SeoKeywordsEn);
                candidate.SeoKeywordsAr = HtmlSanitizer.Sanitize(candidate.SeoKeywordsAr ?? meta.SeoKeywordsAr);
                candidate.MetaGeneratedAt = meta.MetaGeneratedAt;
                candidate.MetaVersion = meta.MetaVersion;
                candidate.CanonicalUrl = _canonicalService.BuildCanonical("property", finalSlug);

                var codePrefix = (candidate.Location ?? "LOC").Substring(0, Math.Min(3, (candidate.Location ?? string.Empty).Length)).ToUpper() + "-" + (candidate.PropertyType.ToString() ?? "PT").Substring(0, Math.Min(2, candidate.PropertyType.ToString().Length)).ToUpper();
                Domain.Entities.CodeReservation? codeRes = null;
                string code = string.Empty;
                for (int i = 0; i < 10; i++)
                {
                    var suffix = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
                    code = $"{codePrefix}-{suffix}";
                    codeRes = await _reservations.TryCreateCodeReservationAsync("property", codePrefix, code);
                    if (codeRes != null) break;
                }

                if (codeRes == null)
                {
                    try { if (slugRes != null) await _reservations.ReleaseSlugReservationAsync(slugRes.Id); } catch (Exception ex) { _logger?.LogWarning(ex, "Failed to release slug reservation after code reservation failed"); }
                    throw new InvalidOperationException("Unable to reserve unique code for property");
                }

                candidate.Code = code;

                if (slugRes != null) slugRes.Property = candidate;
                if (codeRes != null) codeRes.Property = candidate;

                if ((slugRes != null && slugRes.Property == null) || (codeRes != null && codeRes.Property == null))
                {
                    try { await tx.RollbackAsync(); } catch (Exception rbx) { _logger?.LogError(rbx, "Rollback failed after reservation linking failure"); }
                    try { await _reservations.CleanupPendingReservationsAsync(); } catch (Exception cleanupEx) { _logger?.LogWarning(cleanupEx, "Failed to cleanup pending reservations after reservation linking failure"); }
                    _logger?.LogError("Reservation linking failed: navigation not attached for property candidate");
                    throw new InvalidOperationException("Reservation linking failed: navigation not attached");
                }

                await _unitOfWork.Properties.AddAsync(candidate);
                await _unitOfWork.CommitAsync();

                await tx.CommitAsync();
                createdProperty = candidate;
            }
            catch (Exception ex)
            {
                try { await tx.RollbackAsync(); }
                catch (Exception rbEx) { _logger?.LogError(rbEx, "Rollback failed for property creation"); }
                try { await _reservations.CleanupPendingReservationsAsync(); } catch (Exception cleanupEx) { _logger?.LogWarning(cleanupEx, "Failed to cleanup pending reservations after property creation failure"); }
                _logger?.LogError(ex, "Create property failed during commit");
                throw;
            }

            var reloaded = await _unitOfWork.Properties.Query()
                .Include(p => p.Images)
                .Include(p => p.Installments)
                .Include(p => p.Contact)
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == createdProperty!.Id);
            if (reloaded != null)
            {
                return reloaded;
            }
            return createdProperty!;
        }

        private async Task<string> GeneratePropertyCodeCandidateAsync(Property property)
        {
            string locationPart = string.IsNullOrEmpty(property.Location)
                ? "LOC"
                : property.Location.Substring(0, Math.Min(3, property.Location.Length)).ToUpper();

            var typeString = property.PropertyType.ToString();
            string typePart = string.IsNullOrEmpty(typeString)
                ? "PT"
                : typeString.Substring(0, Math.Min(2, typeString.Length)).ToUpper();

            string prefix = $"{locationPart}-{typePart}";
            string suffix = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
            var code = $"{prefix}-{suffix}";
            _logger?.LogInformation("Generated code candidate {Code} for prefix {Prefix}", code, prefix);
            return code;
        }

        public async Task<List<(int Id, string Url, string? PublicId)>> AddImagesAsync(int propertyId, List<(string Url, string? PublicId)> files)
        {
            var result = new List<(int Id, string Url, string? PublicId)>();

            if (files == null || files.Count == 0)
                return result;

            var urls = new List<(string Url, string? PublicId)>();
            foreach (var (file, publicId) in files)
            {
                if (string.IsNullOrWhiteSpace(file)) continue;
                if (!file.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !file.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    continue;

                var uri = new Uri(file);
                var allowedDomains = new[] { "res.cloudinary.com", "cloudinary.com" };
                if (!allowedDomains.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
                {
                    _logger?.LogWarning("Rejected external image URL with untrusted domain: {Host}", uri.Host);
                    continue;
                }

                urls.Add((file, publicId));
            }

            if (!urls.Any())
                return result;

            var property = await _unitOfWork.Properties.QueryTracked()
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == propertyId);
            if (property == null) throw new KeyNotFoundException("Property not found");

            property.Images ??= new List<PropertyImage>();

            var slug = property.Slug ?? string.Empty;

            var createdImages = new List<PropertyImage>();
            int idx = property.Images.Count + 1;
            foreach (var (u, pid) in urls)
            {
                var fname = $"{slug}-{idx++}.jpg";
                var img = new PropertyImage { Url = u, FileName = fname, PublicId = pid };
                img.Property = property;
                property.Images.Add(img);
                createdImages.Add(img);
            }

            property.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Properties.Update(property);
            await _unitOfWork.CommitAsync();

            return createdImages.Select(i => (i.Id, i.Url, i.PublicId)).ToList();
        }

        public async Task<bool> RemoveImageAsync(int propertyId, int imageId)
        {
            var prop = await _unitOfWork.Properties.QueryTracked()
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == propertyId);
            if (prop == null) return false;

            var img = prop.Images?.FirstOrDefault(i => i.Id == imageId);
            if (img == null) return false;

            if (!string.IsNullOrWhiteSpace(img.PublicId))
            {
                try { await _cloudinaryService.DeleteImageAsync(img.PublicId); }
                catch (Exception ex) { _logger?.LogWarning(ex, "Failed to delete Cloudinary image {PublicId}", img.PublicId); }
            }

            var ctx = _reservations.Context;
            if (ctx != null)
                ctx.Remove(img);

            prop.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task ReplaceImageAsync(int propertyId, int imageId, string newUrl, string? newPublicId)
        {
            var prop = await _unitOfWork.Properties.QueryTracked()
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == propertyId);
            if (prop == null) throw new KeyNotFoundException("Property not found");

            var img = prop.Images?.FirstOrDefault(i => i.Id == imageId);
            if (img == null) throw new KeyNotFoundException("Image not found");

            if (!string.IsNullOrWhiteSpace(img.PublicId))
            {
                try { await _cloudinaryService.DeleteImageAsync(img.PublicId); }
                catch (Exception ex) { _logger?.LogWarning(ex, "Failed to delete old Cloudinary image {PublicId}", img.PublicId); }
            }

            img.Url = newUrl;
            img.PublicId = newPublicId;
            prop.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CommitAsync();
        }

        public async Task<Property?> GetByIdAsync(int id)
        {
            return await _unitOfWork.Properties.Query()
                .Include(p => p.Images)
                .Include(p => p.Installments)
                .Include(p => p.Contact)
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        private async Task DeleteCloudinaryImageSafe(string publicId)
        {
            try { await _cloudinaryService.DeleteImageAsync(publicId); }
            catch (Exception ex) { _logger?.LogWarning(ex, "Failed to delete Cloudinary image {PublicId}", publicId); }
        }

        public async Task IncrementViewCountAsync(int id)
        {
            try
            {
                var prop = await _unitOfWork.Properties.QueryTracked().FirstOrDefaultAsync(p => p.Id == id);
                if (prop == null || prop.IsDeleted) return;
                prop.ViewCount += 1;
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to increment view count for property {PropertyId}", id);
            }
        }

        public async Task<Property> PatchAsync(int id, PatchPropertyDto dto)
        {
            var tx = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var prop = await _unitOfWork.Properties.QueryTracked()
                    .Include(p => p.Images)
                    .Include(p => p.Installments)
                    .Include(p => p.Contact)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(p => p.Id == id);
                if (prop == null) throw new KeyNotFoundException("Property not found");

                // Duplicate check: prevent creating a duplicate when changing title/location
                if (dto.TitleEn != null || dto.Location != null)
                {
                    var dupTitleEn = (dto.TitleEn ?? prop.TitleEn ?? string.Empty).Trim();
                    var dupLocation = (dto.Location ?? prop.Location ?? string.Empty).Trim();
                    var dup = await _unitOfWork.Properties.Query()
                        .AnyAsync(p =>
                            ((p.TitleEn ?? string.Empty).Trim() == dupTitleEn.Trim())
                            && ((p.Location ?? string.Empty).Trim() == dupLocation.Trim())
                            && p.Id != id
                            && !p.IsDeleted);
                    if (dup)
                        throw new InvalidOperationException("A property with the same name and location already exists.");
                }

                bool slugWasAuto = prop.SlugIsAuto || string.IsNullOrWhiteSpace(prop.Slug);
                bool titleChanged = false;

                // Apply partial updates — only fields that are explicitly provided
                if (dto.TitleEn != null) { titleChanged = dto.TitleEn != prop.TitleEn; prop.TitleEn = dto.TitleEn; }
                if (dto.TitleAr != null) prop.TitleAr = dto.TitleAr;
                if (dto.DescriptionEn != null) prop.DescriptionEn = dto.DescriptionEn;
                if (dto.DescriptionAr != null) prop.DescriptionAr = dto.DescriptionAr;
                if (dto.Price.HasValue) prop.Price = dto.Price.Value;
                if (dto.RentPerMonth.HasValue) prop.RentPerMonth = dto.RentPerMonth.Value;
                if (dto.Location != null) prop.Location = dto.Location;
                var userProvidedLocationAr = dto.LocationAr;
                if (dto.LocationAr != null) prop.LocationAr = dto.LocationAr;
                if (dto.GovernorateId.HasValue || dto.CityId.HasValue || dto.AreaId.HasValue)
                {
                    var resolved = await _locationService.ResolveLocationAsync(dto.GovernorateId, dto.CityId, dto.AreaId, default);
                    if (resolved != null)
                    {
                        prop.Location = resolved.LocationString;
                        prop.LocationAr = resolved.LocationStringAr;
                        prop.LocationId = resolved.DeepestId;
                    }
                }
                else if (dto.Location != null && !string.IsNullOrWhiteSpace(dto.Location))
                {
                    var resolved = await _locationService.ResolveOrCreateFromStringAsync(dto.Location, default);
                    if (resolved != null)
                    {
                        prop.Location = resolved.LocationString;
                        prop.LocationAr = resolved.LocationStringAr;
                        prop.LocationId = resolved.DeepestId;
                    }
                }
                if (!string.IsNullOrWhiteSpace(userProvidedLocationAr))
                {
                    prop.LocationAr = userProvidedLocationAr;
                }
                if (dto.Size.HasValue) prop.Size = dto.Size.Value;
                if (dto.IsFeatured.HasValue) prop.IsFeatured = dto.IsFeatured.Value;
                if (dto.PropertyType.HasValue) prop.PropertyType = dto.PropertyType.Value;
                if (dto.ListingType.HasValue) prop.ListingType = dto.ListingType.Value;
                if (dto.Features != null) prop.Features = dto.Features;
                if (dto.FeaturesAr != null) prop.FeaturesAr = dto.FeaturesAr;
                if (dto.Bedrooms.HasValue) prop.Bedrooms = dto.Bedrooms.Value;
                if (dto.Bathrooms.HasValue) prop.Bathrooms = dto.Bathrooms.Value;
                if (dto.Floor.HasValue) prop.Floor = dto.Floor.Value;
                if (dto.TotalFloors.HasValue) prop.TotalFloors = dto.TotalFloors.Value;
                if (dto.IsFurnished.HasValue) prop.IsFurnished = dto.IsFurnished.Value;
                if (dto.View.HasValue) prop.View = dto.View.Value;
                if (dto.SortOrder.HasValue) prop.SortOrder = dto.SortOrder.Value;
                if (dto.IsRecommended.HasValue) prop.IsRecommended = dto.IsRecommended.Value;
                if (dto.DeliveryText != null) prop.DeliveryText = dto.DeliveryText;
                if (dto.DeliveryTextAr != null) prop.DeliveryTextAr = dto.DeliveryTextAr;
                if (dto.ConstructionStatus.HasValue) prop.ConstructionStatus = dto.ConstructionStatus.Value;
                if (dto.AvailabilityStatus != null) prop.AvailabilityStatus = dto.AvailabilityStatus;
                if (dto.OwnershipType.HasValue) prop.OwnershipType = dto.OwnershipType.Value;
                if (dto.VirtualTourUrl != null) prop.VirtualTourUrl = dto.VirtualTourUrl;
                if (dto.HighlightsAr != null) prop.HighlightsAr = dto.HighlightsAr;
                if (dto.NearbyPlaces != null) prop.NearbyPlaces = dto.NearbyPlaces;
                if (dto.NearbyPlacesAr != null) prop.NearbyPlacesAr = dto.NearbyPlacesAr;

                if (dto.Contact != null)
                {
                    var existingContact = await _unitOfWork.Contacts.Query()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Phone == dto.Contact.Phone);

                    if (existingContact != null)
                    {
                        if (prop.Contact?.Id == existingContact.Id)
                        {
                            if (dto.Contact.Name != null) prop.Contact.Name = dto.Contact.Name;
                            prop.Contact.Type = dto.Contact.Type;
                        }
                        else
                        {
                            var tracked = await _unitOfWork.Contacts.QueryTracked()
                                .FirstAsync(c => c.Id == existingContact.Id);
                            prop.Contact = tracked;
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
                        await _unitOfWork.Contacts.AddAsync(newContact);
                        prop.Contact = newContact;
                    }
                }

                // Slug regeneration — build candidate in memory, do NOT commit yet
                if ((dto.SlugRegenerateRequested == true || titleChanged) && slugWasAuto && !string.IsNullOrWhiteSpace(prop.TitleEn))
                {
                    string? newSlug = null;
                    string? newCanonical = null;

                    await _metaService.GenerateMeta("property",
                        async (s, m) =>
                        {
                            newSlug = s;
                            prop.SlugLanguage = m.SlugLanguage;

                            if (string.IsNullOrWhiteSpace(prop.SeoTitle)) prop.SeoTitle = m.SeoTitleEn;
                            if (string.IsNullOrWhiteSpace(prop.SeoTitleAr)) prop.SeoTitleAr = m.SeoTitleAr;
                            if (string.IsNullOrWhiteSpace(prop.SeoDescription)) prop.SeoDescription = m.SeoDescriptionEn;
                            if (string.IsNullOrWhiteSpace(prop.SeoDescriptionAr)) prop.SeoDescriptionAr = m.SeoDescriptionAr;
                            if (string.IsNullOrWhiteSpace(prop.SeoKeywords)) prop.SeoKeywords = m.SeoKeywordsEn;
                            if (string.IsNullOrWhiteSpace(prop.SeoKeywordsAr)) prop.SeoKeywordsAr = m.SeoKeywordsAr;
                            prop.MetaGeneratedAt = m.MetaGeneratedAt;
                            prop.MetaVersion = m.MetaVersion;
                        },
                        async () => { await Task.CompletedTask; },
                        prop.TitleEn,
                        prop.TitleAr,
                        prop.DescriptionEn,
                        prop.DescriptionAr,
                        prop.Location);

                    if (!string.IsNullOrWhiteSpace(newSlug))
                    {
                        var baseSlug = newSlug;
                        var attemptsLimit = 4;
                        for (int attempt = 1; attempt <= attemptsLimit; attempt++)
                        {
                            var attemptSlug = baseSlug;
                            if (attempt > 1)
                            {
                                attemptSlug = _slugService.NormalizeSlug(attempt < 3 ? baseSlug + "-" + attempt.ToString() : baseSlug + "-" + Guid.NewGuid().ToString("N").Substring(0, 6));
                            }

                            var exists = await _unitOfWork.Properties.Query()
                                .AnyAsync(p => p.Slug == attemptSlug && p.Id != id && !p.IsDeleted);
                            if (!exists)
                            {
                                prop.Slug = attemptSlug;
                                prop.SlugIsAuto = true;
                                newCanonical = _canonicalService.BuildCanonical("property", attemptSlug);
                                break;
                            }
                            if (attempt == attemptsLimit) throw new SlugConflictException("Unable to generate unique slug for property");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(newCanonical))
                    {
                        prop.CanonicalUrl = newCanonical;
                    }
                }

                // SEO overrides (only when explicitly provided)
                if (dto.SeoTitle != null) prop.SeoTitle = string.IsNullOrWhiteSpace(dto.SeoTitle) ? null : HtmlSanitizer.Sanitize(dto.SeoTitle);
                if (dto.SeoDescription != null) prop.SeoDescription = string.IsNullOrWhiteSpace(dto.SeoDescription) ? null : HtmlSanitizer.Sanitize(dto.SeoDescription);
                if (dto.SeoKeywords != null) prop.SeoKeywords = string.IsNullOrWhiteSpace(dto.SeoKeywords) ? null : HtmlSanitizer.Sanitize(dto.SeoKeywords);
                if (dto.SeoTitleAr != null) prop.SeoTitleAr = string.IsNullOrWhiteSpace(dto.SeoTitleAr) ? null : HtmlSanitizer.Sanitize(dto.SeoTitleAr);
                if (dto.SeoDescriptionAr != null) prop.SeoDescriptionAr = string.IsNullOrWhiteSpace(dto.SeoDescriptionAr) ? null : HtmlSanitizer.Sanitize(dto.SeoDescriptionAr);
                if (dto.SeoKeywordsAr != null) prop.SeoKeywordsAr = string.IsNullOrWhiteSpace(dto.SeoKeywordsAr) ? null : HtmlSanitizer.Sanitize(dto.SeoKeywordsAr);

                // Installments — soft-delete existing tracked ones, then add new via repo
                if (dto.Installments != null)
                {
                    var existingInsts = prop.Installments?.Where(x => !x.IsDeleted).ToList() ?? new List<Domain.Entities.PropertyInstallmentPlan>();
                    foreach (var inst in existingInsts)
                    {
                        inst.IsDeleted = true;
                    }

                    foreach (var instDto in dto.Installments)
                    {
                        var newInst = new Domain.Entities.PropertyInstallmentPlan
                        {
                            PropertyId = id,
                            PaymentType = instDto.PaymentType,
                            DownPaymentPercent = instDto.PaymentType == PaymentType.Cash ? 100 : instDto.DownPaymentPercent,
                            DiscountPercent = instDto.DiscountPercent,
                            Years = instDto.PaymentType == PaymentType.Cash ? 0 : instDto.Years,
                            IsEnabled = instDto.IsEnabled,
                            IsDeleted = false
                        };
                        await _unitOfWork.PropertyInstallmentPlans.AddAsync(newInst);
                    }
                }

                prop.UpdatedAt = DateTime.UtcNow;

                // Single commit at the end — no intermediate commits
                await _unitOfWork.CommitAsync();
                await tx.CommitAsync();

                var reloaded = await _unitOfWork.Properties.Query()
                    .Include(p => p.Images)
                    .Include(p => p.Installments)
                    .Include(p => p.Contact)
                    .AsSplitQuery()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id);
                if (reloaded != null) return reloaded;
                return prop;
            }
            catch (KeyNotFoundException)
            {
                try { await tx.RollbackAsync(); } catch (Exception rbEx) { _logger?.LogWarning(rbEx, "Rollback failed in patch (KeyNotFoundException path)"); }
                throw;
            }
            catch (SlugConflictException)
            {
                try { await tx.RollbackAsync(); } catch (Exception rbEx) { _logger?.LogWarning(rbEx, "Rollback failed in patch (SlugConflictException path)"); }
                throw;
            }
            catch (Exception ex)
            {
                try { await tx.RollbackAsync(); } catch (Exception rbEx) { _logger?.LogWarning(rbEx, "Rollback failed in patch (general error path)"); }
                _logger?.LogError(ex, "Patch property failed for property {PropertyId}. InnerException: {Inner}", id, ex.InnerException?.Message);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var tx = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var prop = await _unitOfWork.Properties.QueryTracked().IgnoreQueryFilters()
                    .Include(p => p.Images)
                    .Include(p => p.Videos)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(p => p.Id == id);
                if (prop == null)
                {
                    await tx.RollbackAsync();
                    return false;
                }

                if (prop.Images != null)
                {
                    var deleteTasks = new List<Task>();
                    foreach (var img in prop.Images)
                    {
                        if (!string.IsNullOrWhiteSpace(img.PublicId))
                        {
                            deleteTasks.Add(DeleteCloudinaryImageSafe(img.PublicId));
                        }
                    }
                    await Task.WhenAll(deleteTasks);
                }

                if (prop.Videos != null)
                {
                    foreach (var video in prop.Videos)
                    {
                        if (!string.IsNullOrWhiteSpace(video.PublicId))
                        {
                            await _videoUploadService.DeleteVideoAsync(video.PublicId);
                        }
                    }
                }

                if (_reservations.Context != null)
                {
                    var ctx = _reservations.Context;
                    var codeRes = await ctx.Set<Domain.Entities.CodeReservation>()
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(cr => cr.EntityType == "property"
                            && EF.Property<int?>(cr, "PropertyId") == id);
                    if (codeRes != null)
                    {
                        ctx.Remove(codeRes);
                    }

                    var slugRes = await ctx.Set<Domain.Entities.SlugReservation>()
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(cr => cr.EntityType == "property"
                            && EF.Property<int?>(cr, "PropertyId") == id);
                    if (slugRes != null)
                    {
                        ctx.Remove(slugRes);
                    }
                }

                _unitOfWork.Properties.Delete(prop);

                await _unitOfWork.CommitAsync();
                await tx.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger?.LogError(ex, "Delete failed for property {PropertyId}", id);
                throw;
            }
        }

        public async Task<(List<Application.DTOs.PropertyPublicDto> Data, int Total, int Page, int PageSize, int TotalPages)> GetPublicAsync(
            decimal? minPrice,
            decimal? maxPrice,
            string? location,
            string? propertyType,
            string? listingType,
            string? locations,
            string? types,
            bool? isFeatured,
            bool? hasInstallment,
            int page,
            int pageSize,
            string sortBy,
            string sortOrder)
        {
            PropertyListingType? parsedListing = null;

            if (!string.IsNullOrWhiteSpace(listingType) && Enum.TryParse<PropertyListingType>(listingType, true, out var parsedListingType))
                parsedListing = parsedListingType;

            var locationList = locations?
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            var typeList = types?
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            var query = _unitOfWork.Properties.Query();

            if (parsedListing.HasValue)
                query = query.Where(p => p.ListingType == parsedListing.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            if (!string.IsNullOrEmpty(location))
                query = query.Where(p => p.Location.Contains(location) || (p.LocationAr != null && p.LocationAr.Contains(location)));

            if (locationList != null && locationList.Any())
                query = query.Where(p => locationList.Contains(p.Location) || (p.LocationAr != null && locationList.Contains(p.LocationAr)));

            if (!string.IsNullOrEmpty(propertyType) &&
                Enum.TryParse<PropertyType>(propertyType, true, out var parsedPropType))
            {
                query = query.Where(p => p.PropertyType == parsedPropType);
            }

            if (typeList != null && typeList.Any())
            {
                var parsedTypes = typeList
                    .Select(t => (ok: Enum.TryParse<PropertyType>(t, true, out var v), val: v))
                    .Where(x => x.ok)
                    .Select(x => x.val)
                    .ToList();

                if (parsedTypes.Any())
                    query = query.Where(p => parsedTypes.Contains(p.PropertyType));
            }

            if (isFeatured.HasValue)
                query = query.Where(p => p.IsFeatured == isFeatured);

            var installmentQuery = _unitOfWork.PropertyInstallmentPlans.Query();
            if (hasInstallment.HasValue)
            {
                if (hasInstallment.Value)
                    query = query.Where(p => installmentQuery.Any(ip => ip.PropertyId == p.Id && !ip.IsDeleted && ip.IsEnabled));
                else
                    query = query.Where(p => !installmentQuery.Any(ip => ip.PropertyId == p.Id && !ip.IsDeleted && ip.IsEnabled));
            }

            var total = await query.CountAsync();

            query = sortBy.ToLower() switch
            {
                "price" => sortOrder == "asc" ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price),
                _ => sortOrder == "asc" ? query.OrderBy(p => p.Id) : query.OrderByDescending(p => p.Id)
            };

            var items = await query
                .Include(x => x.Installments)
                .Include(x => x.Images)
                .Include(x => x.Contact)
                .AsSplitQuery()
                .AsNoTracking()
                .Take(1000)
                .ToListAsync();

            var data = items.Select(p => new Application.DTOs.PropertyPublicDto
            {
                Id = p.Id,
                TitleEn = p.TitleEn,
                TitleAr = p.TitleAr,
                DescriptionEn = p.DescriptionEn,
                DescriptionAr = p.DescriptionAr,
                Price = p.Price,
                Location = p.Location,
                LocationAr = p.LocationAr,
                Size = p.Size,
                RentPerMonth = p.RentPerMonth,
                Currency = p.Currency,
                IsFeatured = p.IsFeatured,
                PropertyType = p.PropertyType.ToString(),
                ListingType = p.ListingType.ToString(),
                Images = p.Images == null ? new List<string>() : p.Images.Select(i => i.Url).ToList(),
                Features = p.Features ?? new List<string>(),
                FeaturesAr = p.FeaturesAr ?? new List<string>(),
                Installments = p.Installments == null
                    ? new List<Application.DTOs.InstallmentDto>()
                    : p.Installments
                        .Where(i => !i.IsDeleted && i.IsEnabled)
                        .Select(i => new Application.DTOs.InstallmentDto
                        {
                            PaymentType = i.PaymentType.ToString(),
                            DownPaymentPercent = i.DownPaymentPercent,
                            Years = i.Years,
                            IsEnabled = i.IsEnabled,
                            IsDeleted = i.IsDeleted
                        }).ToList(),
                Slug = p.Slug,
                SeoTitle = p.SeoTitle,
                SeoDescription = p.SeoDescription,
                SeoTitleAr = p.SeoTitleAr,
                SeoDescriptionAr = p.SeoDescriptionAr,
                SeoKeywords = p.SeoKeywords,
                SeoKeywordsAr = p.SeoKeywordsAr,
                CanonicalUrl = p.CanonicalUrl,
                JsonLd = BuildJsonLd(p)
            }).ToList();

            return (data, total, page, pageSize, (int)Math.Ceiling((double)total / pageSize));
        }

        public async Task<Application.DTOs.PropertyPublicDto?> GetPublicByIdAsync(int id)
        {
            var p = await _unitOfWork.Properties.Query()
                .Include(x => x.Installments)
                .Include(x => x.Images)
                .Include(x => x.Contact)
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p == null) return null;

            var insts = p.Installments == null
                ? new List<InstallmentDto>()
                : p.Installments
                    .Where(i => !i.IsDeleted && i.IsEnabled)
                    .Select(i => new InstallmentDto
                    {
                        PaymentType = i.PaymentType.ToString(),
                        DownPaymentPercent = i.DownPaymentPercent,
                        Years = i.Years,
                        IsEnabled = i.IsEnabled,
                        IsDeleted = i.IsDeleted
                    }).ToList();

            var images = p.Images == null ? new List<string>() : p.Images.Select(i => i.Url).ToList();
            if (_cloudinaryService != null && images.Any())
            {
                images = images.Select(u => _cloudinaryService.GetOptimizedUrl(u)).ToList();
            }

            var dto = new Application.DTOs.PropertyPublicDto
            {
                Id = p.Id,
                TitleEn = p.TitleEn,
                TitleAr = p.TitleAr,
                DescriptionEn = p.DescriptionEn,
                DescriptionAr = p.DescriptionAr,
                Price = p.Price,
                Location = p.Location,
                LocationAr = p.LocationAr,
                Size = p.Size,
                RentPerMonth = p.RentPerMonth,
                Currency = p.Currency,
                IsFeatured = p.IsFeatured,
                PropertyType = p.PropertyType.ToString(),
                ListingType = p.ListingType.ToString(),
                Images = images,
                Features = p.Features ?? new List<string>(),
                FeaturesAr = p.FeaturesAr ?? new List<string>(),
                Installments = insts,
                Slug = p.Slug,
                SeoTitle = p.SeoTitle,
                SeoDescription = p.SeoDescription,
                SeoTitleAr = p.SeoTitleAr,
                SeoDescriptionAr = p.SeoDescriptionAr,
                SeoKeywords = p.SeoKeywords,
                SeoKeywordsAr = p.SeoKeywordsAr,
                CanonicalUrl = p.CanonicalUrl,
                JsonLd = BuildJsonLd(p),
                ImagesMeta = images.Select(u => new ImageDto { Url = u, Width = 1200, Height = 800 }).ToList()
            };

            return dto;
        }

        public async Task<Application.DTOs.PropertyAdminDto?> GetAdminByCodeAsync(string code)
        {
            var p = await _unitOfWork.Properties.Query().IgnoreQueryFilters()
                .Include(x => x.Installments)
                .Include(x => x.Images)
                .Include(x => x.Contact)
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == code);

            if (p == null) return null;

            return MapToAdminDto(p);
        }

        public async Task<Application.DTOs.PropertyAdminDto?> GetAdminByIdAsync(int id)
        {
            var p = await _unitOfWork.Properties.Query().IgnoreQueryFilters()
                .Include(x => x.Installments)
                .Include(x => x.Images)
                .Include(x => x.Contact)
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p == null) return null;

            return MapToAdminDto(p);
        }

        private Application.DTOs.PropertyAdminDto MapToAdminDto(Property p)
        {
            return new Application.DTOs.PropertyAdminDto
            {
                Id = p.Id,
                TitleEn = p.TitleEn,
                TitleAr = p.TitleAr,
                DescriptionEn = p.DescriptionEn,
                DescriptionAr = p.DescriptionAr,
                Price = p.Price,
                Location = p.Location,
                LocationAr = p.LocationAr,
                Size = p.Size,
                RentPerMonth = p.RentPerMonth,
                Currency = p.Currency,
                IsFeatured = p.IsFeatured,
                PropertyType = p.PropertyType.ToString(),
                ListingType = p.ListingType.ToString(),
                Images = p.Images == null ? new List<string>() : p.Images.Select(i => i.Url).ToList(),
                Features = p.Features ?? new List<string>(),
                FeaturesAr = p.FeaturesAr ?? new List<string>(),
                Installments = p.Installments == null
                    ? new List<Application.DTOs.InstallmentDto>()
                    : p.Installments
                        .Where(i => !i.IsDeleted)
                        .Select(i => new Application.DTOs.InstallmentDto
                        {
                            PaymentType = i.PaymentType.ToString(),
                            DownPaymentPercent = i.DownPaymentPercent,
                            Years = i.Years,
                            IsEnabled = i.IsEnabled,
                            IsDeleted = i.IsDeleted
                        }).ToList(),
                Code = p.Code,
                Contact = p.Contact == null ? null : new Application.DTOs.ContactDto
                {
                    Name = p.Contact.Name,
                    Phone = p.Contact.Phone,
                    Type = p.Contact.Type
                },
                AdminImages = p.Images == null ? new List<Application.DTOs.ImageInfoDto>() : p.Images
                    .Where(i => !i.IsDeleted)
                    .Select(i => new Application.DTOs.ImageInfoDto
                    {
                        Id = i.Id,
                        Url = i.Url,
                        PublicId = i.PublicId
                    }).ToList(),
                SeoKeywords = p.SeoKeywords,
                SeoKeywordsAr = p.SeoKeywordsAr,
                CanonicalUrl = p.CanonicalUrl,
                SeoTitleAr = p.SeoTitleAr,
                SeoDescriptionAr = p.SeoDescriptionAr,
                SeoTitle = p.SeoTitle,
                SeoDescription = p.SeoDescription,
                Slug = p.Slug,
                SlugIsAuto = p.SlugIsAuto,
                SlugLanguage = p.SlugLanguage,
                JsonLd = BuildJsonLd(p)
            };
        }

        public async Task<Property> UpdateAsync(int id, CreatePropertyDto dto)
        {
            var property = await _unitOfWork.Properties.QueryTracked().IgnoreQueryFilters()
                .Include(p => p.Installments)
                .Include(p => p.Images)
                .Include(p => p.Contact)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == id);
            if (property == null) throw new ArgumentException("Property not found");
            if (property.IsDeleted) throw new InvalidOperationException("Cannot update deleted property");

            var tx = await _unitOfWork.BeginTransactionAsync();
            try
            {
                property.TitleEn = dto.TitleEn;
                property.TitleAr = dto.TitleAr;
                property.DescriptionEn = dto.DescriptionEn;
                property.DescriptionAr = dto.DescriptionAr;
                property.Price = dto.Price;
                property.Location = dto.Location;
                property.LocationAr = dto.LocationAr;

                if (dto.GovernorateId.HasValue || dto.CityId.HasValue || dto.AreaId.HasValue)
                {
                    var resolved = await _locationService.ResolveLocationAsync(dto.GovernorateId, dto.CityId, dto.AreaId, default);
                    if (resolved != null)
                    {
                        property.Location = resolved.LocationString;
                        property.LocationAr = resolved.LocationStringAr;
                        property.LocationId = resolved.DeepestId;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(dto.Location))
                {
                    var resolved = await _locationService.ResolveOrCreateFromStringAsync(dto.Location, default);
                    if (resolved != null)
                    {
                        property.Location = resolved.LocationString;
                        property.LocationId = resolved.DeepestId;
                    }
                }

                property.Size = dto.Size;
                property.IsFeatured = dto.IsFeatured;
                property.Features = dto.Features ?? new List<string>();
                property.FeaturesAr = dto.FeaturesAr ?? new List<string>();
                property.ListingType = dto.ListingType;

                // Regenerate slug if title changed and slug was auto
                if (property.SlugIsAuto && !string.IsNullOrWhiteSpace(property.TitleEn))
                {
                    var metaForUpdate = await _metaService.GenerateAsync("property", property.TitleEn, property.TitleAr ?? string.Empty, property.DescriptionEn ?? string.Empty, property.DescriptionAr ?? string.Empty, property.Location ?? string.Empty);
                    property.Slug = metaForUpdate.BaseSlug;
                    property.SlugIsAuto = true;
                    property.SlugLanguage = metaForUpdate.SlugLanguage;
                    if (string.IsNullOrWhiteSpace(property.SeoTitle)) property.SeoTitle = metaForUpdate.SeoTitleEn;
                    if (string.IsNullOrWhiteSpace(property.SeoTitleAr)) property.SeoTitleAr = metaForUpdate.SeoTitleAr;
                    if (string.IsNullOrWhiteSpace(property.SeoDescription)) property.SeoDescription = metaForUpdate.SeoDescriptionEn;
                    if (string.IsNullOrWhiteSpace(property.SeoDescriptionAr)) property.SeoDescriptionAr = metaForUpdate.SeoDescriptionAr;
                    if (string.IsNullOrWhiteSpace(property.SeoKeywords)) property.SeoKeywords = metaForUpdate.SeoKeywordsEn;
                    if (string.IsNullOrWhiteSpace(property.SeoKeywordsAr)) property.SeoKeywordsAr = metaForUpdate.SeoKeywordsAr;
                    property.MetaGeneratedAt = metaForUpdate.MetaGeneratedAt;
                    property.MetaVersion = metaForUpdate.MetaVersion;
                    property.CanonicalUrl = _canonicalService.BuildCanonical("property", property.Slug);
                }

                property.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Properties.Update(property);

                var existingInsts = await _unitOfWork.PropertyInstallmentPlans.QueryTracked()
                    .Where(x => x.PropertyId == id && !x.IsDeleted)
                    .ToListAsync();

                foreach (var inst in existingInsts)
                {
                    inst.IsDeleted = true;
                }

                if (dto.Installments != null && dto.Installments.Count > 0)
                {
                    foreach (var instDto in dto.Installments)
                    {
                        var newInst = new Domain.Entities.PropertyInstallmentPlan
                        {
                            PropertyId = id,
                            PaymentType = instDto.PaymentType,
                            DownPaymentPercent = instDto.PaymentType == PaymentType.Cash ? 100 : instDto.DownPaymentPercent,
                            DiscountPercent = instDto.DiscountPercent,
                            Years = instDto.PaymentType == PaymentType.Cash ? 0 : instDto.Years,
                            IsEnabled = instDto.IsEnabled,
                            IsDeleted = false
                        };
                        await _unitOfWork.PropertyInstallmentPlans.AddAsync(newInst);
                    }
                }

                await _unitOfWork.CommitAsync();
                await tx.CommitAsync();
                return property;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger?.LogError(ex, "Update failed for property {PropertyId}", id);
                throw;
            }
        }

        private string BuildJsonLd(Property p)
        {
            var images = p.Images?.Select(i => i.Url).Where(u => !string.IsNullOrEmpty(u)).ToList();
            return _jsonLdService.BuildPropertyJsonLd(
                p.TitleEn,
                p.DescriptionEn,
                p.SeoDescription,
                p.CanonicalUrl,
                p.Code,
                p.Location,
                p.Currency,
                p.ListingType.ToString(),
                p.Price,
                p.RentPerMonth,
                images,
                p.Code);
        }
    }
}
