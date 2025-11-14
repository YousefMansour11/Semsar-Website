using Domain.Entities;
using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ILandRequestService
    {
        Task<LandRequest> CreateAsync(LandRequestDto dto);
        Task<IEnumerable<LandRequest>> GetAllAsync();
    }
}