using Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ILocationService
    {
        Task<List<LocationTreeNodeDto>> GetTreeAsync(CancellationToken ct = default);
        Task<List<LocationSearchResultDto>> SearchAsync(string query, int maxResults = 15, CancellationToken ct = default);
        Task<List<int>> GetDescendantIdsAsync(int locationId, CancellationToken ct = default);
        Task<LocationResolutionResult?> ResolveLocationAsync(int? governorateId, int? cityId, int? areaId, CancellationToken ct = default);
        Task<LocationResolutionResult?> ResolveOrCreateFromStringAsync(string locationString, CancellationToken ct = default);
    }
}
