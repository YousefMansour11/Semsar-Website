using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class VideoService : IVideoService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVideoUploadService _videoUpload;
        private readonly ILogger<VideoService>? _logger;

        public VideoService(IUnitOfWork unitOfWork, IVideoUploadService videoUpload, ILogger<VideoService>? logger = null)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _videoUpload = videoUpload ?? throw new ArgumentNullException(nameof(videoUpload));
            _logger = logger;
        }

        public async Task<List<VideoResultDto>> AddPropertyVideosAsync(int propertyId, List<(string Url, string? PublicId, string? ThumbnailUrl, string? FileName)> files)
        {
            var property = await _unitOfWork.Properties.QueryTracked()
                .Include(p => p.Videos)
                .FirstOrDefaultAsync(p => p.Id == propertyId);
            if (property == null) throw new KeyNotFoundException("Property not found");

            property.Videos ??= new List<PropertyVideo>();
            int nextOrder = property.Videos.Count + 1;

            foreach (var (url, publicId, thumbnailUrl, fileName) in files)
            {
                if (string.IsNullOrWhiteSpace(url)) continue;
                var video = new PropertyVideo
                {
                    Url = url,
                    PublicId = publicId,
                    ThumbnailUrl = thumbnailUrl,
                    Title = fileName,
                    SortOrder = nextOrder++,
                    Property = property,
                };
                property.Videos.Add(video);
            }

            property.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Properties.Update(property);
            await _unitOfWork.CommitAsync();
            return property.Videos.Skip(property.Videos.Count - files.Count).Select(v => new VideoResultDto { Id = v.Id, Url = v.Url, PublicId = v.PublicId }).ToList();
        }

        public async Task<bool> RemovePropertyVideoAsync(int propertyId, int videoId)
        {
            var prop = await _unitOfWork.Properties.QueryTracked()
                .Include(p => p.Videos)
                .FirstOrDefaultAsync(p => p.Id == propertyId);
            if (prop?.Videos == null) return false;

            var video = prop.Videos.FirstOrDefault(v => v.Id == videoId);
            if (video == null) return false;

            if (!string.IsNullOrWhiteSpace(video.PublicId))
            {
                // Only delete from Cloudinary if no other entity references same PublicId
                if (!await IsVideoReferencedElsewhereAsync(video.PublicId, propertyId: propertyId))
                {
                    try { await _videoUpload.DeleteVideoAsync(video.PublicId); }
                    catch (Exception ex) { _logger?.LogWarning(ex, "Failed to delete Cloudinary video {PublicId}", video.PublicId); }
                }
            }

            prop.Videos.Remove(video);
            prop.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<List<VideoResultDto>> AddProjectVideosAsync(int projectId, List<(string Url, string? PublicId, string? ThumbnailUrl, string? FileName)> files)
        {
            var project = await _unitOfWork.Projects.QueryTracked()
                .Include(p => p.Videos)
                .FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) throw new KeyNotFoundException("Project not found");

            project.Videos ??= new List<ProjectVideo>();
            int nextOrder = project.Videos.Count + 1;

            foreach (var (url, publicId, thumbnailUrl, fileName) in files)
            {
                if (string.IsNullOrWhiteSpace(url)) continue;
                var video = new ProjectVideo
                {
                    Url = url,
                    PublicId = publicId,
                    ThumbnailUrl = thumbnailUrl,
                    Title = fileName,
                    SortOrder = nextOrder++,
                    Project = project,
                };
                project.Videos.Add(video);
            }

            project.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Projects.Update(project);
            await _unitOfWork.CommitAsync();
            return project.Videos.Skip(project.Videos.Count - files.Count).Select(v => new VideoResultDto { Id = v.Id, Url = v.Url, PublicId = v.PublicId }).ToList();
        }

        public async Task<bool> RemoveProjectVideoAsync(int projectId, int videoId)
        {
            var proj = await _unitOfWork.Projects.QueryTracked()
                .Include(p => p.Videos)
                .FirstOrDefaultAsync(p => p.Id == projectId);
            if (proj?.Videos == null) return false;

            var video = proj.Videos.FirstOrDefault(v => v.Id == videoId);
            if (video == null) return false;

            if (!string.IsNullOrWhiteSpace(video.PublicId))
            {
                if (!await IsVideoReferencedElsewhereAsync(video.PublicId, projectId: projectId))
                {
                    try { await _videoUpload.DeleteVideoAsync(video.PublicId); }
                    catch (Exception ex) { _logger?.LogWarning(ex, "Failed to delete Cloudinary video {PublicId}", video.PublicId); }
                }
            }

            proj.Videos.Remove(video);
            proj.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<List<VideoResultDto>> AddUnitVideosAsync(int unitId, List<(string Url, string? PublicId, string? ThumbnailUrl, string? FileName)> files)
        {
            var unit = await _unitOfWork.Units.QueryTracked()
                .Include(u => u.Videos)
                .FirstOrDefaultAsync(u => u.Id == unitId);
            if (unit == null) throw new KeyNotFoundException("Unit not found");

            unit.Videos ??= new List<UnitVideo>();
            int nextOrder = unit.Videos.Count + 1;

            foreach (var (url, publicId, thumbnailUrl, fileName) in files)
            {
                if (string.IsNullOrWhiteSpace(url)) continue;
                var video = new UnitVideo
                {
                    Url = url,
                    PublicId = publicId,
                    ThumbnailUrl = thumbnailUrl,
                    Title = fileName,
                    SortOrder = nextOrder++,
                    Unit = unit,
                };
                unit.Videos.Add(video);
            }

            unit.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Units.Update(unit);
            await _unitOfWork.CommitAsync();
            return unit.Videos.Skip(unit.Videos.Count - files.Count).Select(v => new VideoResultDto { Id = v.Id, Url = v.Url, PublicId = v.PublicId }).ToList();
        }

        public async Task<bool> RemoveUnitVideoAsync(int unitId, int videoId)
        {
            var unit = await _unitOfWork.Units.QueryTracked()
                .Include(u => u.Videos)
                .FirstOrDefaultAsync(u => u.Id == unitId);
            if (unit?.Videos == null) return false;

            var video = unit.Videos.FirstOrDefault(v => v.Id == videoId);
            if (video == null) return false;

            if (!string.IsNullOrWhiteSpace(video.PublicId))
            {
                if (!await IsVideoReferencedElsewhereAsync(video.PublicId, unitId: unitId))
                {
                    try { await _videoUpload.DeleteVideoAsync(video.PublicId); }
                    catch (Exception ex) { _logger?.LogWarning(ex, "Failed to delete Cloudinary video {PublicId}", video.PublicId); }
                }
            }

            unit.Videos.Remove(video);
            await _unitOfWork.CommitAsync();
            return true;
        }

        private async Task<bool> IsVideoReferencedElsewhereAsync(string publicId, int? propertyId = null, int? unitId = null, int? projectId = null)
        {
            // Count ALL references to this PublicId across all three tables
            var propCount = await _unitOfWork.Properties.QueryTracked()
                .SelectMany(p => p.Videos!)
                .Where(v => v != null && v.PublicId == publicId)
                .CountAsync();

            var unitCount = await _unitOfWork.Units.QueryTracked()
                .SelectMany(u => u.Videos!)
                .Where(v => v != null && v.PublicId == publicId)
                .CountAsync();

            var projCount = await _unitOfWork.Projects.QueryTracked()
                .SelectMany(p => p.Videos!)
                .Where(v => v != null && v.PublicId == publicId)
                .CountAsync();

            // Total references (the caller's row hasn't been removed yet, so total includes it)
            return (propCount + unitCount + projCount) > 1;
        }
    }
}
