using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class LandRequestService : ILandRequestService
    {
        private readonly IRepository<LandRequest> _repo;
        private readonly IUnitOfWork _unitOfWork;

        public LandRequestService(IRepository<LandRequest> repo, IUnitOfWork unitOfWork)
        {
            _repo = repo;
            _unitOfWork = unitOfWork;
        }

        private static string N(string? v) => v?.Trim().ToLowerInvariant() ?? string.Empty;
        private static string P(string? v) => v?.Trim() ?? string.Empty;

        public async Task<LandRequest> CreateAsync(LandRequestDto dto)
        {
            var entity = new LandRequest
            {
                Name = dto.Name,
                Phone = dto.Phone,
                Location = dto.Location,
                MinPrice = dto.MinPrice,
                MaxPrice = dto.MaxPrice,
                MinArea = dto.MinArea,
                MaxArea = dto.MaxArea,
                Notes = dto.Notes,
                Source = string.IsNullOrEmpty(N(dto.Source)) ? "direct" : N(dto.Source),
                Medium = string.IsNullOrEmpty(N(dto.Medium)) ? null : N(dto.Medium),
                Campaign = string.IsNullOrEmpty(N(dto.Campaign)) ? null : N(dto.Campaign),
                Term = string.IsNullOrEmpty(N(dto.Term)) ? null : N(dto.Term),
                Content = string.IsNullOrEmpty(N(dto.Content)) ? null : N(dto.Content),
                LandingPage = string.IsNullOrEmpty(P(dto.LandingPage)) ? null : P(dto.LandingPage),
                FirstVisitAt = dto.FirstVisitAt,
                CurrentPage = string.IsNullOrEmpty(P(dto.CurrentPage)) ? null : P(dto.CurrentPage),
                Referrer = string.IsNullOrEmpty(P(dto.Referrer)) ? null : P(dto.Referrer),
                UserAgent = string.IsNullOrEmpty(P(dto.UserAgent)) ? null : P(dto.UserAgent),
                PageViews = dto.PageViews,
                SessionDuration = dto.SessionDuration,
                LastReferrer = string.IsNullOrEmpty(P(dto.LastReferrer)) ? null : P(dto.LastReferrer),
                VisitHistory = string.IsNullOrEmpty(P(dto.VisitHistory)) ? null : P(dto.VisitHistory),
            };

            await _repo.AddAsync(entity);
            await _unitOfWork.CommitAsync();

            return entity;
        }

        public async Task<IEnumerable<LandRequest>> GetAllAsync()
        {
            return await _repo.Query()
                              .OrderByDescending(lr => lr.CreatedAt)
                              .ToListAsync();
        }
    }
}