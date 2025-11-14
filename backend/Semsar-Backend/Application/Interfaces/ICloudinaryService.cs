using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ICloudinaryService
    {
        Task<Application.DTOs.CloudinaryUploadResult> UploadImageAsync(Stream fileStream, string fileName, string folder = "properties");
        Task<List<Application.DTOs.CloudinaryUploadResult>> UploadImagesAsync(List<(Stream Stream, string FileName)> files, string folder = "properties");
        Task<bool> DeleteImageAsync(string publicId);
        string GetOptimizedUrl(string url);
    }
}
