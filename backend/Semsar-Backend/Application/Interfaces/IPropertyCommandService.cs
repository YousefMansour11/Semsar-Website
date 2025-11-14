using Application.DTOs;
using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IPropertyCommandService
    {
        Task<Property> CreateAsync(CreatePropertyDto dto);
        Task<Property> UpdateAsync(int id, CreatePropertyDto dto);
        Task<bool> DeleteAsync(int id);
        Task<List<(int Id, string Url, string? PublicId)>> AddImagesAsync(int propertyId, List<(string Url, string? PublicId)> files);
        Task<Property?> GetByIdAsync(int id);
    }
}
