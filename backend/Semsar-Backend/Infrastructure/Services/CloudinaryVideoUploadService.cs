using Application.Interfaces;
using Application.DTOs;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class CloudinaryVideoUploadService : IVideoUploadService
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mov", ".webm"
        };

        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "video/mp4", "video/quicktime", "video/webm"
        };

        private static readonly long MaxFileSize = 150 * 1024 * 1024;

        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryVideoUploadService> _logger;

        public CloudinaryVideoUploadService(
            IOptions<CloudinarySettings> options,
            ILogger<CloudinaryVideoUploadService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var settings = options?.Value ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(settings.CloudName) ||
                string.IsNullOrWhiteSpace(settings.ApiKey) ||
                string.IsNullOrWhiteSpace(settings.ApiSecret))
            {
                throw new InvalidOperationException("Cloudinary configuration is incomplete.");
            }

            _cloudinary = new Cloudinary(new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret));
        }

        public async Task<CloudinaryUploadResult> UploadVideoAsync(Stream fileStream, string fileName, string folder = "videos")
        {
            ValidateFile(fileName, fileStream.Length);

            if (!ValidateMagicBytes(fileStream))
                throw new VideoUploadException("File content validation failed", fileName: fileName);

            fileStream.Position = 0;

            // Compute SHA256 hash for dedup
            var fileHash = ComputeSha256(fileStream);
            var publicId = $"semsar/library/{fileHash}";

            fileStream.Position = 0;

            try
            {
                var uploadParams = new VideoUploadParams
                {
                    File = new FileDescription(fileName, fileStream),
                    PublicId = publicId,
                    Overwrite = false,
                    Invalidate = true,
                    EagerAsync = false,
                    EagerTransforms = new List<Transformation>
                    {
                        new Transformation().Width(1280).Height(720).Crop("limit").Quality("auto:good"),
                    }
                };

                var result = await _cloudinary.UploadAsync(uploadParams);

                if (result.Error != null)
                    throw new VideoUploadException($"Cloudinary video upload error: {result.Error.Message}", publicId: publicId, fileName: fileName);

                if (result.SecureUrl == null)
                    throw new VideoUploadException("Cloudinary returned no SecureUrl", publicId: result.PublicId, fileName: fileName);

                var thumbnailUrl = result.SecureUrl.AbsoluteUri.Replace("/upload/", "/upload/so_2.0,q_auto:good,w_640,f_jpg/");

                return new CloudinaryUploadResult
                {
                    Success = true,
                    Url = result.SecureUrl.AbsoluteUri,
                    ThumbnailUrl = thumbnailUrl,
                    PublicId = result.PublicId,
                    FileHash = fileHash,
                };
            }
            catch (VideoUploadException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Video upload failed: {FileName}", fileName);
                throw new VideoUploadException("Video upload failed", fileName: fileName, inner: ex);
            }
        }

        public async Task<bool> DeleteVideoAsync(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId)) return false;
            try
            {
                var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Video
                });
                return result.Result == "ok" || result.Result == "not found";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Video delete failed: {PublicId}", publicId);
                return false;
            }
        }

        public async Task<bool> VideoExistsByHashAsync(string fileHash)
        {
            if (string.IsNullOrWhiteSpace(fileHash)) return false;
            try
            {
                var publicId = $"semsar/library/{fileHash}";
                var result = await _cloudinary.GetResourceAsync(new GetResourceParams(publicId)
                {
                    ResourceType = ResourceType.Video
                });
                return result != null && result.StatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "VideoExistsByHashAsync check failed for hash {Hash}", fileHash);
                return false;
            }
        }

        private static string ComputeSha256(Stream stream)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static void ValidateFile(string fileName, long length)
        {
            if (length == 0)
                throw new VideoUploadException("File is empty", fileName: fileName);

            if (length > MaxFileSize)
                throw new VideoUploadException($"File size ({length:N0} bytes) exceeds max allowed size of {MaxFileSize:N0} bytes", fileName: fileName);

            var ext = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
                throw new VideoUploadException($"Extension '{ext}' not allowed. Allowed: mp4, mov, webm", fileName: fileName);
        }

        private static bool ValidateMagicBytes(Stream stream)
        {
            try
            {
                var buf = new byte[12];
                int read = stream.Read(buf, 0, buf.Length);
                if (read < 4) return false;

                if (buf[4] == (byte)'f' && buf[5] == (byte)'t' && buf[6] == (byte)'y' && buf[7] == (byte)'p')
                    return true;

                if (buf[0] == 0x1A && buf[1] == 0x45 && buf[2] == 0xDF && buf[3] == 0xA3)
                    return true;

                return false;
            }
            catch { return false; }
        }
    }
}
