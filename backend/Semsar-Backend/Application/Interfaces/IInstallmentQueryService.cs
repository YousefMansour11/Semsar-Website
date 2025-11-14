using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IInstallmentQueryService
    {
        Task<List<InstallmentDto>> GetPublicByPropertyIdAsync(int propertyId);
        Task<Dictionary<int, List<InstallmentDto>>> GetPublicByPropertyIdsAsync(List<int> propertyIds);
        Task<List<InstallmentDto>> GetAdminByPropertyIdAsync(int propertyId);
        Task<Dictionary<int, List<InstallmentDto>>> GetAdminByPropertyIdsAsync(List<int> propertyIds);
    }
}
