using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services
{
    public class LeadService : ILeadService
    {
        private readonly IUnitOfWork _uow;

        public LeadService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        private static string N(string? v) => v?.Trim().ToLowerInvariant() ?? string.Empty;
        private static string P(string? v) => v?.Trim() ?? string.Empty;

        public async Task<Lead> CreateLeadAsync(LeadDto dto)
        {
            var lead = new Lead
            {
                PropertyId = dto.PropertyId,
                Name = dto.Name,
                Phone = dto.Phone,
                Message = dto.Message,
                CreatedAt = DateTime.UtcNow,
                Source = string.IsNullOrEmpty(N(dto.Source)) ? "direct" : N(dto.Source),
                Medium = string.IsNullOrEmpty(N(dto.Medium)) ? null : N(dto.Medium),
                Campaign = string.IsNullOrEmpty(N(dto.Campaign)) ? null : N(dto.Campaign),
                Term = string.IsNullOrEmpty(N(dto.Term)) ? null : N(dto.Term),
                Content = string.IsNullOrEmpty(N(dto.Content)) ? null : N(dto.Content),
                LandingPage = string.IsNullOrEmpty(P(dto.LandingPage)) ? null : P(dto.LandingPage),
                FirstVisitAt = dto.FirstVisitAt,
                CurrentPage = string.IsNullOrEmpty(P(dto.CurrentPage)) ? null : P(dto.CurrentPage),
                IsPaid = string.Equals(N(dto.Medium), "cpc", StringComparison.OrdinalIgnoreCase),
                Referrer = string.IsNullOrEmpty(P(dto.Referrer)) ? null : P(dto.Referrer),
                UserAgent = string.IsNullOrEmpty(P(dto.UserAgent)) ? null : P(dto.UserAgent),
                PageViews = dto.PageViews,
                SessionDuration = dto.SessionDuration,
                LastReferrer = string.IsNullOrEmpty(P(dto.LastReferrer)) ? null : P(dto.LastReferrer),
                VisitHistory = string.IsNullOrEmpty(P(dto.VisitHistory)) ? null : P(dto.VisitHistory),
            };

            await _uow.Leads.AddAsync(lead);
            await _uow.CommitAsync();

            return lead;
        }

        public async Task<IEnumerable<Lead>> GetAllAsync()
        {
            return await _uow.Leads.GetAllAsync();
        }

        public async Task<Lead?> GetByIdAsync(int id)
        {
            return await _uow.Leads.GetByIdAsync(id);
        }

        public async Task UpdateStatusAsync(int id, string status)
        {
            var lead = await _uow.Leads.GetByIdAsync(id);
            if (lead == null) throw new KeyNotFoundException($"Lead with ID {id} not found");

            if (Enum.TryParse<LeadStatus>(status, true, out var newStatus))
            {
                lead.Status = newStatus;
            }
            else
            {
                throw new ArgumentException($"Invalid status value: {status}. Valid values are: {string.Join(", ", Enum.GetNames<LeadStatus>())}", nameof(status));
            }

            _uow.Leads.Update(lead);
            await _uow.CommitAsync();
        }
    }
}