using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using AppUploadResult = Application.DTOs.UploadResult;
using Application.Interfaces;
using Application.Services;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System.Diagnostics;

namespace Infrastructure.Services
{
    // Direct upload service implementing IImageService.
    // Used by UploadController for immediate, single-file admin uploads.
    // For application-layer uploads with retry/correlation, see ResilientCloudinaryService (ICloudinaryService).
    public class CloudinaryService : IImageService
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp"
        };

        private static readonly long MaxFileSize = 10 * 1024 * 1024; // 10 MB (increased from 5 for high-res images)
        private const int MinWidth = 400;
        private const int MinHeight = 300;

        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CloudinaryService(
            IOptions<CloudinarySettings> options,
            ILogger<CloudinaryService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

            var settings = options?.Value ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(settings.CloudName) ||
                string.IsNullOrWhiteSpace(settings.ApiKey) ||
                string.IsNullOrWhiteSpace(settings.ApiSecret))
            {
                throw new InvalidOperationException(
                    "Cloudinary configuration is incomplete. CloudName, ApiKey, and ApiSecret must all be configured.");
            }

            _cloudinary = new Cloudinary(new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret));

            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        var cid = GetCorrelationId();
                        _logger.LogWarning(
                            exception,
                            "Cloudinary upload attempt {RetryCount}/3 failed. Retrying in {DelaySeconds}s. CorrelationId={CorrelationId}",
                            retryCount, timeSpan.TotalSeconds, cid);
                    });
        }

        public async Task<AppUploadResult> UploadAsync(IFormFile file, string folder)
        {
            var correlationId = GetCorrelationId();

            if (file == null)
                throw new ImageUploadException("File cannot be null", fileName: null);

            ValidateFile(file);

            var extension = Path.GetExtension(file.FileName);
            var publicId = $"semsar/{folder.Trim('/')}/{Guid.NewGuid()}";

            _logger.LogInformation(
                "Upload started. CorrelationId={CorrelationId} File={FileName} Size={Size} Folder={Folder}",
                correlationId, file.FileName, file.Length, folder);

            await using var stream = file.OpenReadStream();

            if (!ValidateMagicBytes(stream))
                throw new ImageUploadException("File content does not match its declared MIME type", fileName: file.FileName);

            stream.Position = 0;

            var (width, height) = ImageHeaderParser.GetDimensions(stream, extension);
            var warnings = ValidateImageQuality(file, width, height);

            stream.Position = 0;

            try
            {
                var result = await _retryPolicy.ExecuteAsync(() => UploadToCloudinaryAsync(stream, publicId, file.FileName));

                result.Width = width;
                result.Height = height;
                result.Warnings = warnings;

                _logger.LogInformation(
                    "Upload succeeded. CorrelationId={CorrelationId} File={FileName} Url={Url} PublicId={PublicId} Dimensions={Width}x{Height}",
                    correlationId, file.FileName, result.Url, result.PublicId, width, height);

                return result;
            }
            catch (ImageUploadException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Upload failed. CorrelationId={CorrelationId} File={FileName} Folder={Folder}",
                    correlationId, file.FileName, folder);

                throw new ImageUploadException(
                    "Image upload failed after all retry attempts", fileName: file.FileName, inner: ex);
            }
        }

        public async Task<bool> DeleteAsync(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                return false;

            var correlationId = GetCorrelationId();

            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));
                    var success = result.Result == "ok" || result.Result == "not found";

                    if (!success)
                    {
                        _logger.LogWarning(
                            "Delete returned unexpected status. CorrelationId={CorrelationId} PublicId={PublicId} Status={Status}",
                            correlationId, publicId, result.Result);
                    }

                    return success;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Delete failed after retries. CorrelationId={CorrelationId} PublicId={PublicId}",
                    correlationId, publicId);
                return false;
            }
        }

        private void ValidateFile(IFormFile file)
        {
            if (file.Length == 0)
                throw new ImageUploadException("File is empty", fileName: file.FileName);

            if (file.Length > MaxFileSize)
                throw new ImageUploadException(
                    $"File size ({file.Length:N0} bytes) exceeds the maximum allowed size of {MaxFileSize:N0} bytes",
                    fileName: file.FileName);

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
                throw new ImageUploadException(
                    $"File extension '{extension}' is not allowed. Allowed: {string.Join(", ", AllowedExtensions)}",
                    fileName: file.FileName);

            var mimeType = file.ContentType ?? string.Empty;
            if (string.IsNullOrEmpty(mimeType) || !AllowedMimeTypes.Contains(mimeType))
            {
                if (extension is ".jpeg" or ".jpg")
                {
                    if (!mimeType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
                        throw new ImageUploadException(
                            $"MIME type '{mimeType}' does not match the expected type for '{extension}' files",
                            fileName: file.FileName);
                }
                else if (extension == ".png")
                {
                    if (!mimeType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
                        throw new ImageUploadException(
                            $"MIME type '{mimeType}' does not match the expected type for '{extension}' files",
                            fileName: file.FileName);
                }
                else if (extension == ".webp")
                {
                    if (!mimeType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
                        throw new ImageUploadException(
                            $"MIME type '{mimeType}' does not match the expected type for '{extension}' files",
                            fileName: file.FileName);
                }
            }
        }

        private static bool ValidateMagicBytes(Stream stream)
        {
            try
            {
                var buffer = new byte[12];
                var read = stream.Read(buffer, 0, buffer.Length);
                if (read < 3) return false;

                if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF) return true;
                if (read >= 8 && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47) return true;
                if (read >= 12 && buffer[0] == (byte)'R' && buffer[1] == (byte)'I' && buffer[2] == (byte)'F' && buffer[3] == (byte)'F' && buffer[8] == (byte)'W' && buffer[9] == (byte)'E' && buffer[10] == (byte)'B' && buffer[11] == (byte)'P') return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        private List<string> ValidateImageQuality(IFormFile file, int width, int height)
        {
            var warnings = new List<string>();

            if (width == 0 || height == 0)
            {
                _logger.LogWarning("Could not detect dimensions for {FileName}", file.FileName);
                return warnings;
            }

            if (width < MinWidth || height < MinHeight)
            {
                warnings.Add($"Low resolution detected: {width}×{height}. Minimum recommended: {MinWidth}×{MinHeight}px. This image may appear blurry on modern devices.");
            }

            var mp = ImageHeaderParser.EstimateMegapixels(width, height);
            if (mp > 0 && mp < 1.5)
            {
                warnings.Add($"Image is only {mp}MP. For premium real-estate display, 2MP+ images are recommended.");
            }

            var ratio = ImageHeaderParser.EstimateCompressionRatio(file.Length, width, height);
            if (ratio > 0 && ratio < 0.15)
            {
                warnings.Add("Heavy compression artifacts detected. The image may appear blurry on large screens.");
            }
            else if (ratio > 4)
            {
                warnings.Add("Unusually large file size for resolution. Consider optimizing before upload.");
            }

            return warnings;
        }

        private async Task<AppUploadResult> UploadToCloudinaryAsync(Stream stream, string publicId, string fileName)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream),
                PublicId = publicId,
                Overwrite = false,
                Invalidate = true
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new ImageUploadException(
                    $"Cloudinary upload error: {result.Error.Message}",
                    publicId: publicId,
                    fileName: fileName);

            if (result.SecureUrl == null)
                throw new ImageUploadException(
                    "Cloudinary returned a successful response but no SecureUrl",
                    publicId: result.PublicId,
                    fileName: fileName);

            return new AppUploadResult
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };
        }

        private string GetCorrelationId()
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                if (context?.Items["X-Correlation-Id"] is string cid && !string.IsNullOrWhiteSpace(cid))
                    return cid;
                if (context?.Request.Headers.TryGetValue("X-Correlation-Id", out var header) == true)
                    return header.ToString();
            }
            catch (Exception ex) { _logger?.LogDebug(ex, "Failed to get correlation ID from HTTP context"); }

            return Activity.Current?.Id ?? Guid.NewGuid().ToString();
        }
    }
}
