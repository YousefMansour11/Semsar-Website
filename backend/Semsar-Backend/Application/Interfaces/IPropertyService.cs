using Application.DTOs;
using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IPropertyService
    {
        Task<Property> CreateAsync(CreatePropertyDto dto);
        Task<Property> UpdateAsync(int id, CreatePropertyDto dto);
        Task<Property> PatchAsync(int id, PatchPropertyDto dto);
        Task<List<(int Id, string Url, string? PublicId)>> AddImagesAsync(int propertyId, List<(string Url, string? PublicId)> files);
        Task<bool> RemoveImageAsync(int propertyId, int imageId);
        Task ReplaceImageAsync(int propertyId, int imageId, string newUrl, string? newPublicId);
        Task<Property?> GetByIdAsync(int id);
        Task<bool> DeleteAsync(int id);

        Task<(List<Application.DTOs.PropertyPublicDto> Data, int Total, int Page, int PageSize, int TotalPages)> GetPublicAsync(
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
            string sortOrder);

        Task<Application.DTOs.PropertyPublicDto?> GetPublicByIdAsync(int id);

        Task<Application.DTOs.PropertyAdminDto?> GetAdminByIdAsync(int id);
        Task<Application.DTOs.PropertyAdminDto?> GetAdminByCodeAsync(string code);
        Task IncrementViewCountAsync(int id);
    }
}