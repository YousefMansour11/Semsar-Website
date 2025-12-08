using Application.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{
    public class PropertyQueryService : IPropertyQueryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IInstallmentQueryService _installmentQueryService;
        private readonly IJsonLdService _jsonLdService;
        private readonly ICanonicalService _canonicalService;
        private readonly ISearchService _searchService;
        private readonly ISeoContentGenerator _seoContentGenerator;
        private readonly IInternalLinkingService _internalLinkingService;
        private readonly ISERPVariantGenerator _serpVariantGenerator;
        private readonly IEntityGraphService _entityGraphService;
        private readonly ISemanticDeduplicationService _semanticDedup;
        private readonly ISeoValidationGate _seoValidationGate;
        private readonly IPublicIdService _publicIdService;
        private readonly Application.Interfaces.ICloudinaryService? _cloud;
        private readonly Microsoft.Extensions.Logging.ILogger<PropertyQueryService>? _logger;

        private static List<string> SafeSelectImages(IEnumerable<Domain.Entities.PropertyImage>? images)
        {
            return images?.Select(i => i.Url).ToList() ?? new List<string>();
        }

        private static List<string> SafeSelectUnitImages(IEnumerable<Domain.Entities.UnitImage>? images)
        {
            return images?.Select(i => i.Url).ToList() ?? new List<string>();
        }

        public PropertyQueryService(IUnitOfWork unitOfWork, IInstallmentQueryService installmentQueryService, IJsonLdService jsonLdService, ICanonicalService canonicalService, ISearchService searchService, ISeoContentGenerator seoContentGenerator, IInternalLinkingService internalLinkingService, ISERPVariantGenerator serpVariantGenerator, IEntityGraphService entityGraphService, ISemanticDeduplicationService semanticDedup, ISeoValidationGate seoValidationGate, IPublicIdService publicIdService, Application.Interfaces.ICloudinaryService? cloud = null, ILogger<PropertyQueryService>? logger = null)
        {
            _unitOfWork = unitOfWork;
            _installmentQueryService = installmentQueryService;
            _jsonLdService = jsonLdService ?? throw new ArgumentNullException(nameof(jsonLdService));
            _canonicalService = canonicalService ?? throw new ArgumentNullException(nameof(canonicalService));
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _seoContentGenerator = seoContentGenerator ?? throw new ArgumentNullException(nameof(seoContentGenerator));
            _internalLinkingService = internalLinkingService ?? throw new ArgumentNullException(nameof(internalLinkingService));
            _serpVariantGenerator = serpVariantGenerator ?? throw new ArgumentNullException(nameof(serpVariantGenerator));
            _entityGraphService = entityGraphService ?? throw new ArgumentNullException(nameof(entityGraphService));
            _semanticDedup = semanticDedup ?? throw new ArgumentNullException(nameof(semanticDedup));
            _seoValidationGate = seoValidationGate ?? throw new ArgumentNullException(nameof(seoValidationGate));
            _publicIdService = publicIdService;
            _cloud = cloud;
            _logger = logger;
        }

        private string ResolvePublicKey(string? publicKey, string entityTypePrefix)
        {
            return !string.IsNullOrEmpty(publicKey) ? publicKey : _publicIdService.GenerateId(entityTypePrefix);
        }

        public async Task<(List<PropertyPublicDto> Data, int Total, int Page, int PageSize, int TotalPages)> GetPublicAsync(
            decimal? minPrice,
            decimal? maxPrice,
            string? location,
            string? propertyType,
            string? listingType,
            string? locations,
            string? types,
            bool? isFeatured,
            bool? hasInstallment,
            double? minSize,
            double? maxSize,
            int page,
            int pageSize,
            string sortBy,
            string sortOrder,
            CancellationToken ct = default)
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

            var query = _unitOfWork.Properties.Query().AsNoTracking();

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

            if (minSize.HasValue)
                query = query.Where(p => p.Size >= minSize.Value);

            if (maxSize.HasValue)
                query = query.Where(p => p.Size <= maxSize.Value);

            var total = await query.CountAsync(ct);

            query = sortBy.ToLower() switch
            {
                "price" => sortOrder == "asc" ? query.OrderBy(p => p.SortOrder).ThenBy(p => p.Price) : query.OrderBy(p => p.SortOrder).ThenByDescending(p => p.Price),
                _ => sortOrder == "asc" ? query.OrderBy(p => p.SortOrder).ThenBy(p => p.Id) : query.OrderBy(p => p.SortOrder).ThenByDescending(p => p.Id)
            };

            pageSize = Math.Min(Math.Max(1, pageSize), 50);

            var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
                .Include(p => p.Images)
                .Include(p => p.Installments)
                .AsSplitQuery()
                .Select(p => new
                {
                    p.Id,
                    p.PublicKey,
                    p.TitleEn,
                    p.TitleAr,
                    p.DescriptionEn,
                    p.DescriptionAr,
                    p.Price,
                    p.Location,
                    LocationAr = p.LocationAr,
                    p.Size,
                    p.RentPerMonth,
                    p.Currency,
                    p.IsFeatured,
                    PropertyType = p.PropertyType.ToString(),
                    ListingType = p.ListingType.ToString(),
                    Images = p.Images != null ? p.Images.Select(i => i.Url) : Enumerable.Empty<string>(),
                    Features = p.Features,
                    FeaturesAr = p.FeaturesAr,
                    p.Code,
                    Slug = p.Slug,
                    SeoTitle = p.SeoTitle,
                    SeoDescription = p.SeoDescription,
                    SeoTitleAr = p.SeoTitleAr,
                    SeoDescriptionAr = p.SeoDescriptionAr,
                    SeoKeywords = p.SeoKeywords,
                    SeoKeywordsAr = p.SeoKeywordsAr,
                    CanonicalUrl = p.CanonicalUrl,
                    p.SortOrder,
                    DeliveryText = p.DeliveryText,
                    DeliveryTextAr = p.DeliveryTextAr,
                    IsRecommended = p.IsRecommended,
                    ConstructionStatus = p.ConstructionStatus != null ? p.ConstructionStatus.ToString() : null,
                    AvailabilityStatus = p.AvailabilityStatus ?? "Available",
                    OwnershipType = p.OwnershipType != null ? p.OwnershipType.ToString() : null,
                    ViewCount = p.ViewCount,
                    InquiryCount = p.InquiryCount,
                    FavoriteCount = p.FavoriteCount,
                    VirtualTourUrl = p.VirtualTourUrl,
                    HighlightsAr = p.HighlightsAr,
                    NearbyPlaces = p.NearbyPlaces,
                    NearbyPlacesAr = p.NearbyPlacesAr
                })
                .ToListAsync(ct);

            var propIds = items.Select(i => i.Id).ToList();
            var installments = await _installmentQueryService.GetPublicByPropertyIdsAsync(propIds);

            var data = new List<PropertyPublicDto>(items.Count);
            foreach (var p in items)
            {
                var dto = new PropertyPublicDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    PublicKey = ResolvePublicKey(p.PublicKey, Application.Common.EntityType.Property),
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
                    IsRecommended = p.IsRecommended,
                    PropertyType = p.PropertyType,
                    ListingType = p.ListingType,
                    Images = p.Images != null ? p.Images.ToList() : new List<string>(),
                    Features = p.Features ?? new List<string>(),
                    FeaturesAr = p.FeaturesAr ?? new List<string>(),
                    HighlightsAr = p.HighlightsAr,
                    NearbyPlaces = p.NearbyPlaces,
                    NearbyPlacesAr = p.NearbyPlacesAr,
                    Installments = installments.TryGetValue(p.Id, out var list) ? list : new List<InstallmentDto>(),
                    Slug = p.Slug,
                    SeoTitle = p.SeoTitle,
                    SeoDescription = p.SeoDescription,
                    SeoTitleAr = p.SeoTitleAr,
                    SeoDescriptionAr = p.SeoDescriptionAr,
                    SeoKeywords = p.SeoKeywords,
                    SeoKeywordsAr = p.SeoKeywordsAr,
                    CanonicalUrl = p.CanonicalUrl,
                    SortOrder = p.SortOrder,
                    DeliveryText = p.DeliveryText,
                    DeliveryTextAr = p.DeliveryTextAr,
                    ConstructionStatus = p.ConstructionStatus,
                    AvailabilityStatus = p.AvailabilityStatus ?? "Available",
                    OwnershipType = p.OwnershipType,
                    ViewCount = p.ViewCount,
                    InquiryCount = p.InquiryCount,
                    FavoriteCount = p.FavoriteCount,
                    VirtualTourUrl = p.VirtualTourUrl,
                    JsonLd = BuildJsonLdFromProjected(p)
                };
                dto = (PropertyPublicDto)await ApplySeoEnhancementsAsync(dto, p);
                data.Add(dto);
            }

            if (_cloud != null)
            {
                foreach (var item in data)
                {
                    if (item.Images != null && item.Images.Any())
                    {
                        item.Images = item.Images.Select(u => _cloud.GetOptimizedUrl(u)).ToList();
                    }
                }
            }

            _logger?.LogDebug("Returning DTOs for GetPublicAsync with total items: {Total}", total);
            return (data, total, page, pageSize, (int)Math.Ceiling((double)total / pageSize));
        }

        public async Task<PropertyPublicDto?> GetPublicByIdAsync(int id, CancellationToken ct = default)
        {
            var p = await _unitOfWork.Properties.Query()
                .Where(x => x.Id == id)
                .Include(x => x.Images)
                .Include(x => x.Installments)
                .Include(x => x.Contact)
                .Include(x => x.Videos)
                .AsSplitQuery()
                .AsNoTracking()
                .Select(x => new
                {
                    x.Id,
                    x.PublicKey,
                    x.TitleEn,
                    x.TitleAr,
                    x.DescriptionEn,
                    x.DescriptionAr,
                    x.Price,
                    x.Location,
                    LocationAr = x.LocationAr,
                    x.Size,
                    x.RentPerMonth,
                    x.Currency,
                    x.IsFeatured,
                    x.Bedrooms,
                    x.Bathrooms,
                    x.Floor,
                    x.TotalFloors,
                    x.IsFurnished,
                    View = x.View.ToString(),
                    PropertyType = x.PropertyType.ToString(),
                    ListingType = x.ListingType.ToString(),
                    Images = x.Images != null ? x.Images.Select(i => i.Url) : Enumerable.Empty<string>(),
                    Features = x.Features,
                    FeaturesAr = x.FeaturesAr,
                    x.Code,
                    x.Slug,
                    x.SeoTitle,
                    x.SeoDescription,
                    x.SeoTitleAr,
                    x.SeoDescriptionAr,
                    x.SeoKeywords,
                    x.SeoKeywordsAr,
                    x.CanonicalUrl,
                    Videos = x.Videos != null ? x.Videos.Where(v => !v.IsDeleted).OrderBy(v => v.SortOrder).Select(v => new VideoDto
                    {
                        Id = v.Id,
                        Url = v.Url,
                        PublicId = v.PublicId,
                        ThumbnailUrl = v.ThumbnailUrl,
                        Duration = v.Duration,
                        Width = v.Width,
                        Height = v.Height,
                        Title = v.Title,
                        SortOrder = v.SortOrder,
                        IsMain = v.IsMain,
                        CreatedAt = v.CreatedAt
                    }).ToList() : new List<VideoDto>(),
                    IsRecommended = x.IsRecommended,
                    HighlightsAr = x.HighlightsAr,
                    NearbyPlaces = x.NearbyPlaces,
                    NearbyPlacesAr = x.NearbyPlacesAr,
                    DeliveryText = x.DeliveryText,
                    DeliveryTextAr = x.DeliveryTextAr,
                    ConstructionStatus = x.ConstructionStatus != null ? x.ConstructionStatus.ToString() : null,
                    AvailabilityStatus = x.AvailabilityStatus ?? "Available",
                    OwnershipType = x.OwnershipType != null ? x.OwnershipType.ToString() : null,
                    ViewCount = x.ViewCount,
                    InquiryCount = x.InquiryCount,
                    FavoriteCount = x.FavoriteCount,
                    VirtualTourUrl = x.VirtualTourUrl
                })
                .FirstOrDefaultAsync(ct);
            if (p == null) return null;

            var insts = await _installmentQueryService.GetPublicByPropertyIdAsync(p.Id);

            var images = p.Images != null ? p.Images.ToList() : new List<string>();
            if (_cloud != null && images.Any())
            {
                images = images.Select(u => _cloud.GetOptimizedUrl(u)).ToList();
            }

            var dto = new PropertyPublicDto
            {
                Id = p.Id,
                PublicKey = ResolvePublicKey(p.PublicKey, Application.Common.EntityType.Property),
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
                IsRecommended = p.IsRecommended,
                Bedrooms = p.Bedrooms,
                Bathrooms = p.Bathrooms,
                Floor = p.Floor,
                TotalFloors = p.TotalFloors,
                IsFurnished = p.IsFurnished,
                View = p.View,
                PropertyType = p.PropertyType,
                ListingType = p.ListingType,
                Images = images,
                Features = p.Features ?? new List<string>(),
                FeaturesAr = p.FeaturesAr ?? new List<string>(),
                HighlightsAr = p.HighlightsAr,
                NearbyPlaces = p.NearbyPlaces,
                NearbyPlacesAr = p.NearbyPlacesAr,
                Installments = insts,
                Code = p.Code,
                Slug = p.Slug,
                SeoTitle = p.SeoTitle,
                SeoDescription = p.SeoDescription,
                SeoTitleAr = p.SeoTitleAr,
                SeoDescriptionAr = p.SeoDescriptionAr,
                SeoKeywords = p.SeoKeywords,
                SeoKeywordsAr = p.SeoKeywordsAr,
                CanonicalUrl = p.CanonicalUrl,
                DeliveryText = p.DeliveryText,
                    DeliveryTextAr = p.DeliveryTextAr,
                ConstructionStatus = p.ConstructionStatus,
                AvailabilityStatus = p.AvailabilityStatus ?? "Available",
                OwnershipType = p.OwnershipType,
                ViewCount = p.ViewCount,
                InquiryCount = p.InquiryCount,
                FavoriteCount = p.FavoriteCount,
                VirtualTourUrl = p.VirtualTourUrl,
                JsonLd = BuildJsonLdFromProjected(p),
                ImagesMeta = images.Select(u => new ImageDto { Url = u, Width = 1200, Height = 800 }).ToList(),
                HreflangTags = BuildHreflangTags(p.Slug, null),
                Videos = p.Videos
            };

            dto = (PropertyPublicDto)await ApplySeoEnhancementsAsync(dto, p);

            _logger?.LogDebug("GetPublicByIdAsync returning id={Id} slug={Slug} canonical={Canonical}", p.Id, p.Slug, p.CanonicalUrl);

            return dto;
        }

        public async Task<PropertyPublicDto?> GetPublicByPublicKeyAsync(string publicKey, CancellationToken ct = default)
        {
            var p = await _unitOfWork.Properties.Query().AsNoTracking()
                .Where(x => x.PublicKey == publicKey)
                .Include(x => x.Images)
                .Include(x => x.Installments)
                .Include(x => x.Contact)
                .Include(x => x.Videos)
                .AsSplitQuery()
                .Select(x => new
                {
                    x.Id,
                    x.PublicKey,
                    x.TitleEn,
                    x.TitleAr,
                    x.DescriptionEn,
                    x.DescriptionAr,
                    x.Price,
                    x.Location,
                    LocationAr = x.LocationAr,
                    x.Size,
                    x.RentPerMonth,
                    x.Currency,
                    x.IsFeatured,
                    x.Bedrooms,
                    x.Bathrooms,
                    x.Floor,
                    x.TotalFloors,
                    x.IsFurnished,
                    View = x.View.ToString(),
                    PropertyType = x.PropertyType.ToString(),
                    ListingType = x.ListingType.ToString(),
                    Images = x.Images != null ? x.Images.Select(i => i.Url) : Enumerable.Empty<string>(),
                    Features = x.Features,
                    FeaturesAr = x.FeaturesAr,
                    x.Code,
                    x.Slug,
                    x.SeoTitle,
                    x.SeoDescription,
                    x.SeoTitleAr,
                    x.SeoDescriptionAr,
                    x.SeoKeywords,
                    x.SeoKeywordsAr,
                    x.CanonicalUrl,
                    DeliveryText = x.DeliveryText,
                    DeliveryTextAr = x.DeliveryTextAr,
                    IsRecommended = x.IsRecommended,
                    ConstructionStatus = x.ConstructionStatus != null ? x.ConstructionStatus.ToString() : null,
                    AvailabilityStatus = x.AvailabilityStatus ?? "Available",
                    OwnershipType = x.OwnershipType != null ? x.OwnershipType.ToString() : null,
                    ViewCount = x.ViewCount,
                    InquiryCount = x.InquiryCount,
                    FavoriteCount = x.FavoriteCount,
                    VirtualTourUrl = x.VirtualTourUrl,
                    HighlightsAr = x.HighlightsAr,
                    NearbyPlaces = x.NearbyPlaces,
                    NearbyPlacesAr = x.NearbyPlacesAr,
                    Videos = x.Videos != null ? x.Videos.Where(v => !v.IsDeleted).OrderBy(v => v.SortOrder).Select(v => new VideoDto
                    {
                        Id = v.Id,
                        Url = v.Url,
                        PublicId = v.PublicId,
                        ThumbnailUrl = v.ThumbnailUrl,
                        Duration = v.Duration,
                        Width = v.Width,
                        Height = v.Height,
                        Title = v.Title,
                        SortOrder = v.SortOrder,
                        IsMain = v.IsMain,
                        CreatedAt = v.CreatedAt
                    }).ToList() : new List<VideoDto>()
                })
                .FirstOrDefaultAsync(ct);

            if (p == null) return null;

            var insts = await _installmentQueryService.GetPublicByPropertyIdAsync(p.Id);

            var images = p.Images != null ? p.Images.ToList() : new List<string>();
            if (_cloud != null && images.Any())
            {
                images = images.Select(u => _cloud.GetOptimizedUrl(u)).ToList();
            }

            var dto = new PropertyPublicDto
            {
                Id = p.Id,
                PublicKey = ResolvePublicKey(p.PublicKey, Application.Common.EntityType.Property),
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
                Bedrooms = p.Bedrooms,
                Bathrooms = p.Bathrooms,
                Floor = p.Floor,
                TotalFloors = p.TotalFloors,
                IsFurnished = p.IsFurnished,
                View = p.View,
                PropertyType = p.PropertyType,
                ListingType = p.ListingType,
                Images = images,
                Features = p.Features ?? new List<string>(),
                FeaturesAr = p.FeaturesAr ?? new List<string>(),
                Installments = insts,
                Code = p.Code,
                Slug = p.Slug,
                SeoTitle = p.SeoTitle,
                SeoDescription = p.SeoDescription,
                SeoTitleAr = p.SeoTitleAr,
                SeoDescriptionAr = p.SeoDescriptionAr,
                SeoKeywords = p.SeoKeywords,
                SeoKeywordsAr = p.SeoKeywordsAr,
                CanonicalUrl = p.CanonicalUrl,
                IsRecommended = p.IsRecommended,
                DeliveryText = p.DeliveryText,
                    DeliveryTextAr = p.DeliveryTextAr,
                ConstructionStatus = p.ConstructionStatus,
                AvailabilityStatus = p.AvailabilityStatus ?? "Available",
                OwnershipType = p.OwnershipType,
                ViewCount = p.ViewCount,
                InquiryCount = p.InquiryCount,
                FavoriteCount = p.FavoriteCount,
                VirtualTourUrl = p.VirtualTourUrl,
                HighlightsAr = p.HighlightsAr,
                NearbyPlaces = p.NearbyPlaces,
                NearbyPlacesAr = p.NearbyPlacesAr,
                JsonLd = BuildJsonLdFromProjected(p),
                ImagesMeta = images.Select(u => new ImageDto { Url = u, Width = 1200, Height = 800 }).ToList(),
                HreflangTags = BuildHreflangTags(p.Slug, null),
                Videos = p.Videos
            };

            dto = (PropertyPublicDto)await ApplySeoEnhancementsAsync(dto, p);

            _logger?.LogDebug("GetPublicByPublicKeyAsync returning publicKey={PublicKey} slug={Slug} canonical={Canonical}", p.PublicKey, p.Slug, p.CanonicalUrl);

            return dto;
        }

        public async Task<PropertyAdminDto?> GetAdminByIdAsync(int id, CancellationToken ct = default)
        {
            var p = await _unitOfWork.Properties.Query().AsNoTracking().IgnoreQueryFilters()
                .Where(x => x.Id == id)
                .Include(x => x.Images)
                .Include(x => x.Installments)
                .Include(x => x.Videos)
                .AsSplitQuery()
                .Select(x => new
                {
                    x.Id,
                    x.PublicKey,
                    x.TitleEn,
                    x.TitleAr,
                    x.DescriptionEn,
                    x.DescriptionAr,
                    x.Price,
                    x.Location,
                    LocationAr = x.LocationAr,
                    x.Size,
                    x.RentPerMonth,
                    x.Currency,
                    x.IsFeatured,
                    x.Bedrooms,
                    x.Bathrooms,
                    x.Floor,
                    x.TotalFloors,
                    x.IsFurnished,
                    View = x.View.ToString(),
                    PropertyType = x.PropertyType.ToString(),
                    ListingType = x.ListingType.ToString(),
                    Images = x.Images != null ? x.Images.Select(i => i.Url) : Enumerable.Empty<string>(),
                    ImageMeta = x.Images != null ? x.Images.Select(i => new { i.Id, i.Url, i.PublicId }) : Enumerable.Empty<dynamic>(),
                    Videos = x.Videos != null ? x.Videos.Select(v => new VideoResultDto { Id = v.Id, Url = v.Url, PublicId = v.PublicId }).ToList() : new List<VideoResultDto>(),
                    Features = x.Features,
                    FeaturesAr = x.FeaturesAr,
                    x.Code,
                    x.ContactId,
                    ContactName = x.Contact != null ? x.Contact.Name : null,
                    ContactPhone = x.Contact != null ? x.Contact.Phone : null,
                    ContactType = x.Contact != null ? x.Contact.Type : (Domain.Enums.ContactType?)null,
                    x.Slug,
                    x.SlugIsAuto,
                    x.SlugLanguage,
                    x.SeoTitle,
                    x.SeoDescription,
                    x.SeoTitleAr,
                    x.SeoDescriptionAr,
                    x.SeoKeywords,
                    x.SeoKeywordsAr,
                    x.CanonicalUrl,
                    DeliveryText = x.DeliveryText,
                    DeliveryTextAr = x.DeliveryTextAr,
                    IsRecommended = x.IsRecommended,
                    ConstructionStatus = x.ConstructionStatus != null ? x.ConstructionStatus.ToString() : null,
                    AvailabilityStatus = x.AvailabilityStatus ?? "Available",
                    OwnershipType = x.OwnershipType != null ? x.OwnershipType.ToString() : null,
                    ViewCount = x.ViewCount,
                    InquiryCount = x.InquiryCount,
                    FavoriteCount = x.FavoriteCount,
                    VirtualTourUrl = x.VirtualTourUrl,
                    HighlightsAr = x.HighlightsAr,
                    NearbyPlaces = x.NearbyPlaces,
                    NearbyPlacesAr = x.NearbyPlacesAr
                })
                .FirstOrDefaultAsync(ct);

            if (p == null) return null;

            var insts = await _installmentQueryService.GetAdminByPropertyIdAsync(p.Id);

            var imageList = p.Images?.ToList() ?? new List<string>();

            return new PropertyAdminDto
            {
                Id = p.Id,
                PublicKey = ResolvePublicKey(p.PublicKey, Application.Common.EntityType.Property),
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
                Bedrooms = p.Bedrooms,
                Bathrooms = p.Bathrooms,
                Floor = p.Floor,
                TotalFloors = p.TotalFloors,
                IsFurnished = p.IsFurnished,
                View = p.View,
                PropertyType = p.PropertyType,
                ListingType = p.ListingType,
                Images = imageList,
                Features = p.Features ?? new List<string>(),
                FeaturesAr = p.FeaturesAr ?? new List<string>(),
                Installments = insts,
                Code = p.Code,
                ContactId = p.ContactId,
                ContactName = p.ContactName,
                ContactPhone = p.ContactPhone,
                Contact = p.ContactId.HasValue && p.ContactName != null ? new ContactDto
                {
                    Name = p.ContactName,
                    Phone = p.ContactPhone ?? string.Empty,
                    Type = p.ContactType ?? Domain.Enums.ContactType.Owner
                } : null,
                AdminImages = p.ImageMeta?.Select(i => new ImageInfoDto
                {
                    Id = (int)i.Id,
                    Url = (string)i.Url,
                    PublicId = (string?)i.PublicId
                }).ToList() ?? new List<ImageInfoDto>(),
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
                JsonLd = BuildJsonLdFromProjected(p)
            };
        }

        public async Task<PropertyAdminDto?> GetAdminByCodeAsync(string code, CancellationToken ct = default)
        {
            var p = await _unitOfWork.Properties.Query().AsNoTracking().IgnoreQueryFilters()
                .Where(x => x.Code == code)
                .Include(x => x.Images)
                .Include(x => x.Installments)
                .AsSplitQuery()
                .Select(x => new
                {
                    x.Id,
                    x.PublicKey,
                    x.TitleEn,
                    x.TitleAr,
                    x.DescriptionEn,
                    x.DescriptionAr,
                    x.Price,
                    x.Location,
                    LocationAr = x.LocationAr,
                    x.Size,
                    x.RentPerMonth,
                    x.Currency,
                    x.IsFeatured,
                    x.Bedrooms,
                    x.Bathrooms,
                    x.Floor,
                    x.TotalFloors,
                    x.IsFurnished,
                    View = x.View.ToString(),
                    PropertyType = x.PropertyType.ToString(),
                    ListingType = x.ListingType.ToString(),
                    Images = x.Images != null ? x.Images.Select(i => i.Url) : Enumerable.Empty<string>(),
                    ImageMeta = x.Images != null ? x.Images.Select(i => new { i.Id, i.Url, i.PublicId }) : Enumerable.Empty<dynamic>(),
                    Features = x.Features,
                    FeaturesAr = x.FeaturesAr,
                    x.Code,
                    x.ContactId,
                    ContactName = x.Contact != null ? x.Contact.Name : null,
                    ContactPhone = x.Contact != null ? x.Contact.Phone : null,
                    ContactType = x.Contact != null ? x.Contact.Type : (Domain.Enums.ContactType?)null,
                    x.Slug,
                    x.SlugIsAuto,
                    x.SlugLanguage,
                    x.SeoTitle,
                    x.SeoDescription,
                    x.SeoTitleAr,
                    x.SeoDescriptionAr,
                    x.SeoKeywords,
                    x.SeoKeywordsAr,
                    x.CanonicalUrl,
                    DeliveryText = x.DeliveryText,
                    DeliveryTextAr = x.DeliveryTextAr,
                    IsRecommended = x.IsRecommended,
                    ConstructionStatus = x.ConstructionStatus != null ? x.ConstructionStatus.ToString() : null,
                    AvailabilityStatus = x.AvailabilityStatus ?? "Available",
                    OwnershipType = x.OwnershipType != null ? x.OwnershipType.ToString() : null,
                    ViewCount = x.ViewCount,
                    InquiryCount = x.InquiryCount,
                    FavoriteCount = x.FavoriteCount,
                    VirtualTourUrl = x.VirtualTourUrl,
                    HighlightsAr = x.HighlightsAr,
                    NearbyPlaces = x.NearbyPlaces,
                    NearbyPlacesAr = x.NearbyPlacesAr
                })
                .FirstOrDefaultAsync(ct);

            if (p == null) return null;

            var insts = await _installmentQueryService.GetAdminByPropertyIdAsync(p.Id);

            var imageList = p.Images?.ToList() ?? new List<string>();

            return new PropertyAdminDto
            {
                Id = p.Id,
                PublicKey = ResolvePublicKey(p.PublicKey, Application.Common.EntityType.Property),
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
                Bedrooms = p.Bedrooms,
                Bathrooms = p.Bathrooms,
                Floor = p.Floor,
                TotalFloors = p.TotalFloors,
                IsFurnished = p.IsFurnished,
                View = p.View,
                PropertyType = p.PropertyType,
                ListingType = p.ListingType,
                Images = imageList,
                Features = p.Features ?? new List<string>(),
                FeaturesAr = p.FeaturesAr ?? new List<string>(),
                Installments = insts,
                Code = p.Code,
                ContactId = p.ContactId,
                ContactName = p.ContactName,
                ContactPhone = p.ContactPhone,
                Contact = p.ContactId.HasValue && p.ContactName != null ? new ContactDto
                {
                    Name = p.ContactName,
                    Phone = p.ContactPhone ?? string.Empty,
                    Type = p.ContactType ?? Domain.Enums.ContactType.Owner
                } : null,
                AdminImages = p.ImageMeta?.Select(i => new ImageInfoDto
                {
                    Id = (int)i.Id,
                    Url = (string)i.Url,
                    PublicId = (string?)i.PublicId
                }).ToList() ?? new List<ImageInfoDto>(),
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
                JsonLd = BuildJsonLdFromProjected(p)
            };
        }

        public async Task<PropertyPublicDto?> GetPublicBySlugAsync(string slug, CancellationToken ct = default)
        {
            var p = await _unitOfWork.Properties.Query()
                .Where(x => x.Slug == slug)
                .Include(x => x.Images)
                .Include(x => x.Installments)
                .Include(x => x.Contact)
                .Include(x => x.Videos)
                .AsSplitQuery()
                .Select(x => new
                {
                    x.Id,
                    x.PublicKey,
                    x.TitleEn,
                    x.TitleAr,
                    x.DescriptionEn,
                    x.DescriptionAr,
                    x.Price,
                    x.Location,
                    LocationAr = x.LocationAr,
                    x.Size,
                    x.RentPerMonth,
                    x.Currency,
                    x.IsFeatured,
                    x.Bedrooms,
                    x.Bathrooms,
                    x.Floor,
                    x.TotalFloors,
                    x.IsFurnished,
                    View = x.View.ToString(),
                    PropertyType = x.PropertyType.ToString(),
                    ListingType = x.ListingType.ToString(),
                    Images = SafeSelectImages(x.Images),
                    Features = x.Features,
                    FeaturesAr = x.FeaturesAr,
                    x.Code,
                    x.SeoTitle,
                    x.SeoDescription,
                    x.SeoTitleAr,
                    x.SeoDescriptionAr,
                    x.SeoKeywords,
                    x.SeoKeywordsAr,
                    x.CanonicalUrl,
                    DeliveryText = x.DeliveryText,
                    DeliveryTextAr = x.DeliveryTextAr,
                    IsRecommended = x.IsRecommended,
                    ConstructionStatus = x.ConstructionStatus != null ? x.ConstructionStatus.ToString() : null,
                    AvailabilityStatus = x.AvailabilityStatus ?? "Available",
                    OwnershipType = x.OwnershipType != null ? x.OwnershipType.ToString() : null,
                    ViewCount = x.ViewCount,
                    InquiryCount = x.InquiryCount,
                    FavoriteCount = x.FavoriteCount,
                    VirtualTourUrl = x.VirtualTourUrl,
                    HighlightsAr = x.HighlightsAr,
                    NearbyPlaces = x.NearbyPlaces,
                    NearbyPlacesAr = x.NearbyPlacesAr,
                    x.Slug,
                    Videos = x.Videos != null ? x.Videos.Where(v => !v.IsDeleted).OrderBy(v => v.SortOrder).Select(v => new VideoDto
                    {
                        Id = v.Id,
                        Url = v.Url,
                        PublicId = v.PublicId,
                        ThumbnailUrl = v.ThumbnailUrl,
                        Duration = v.Duration,
                        Width = v.Width,
                        Height = v.Height,
                        Title = v.Title,
                        SortOrder = v.SortOrder,
                        IsMain = v.IsMain,
                        CreatedAt = v.CreatedAt
                    }).ToList() : new List<VideoDto>()
                })
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);

            if (p == null) return null;

            var insts = await _installmentQueryService.GetPublicByPropertyIdAsync(p.Id);

            var images = p.Images != null ? p.Images.ToList() : new List<string>();

            var dto = new PropertyPublicDto
            {
                Id = p.Id,
                PublicKey = ResolvePublicKey(p.PublicKey, Application.Common.EntityType.Property),
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
                Bedrooms = p.Bedrooms,
                Bathrooms = p.Bathrooms,
                Floor = p.Floor,
                TotalFloors = p.TotalFloors,
                IsFurnished = p.IsFurnished,
                View = p.View,
                PropertyType = p.PropertyType,
                ListingType = p.ListingType,
                Images = images,
                Features = p.Features ?? new List<string>(),
                FeaturesAr = p.FeaturesAr ?? new List<string>(),
                Installments = insts,
                Code = p.Code,
                SeoTitle = p.SeoTitle,
                SeoDescription = p.SeoDescription,
                SeoTitleAr = p.SeoTitleAr,
                SeoDescriptionAr = p.SeoDescriptionAr,
                SeoKeywords = p.SeoKeywords,
                SeoKeywordsAr = p.SeoKeywordsAr,
                CanonicalUrl = p.CanonicalUrl,
                IsRecommended = p.IsRecommended,
                DeliveryText = p.DeliveryText,
                    DeliveryTextAr = p.DeliveryTextAr,
                ConstructionStatus = p.ConstructionStatus,
                AvailabilityStatus = p.AvailabilityStatus ?? "Available",
                OwnershipType = p.OwnershipType,
                ViewCount = p.ViewCount,
                InquiryCount = p.InquiryCount,
                FavoriteCount = p.FavoriteCount,
                VirtualTourUrl = p.VirtualTourUrl,
                HighlightsAr = p.HighlightsAr,
                NearbyPlaces = p.NearbyPlaces,
                NearbyPlacesAr = p.NearbyPlacesAr,
                Slug = p.Slug,
                JsonLd = BuildJsonLdFromProjected(p),
                ImagesMeta = images.Select(u => new ImageDto { Url = u, Width = 1200, Height = 800 }).ToList(),
                HreflangTags = BuildHreflangTags(p.Slug, null),
                Videos = p.Videos
            };

            dto = (PropertyPublicDto)await ApplySeoEnhancementsAsync(dto, p);

            try { _logger?.LogDebug("GetPublicBySlugAsync returning slug={Slug} canonical={Canonical}", dto.Slug, dto.CanonicalUrl); } catch (Exception logEx) { _logger?.LogWarning(logEx, "Failed to log debug info for slug {Slug}", dto.Slug); }

            return dto;
        }

        public async Task<List<PropertyPublicDto>> GetRelatedAsync(int id, int limit = 5, CancellationToken ct = default)
        {
            var prop = await _unitOfWork.Properties.Query().Where(p => p.Id == id).Select(p => new { p.Id, p.Location, p.LocationAr, p.Price }).FirstOrDefaultAsync(ct);
            if (prop == null) return new List<PropertyPublicDto>();

            var q = _unitOfWork.Properties.Query()
                .Where(p => (p.Location == prop.Location || (p.LocationAr != null && p.LocationAr == prop.Location) || (prop.LocationAr != null && p.Location == prop.LocationAr) || (p.LocationAr != null && prop.LocationAr != null && p.LocationAr == prop.LocationAr)) && p.Id != prop.Id && p.Price >= prop.Price * 0.8m && p.Price <= prop.Price * 1.2m)
                .OrderBy(p => p.SortOrder).ThenByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Take(limit)
                .Include(p => p.Images)
                .AsSplitQuery()
                .Select(p => new
                {
                    p.Id,
                    p.PublicKey,
                    p.TitleEn,
                    p.TitleAr,
                    p.DescriptionEn,
                    p.DescriptionAr,
                    p.Price,
                    p.Location,
                    LocationAr = p.LocationAr,
                    p.Code,
                    p.Slug,
                    p.RentPerMonth,
                    p.Currency,
                    PropertyType = p.PropertyType.ToString(),
                    ListingType = p.ListingType.ToString(),
                    Features = p.Features,
                    FeaturesAr = p.FeaturesAr,
                    p.SeoTitle,
                    p.SeoDescription,
                    p.SeoTitleAr,
                    p.SeoDescriptionAr,
                    p.SeoKeywords,
                    p.SeoKeywordsAr,
                    p.CanonicalUrl,
                    DeliveryText = p.DeliveryText,
                    DeliveryTextAr = p.DeliveryTextAr,
                    IsRecommended = p.IsRecommended,
                    ConstructionStatus = p.ConstructionStatus != null ? p.ConstructionStatus.ToString() : null,
                    AvailabilityStatus = p.AvailabilityStatus ?? "Available",
                    OwnershipType = p.OwnershipType != null ? p.OwnershipType.ToString() : null,
                    ViewCount = p.ViewCount,
                    InquiryCount = p.InquiryCount,
                    FavoriteCount = p.FavoriteCount,
                    VirtualTourUrl = p.VirtualTourUrl,
                    HighlightsAr = p.HighlightsAr,
                    NearbyPlaces = p.NearbyPlaces,
                    NearbyPlacesAr = p.NearbyPlacesAr,
                    Images = p.Images != null ? p.Images.Select(i => i.Url) : Enumerable.Empty<string>()
                })
                .AsNoTracking();

            var items = await q.ToListAsync(ct);
            var ids = items.Select(i => i.Id).ToList();
            var installments = await _installmentQueryService.GetPublicByPropertyIdsAsync(ids);

            var result = new List<PropertyPublicDto>(items.Count);
            foreach (var i in items)
            {
                var dto = new PropertyPublicDto
                {
                    Id = i.Id,
                    PublicKey = ResolvePublicKey(i.PublicKey, Application.Common.EntityType.Property),
                    TitleEn = i.TitleEn,
                    TitleAr = i.TitleAr,
                    DescriptionEn = i.DescriptionEn,
                    DescriptionAr = i.DescriptionAr,
                    Price = i.Price,
                    Location = i.Location,
                    LocationAr = i.LocationAr,
                    RentPerMonth = i.RentPerMonth,
                    Currency = i.Currency ?? "EGP",
                    PropertyType = i.PropertyType,
                    ListingType = i.ListingType,
                    Features = i.Features ?? new List<string>(),
                    FeaturesAr = i.FeaturesAr ?? new List<string>(),
                    SeoTitle = i.SeoTitle,
                    SeoDescription = i.SeoDescription,
                    SeoTitleAr = i.SeoTitleAr,
                    SeoDescriptionAr = i.SeoDescriptionAr,
                    SeoKeywords = i.SeoKeywords,
                    SeoKeywordsAr = i.SeoKeywordsAr,
                    CanonicalUrl = i.CanonicalUrl,
                    IsRecommended = i.IsRecommended,
                    DeliveryText = i.DeliveryText,
                    DeliveryTextAr = i.DeliveryTextAr,
                    ConstructionStatus = i.ConstructionStatus,
                    AvailabilityStatus = i.AvailabilityStatus ?? "Available",
                    OwnershipType = i.OwnershipType,
                    ViewCount = i.ViewCount,
                    InquiryCount = i.InquiryCount,
                    FavoriteCount = i.FavoriteCount,
                    VirtualTourUrl = i.VirtualTourUrl,
                    HighlightsAr = i.HighlightsAr,
                    NearbyPlaces = i.NearbyPlaces,
                    NearbyPlacesAr = i.NearbyPlacesAr,
                    Images = i.Images == null ? new List<string>() : i.Images.ToList(),
                    Installments = installments.TryGetValue(i.Id, out var list) ? list : new List<InstallmentDto>(),
                    Slug = i.Slug,
                    JsonLd = BuildJsonLdFromProjected(i)
                };
                dto = (PropertyPublicDto)await ApplySeoEnhancementsAsync(dto, i);
                result.Add(dto);
            }
            return result;
        }

        public async Task<(List<PropertyPublicDto> Data, int Total, int Page, int PageSize, int TotalPages, string SeoTitle, string SeoDescription)> GetByLocationAsync(string location, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _unitOfWork.Properties.Query().Where(p => p.Location == location || (p.LocationAr != null && p.LocationAr == location));
            var total = await query.CountAsync(ct);
            var items = await query.OrderBy(p => p.SortOrder).ThenByDescending(p => p.UpdatedAt ?? p.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
                .Include(p => p.Images)
                .AsSplitQuery()
                .Select(p => new
                {
                    p.Id,
                    p.PublicKey,
                    p.TitleEn,
                    p.TitleAr,
                    p.DescriptionEn,
                    p.DescriptionAr,
                    p.Price,
                    p.Location,
                    LocationAr = p.LocationAr,
                    p.Code,
                    p.Slug,
                    p.SortOrder,
                    p.RentPerMonth,
                    p.Currency,
                    PropertyType = p.PropertyType.ToString(),
                    ListingType = p.ListingType.ToString(),
                    Features = p.Features,
                    FeaturesAr = p.FeaturesAr,
                    p.SeoTitle,
                    p.SeoDescription,
                    p.SeoTitleAr,
                    p.SeoDescriptionAr,
                    p.SeoKeywords,
                    p.SeoKeywordsAr,
                    p.CanonicalUrl,
                    DeliveryText = p.DeliveryText,
                    DeliveryTextAr = p.DeliveryTextAr,
                    IsRecommended = p.IsRecommended,
                    ConstructionStatus = p.ConstructionStatus != null ? p.ConstructionStatus.ToString() : null,
                    AvailabilityStatus = p.AvailabilityStatus ?? "Available",
                    OwnershipType = p.OwnershipType != null ? p.OwnershipType.ToString() : null,
                    ViewCount = p.ViewCount,
                    InquiryCount = p.InquiryCount,
                    FavoriteCount = p.FavoriteCount,
                    VirtualTourUrl = p.VirtualTourUrl,
                    HighlightsAr = p.HighlightsAr,
                    NearbyPlaces = p.NearbyPlaces,
                    NearbyPlacesAr = p.NearbyPlacesAr,
                    Images = p.Images != null ? p.Images.Select(i => i.Url) : Enumerable.Empty<string>()
                })
                .AsNoTracking()
                .ToListAsync(ct);

            var ids = items.Select(i => i.Id).ToList();
            var installments = await _installmentQueryService.GetPublicByPropertyIdsAsync(ids);

            var data = new List<PropertyPublicDto>(items.Count);
            foreach (var i in items)
            {
                var dto = new PropertyPublicDto
                {
                    Id = i.Id,
                    PublicKey = ResolvePublicKey(i.PublicKey, Application.Common.EntityType.Property),
                    SortOrder = i.SortOrder,
                    TitleEn = i.TitleEn,
                    TitleAr = i.TitleAr,
                    DescriptionEn = i.DescriptionEn,
                    DescriptionAr = i.DescriptionAr,
                    Price = i.Price,
                    Location = i.Location,
                    LocationAr = i.LocationAr,
                    RentPerMonth = i.RentPerMonth,
                    Currency = i.Currency ?? "EGP",
                    PropertyType = i.PropertyType,
                    ListingType = i.ListingType,
                    Features = i.Features ?? new List<string>(),
                    FeaturesAr = i.FeaturesAr ?? new List<string>(),
                    SeoTitle = i.SeoTitle,
                    SeoDescription = i.SeoDescription,
                    SeoTitleAr = i.SeoTitleAr,
                    SeoDescriptionAr = i.SeoDescriptionAr,
                    SeoKeywords = i.SeoKeywords,
                    SeoKeywordsAr = i.SeoKeywordsAr,
                    CanonicalUrl = i.CanonicalUrl,
                    IsRecommended = i.IsRecommended,
                    DeliveryText = i.DeliveryText,
                    DeliveryTextAr = i.DeliveryTextAr,
                    ConstructionStatus = i.ConstructionStatus,
                    AvailabilityStatus = i.AvailabilityStatus ?? "Available",
                    OwnershipType = i.OwnershipType,
                    ViewCount = i.ViewCount,
                    InquiryCount = i.InquiryCount,
                    FavoriteCount = i.FavoriteCount,
                    VirtualTourUrl = i.VirtualTourUrl,
                    HighlightsAr = i.HighlightsAr,
                    NearbyPlaces = i.NearbyPlaces,
                    NearbyPlacesAr = i.NearbyPlacesAr,
                    Images = i.Images == null ? new List<string>() : i.Images.ToList(),
                    Installments = installments.TryGetValue(i.Id, out var list) ? list : new List<InstallmentDto>(),
                    Slug = i.Slug,
                    JsonLd = BuildJsonLdFromProjected(i)
                };
                dto = (PropertyPublicDto)await ApplySeoEnhancementsAsync(dto, i);
                data.Add(dto);
            }

            var seoTitle = $"Properties for sale in {location}";
            var seoDescription = $"Find the best properties in {location}";

            var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)total / pageSize) : 0;
            _logger?.LogDebug("Returning DTOs for GetByLocationAsync with total items: {Total}", total);
            return (data, total, page, pageSize, totalPages, seoTitle, seoDescription);
        }

        public async Task<List<PropertyPublicDto>> SearchAsync(string q, int limit = 20, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(q)) return new List<PropertyPublicDto>();
            var term = q.Trim();

            var searchResults = await _searchService.SearchPropertiesAsync(term, limit);

            if (searchResults.Count == 0)
                return new List<PropertyPublicDto>();

            var ids = searchResults.Select(r => r.Id).ToList();
            var installments = await _installmentQueryService.GetPublicByPropertyIdsAsync(ids);

            return searchResults.Select(r => new PropertyPublicDto
            {
                Id = r.Id,
                TitleEn = r.TitleEn,
                Price = r.Price,
                Location = r.Location,
                LocationAr = r.LocationAr,
                Slug = r.Slug,
                Images = r.Images,
                Installments = installments.TryGetValue(r.Id, out var list) ? list : new List<InstallmentDto>(),
            }).ToList();
        }

        public async Task<List<PropertyCardDto>> GetLatestCardsAsync(int page = 1, int pageSize = 20, string? listingType = null, CancellationToken ct = default)
        {
            var pageNum = Math.Max(1, page);
            var pageSizeNum = Math.Clamp(pageSize, 1, 100);

            var query = _unitOfWork.Properties.Query().Where(p => !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(listingType) && Enum.TryParse<PropertyListingType>(listingType, true, out var parsedListing))
                query = query.Where(p => p.ListingType == parsedListing);

            var items = await query
                .OrderBy(p => p.SortOrder).ThenByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Skip((pageNum - 1) * pageSizeNum)
                .Take(pageSizeNum)
                .Select(p => new PropertyCardDto
                {
                    Id = p.Id,
                    PublicKey = p.PublicKey,
                    TitleEn = p.TitleEn,
                    TitleAr = p.TitleAr,
                    Price = p.Price,
                    RentPerMonth = p.RentPerMonth,
                    Location = p.Location,
                    LocationAr = p.LocationAr,
                    Size = p.Size,
                    IsFeatured = p.IsFeatured,
                    PropertyType = p.PropertyType.ToString(),
                    ListingType = p.ListingType.ToString(),
                    Slug = p.Slug,
                    SortOrder = p.SortOrder,
                    Image = p.Images != null && p.Images.Any()
                        ? p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault()
                        : null,
                    Installment = p.Installments != null
                        ? p.Installments.Where(i => !i.IsDeleted && i.IsEnabled)
                            .Select(i => new InstallmentDto
                            {
                                DownPaymentPercent = i.DownPaymentPercent,
                                DiscountPercent = i.DiscountPercent,
                                Years = i.Years,
                                IsEnabled = i.IsEnabled,
                                IsDeleted = i.IsDeleted,
                                PaymentType = i.PaymentType.ToString()
                            }).FirstOrDefault()
                        : null
                })
                .AsNoTracking()
                .ToListAsync(ct);

            if (_cloud != null)
            {
                foreach (var item in items)
                {
                    if (!string.IsNullOrWhiteSpace(item.Image))
                        item.Image = _cloud.GetOptimizedUrl(item.Image);
                }
            }

            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.PublicKey))
                    item.PublicKey = _publicIdService.GenerateId(Application.Common.EntityType.Property);
            }

            return items;
        }

        public async Task<List<PropertyPublicDto>> GetLatestPropertiesAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var pageNum = Math.Max(1, page);
            var pageSizeNum = Math.Clamp(pageSize, 1, 100);

            var items = await _unitOfWork.Properties.Query()
                .OrderBy(p => p.SortOrder).ThenByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Skip((pageNum - 1) * pageSizeNum)
                .Take(pageSizeNum)
                .Select(p => new
                {
                    p.Id,
                    p.PublicKey,
                    p.TitleEn,
                    p.TitleAr,
                    p.DescriptionEn,
                    p.DescriptionAr,
                    p.Price,
                    p.Location,
                    LocationAr = p.LocationAr,
                    p.Size,
                    p.RentPerMonth,
                    p.Currency,
                    p.IsFeatured,
                    PropertyType = p.PropertyType.ToString(),
                    ListingType = p.ListingType.ToString(),
                    Images = p.Images != null ? p.Images.Select(i => i.Url) : Enumerable.Empty<string>(),
                    Features = p.Features,
                    FeaturesAr = p.FeaturesAr,
                    p.Code,
                    p.Slug,
                    p.SeoTitle,
                    p.SeoDescription,
                    p.SeoTitleAr,
                    p.SeoDescriptionAr,
                    p.SeoKeywords,
                    p.SeoKeywordsAr,
                    p.CanonicalUrl,
                    DeliveryText = p.DeliveryText,
                    DeliveryTextAr = p.DeliveryTextAr,
                    IsRecommended = p.IsRecommended,
                    ConstructionStatus = p.ConstructionStatus != null ? p.ConstructionStatus.ToString() : null,
                    AvailabilityStatus = p.AvailabilityStatus ?? "Available",
                    OwnershipType = p.OwnershipType != null ? p.OwnershipType.ToString() : null,
                    ViewCount = p.ViewCount,
                    InquiryCount = p.InquiryCount,
                    FavoriteCount = p.FavoriteCount,
                    VirtualTourUrl = p.VirtualTourUrl,
                    HighlightsAr = p.HighlightsAr,
                    NearbyPlaces = p.NearbyPlaces,
                    NearbyPlacesAr = p.NearbyPlacesAr,
                    p.SortOrder,
                    Installments = p.Installments != null
                        ? p.Installments.Where(i => !i.IsDeleted && i.IsEnabled).Select(i => new InstallmentDto
                        {
                            DownPaymentPercent = i.DownPaymentPercent,
                            Years = i.Years,
                            IsEnabled = i.IsEnabled,
                            IsDeleted = i.IsDeleted,
                            PaymentType = i.PaymentType.ToString()
                        }).ToList()
                        : new List<InstallmentDto>()
                })
                .ToListAsync(ct);

            return items.Select(p =>
            {
                var images = p.Images?.ToList() ?? new List<string>();

                return new PropertyPublicDto
                {
                    Id = p.Id,
                    PublicKey = ResolvePublicKey(p.PublicKey, Application.Common.EntityType.Property),
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
                    PropertyType = p.PropertyType,
                    ListingType = p.ListingType,
                    Images = images,
                    Features = p.Features ?? new List<string>(),
                    FeaturesAr = p.FeaturesAr ?? new List<string>(),
                    Installments = p.Installments,
                    Slug = p.Slug,
                    SeoTitle = p.SeoTitle,
                    SeoDescription = p.SeoDescription,
                    SeoTitleAr = p.SeoTitleAr,
                    SeoDescriptionAr = p.SeoDescriptionAr,
                    SeoKeywords = p.SeoKeywords,
                    SeoKeywordsAr = p.SeoKeywordsAr,
                    CanonicalUrl = p.CanonicalUrl,
                    IsRecommended = p.IsRecommended,
                    DeliveryText = p.DeliveryText,
                    DeliveryTextAr = p.DeliveryTextAr,
                    ConstructionStatus = p.ConstructionStatus,
                    AvailabilityStatus = p.AvailabilityStatus ?? "Available",
                    OwnershipType = p.OwnershipType,
                    ViewCount = p.ViewCount,
                    InquiryCount = p.InquiryCount,
                    FavoriteCount = p.FavoriteCount,
                    VirtualTourUrl = p.VirtualTourUrl,
                    HighlightsAr = p.HighlightsAr,
                    NearbyPlaces = p.NearbyPlaces,
                    NearbyPlacesAr = p.NearbyPlacesAr,
                    JsonLd = BuildJsonLdFromProjected(p),
                    ImagesMeta = images.Select(u => new ImageDto { Url = u, Width = 1200, Height = 800 }).ToList()
                };
            }).ToList();
        }

        private string BuildJsonLdFromProperty(Property p, List<string> images)
        {
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

        private string BuildJsonLdFromProjected(dynamic p)
        {
            try
            {
                System.Collections.Generic.IEnumerable<string> imgs = p.Images;
                var images = imgs?.ToList() ?? new List<string>();
                var listingType = TryGetDynamicString(p.ListingType) ?? "Sale";
                decimal? rentPerMonth = TryGetDynamicDecimal(p.RentPerMonth);
                decimal price = (decimal)p.Price;
                var displayPrice = string.Equals(listingType, "Rental", System.StringComparison.OrdinalIgnoreCase)
                    && rentPerMonth.GetValueOrDefault() > 0
                    ? rentPerMonth.Value
                    : price;

                string? code = TryGetDynamicString(p.Code);
                string? canonicalUrl = TryGetDynamicString(p.CanonicalUrl);
                string? seoDescription = TryGetDynamicString(p.SeoDescription);
                string? descriptionEn = TryGetDynamicString(p.DescriptionEn);
                string? currency = TryGetDynamicString(p.Currency) ?? "EGP";

                return _jsonLdService.BuildPropertyJsonLd(
                    TryGetDynamicString(p.TitleEn),
                    descriptionEn,
                    seoDescription,
                    canonicalUrl,
                    code,
                    TryGetDynamicString(p.Location),
                    currency,
                    listingType,
                    displayPrice,
                    rentPerMonth,
                    images,
                    ((int)p.Id).ToString());
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to generate JSON-LD for property");
                return string.Empty;
            }
        }

        private async Task<PropertyBaseDto> ApplySeoEnhancementsAsync(PropertyBaseDto dto, dynamic p)
        {
            try
            {
                var titleEn = TryGetDynamicString(p.TitleEn);
                var descriptionEn = TryGetDynamicString(p.DescriptionEn);
                var location = TryGetDynamicString(p.Location);
                var propertyType = TryGetDynamicString(p.PropertyType);
                var listingType = TryGetDynamicString(p.ListingType);
                List<string>? features = null;
                try { var raw = p.Features; if (raw != null) features = ((System.Collections.IEnumerable)raw).Cast<object>().Select(x => x?.ToString() ?? "").ToList(); } catch { }
                var slug = TryGetDynamicString(p.Slug);
                decimal price = (decimal)p.Price;
                string? canonicalUrl = TryGetDynamicString(p.CanonicalUrl);

                var seoContent = (SeoContentResult)_seoContentGenerator.Generate(
                    Application.Interfaces.SeoEntityType.Property,
                    titleEn, TryGetDynamicString(p.TitleAr),
                    descriptionEn, TryGetDynamicString(p.DescriptionAr),
                    location, propertyType, listingType, price, TryGetDynamicString(p.Currency) ?? "EGP",
                    features);

                if (string.IsNullOrWhiteSpace(dto.SeoTitle))
                    dto.SeoTitle = seoContent.TitleEn;
                if (string.IsNullOrWhiteSpace(dto.SeoDescription))
                    dto.SeoDescription = seoContent.DescriptionEn;
                if (string.IsNullOrWhiteSpace(dto.SeoKeywords))
                    dto.SeoKeywords = seoContent.PrimaryKeyword;

                var faqs = seoContent.Faqs
                    .Select(f => (f.QuestionEn, f.AnswerEn))
                    .ToList();

                if (faqs.Count > 0)
                    dto.FaqJsonLd = _jsonLdService.BuildFaqJsonLd(faqs);

                if (!string.IsNullOrWhiteSpace(canonicalUrl))
                {
                    var breadcrumbItems = new List<(string, string)>
                    {
                        ("Home", "/"),
                        ("Properties", "/properties/filter"),
                        (titleEn ?? slug ?? "Details", canonicalUrl)
                    };
                    dto.BreadcrumbJsonLd = _jsonLdService.BuildBreadcrumbJsonLd(breadcrumbItems);
                }

                var internalLinks = _internalLinkingService.GenerateLinks(
                    location, propertyType, listingType, slug, null);

                if (!_internalLinkingService.MeetsMinimumRequirement(internalLinks))
                {
                    var missing = _internalLinkingService.GetMissingLinks(location, propertyType, listingType, slug);
                    if (missing.Count > 0)
                        internalLinks.AddRange(missing);
                }

                dto.InternalLinksJson = InternalLinkingService.ToJson(internalLinks);

                var serpRequest = new Application.Interfaces.SerpVariantRequest
                {
                    EntityType = Application.Interfaces.SeoEntityType.Property,
                    TitleEn = titleEn,
                    TitleAr = TryGetDynamicString(p.TitleAr),
                    DescriptionEn = descriptionEn,
                    DescriptionAr = TryGetDynamicString(p.DescriptionAr),
                    Location = location,
                    PropertyType = propertyType,
                    ListingType = listingType,
                    Price = price,
                    Currency = TryGetDynamicString(p.Currency) ?? "EGP",
                    Features = features
                };
                var variants = _serpVariantGenerator.GenerateVariants(serpRequest);
                var bestVariant = _serpVariantGenerator.SelectBestVariant(variants);
                if (bestVariant.PredictedCtrScore > 75)
                {
                    dto.SeoTitle = dto.SeoTitle ?? bestVariant.TitleEn;
                    dto.SeoDescription = dto.SeoDescription ?? bestVariant.DescriptionEn;
                }

                var entityNode = _entityGraphService.BuildEntityNode("property", (TryGetDynamicString(p.Slug) ?? ""), titleEn ?? "", descriptionEn);
                var entityGraph = _entityGraphService.BuildKnowledgeGraph("property", TryGetDynamicString(p.Slug) ?? "");
                if (!string.IsNullOrWhiteSpace(entityGraph.JsonLd))
                    dto.EntityGraphJson = entityGraph.JsonLd;

                var dupResult = await _semanticDedup.AnalyzePageAsync(
                    canonicalUrl ?? $"/property/{slug}",
                    seoContent.TitleEn, seoContent.DescriptionEn);

                var validation = _seoValidationGate.ValidatePropertySeo(
                    dto.SeoTitle,
                    dto.SeoDescription,
                    dto.CanonicalUrl,
                    propertyType,
                    location,
                    dto.FaqJsonLd,
                    dto.BreadcrumbJsonLd,
                    dto.EntityGraphJson,
                    dto.InternalLinksJson,
                    listingType,
                    price);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to apply SEO enhancements");
            }
            return dto;
        }

        private List<HreflangTagDto> BuildHreflangTags(string? slugEn, string? slugAr)
        {
            var tags = _canonicalService.BuildHreflangTags("property", slugEn ?? string.Empty, slugAr, null, null);
            return tags.Select(t => new HreflangTagDto
            {
                HrefLang = t.HrefLang,
                Href = t.Href
            }).ToList();
        }

        private static string? TryGetDynamicString(object value)
        {
            try { return (string?)value; }
            catch (Exception) { return null; }
        }

        private static decimal? TryGetDynamicDecimal(object value)
        {
            try { return (decimal?)value; }
            catch (Exception) { return null; }
        }
    }
}
