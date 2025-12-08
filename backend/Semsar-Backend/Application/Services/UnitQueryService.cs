using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{
    public class UnitQueryService : IUnitQueryService
    {
        private readonly IUnitOfWork _uow;
        private readonly IJsonLdService _jsonLdService;
        private readonly ICanonicalService _canonicalService;
        private readonly ISeoContentGenerator _seoContentGenerator;
        private readonly ISERPVariantGenerator _serpVariantGenerator;
        private readonly IEntityGraphService _entityGraphService;
        private readonly IInternalLinkingService _internalLinkingService;
        private readonly IClickBehaviorOptimizationService _clickBehavior;
        private readonly IPublicIdService _publicIdService;
        private readonly Application.Interfaces.ICloudinaryService? _cloud;
        private readonly ILogger<UnitQueryService>? _logger;

        public UnitQueryService(
            IUnitOfWork uow,
            IJsonLdService jsonLdService,
            ICanonicalService canonicalService,
            ISeoContentGenerator seoContentGenerator,
            ISERPVariantGenerator serpVariantGenerator,
            IEntityGraphService entityGraphService,
            IInternalLinkingService internalLinkingService,
            IClickBehaviorOptimizationService clickBehavior,
            IPublicIdService publicIdService,
            Application.Interfaces.ICloudinaryService? cloud = null,
            ILogger<UnitQueryService>? logger = null)
        {
            _uow = uow;
            _jsonLdService = jsonLdService;
            _canonicalService = canonicalService;
            _seoContentGenerator = seoContentGenerator;
            _serpVariantGenerator = serpVariantGenerator;
            _entityGraphService = entityGraphService;
            _internalLinkingService = internalLinkingService;
            _clickBehavior = clickBehavior;
            _publicIdService = publicIdService;
            _cloud = cloud;
            _logger = logger;
        }

        private string ResolvePublicKey(string? publicKey, string entityTypePrefix)
        {
            return !string.IsNullOrEmpty(publicKey) ? publicKey : _publicIdService.GenerateId(entityTypePrefix);
        }

        public async Task<List<UnitPublicDto>> GetPublicCardsAsync(int? projectId, CancellationToken ct = default)
        {
            var q = _uow.Units.Query()
                .Where(u => !u.IsDeleted);
            if (projectId.HasValue) q = q.Where(u => u.ProjectId == projectId.Value);

            var units = await q.OrderBy(u => u.CreatedAt)
                .Select(u => new
                {
                    u.Id,
                    u.PublicKey,
                    u.TitleEn,
                    u.TitleAr,
                    u.DescriptionEn,
                    u.DescriptionAr,
                    u.MinPrice,
                    u.MaxPrice,
                    u.MinArea,
                    u.MaxArea,
                    u.Location,
                    LocationAr = u.LocationAr,
                    u.Currency,
                    u.RentPerMonth,
                    u.IsFeatured,
                    PropertyType = u.PropertyType.ToString(),
                    ListingType = u.ListingType.ToString(),
                    Features = u.Features,
                    FeaturesAr = u.FeaturesAr,
                    Images = u.Images != null ? u.Images.Select(i => i.Url) : Enumerable.Empty<string>(),
                    Installments = u.Installments != null
                        ? u.Installments.Where(i => !i.IsDeleted && i.IsEnabled).Select(i => new InstallmentDto
                        {
                            DownPaymentPercent = i.DownPaymentPercent,
                            DiscountPercent = i.DiscountPercent,
                            Years = i.Years,
                            IsEnabled = i.IsEnabled,
                            IsDeleted = i.IsDeleted,
                            PaymentType = i.PaymentType.ToString()
                        }).ToList()
                        : new List<InstallmentDto>(),
                    u.ProjectId,
                    ProjectName = u.Project != null ? u.Project.NameEn : null,
                    u.Slug,
                    u.SeoTitle,
                    u.SeoDescription,
                    u.SeoTitleAr,
                    u.SeoDescriptionAr,
                    u.SeoKeywords,
                    u.SeoKeywordsAr,
                    u.CanonicalUrl,
                    u.DeliveryText,
                    u.DeliveryTextAr,
                    u.IsRecommended,
                    ConstructionStatus = u.ConstructionStatus != null ? u.ConstructionStatus.ToString() : null,
                    AvailabilityStatus = u.AvailabilityStatus ?? "Available",
                    OwnershipType = u.OwnershipType != null ? u.OwnershipType.ToString() : null,
                    u.ViewCount,
                    u.InquiryCount,
                    u.FavoriteCount,
                    u.VirtualTourUrl,
                    u.HighlightsAr,
                    u.NearbyPlaces,
                    u.NearbyPlacesAr,
                    u.Code,
                    u.Bedrooms,
                    u.Bathrooms,
                    u.Floor,
                    u.IsFurnished,
                    View = u.View,
                    u.UnitNumber,
                    u.BuildingNumber,
                    u.DeliveryDate,
                    FinishingType = u.FinishingType,
                    u.HasBalcony,
                    u.HasParking,
                    Videos = u.Videos != null ? u.Videos.Where(v => !v.IsDeleted).OrderBy(v => v.SortOrder).Select(v => new VideoDto
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
                    Variants = new List<UnitVariantDto>()
                })
                .ToListAsync(ct);

            return units.Select(u => MapProjectionToPublicDto(u)).ToList();
        }

        public async Task<(List<UnitPublicDto> Data, int Total)> GetPublicCardsPagedAsync(int? projectId, int page, int pageSize, CancellationToken ct = default)
        {
            var q = _uow.Units.Query()
                .Where(u => !u.IsDeleted);
            if (projectId.HasValue) q = q.Where(u => u.ProjectId == projectId.Value);

            var total = await q.CountAsync(ct);
            var units = await q
                .OrderBy(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.PublicKey,
                    u.TitleEn,
                    u.TitleAr,
                    u.DescriptionEn,
                    u.DescriptionAr,
                    u.MinPrice,
                    u.MaxPrice,
                    u.MinArea,
                    u.MaxArea,
                    u.Location,
                    LocationAr = u.LocationAr,
                    u.Currency,
                    u.RentPerMonth,
                    u.IsFeatured,
                    PropertyType = u.PropertyType.ToString(),
                    ListingType = u.ListingType.ToString(),
                    Features = u.Features,
                    FeaturesAr = u.FeaturesAr,
                    Images = u.Images != null ? u.Images.Select(i => i.Url) : Enumerable.Empty<string>(),
                    Installments = u.Installments != null
                        ? u.Installments.Where(i => !i.IsDeleted && i.IsEnabled).Select(i => new InstallmentDto
                        {
                            DownPaymentPercent = i.DownPaymentPercent,
                            DiscountPercent = i.DiscountPercent,
                            Years = i.Years,
                            IsEnabled = i.IsEnabled,
                            IsDeleted = i.IsDeleted,
                            PaymentType = i.PaymentType.ToString()
                        }).ToList()
                        : new List<InstallmentDto>(),
                    u.ProjectId,
                    ProjectName = u.Project != null ? u.Project.NameEn : null,
                    u.Slug,
                    u.SeoTitle,
                    u.SeoDescription,
                    u.SeoTitleAr,
                    u.SeoDescriptionAr,
                    u.SeoKeywords,
                    u.SeoKeywordsAr,
                    u.CanonicalUrl,
                    u.DeliveryText,
                    u.DeliveryTextAr,
                    u.IsRecommended,
                    ConstructionStatus = u.ConstructionStatus != null ? u.ConstructionStatus.ToString() : null,
                    AvailabilityStatus = u.AvailabilityStatus ?? "Available",
                    OwnershipType = u.OwnershipType != null ? u.OwnershipType.ToString() : null,
                    u.ViewCount,
                    u.InquiryCount,
                    u.FavoriteCount,
                    u.VirtualTourUrl,
                    u.HighlightsAr,
                    u.NearbyPlaces,
                    u.NearbyPlacesAr,
                    u.Code,
                    u.Bedrooms,
                    u.Bathrooms,
                    u.Floor,
                    u.IsFurnished,
                    View = u.View,
                    u.UnitNumber,
                    u.BuildingNumber,
                    u.DeliveryDate,
                    FinishingType = u.FinishingType,
                    u.HasBalcony,
                    u.HasParking,
                    Videos = u.Videos != null ? u.Videos.Where(v => !v.IsDeleted).OrderBy(v => v.SortOrder).Select(v => new VideoDto
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
                    Variants = new List<UnitVariantDto>()
                })
                .ToListAsync(ct);

            var data = units.Select(u => MapProjectionToPublicDto(u)).ToList();
            return (data, total);
        }

        public async Task<UnitPublicDto?> GetBySlugAsync(string slug, CancellationToken ct = default)
        {
            var u = await _uow.Units.Query()
                .Where(unit => unit.Slug == slug && !unit.IsDeleted)
                .Select(u2 => new
                {
                    u2.Id,
                    u2.PublicKey,
                    u2.TitleEn,
                    u2.TitleAr,
                    u2.DescriptionEn,
                    u2.DescriptionAr,
                    u2.MinPrice,
                    u2.MaxPrice,
                    u2.MinArea,
                    u2.MaxArea,
                    u2.Location,
                    LocationAr = u2.LocationAr,
                    u2.Currency,
                    u2.RentPerMonth,
                    u2.IsFeatured,
                    PropertyType = u2.PropertyType.ToString(),
                    ListingType = u2.ListingType.ToString(),
                    Features = u2.Features,
                    FeaturesAr = u2.FeaturesAr,
                    Images = u2.Images != null ? u2.Images.Select(i => i.Url) : Enumerable.Empty<string>(),
                    Installments = u2.Installments != null
                        ? u2.Installments.Where(i => !i.IsDeleted && i.IsEnabled).Select(i => new InstallmentDto
                        {
                            DownPaymentPercent = i.DownPaymentPercent,
                            DiscountPercent = i.DiscountPercent,
                            Years = i.Years,
                            IsEnabled = i.IsEnabled,
                            IsDeleted = i.IsDeleted,
                            PaymentType = i.PaymentType.ToString()
                        }).ToList()
                        : new List<InstallmentDto>(),
                    u2.ProjectId,
                    ProjectName = u2.Project != null ? u2.Project.NameEn : null,
                    u2.Slug,
                    u2.SeoTitle,
                    u2.SeoDescription,
                    u2.SeoTitleAr,
                    u2.SeoDescriptionAr,
                    u2.SeoKeywords,
                    u2.SeoKeywordsAr,
                    u2.CanonicalUrl,
                    u2.DeliveryText,
                    u2.DeliveryTextAr,
                    u2.IsRecommended,
                    ConstructionStatus = u2.ConstructionStatus != null ? u2.ConstructionStatus.ToString() : null,
                    AvailabilityStatus = u2.AvailabilityStatus ?? "Available",
                    OwnershipType = u2.OwnershipType != null ? u2.OwnershipType.ToString() : null,
                    u2.ViewCount,
                    u2.InquiryCount,
                    u2.FavoriteCount,
                    u2.VirtualTourUrl,
                    u2.HighlightsAr,
                    u2.NearbyPlaces,
                    u2.NearbyPlacesAr,
                    u2.Code,
                    u2.Bedrooms,
                    u2.Bathrooms,
                    u2.Floor,
                    u2.IsFurnished,
                    View = u2.View,
                    u2.UnitNumber,
                    u2.BuildingNumber,
                    u2.DeliveryDate,
                    FinishingType = u2.FinishingType,
                    u2.HasBalcony,
                    u2.HasParking,
                    Videos = u2.Videos != null ? u2.Videos.Where(v => !v.IsDeleted).OrderBy(v => v.SortOrder).Select(v => new VideoDto
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
                    Variants = u2.Variants != null
                        ? u2.Variants.Where(v => !v.IsDeleted && v.IsActive).OrderBy(v => v.SortOrder).Select(v => new UnitVariantDto
                        {
                            Id = v.Id,
                            PublicKey = v.PublicKey,
                            Name = v.Name,
                            NameAr = v.NameAr,
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
                            IsFeatured = v.IsFeatured,
                            IsRecommended = v.IsRecommended,
                            ViewCount = v.ViewCount,
                            InquiryCount = v.InquiryCount,
                            FavoriteCount = v.FavoriteCount,
                            DeliveryText = v.DeliveryText,
                            DeliveryTextAr = v.DeliveryTextAr,
                            SortOrder = v.SortOrder,
                            IsActive = v.IsActive
                        }).ToList()
                        : new List<UnitVariantDto>()
                })
                .FirstOrDefaultAsync(ct);

            if (u == null) return null;

            _clickBehavior.RecordImpression($"/unit/{u.Slug}");
            return MapProjectionToPublicDto(u);
        }

        public async Task<UnitPublicDto?> GetPublicByIdAsync(int id, CancellationToken ct = default)
        {
            var u = await _uow.Units.Query()
                .Where(unit => unit.Id == id && !unit.IsDeleted)
                .Select(u2 => new
                {
                    u2.Id,
                    u2.PublicKey,
                    u2.TitleEn,
                    u2.TitleAr,
                    u2.DescriptionEn,
                    u2.DescriptionAr,
                    u2.MinPrice,
                    u2.MaxPrice,
                    u2.MinArea,
                    u2.MaxArea,
                    u2.Location,
                    LocationAr = u2.LocationAr,
                    u2.Currency,
                    u2.RentPerMonth,
                    u2.IsFeatured,
                    PropertyType = u2.PropertyType.ToString(),
                    ListingType = u2.ListingType.ToString(),
                    Features = u2.Features,
                    FeaturesAr = u2.FeaturesAr,
                    Images = u2.Images != null ? u2.Images.Select(i => i.Url) : Enumerable.Empty<string>(),
                    Installments = u2.Installments != null
                        ? u2.Installments.Where(i => !i.IsDeleted && i.IsEnabled).Select(i => new InstallmentDto
                        {
                            DownPaymentPercent = i.DownPaymentPercent,
                            DiscountPercent = i.DiscountPercent,
                            Years = i.Years,
                            IsEnabled = i.IsEnabled,
                            IsDeleted = i.IsDeleted,
                            PaymentType = i.PaymentType.ToString()
                        }).ToList()
                        : new List<InstallmentDto>(),
                    u2.ProjectId,
                    ProjectName = u2.Project != null ? u2.Project.NameEn : null,
                    u2.Slug,
                    u2.SeoTitle,
                    u2.SeoDescription,
                    u2.SeoTitleAr,
                    u2.SeoDescriptionAr,
                    u2.SeoKeywords,
                    u2.SeoKeywordsAr,
                    u2.CanonicalUrl,
                    u2.DeliveryText,
                    u2.DeliveryTextAr,
                    u2.IsRecommended,
                    ConstructionStatus = u2.ConstructionStatus != null ? u2.ConstructionStatus.ToString() : null,
                    AvailabilityStatus = u2.AvailabilityStatus ?? "Available",
                    OwnershipType = u2.OwnershipType != null ? u2.OwnershipType.ToString() : null,
                    u2.ViewCount,
                    u2.InquiryCount,
                    u2.FavoriteCount,
                    u2.VirtualTourUrl,
                    u2.HighlightsAr,
                    u2.NearbyPlaces,
                    u2.NearbyPlacesAr,
                    u2.Code,
                    u2.Bedrooms,
                    u2.Bathrooms,
                    u2.Floor,
                    u2.IsFurnished,
                    View = u2.View,
                    u2.UnitNumber,
                    u2.BuildingNumber,
                    u2.DeliveryDate,
                    FinishingType = u2.FinishingType,
                    u2.HasBalcony,
                    u2.HasParking,
                    Videos = u2.Videos != null ? u2.Videos.Where(v => !v.IsDeleted).OrderBy(v => v.SortOrder).Select(v => new VideoDto
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
                    Variants = u2.Variants != null
                        ? u2.Variants.Where(v => !v.IsDeleted && v.IsActive).OrderBy(v => v.SortOrder).Select(v => new UnitVariantDto
                        {
                            Id = v.Id,
                            PublicKey = v.PublicKey,
                            Name = v.Name,
                            NameAr = v.NameAr,
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
                            IsFeatured = v.IsFeatured,
                            IsRecommended = v.IsRecommended,
                            ViewCount = v.ViewCount,
                            InquiryCount = v.InquiryCount,
                            FavoriteCount = v.FavoriteCount,
                            DeliveryText = v.DeliveryText,
                            DeliveryTextAr = v.DeliveryTextAr,
                            SortOrder = v.SortOrder,
                            IsActive = v.IsActive
                        }).ToList()
                        : new List<UnitVariantDto>()
                })
                .FirstOrDefaultAsync(ct);

            if (u == null) return null;
            return MapProjectionToPublicDto(u);
        }

        public async Task<UnitPublicDto?> GetPublicByPublicKeyAsync(string publicKey, CancellationToken ct = default)
        {
            var u = await _uow.Units.Query()
                .Where(unit => unit.PublicKey == publicKey && !unit.IsDeleted)
                .Select(u2 => new
                {
                    u2.Id,
                    u2.PublicKey,
                    u2.TitleEn,
                    u2.TitleAr,
                    u2.DescriptionEn,
                    u2.DescriptionAr,
                    u2.MinPrice,
                    u2.MaxPrice,
                    u2.MinArea,
                    u2.MaxArea,
                    u2.Location,
                    LocationAr = u2.LocationAr,
                    u2.Currency,
                    u2.RentPerMonth,
                    u2.IsFeatured,
                    PropertyType = u2.PropertyType.ToString(),
                    ListingType = u2.ListingType.ToString(),
                    Features = u2.Features,
                    FeaturesAr = u2.FeaturesAr,
                    Images = u2.Images != null ? u2.Images.Select(i => i.Url) : Enumerable.Empty<string>(),
                    Installments = u2.Installments != null
                        ? u2.Installments.Where(i => !i.IsDeleted && i.IsEnabled).Select(i => new InstallmentDto
                        {
                            DownPaymentPercent = i.DownPaymentPercent,
                            DiscountPercent = i.DiscountPercent,
                            Years = i.Years,
                            IsEnabled = i.IsEnabled,
                            IsDeleted = i.IsDeleted,
                            PaymentType = i.PaymentType.ToString()
                        }).ToList()
                        : new List<InstallmentDto>(),
                    u2.ProjectId,
                    ProjectName = u2.Project != null ? u2.Project.NameEn : null,
                    u2.Slug,
                    u2.SeoTitle,
                    u2.SeoDescription,
                    u2.SeoTitleAr,
                    u2.SeoDescriptionAr,
                    u2.SeoKeywords,
                    u2.SeoKeywordsAr,
                    u2.CanonicalUrl,
                    u2.DeliveryText,
                    u2.DeliveryTextAr,
                    u2.IsRecommended,
                    ConstructionStatus = u2.ConstructionStatus != null ? u2.ConstructionStatus.ToString() : null,
                    AvailabilityStatus = u2.AvailabilityStatus ?? "Available",
                    OwnershipType = u2.OwnershipType != null ? u2.OwnershipType.ToString() : null,
                    u2.ViewCount,
                    u2.InquiryCount,
                    u2.FavoriteCount,
                    u2.VirtualTourUrl,
                    u2.HighlightsAr,
                    u2.NearbyPlaces,
                    u2.NearbyPlacesAr,
                    u2.Code,
                    u2.Bedrooms,
                    u2.Bathrooms,
                    u2.Floor,
                    u2.IsFurnished,
                    View = u2.View,
                    u2.UnitNumber,
                    u2.BuildingNumber,
                    u2.DeliveryDate,
                    FinishingType = u2.FinishingType,
                    u2.HasBalcony,
                    u2.HasParking,
                    Videos = u2.Videos != null ? u2.Videos.Where(v => !v.IsDeleted).OrderBy(v => v.SortOrder).Select(v => new VideoDto
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
                    Variants = u2.Variants != null
                        ? u2.Variants.Where(v => !v.IsDeleted && v.IsActive).OrderBy(v => v.SortOrder).Select(v => new UnitVariantDto
                        {
                            Id = v.Id,
                            PublicKey = v.PublicKey,
                            Name = v.Name,
                            NameAr = v.NameAr,
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
                            IsFeatured = v.IsFeatured,
                            IsRecommended = v.IsRecommended,
                            ViewCount = v.ViewCount,
                            InquiryCount = v.InquiryCount,
                            FavoriteCount = v.FavoriteCount,
                            DeliveryText = v.DeliveryText,
                            DeliveryTextAr = v.DeliveryTextAr,
                            SortOrder = v.SortOrder,
                            IsActive = v.IsActive
                        }).ToList()
                        : new List<UnitVariantDto>()
                })
                .FirstOrDefaultAsync(ct);

            if (u == null) return null;
            return MapProjectionToPublicDto(u);
        }

        public async Task<UnitDetailsDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var unit = await _uow.Units.Query()
                .Include(u => u.Images)
                .Include(u => u.Installments)
                .Include(u => u.Project)
                .Include(u => u.Contact)
                .Include(u => u.Variants)
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);

            if (unit == null) return null;

            _clickBehavior.RecordImpression($"/unit/{unit.Slug}");
            return MapToDetailsDto(unit, unit.Contact);
        }

        public async Task<Domain.Entities.Unit?> GetByCodeAsync(string code, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            return await _uow.Units.Query()
                .Include(u => u.Images)
                .Include(u => u.Installments)
                .Include(u => u.Project)
                .Include(u => u.Contact)
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Code == code && !u.IsDeleted, ct);
        }

        private UnitPublicDto MapProjectionToPublicDto(dynamic u)
        {
            var images = ((IEnumerable<string>?)u.Images)?.ToList() ?? new List<string>();
            if (_cloud != null && images.Count > 0)
                images = images.Select(img => _cloud.GetOptimizedUrl(img)).ToList();

            var dto = new UnitPublicDto
            {
                Id = u.Id,
                PublicKey = ResolvePublicKey(u.PublicKey, Application.Common.EntityType.Unit),
                Code = u.Code ?? string.Empty,
                Bedrooms = u.Bedrooms,
                Bathrooms = u.Bathrooms,
                Floor = u.Floor,
                IsFurnished = u.IsFurnished,
                View = u.View?.ToString() ?? "Unknown",
                UnitNumber = u.UnitNumber,
                BuildingNumber = u.BuildingNumber,
                DeliveryDate = u.DeliveryDate,
                FinishingType = u.FinishingType?.ToString(),
                HasBalcony = u.HasBalcony,
                HasParking = u.HasParking,
                TitleEn = u.TitleEn,
                TitleAr = u.TitleAr,
                DescriptionEn = u.DescriptionEn,
                DescriptionAr = u.DescriptionAr,
                MinPrice = u.MinPrice,
                MaxPrice = u.MaxPrice,
                MinArea = u.MinArea,
                MaxArea = u.MaxArea,
                Location = u.Location,
                LocationAr = u.LocationAr,
                Currency = u.Currency,
                RentPerMonth = u.RentPerMonth,
                IsFeatured = u.IsFeatured,
                PropertyType = u.PropertyType,
                ListingType = u.ListingType,
                Features = u.Features ?? new List<string>(),
                FeaturesAr = u.FeaturesAr ?? new List<string>(),
                Images = images,
                Installments = u.Installments ?? new List<InstallmentDto>(),
                ProjectId = u.ProjectId,
                ProjectName = u.ProjectName ?? string.Empty,
                Slug = u.Slug,
                SeoTitle = u.SeoTitle,
                SeoDescription = u.SeoDescription,
                SeoTitleAr = u.SeoTitleAr,
                SeoDescriptionAr = u.SeoDescriptionAr,
                SeoKeywords = u.SeoKeywords,
                SeoKeywordsAr = u.SeoKeywordsAr,
                CanonicalUrl = u.CanonicalUrl,
                IsRecommended = u.IsRecommended,
                DeliveryText = u.DeliveryText,
                    DeliveryTextAr = u.DeliveryTextAr,
                ConstructionStatus = u.ConstructionStatus,
                AvailabilityStatus = u.AvailabilityStatus ?? "Available",
                OwnershipType = u.OwnershipType,
                ViewCount = u.ViewCount,
                InquiryCount = u.InquiryCount,
                FavoriteCount = u.FavoriteCount,
                VirtualTourUrl = u.VirtualTourUrl,
                HighlightsAr = u.HighlightsAr,
                NearbyPlaces = u.NearbyPlaces,
                NearbyPlacesAr = u.NearbyPlacesAr,
                JsonLd = BuildJsonLdFromProjection(u, images),
                ImagesMeta = images.Select(img => new ImageDto { Url = img, Width = 1200, Height = 800 }).ToList(),
                Videos = u.Videos,
                Variants = u.Variants ?? new List<UnitVariantDto>()
            };

            ApplySeoEnhancements(dto, u);
            return dto;
        }

        private void ApplySeoEnhancements(UnitPublicDto dto, dynamic u)
        {
            try
            {
                string? titleEn = TryGetString(u.TitleEn);
                string? titleAr = TryGetString(u.TitleAr);
                string? descriptionEn = TryGetString(u.DescriptionEn);
                string? descriptionAr = TryGetString(u.DescriptionAr);
                string? location = TryGetString(u.Location);
                string? propertyType = TryGetString(u.PropertyType);
                string? listingType = TryGetString(u.ListingType);
                string? slug = TryGetString(u.Slug);
                string? canonicalUrl = TryGetString(u.CanonicalUrl);
                decimal price = (decimal)(u.MinPrice ?? 0);
                List<string>? features = null;
                try { var raw = u.Features; if (raw != null) features = ((System.Collections.IEnumerable)raw).Cast<object>().Select(x => x?.ToString() ?? "").ToList(); } catch { }

                var seoContent = _seoContentGenerator.Generate(
                    SeoEntityType.Unit,
                    titleEn, titleAr,
                    descriptionEn, descriptionAr,
                    location, propertyType, listingType,
                    price, "EGP", features);

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
                        ("Units", "/units"),
                        (titleEn ?? slug ?? "Details", canonicalUrl)
                    };
                    dto.BreadcrumbJsonLd = _jsonLdService.BuildBreadcrumbJsonLd(breadcrumbItems);
                }

                var internalLinks = _internalLinkingService.GenerateLinks(
                    location, propertyType, listingType, slug, null);

                if (!_internalLinkingService.MeetsMinimumRequirement(internalLinks))
                {
                    var missing = _internalLinkingService.GetMissingLinks(
                        location, propertyType, listingType, slug);
                    if (missing.Count > 0)
                        internalLinks.AddRange(missing);
                }

                dto.InternalLinksJson = InternalLinkingService.ToJson(internalLinks);

                var serpRequest = new SerpVariantRequest
                {
                    EntityType = SeoEntityType.Unit,
                    TitleEn = titleEn,
                    TitleAr = titleAr,
                    DescriptionEn = descriptionEn,
                    DescriptionAr = descriptionAr,
                    Location = location,
                    PropertyType = propertyType,
                    ListingType = listingType,
                    Price = price,
                    Currency = "EGP",
                    Features = features
                };
                var variants = _serpVariantGenerator.GenerateVariants(serpRequest);
                var bestVariant = _serpVariantGenerator.SelectBestVariant(variants);
                if (bestVariant.PredictedCtrScore > 75)
                {
                    dto.SeoTitle = dto.SeoTitle ?? bestVariant.TitleEn;
                    dto.SeoDescription = dto.SeoDescription ?? bestVariant.DescriptionEn;
                }

                var entityNode = _entityGraphService.BuildEntityNode(
                    "unit", slug ?? "", titleEn ?? "", descriptionEn);
                var entityGraph = _entityGraphService.BuildKnowledgeGraph(
                    "unit", slug ?? "");
                if (!string.IsNullOrWhiteSpace(entityGraph.JsonLd))
                    dto.EntityGraphJson = entityGraph.JsonLd;
            }
            catch (System.Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to apply SEO enhancements for unit");
            }
        }

        private string BuildJsonLdFromProjection(dynamic u, List<string> images)
        {
            try
            {
                var isRental = string.Equals((string?)u.ListingType, "Rental", System.StringComparison.OrdinalIgnoreCase);
                var displayPrice = isRental && ((decimal?)u.RentPerMonth).GetValueOrDefault() > 0
                    ? ((decimal?)u.RentPerMonth).Value
                    : (decimal)(u.MinPrice ?? 0);

                return _jsonLdService.BuildPropertyJsonLd(
                    TryGetString(u.TitleEn),
                    TryGetString(u.DescriptionEn),
                    TryGetString(u.SeoDescription),
                    TryGetString(u.CanonicalUrl),
                    ((int)u.Id).ToString(),
                    TryGetString(u.Location),
                    "EGP",
                    TryGetString(u.ListingType),
                    displayPrice,
                    (decimal?)u.RentPerMonth,
                    images,
                    ((int)u.Id).ToString());
            }
            catch (System.Exception)
            {
                return string.Empty;
            }
        }

        private UnitDetailsDto MapToDetailsDto(Domain.Entities.Unit u, Domain.Entities.ContactInfo? contact = null)
        {
            var images = u.Images == null ? new List<string>() : u.Images.Select(i => i.Url).ToList();
            if (_cloud != null && images.Count > 0)
                images = images.Select(img => _cloud.GetOptimizedUrl(img)).ToList();

            var installments = u.Installments == null
                ? new List<InstallmentDto>()
                : u.Installments
                    .Where(i => !i.IsDeleted)
                    .Select(i => new InstallmentDto
                    {
                        DownPaymentPercent = i.DownPaymentPercent,
                        DiscountPercent = i.DiscountPercent,
                        Years = i.Years,
                        IsEnabled = i.IsEnabled,
                        IsDeleted = i.IsDeleted,
                        PaymentType = i.PaymentType.ToString()
                    }).ToList();
            var dto = new UnitDetailsDto
            {
                Id = u.Id,
                PublicKey = ResolvePublicKey(u.PublicKey, Application.Common.EntityType.Unit),
                TitleEn = u.TitleEn,
                TitleAr = u.TitleAr,
                DescriptionEn = u.DescriptionEn,
                DescriptionAr = u.DescriptionAr,
                MinPrice = u.MinPrice,
                MaxPrice = u.MaxPrice,
                MinArea = u.MinArea,
                MaxArea = u.MaxArea,
                Location = u.Location,
                LocationAr = u.LocationAr,
                Currency = u.Currency,
                RentPerMonth = u.RentPerMonth,
                IsFeatured = u.IsFeatured,
                PropertyType = u.PropertyType.ToString(),
                ListingType = u.ListingType.ToString(),
                Features = u.Features ?? new List<string>(),
                FeaturesAr = u.FeaturesAr ?? new List<string>(),
                Images = images,
                Installments = installments,
                ProjectId = u.ProjectId,
                ProjectName = u.Project?.NameEn ?? string.Empty,
                Slug = u.Slug,
                SeoTitle = u.SeoTitle,
                SeoDescription = u.SeoDescription,
                SeoTitleAr = u.SeoTitleAr,
                SeoDescriptionAr = u.SeoDescriptionAr,
                SeoKeywords = u.SeoKeywords,
                SeoKeywordsAr = u.SeoKeywordsAr,
                CanonicalUrl = u.CanonicalUrl,
                IsRecommended = u.IsRecommended,
                DeliveryText = u.DeliveryText,
                    DeliveryTextAr = u.DeliveryTextAr,
                ConstructionStatus = u.ConstructionStatus?.ToString(),
                AvailabilityStatus = u.AvailabilityStatus ?? "Available",
                OwnershipType = u.OwnershipType?.ToString(),
                ViewCount = u.ViewCount,
                InquiryCount = u.InquiryCount,
                FavoriteCount = u.FavoriteCount,
                VirtualTourUrl = u.VirtualTourUrl,
                HighlightsAr = u.HighlightsAr,
                NearbyPlaces = u.NearbyPlaces,
                NearbyPlacesAr = u.NearbyPlacesAr,
                JsonLd = BuildJsonLd(u, images),
                ImagesMeta = images.Select(img => new ImageDto { Url = img, Width = 1200, Height = 800 }).ToList(),
                Code = u.Code,
                Bedrooms = u.Bedrooms,
                Bathrooms = u.Bathrooms,
                Floor = u.Floor,
                IsFurnished = u.IsFurnished,
                View = u.View.ToString(),
                UnitNumber = u.UnitNumber,
                BuildingNumber = u.BuildingNumber,
                DeliveryDate = u.DeliveryDate,
                FinishingType = u.FinishingType?.ToString(),
                HasBalcony = u.HasBalcony,
                HasParking = u.HasParking,
                AdminImages = u.Images?.Select(i => new ImageInfoDto
                {
                    Id = i.Id,
                    Url = i.Url,
                    PublicId = i.PublicId
                }).ToList() ?? new List<ImageInfoDto>(),
                Variants = u.Variants != null
                    ? u.Variants.Where(v => !v.IsDeleted && v.IsActive).OrderBy(v => v.SortOrder).Select(v => new UnitVariantDto
                    {
                        Id = v.Id,
                        PublicKey = v.PublicKey,
                        Name = v.Name,
                        NameAr = v.NameAr,
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
                        IsFeatured = v.IsFeatured,
                        IsRecommended = v.IsRecommended,
                        ViewCount = v.ViewCount,
                        InquiryCount = v.InquiryCount,
                        FavoriteCount = v.FavoriteCount,
                        DeliveryText = v.DeliveryText,
                            DeliveryTextAr = v.DeliveryTextAr,
                        SortOrder = v.SortOrder,
                        IsActive = v.IsActive
                    }).ToList()
                    : new List<UnitVariantDto>()
            };

            ApplySeoEnhancements(dto, u);

            if (contact != null)
            {
                dto.ContactInfo = new ContactDto
                {
                    Name = contact.Name,
                    Phone = contact.Phone,
                    Type = contact.Type
                };
            }
            else if (u.Contact != null)
            {
                dto.ContactInfo = new ContactDto
                {
                    Name = u.Contact.Name,
                    Phone = u.Contact.Phone,
                    Type = u.Contact.Type
                };
            }

            return dto;
        }

        private void ApplySeoEnhancements(UnitDetailsDto dto, Domain.Entities.Unit u)
        {
            try
            {
                var seoContent = _seoContentGenerator.Generate(
                    SeoEntityType.Unit,
                    u.TitleEn, u.TitleAr,
                    u.DescriptionEn, u.DescriptionAr,
                    u.Location, u.PropertyType.ToString(), u.ListingType.ToString(),
                    u.MinPrice ?? 0, u.Currency ?? "EGP",
                    u.Features);

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

                if (!string.IsNullOrWhiteSpace(u.CanonicalUrl))
                {
                    var breadcrumbItems = new List<(string, string)>
                    {
                        ("Home", "/"),
                        ("Units", "/units"),
                        (u.TitleEn ?? u.Slug ?? "Details", u.CanonicalUrl)
                    };
                    dto.BreadcrumbJsonLd = _jsonLdService.BuildBreadcrumbJsonLd(breadcrumbItems);
                }

                var internalLinks = _internalLinkingService.GenerateLinks(
                    u.Location, u.PropertyType.ToString(), u.ListingType.ToString(), u.Slug, null);

                if (!_internalLinkingService.MeetsMinimumRequirement(internalLinks))
                {
                    var missing = _internalLinkingService.GetMissingLinks(
                        u.Location, u.PropertyType.ToString(), u.ListingType.ToString(), u.Slug);
                    if (missing.Count > 0)
                        internalLinks.AddRange(missing);
                }

                dto.InternalLinksJson = InternalLinkingService.ToJson(internalLinks);

                var entityNode = _entityGraphService.BuildEntityNode(
                    "unit", u.Slug ?? u.Id.ToString(), u.TitleEn ?? "", u.DescriptionEn);
                var entityGraph = _entityGraphService.BuildKnowledgeGraph(
                    "unit", u.Slug ?? u.Id.ToString());
                if (!string.IsNullOrWhiteSpace(entityGraph.JsonLd))
                    dto.EntityGraphJson = entityGraph.JsonLd;
            }
            catch (System.Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to apply SEO enhancements for unit {Id}", u.Id);
            }
        }

        private string BuildJsonLd(Domain.Entities.Unit u, List<string> images)
        {
            try
            {
                var displayPrice = u.ListingType == Domain.Enums.PropertyListingType.Rental && u.RentPerMonth.HasValue
                    ? u.RentPerMonth.Value
                    : (u.MinPrice ?? 0);

                return _jsonLdService.BuildPropertyJsonLd(
                    u.TitleEn,
                    u.DescriptionEn,
                    u.SeoDescription,
                    u.CanonicalUrl,
                    u.Code ?? u.Id.ToString(),
                    u.Location,
                    u.Currency ?? "EGP",
                    u.ListingType.ToString(),
                    displayPrice,
                    u.RentPerMonth,
                    images,
                    u.Id.ToString());
            }
            catch (System.Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to build JSON-LD for unit {UnitId}", u.Id);
                return string.Empty;
            }
        }

        private static string? TryGetString(object value)
        {
            try { return (string?)value; }
            catch { return null; }
        }
    }
}
