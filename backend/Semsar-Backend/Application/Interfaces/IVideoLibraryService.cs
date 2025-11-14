using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IVideoLibraryService
    {
        Task<List<VideoLibraryItemDto>> GetLibraryAsync();
        Task<List<VideoLibraryItemDto>> GetLibraryByProjectAsync(int projectId);
        Task<List<VideoResultDto>> AttachLibraryVideoToPropertyAsync(int propertyId, string publicId);
        Task<List<VideoResultDto>> AttachLibraryVideoToUnitAsync(int unitId, string publicId);
        Task<List<VideoResultDto>> AttachLibraryVideoToProjectAsync(int projectId, string publicId);
    }
}