using Application.DTOs;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IVideoUploadService
    {
        Task<CloudinaryUploadResult> UploadVideoAsync(Stream fileStream, string fileName, string folder = "videos");
        Task<bool> DeleteVideoAsync(string publicId);
        Task<bool> VideoExistsByHashAsync(string fileHash);
    }
}
