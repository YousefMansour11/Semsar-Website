using Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IProjectQueryService
    {
        Task<List<ProjectCardDto>> GetPublicCardsAsync(CancellationToken ct = default);
        Task<ProjectDetailsDto?> GetBySlugOrIdAsync(string slugOrId, CancellationToken ct = default);
        Task<ProjectDetailsDto?> GetByPublicKeyAsync(string publicKey, CancellationToken ct = default);
    }
}
