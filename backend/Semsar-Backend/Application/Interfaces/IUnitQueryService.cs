using Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IUnitQueryService
    {
        Task<List<UnitPublicDto>> GetPublicCardsAsync(int? projectId, CancellationToken ct = default);
        Task<(List<UnitPublicDto> Data, int Total)> GetPublicCardsPagedAsync(int? projectId, int page, int pageSize, CancellationToken ct = default);
        Task<UnitPublicDto?> GetBySlugAsync(string slug, CancellationToken ct = default);
        Task<UnitPublicDto?> GetPublicByIdAsync(int id, CancellationToken ct = default);
        Task<UnitPublicDto?> GetPublicByPublicKeyAsync(string publicKey, CancellationToken ct = default);
        Task<UnitDetailsDto?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<Domain.Entities.Unit?> GetByCodeAsync(string code, CancellationToken ct = default);
    }
}
