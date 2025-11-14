using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IVideoService
    {
        Task<List<VideoResultDto>> AddPropertyVideosAsync(int propertyId, List<(string Url, string? PublicId, string? ThumbnailUrl, string? FileName)> files);
        Task<bool> RemovePropertyVideoAsync(int propertyId, int videoId);
        Task<List<VideoResultDto>> AddProjectVideosAsync(int projectId, List<(string Url, string? PublicId, string? ThumbnailUrl, string? FileName)> files);
        Task<bool> RemoveProjectVideoAsync(int projectId, int videoId);
        Task<List<VideoResultDto>> AddUnitVideosAsync(int unitId, List<(string Url, string? PublicId, string? ThumbnailUrl, string? FileName)> files);
        Task<bool> RemoveUnitVideoAsync(int unitId, int videoId);
    }
}
