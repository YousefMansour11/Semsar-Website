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
    public class VideoLibraryService : IVideoLibraryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<VideoLibraryService>? _logger;

        public VideoLibraryService(IUnitOfWork unitOfWork, ILogger<VideoLibraryService>? logger = null)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger;
        }

        public async Task<List<VideoLibraryItemDto>> GetLibraryAsync()
        {
            var propertyVideos = await _unitOfWork.Properties.QueryTracked()
                .SelectMany(p => p.Videos!)
                .Where(v => v != null && !string.IsNullOrWhiteSpace(v.PublicId))
                .Select(v => new { v.Url, v.PublicId, v.ThumbnailUrl, v.Title })
                .ToListAsync();

            var unitVideos = await _unitOfWork.Units.QueryTracked()
                .SelectMany(u => u.Videos!)
                .Where(v => v != null && !string.IsNullOrWhiteSpace(v.PublicId))
                .Select(v => new { v.Url, v.PublicId, v.ThumbnailUrl, v.Title })
                .ToListAsync();

            var projectVideos = await _unitOfWork.Projects.QueryTracked()
                .SelectMany(p => p.Videos!)
                .Where(v => v != null && !string.IsNullOrWhiteSpace(v.PublicId))
                .Select(v => new { v.Url, v.PublicId, v.ThumbnailUrl, v.Title })
                .ToListAsync();

            var all = propertyVideos
                .Concat(unitVideos)
                .Concat(projectVideos)
                .Where(v => !string.IsNullOrWhiteSpace(v.PublicId))
                .GroupBy(v => v.PublicId!, StringComparer.OrdinalIgnoreCase)
                .Select(g => new VideoLibraryItemDto
                {
                    Url = g.First().Url ?? string.Empty,
                    PublicId = g.Key,
                    ThumbnailUrl = g.First().ThumbnailUrl,
                    FileName = g.First().Title ?? g.Key.Split('/').Last(),
                    ReferenceCount = g.Count(),
                })
                .OrderByDescending(v => v.ReferenceCount)
                .ToList();

            return all;
        }

        public async Task<List<VideoLibraryItemDto>> GetLibraryByProjectAsync(int projectId)
        {
            var unitVideos = await _unitOfWork.Units.QueryTracked()
                .Where(u => u.ProjectId == projectId)
                .SelectMany(u => u.Videos!)
                .Where(v => v != null && !string.IsNullOrWhiteSpace(v.PublicId))
                .Select(v => new { v.Url, v.PublicId, v.ThumbnailUrl, v.Title })
                .ToListAsync();

            var grouped = unitVideos
                .Where(v => !string.IsNullOrWhiteSpace(v.PublicId))
                .GroupBy(v => v.PublicId!, StringComparer.OrdinalIgnoreCase)
                .Select(g => new VideoLibraryItemDto
                {
                    Url = g.First().Url ?? string.Empty,
                    PublicId = g.Key,
                    ThumbnailUrl = g.First().ThumbnailUrl,
                    FileName = g.First().Title ?? g.Key.Split('/').Last(),
                    ReferenceCount = g.Count(),
                })
                .OrderByDescending(v => v.ReferenceCount)
                .ToList();

            return grouped;
        }

        public async Task<List<VideoResultDto>> AttachLibraryVideoToPropertyAsync(int propertyId, string publicId)
        {
            var original = await FindOriginalVideoAsync(publicId);
            if (original == null)
                throw new KeyNotFoundException("Video not found in library");
            var (url, pubId, thumbUrl) = original.Value;

            var property = await _unitOfWork.Properties.QueryTracked()
                .Include(p => p.Videos)
                .FirstOrDefaultAsync(p => p.Id == propertyId);
            if (property == null) throw new KeyNotFoundException("Property not found");

            property.Videos ??= new List<PropertyVideo>();
            int nextOrder = property.Videos.Count + 1;

            var video = new PropertyVideo
            {
                Url = url,
                PublicId = pubId,
                ThumbnailUrl = thumbUrl,
                SortOrder = nextOrder,
                Property = property,
            };
            property.Videos.Add(video);
            property.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Properties.Update(property);
            await _unitOfWork.CommitAsync();

            return new List<VideoResultDto>
            {
                new VideoResultDto { Id = video.Id, Url = video.Url, PublicId = video.PublicId, ThumbnailUrl = video.ThumbnailUrl }
            };
        }

        public async Task<List<VideoResultDto>> AttachLibraryVideoToUnitAsync(int unitId, string publicId)
        {
            var original = await FindOriginalVideoAsync(publicId);
            if (original == null)
                throw new KeyNotFoundException("Video not found in library");
            var (url, pubId, thumbUrl) = original.Value;

            var unit = await _unitOfWork.Units.QueryTracked()
                .Include(u => u.Videos)
                .FirstOrDefaultAsync(u => u.Id == unitId);
            if (unit == null) throw new KeyNotFoundException("Unit not found");

            unit.Videos ??= new List<UnitVideo>();
            int nextOrder = unit.Videos.Count + 1;

            var video = new UnitVideo
            {
                Url = url,
                PublicId = pubId,
                ThumbnailUrl = thumbUrl,
                SortOrder = nextOrder,
                Unit = unit,
            };
            unit.Videos.Add(video);
            _unitOfWork.Units.Update(unit);
            await _unitOfWork.CommitAsync();

            return new List<VideoResultDto>
            {
                new VideoResultDto { Id = video.Id, Url = video.Url, PublicId = video.PublicId, ThumbnailUrl = video.ThumbnailUrl }
            };
        }

        public async Task<List<VideoResultDto>> AttachLibraryVideoToProjectAsync(int projectId, string publicId)
        {
            var original = await FindOriginalVideoAsync(publicId);
            if (original == null)
                throw new KeyNotFoundException("Video not found in library");
            var (url, pubId, thumbUrl) = original.Value;

            var project = await _unitOfWork.Projects.QueryTracked()
                .Include(p => p.Videos)
                .FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) throw new KeyNotFoundException("Project not found");

            project.Videos ??= new List<ProjectVideo>();
            int nextOrder = project.Videos.Count + 1;

            var video = new ProjectVideo
            {
                Url = url,
                PublicId = pubId,
                ThumbnailUrl = thumbUrl,
                SortOrder = nextOrder,
                Project = project,
            };
            project.Videos.Add(video);
            project.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Projects.Update(project);
            await _unitOfWork.CommitAsync();

            return new List<VideoResultDto>
            {
                new VideoResultDto { Id = video.Id, Url = video.Url, PublicId = video.PublicId, ThumbnailUrl = video.ThumbnailUrl }
            };
        }

        private async Task<(string Url, string? PublicId, string? ThumbnailUrl)?> FindOriginalVideoAsync(string publicId)
        {
            var propVideo = await _unitOfWork.Properties.QueryTracked()
                .SelectMany(p => p.Videos!)
                .Where(v => v != null && v.PublicId == publicId)
                .Select(v => new { v.Url, v.PublicId, v.ThumbnailUrl })
                .FirstOrDefaultAsync();
            if (propVideo != null) return (propVideo.Url, propVideo.PublicId, propVideo.ThumbnailUrl);

            var unitVideo = await _unitOfWork.Units.QueryTracked()
                .SelectMany(u => u.Videos!)
                .Where(v => v != null && v.PublicId == publicId)
                .Select(v => new { v.Url, v.PublicId, v.ThumbnailUrl })
                .FirstOrDefaultAsync();
            if (unitVideo != null) return (unitVideo.Url, unitVideo.PublicId, unitVideo.ThumbnailUrl);

            var projectVideo = await _unitOfWork.Projects.QueryTracked()
                .SelectMany(p => p.Videos!)
                .Where(v => v != null && v.PublicId == publicId)
                .Select(v => new { v.Url, v.PublicId, v.ThumbnailUrl })
                .FirstOrDefaultAsync();
            if (projectVideo != null) return (projectVideo.Url, projectVideo.PublicId, projectVideo.ThumbnailUrl);

            return null;
        }
    }
}
