using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Application.Interfaces;
using Application.DTOs;
using Application.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using System.Security.Cryptography;

namespace Infrastructure.Services
{
    public class ResilientCloudinaryService : ICloudinaryService
    {
        private static readonly SemaphoreSlim _uploadSemaphore = new(4, 4);
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private const int MinWidth = 400;
        private const int MinHeight = 300;

        private readonly Cloudinary? _cloudinary;
        private readonly ILogger<ResilientCloudinaryService> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public ResilientCloudinaryService(
            IOptions<CloudinarySettings> options,
            ILogger<ResilientCloudinaryService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var settings = options?.Value ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(settings.CloudName) ||
                string.IsNullOrWhiteSpace(settings.ApiKey) ||
                string.IsNullOrWhiteSpace(settings.ApiSecret))
            {
                _logger.LogWarning("Cloudinary configuration is missing or incomplete. Cloudinary operations will fail at runtime.");
                _cloudinary = null;
            }
            else
            {
                var acc = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
                _cloudinary = new Cloudinary(acc);
            }

            // Only retry transient network/timeout errors — not auth, validation, or API-level errors
            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    retryCount: 2,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(exception,
                            "Cloudinary transient failure (retry {RetryCount}/2). Retrying in {DelaySeconds}s",
                            retryCount, timeSpan.TotalSeconds);
                    });
        }

        public async Task<CloudinaryUploadResult> UploadImageAsync(
            Stream fileStream, string fileName, string folder = "properties")
        {
            if (fileStream == null)
                throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            {
                _logger.LogWarning("Unsupported file extension: {Ext}", ext);
                return new CloudinaryUploadResult { Success = false, ErrorMessage = $"Unsupported file extension '{ext}'" };
            }

            if (_cloudinary == null)
            {
                _logger.LogWarning("Cloudinary not configured — upload skipped");
                return new CloudinaryUploadResult { Success = false, ErrorMessage = "Cloudinary not configured" };
            }

            // Read once into byte[] so each Polly retry gets a fresh, undisposed MemoryStream
            byte[] fileBytes;
            try
            {
                using var readBuffer = new MemoryStream();
                await fileStream.CopyToAsync(readBuffer);
                fileBytes = readBuffer.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to buffer file stream");
                return new CloudinaryUploadResult { Success = false, ErrorMessage = $"Failed to read file: {ex.Message}" };
            }

            // Check image dimensions from headers
            var ext2 = Path.GetExtension(fileName);
            using var dimStream = new MemoryStream(fileBytes, writable: false);
            var (width, height) = ImageHeaderParser.GetDimensions(dimStream, ext2);
            if (width > 0 && height > 0 && (width < MinWidth || height < MinHeight))
            {
                _logger.LogWarning("Image too small: {Width}x{Height} for {FileName}. Minimum: {MinW}x{MinH}", width, height, fileName, MinWidth, MinHeight);
                return new CloudinaryUploadResult
                {
                    Success = false,
                    ErrorMessage = $"Image dimensions ({width}x{height}) are below the minimum requirement of {MinWidth}x{MinHeight}px"
                };
            }

            // Content-hash based public ID — same file content = same public_id
            // Cloudinary's Overwrite=false skips re-upload and returns existing URL
            var hash = SHA256.HashData(fileBytes);
            var hashPrefix = Convert.ToHexStringLower(hash)[..32];
            var publicId = $"{folder.Trim('/')}/{hashPrefix}";

            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    // Fresh stream per retry — safe even if CloudinaryDotNet disposes it
                    using var retryStream = new MemoryStream(fileBytes, writable: false);

                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(fileName, retryStream),
                        PublicId = publicId,
                        Overwrite = false,
                        Invalidate = true
                    };

                    var result = await _cloudinary.UploadAsync(uploadParams);

                    if (result?.Error != null)
                    {
                        // Non-retryable — will propagate through Polly unhandled (not in Handle<> filter)
                        throw new InvalidOperationException($"Cloudinary API error: {result.Error.Message}");
                    }

                    if (result?.SecureUrl == null)
                    {
                        throw new InvalidOperationException("Cloudinary returned no SecureUrl");
                    }

                    return new CloudinaryUploadResult
                    {
                        Success = true,
                        Url = result.SecureUrl.AbsoluteUri,
                        PublicId = result.PublicId
                    };
                });
            }
            catch (Exception ex) when (ex is not HttpRequestException and not TaskCanceledException and not TimeoutException)
            {
                // Non-transient error (auth, API rejection, etc.) — no retry, return immediately
                _logger.LogError(ex, "Cloudinary upload failed (non-transient)");
                return new CloudinaryUploadResult
                {
                    Success = false,
                    ErrorMessage = $"Cloudinary upload failed: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                // Transient error that exhausted all retries
                _logger.LogError(ex, "Cloudinary upload failed after retries");
                return new CloudinaryUploadResult
                {
                    Success = false,
                    ErrorMessage = $"Cloudinary upload failed after retries: {ex.Message}"
                };
            }
        }

        public async Task<List<CloudinaryUploadResult>> UploadImagesAsync(
            List<(Stream Stream, string FileName)> files, string folder = "properties")
        {
            var tasks = files.Select(async file =>
            {
                await _uploadSemaphore.WaitAsync();
                try
                {
                    return await UploadImageAsync(file.Stream, file.FileName, folder);
                }
                finally
                {
                    _uploadSemaphore.Release();
                }
            });
            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                return false;

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                if (_cloudinary == null)
                {
                    _logger.LogWarning("Cloudinary not configured — delete skipped for {PublicId}", publicId);
                    return false;
                }
                try
                {
                    var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));
                    return result != null && (result.Result == "ok" || result.Result == "not found");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete Cloudinary image {PublicId}", publicId);
                    return false;
                }
            });
        }

        public string GetOptimizedUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return url;

            try
            {
                var idx = url.IndexOf("/upload/", StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                    return url;

                var before = url.Substring(0, idx + "/upload/".Length);
                var after = url.Substring(idx + "/upload/".Length);

                // Strip existing transforms/version segments to get clean public path
                var segments = after.Split('/');
                var pathStart = 0;
                for (var i = 0; i < segments.Length; i++)
                {
                    var seg = segments[i];
                    if (seg.Contains(',') || (seg.Length > 1 && seg[0] == 'v' && seg.Skip(1).All(char.IsDigit)))
                    {
                        pathStart = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }

                var publicPath = string.Join("/", segments.Skip(pathStart));

                return before + "f_auto,q_auto/" + publicPath;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to optimize URL: {Url}", url);
                return url;
            }
        }
    }
}
