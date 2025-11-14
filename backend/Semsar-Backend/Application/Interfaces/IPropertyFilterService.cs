using Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IPropertyFilterService
    {
        Task<PropertyFilterResponseDto> FilterPropertiesAsync(
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
            CancellationToken ct = default);
    }
}
