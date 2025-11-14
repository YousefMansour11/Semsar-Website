using Application.DTOs;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface ILeadService
    {
        Task<Lead> CreateLeadAsync(LeadDto dto);
        Task<IEnumerable<Lead>> GetAllAsync();
        Task<Lead?> GetByIdAsync(int id);
        Task UpdateStatusAsync(int id, string status);
    }
}