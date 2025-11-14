using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IUnitService
    {
        Task<Unit> CreateAsync(CreateUnitDto dto);
        Task<Unit> UpdateAsync(int id, CreatePropertyDto dto);
        Task<Unit> PatchAsync(int id, PatchUnitDto dto);
        Task<bool> DeleteAsync(int id);
        Task<Unit?> GetByIdAsync(int id);
    }
}
