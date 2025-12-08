using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{
    public class PropertyFilterService : IPropertyFilterService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILocationService _locationService;
        private readonly ILogger<PropertyFilterService> _logger;

        public PropertyFilterService(IUnitOfWork uow, ILocationService locationService, ILogger<PropertyFilterService> logger)
        {
            _uow = uow;
            _locationService = locationService;
            _logger = logger;
        }

        public async Task<PropertyFilterResponseDto> FilterPropertiesAsync(
            int? locationId,
            bool includeChildren,
            int[]? locationIds,
            bool? isFurnished,
            bool? hasInstallment,
            decimal? minPrice,
            decimal? maxPrice,
            double? minSize,
            double? maxSize,
            int? bedrooms,
            int? bathrooms,
            string? propertyType,
            string? listingType,
            string? features,
            int? projectId,
            string? keyword,
            string sortBy,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            page = Math.Max(1, page);

            var query = _uow.Properties.Query().AsNoTracking().Where(p => !p.IsDeleted);

            // Multi-area filter with hierarchical expansion (takes precedence over single locationId)
            if (locationIds is { Length: > 0 })
            {
                var expandedIds = new HashSet<int>(locationIds);
                foreach (var locId in locationIds)
                {
                    var descendants = await _locationService.GetDescendantIdsAsync(locId, ct);
                    foreach (var d in descendants)
                        expandedIds.Add(d);
                }
                query = query.Where(p => p.LocationId.HasValue && expandedIds.Contains(p.LocationId.Value));
            }
            else if (locationId.HasValue)
            {
                if (includeChildren)
                {
                    var descendantIds = await _locationService.GetDescendantIdsAsync(locationId.Value, ct);
                    descendantIds.Add(locationId.Value);
                    query = query.Where(p => p.LocationId.HasValue && descendantIds.Contains(p.LocationId.Value));
                }
                else
                {
                    query = query.Where(p => p.LocationId == locationId.Value);
                }
            }

            if (isFurnished.HasValue)
                query = query.Where(p => p.IsFurnished == isFurnished.Value);

            if (hasInstallment.HasValue)
            {
                if (hasInstallment.Value)
                    query = query.Where(p => p.Installments != null && p.Installments.Any(i => !i.IsDeleted));
                else
                    query = query.Where(p => p.Installments == null || !p.Installments.Any(i => !i.IsDeleted));
            }

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            if (minSize.HasValue)
                query = query.Where(p => p.Size >= minSize.Value);
            if (maxSize.HasValue)
                query = query.Where(p => p.Size <= maxSize.Value);

            if (bedrooms.HasValue)
                query = query.Where(p => p.Bedrooms >= bedrooms.Value);
            if (bathrooms.HasValue)
                query = query.Where(p => p.Bathrooms >= bathrooms.Value);

            if (!string.IsNullOrWhiteSpace(propertyType) && Enum.TryParse<PropertyType>(propertyType, true, out var parsedType))
                query = query.Where(p => p.PropertyType == parsedType);

            if (!string.IsNullOrWhiteSpace(listingType) && Enum.TryParse<PropertyListingType>(listingType, true, out var parsedListing))
                query = query.Where(p => p.ListingType == parsedListing);

            if (projectId.HasValue)
                _logger.LogWarning("projectId filter ({ProjectId}) is not applicable to Properties — Properties have no ProjectId relationship. Ignoring.", projectId.Value);

            if (!string.IsNullOrWhiteSpace(features))
            {
                var featureKeys = features.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (featureKeys.Length > 0)
                {
                    var featureIds = await _uow.Features.Query()
                        .Where(f => featureKeys.Contains(f.Key) || (f.NameEn != null && featureKeys.Contains(f.NameEn)) || (f.NameAr != null && featureKeys.Contains(f.NameAr)))
                        .Select(f => f.Id)
                        .ToListAsync(ct);

                    foreach (var fid in featureIds)
                    {
                        var id = fid;
                        query = query.Where(p => p.PropertyFeatures.Any(pf => pf.FeatureId == id));
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.ToLower().Trim();
                query = query.Where(p =>
                    p.TitleEn.ToLower().Contains(kw) ||
                    p.TitleAr.ToLower().Contains(kw) ||
                    p.DescriptionEn.ToLower().Contains(kw) ||
                    p.DescriptionAr.ToLower().Contains(kw));
            }

            var totalCount = await query.CountAsync(ct);

            query = sortBy.ToLower() switch
            {
                "price_asc" => query.OrderBy(p => p.Price).ThenBy(p => p.SortOrder),
                "price_desc" => query.OrderByDescending(p => p.Price).ThenBy(p => p.SortOrder),
                "size_asc" => query.OrderBy(p => p.Size).ThenBy(p => p.SortOrder),
                "size_desc" => query.OrderByDescending(p => p.Size).ThenBy(p => p.SortOrder),
                _ => query.OrderBy(p => p.SortOrder).ThenByDescending(p => p.CreatedAt)
            };

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.Images)
                .Include(p => p.PropertyFeatures)
                    .ThenInclude(pf => pf.Feature)
                .Include(p => p.Installments)
                .AsSplitQuery()
                .Select(p => new PropertyFilterResultDto
                {
                    Id = p.Id,
                    PublicKey = p.PublicKey,
                    Slug = p.Slug,
                    TitleEn = p.TitleEn,
                    TitleAr = p.TitleAr,
                    DescriptionEn = p.DescriptionEn,
                    DescriptionAr = p.DescriptionAr,
                    Price = p.Price,
                    RentPerMonth = p.RentPerMonth,
                    Currency = p.Currency,
                    PropertyType = p.PropertyType.ToString(),
                    ListingType = p.ListingType.ToString(),
                    Location = p.Location,
                    LocationAr = p.LocationAr,
                    Size = p.Size,
                    Bedrooms = p.Bedrooms,
                    Bathrooms = p.Bathrooms,
                    IsFeatured = p.IsFeatured,
                    IsFurnished = p.IsFurnished,
                    HasInstallment = p.Installments != null && p.Installments.Any(i => !i.IsDeleted),
                    Image = p.Images != null && p.Images.Any() ? p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault() : null,
                    Images = p.Images != null ? p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList() : new List<string>(),
                    Features = p.PropertyFeatures.Select(pf => pf.Feature.Key).ToList(),
                    FeaturesAr = p.FeaturesAr ?? new List<string>(),
                    Code = p.Code,
                    SortOrder = p.SortOrder,
                    CreatedAt = p.CreatedAt,
                    Installments = p.Installments!.Where(i => !i.IsDeleted).Select(i => new InstallmentDto
                    {
                        DownPaymentPercent = i.DownPaymentPercent,
                        Years = i.Years,
                        IsEnabled = i.IsEnabled,
                        IsDeleted = i.IsDeleted,
                        PaymentType = i.PaymentType.ToString()
                    }).ToList()
                })
                .ToListAsync(ct);

            return new PropertyFilterResponseDto
            {
                Data = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }

    }
}
