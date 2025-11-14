using Application.DTOs;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IImageUploadService
    {
        Task<List<CloudinaryUploadResult>> UploadImagesAsync(IEnumerable<(Stream FileStream, string FileName)> files, string folder = "properties");
        Task<bool> DeleteImageAsync(string publicId);
    }
}
