using Application.DTOs;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IImageService
    {
        Task<UploadResult> UploadAsync(IFormFile file, string folder);
        Task<bool> DeleteAsync(string publicId);
    }
}
