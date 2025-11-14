using Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IPropertyQueryService
    {
        Task<(List<PropertyPublicDto> Data, int Total, int Page, int PageSize, int TotalPages)> GetPublicAsync(
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
            CancellationToken ct = default);

        Task<PropertyPublicDto?> GetPublicByIdAsync(int id, CancellationToken ct = default);
        Task<PropertyPublicDto?> GetPublicByPublicKeyAsync(string publicKey, CancellationToken ct = default);
        Task<PropertyAdminDto?> GetAdminByIdAsync(int id, CancellationToken ct = default);
        Task<PropertyAdminDto?> GetAdminByCodeAsync(string code, CancellationToken ct = default);
        Task<PropertyPublicDto?> GetPublicBySlugAsync(string slug, CancellationToken ct = default);
        Task<List<PropertyPublicDto>> GetRelatedAsync(int id, int limit = 5, CancellationToken ct = default);
        Task<(List<PropertyPublicDto> Data, int Total, int Page, int PageSize, int TotalPages, string SeoTitle, string SeoDescription)> GetByLocationAsync(string location, int page, int pageSize, CancellationToken ct = default);
        Task<List<PropertyPublicDto>> SearchAsync(string q, int limit = 20, CancellationToken ct = default);
        Task<List<PropertyPublicDto>> GetLatestPropertiesAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
        Task<List<PropertyCardDto>> GetLatestCardsAsync(int page = 1, int pageSize = 20, string? listingType = null, CancellationToken ct = default);
    }
}
