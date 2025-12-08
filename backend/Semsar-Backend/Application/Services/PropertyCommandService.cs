using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services
{
    // Facade over IPropertyService providing a command-oriented boundary.
    // Enables future cross-cutting concerns (audit, validation, events)
    // without modifying IPropertyService or its callers.
    public class PropertyCommandService : IPropertyCommandService
    {
        private readonly IPropertyService _propService;

        public PropertyCommandService(IPropertyService propService)
        {
            _propService = propService;
        }

        public Task<Property> CreateAsync(CreatePropertyDto dto) => _propService.CreateAsync(dto);
        public Task<Property> UpdateAsync(int id, CreatePropertyDto dto) => _propService.UpdateAsync(id, dto);
        public Task<bool> DeleteAsync(int id) => _propService.DeleteAsync(id);
        public Task<List<(int Id, string Url, string? PublicId)>> AddImagesAsync(int propertyId, List<(string Url, string? PublicId)> files) => _propService.AddImagesAsync(propertyId, files);
        public Task<Property?> GetByIdAsync(int id) => _propService.GetByIdAsync(id);
    }
}
