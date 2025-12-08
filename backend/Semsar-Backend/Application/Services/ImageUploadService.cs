using Application.Interfaces;
using Application.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Application.Services
{
    public class ImageUploadService : IImageUploadService
    {
        private readonly ICloudinaryService _cloudinary;
        private readonly ILogger<ImageUploadService> _logger = null!;

        public ImageUploadService(ICloudinaryService cloudinary, ILogger<ImageUploadService> logger)
        {
            _cloudinary = cloudinary ?? throw new ArgumentNullException(nameof(cloudinary));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<CloudinaryUploadResult>> UploadImagesAsync(IEnumerable<(Stream FileStream, string FileName)> files, string folder = "properties")
        {
            if (files == null) throw new ArgumentNullException(nameof(files));

            var batchId = Guid.NewGuid().ToString();
            var correlationId = GetCorrelationId();
            _logger.LogInformation("Upload batch started. BatchId={BatchId} CorrelationId={CorrelationId} Files={Count}", batchId, correlationId, files.Count());

            var results = new List<CloudinaryUploadResult>();
            foreach (var (stream, fileName) in files)
            {
                var actualFileName = fileName ?? "<unknown>";

                if (stream == null)
                {
                    _logger.LogWarning("Skipping file with null stream. BatchId={BatchId} CorrelationId={CorrelationId} File={File}", batchId, correlationId, actualFileName);
                    results.Add(new CloudinaryUploadResult { Success = false, ErrorMessage = "File stream was null" });
                    continue;
                }

                CloudinaryUploadResult lastResult;
                try
                {
                    _logger.LogInformation("Uploading file. BatchId={BatchId} CorrelationId={CorrelationId} File={File}", batchId, correlationId, actualFileName);
                    var res = await _cloudinary.UploadImageAsync(stream, actualFileName, folder);
                    lastResult = res ?? new CloudinaryUploadResult { Success = false, ErrorMessage = "Null result" };
                    if (lastResult.Success)
                    {
                        _logger.LogInformation("Upload success. BatchId={BatchId} CorrelationId={CorrelationId} File={File} Url={Url} PublicId={PublicId}", batchId, correlationId, actualFileName, lastResult.Url, lastResult.PublicId);
                    }
                    else
                    {
                        _logger.LogWarning("Upload failed. BatchId={BatchId} CorrelationId={CorrelationId} File={File} Error={Error}", batchId, correlationId, actualFileName, lastResult.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Upload exception. BatchId={BatchId} CorrelationId={CorrelationId} File={File}", batchId, correlationId, actualFileName);
                    lastResult = new CloudinaryUploadResult { Success = false, ErrorMessage = ex.Message };
                }

                results.Add(lastResult);
            }

            _logger.LogInformation("Upload batch finished. BatchId={BatchId} CorrelationId={CorrelationId} SuccessCount={SuccessCount} Total={Total}", batchId, correlationId, results.Count(r => r.Success), results.Count);
            return results;
        }

        public Task<bool> DeleteImageAsync(string publicId)
        {
            var correlationId = GetCorrelationId();
            _logger.LogInformation("Delete image requested. CorrelationId={CorrelationId} PublicId={PublicId}", correlationId, publicId);
            return _cloudinary.DeleteImageAsync(publicId);
        }

        private static string GetCorrelationId()
        {
            try
            {
                var ctx = System.Threading.Tasks.Task.CurrentId; // placeholder if HttpContext isn't accessible
                return System.Diagnostics.Activity.Current?.Id ?? System.Guid.NewGuid().ToString();
            }
            catch (Exception) { return Guid.NewGuid().ToString(); } // fallback for rare framework edge cases
        }
    }
}
