using Application.DTOs;
using API.Services;
using Application.Interfaces;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/land-requests")]
    public class LandRequestsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly INotificationService _notifier;
        private readonly IConfiguration _config;
        private readonly ILogger<LandRequestsController> _logger;

        public LandRequestsController(IUnitOfWork uow, INotificationService notifier, IConfiguration config, ILogger<LandRequestsController> logger)
        {
            _uow = uow;
            _notifier = notifier;
            _config = config;
            _logger = logger;
        }

        // =========================
        // PUBLIC: Create land request
        // =========================
        [HttpPost]
        [EnableRateLimiting("form")]
        public async Task<IActionResult> Create([FromBody] CreateLandRequestDto dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            var normalizedPhone = LeadNormalizer.NormalizePhone(dto.Phone);
            var normalizedName = LeadNormalizer.NormalizeText(dto.Name);
            var normalizedLocation = LeadNormalizer.NormalizeText(dto.Location);
            var normalizedNotes = dto.Notes != null ? LeadNormalizer.NormalizeText(dto.Notes) : null;

            if (string.IsNullOrWhiteSpace(normalizedPhone) || string.IsNullOrWhiteSpace(normalizedName))
                return BadRequest(new { message = "Name and phone are required." });

            static string? N(string? v) => string.IsNullOrWhiteSpace(v) ? null : LeadNormalizer.NormalizeText(v).ToLowerInvariant();
            static string? P(string? v) => string.IsNullOrWhiteSpace(v) ? null : LeadNormalizer.NormalizeText(v);

            var request = new Domain.Entities.LandRequest
            {
                Name = normalizedName,
                Phone = normalizedPhone,
                Location = normalizedLocation,
                MinPrice = dto.MinPrice,
                MaxPrice = dto.MaxPrice,
                MinArea = dto.MinArea,
                MaxArea = dto.MaxArea,
                Notes = normalizedNotes,
                Source = N(dto.Source) ?? "direct",
                Medium = N(dto.Medium),
                Campaign = N(dto.Campaign),
                Term = N(dto.Term),
                Content = N(dto.Content),
                LandingPage = P(dto.LandingPage),
                FirstVisitAt = dto.FirstVisitAt,
                CurrentPage = P(dto.CurrentPage),
                Referrer = P(dto.Referrer),
                UserAgent = P(dto.UserAgent),
                PageViews = dto.PageViews,
                SessionDuration = dto.SessionDuration,
                LastReferrer = P(dto.LastReferrer),
                VisitHistory = P(dto.VisitHistory),
                CreatedAt = DateTime.UtcNow
            };

            await _uow.LandRequests.AddAsync(request);

            var existingLead = await _uow.Leads.Query()
                .FirstOrDefaultAsync(l => l.Phone == normalizedPhone && !l.IsDeleted);

            if (existingLead != null)
            {
                existingLead.LandRequest = request;
                existingLead.Message = $"Land request - {dto.Location}";
                _uow.Leads.Update(existingLead);
            }
            else
            {
                var lead = new Domain.Entities.Lead
                {
                    PropertyId = null,
                    Name = normalizedName,
                    Phone = normalizedPhone,
                    Message = $"Land request - {normalizedLocation}",
                    Source = N(dto.Source) ?? "direct",
                    Medium = N(dto.Medium),
                    Campaign = N(dto.Campaign),
                    Term = N(dto.Term),
                    Content = N(dto.Content),
                    LandingPage = P(dto.LandingPage),
                    FirstVisitAt = dto.FirstVisitAt,
                    CurrentPage = P(dto.CurrentPage),
                    Referrer = P(dto.Referrer),
                    UserAgent = P(dto.UserAgent),
                    PageViews = dto.PageViews,
                    SessionDuration = dto.SessionDuration,
                    LastReferrer = P(dto.LastReferrer),
                    VisitHistory = P(dto.VisitHistory),
                    IsPaid = string.Equals(N(dto.Medium), "cpc", StringComparison.OrdinalIgnoreCase),
                    LandRequest = request,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.Leads.AddAsync(lead);
            }

            await _uow.CommitAsync();

            _ = SendLandRequestEmailAsync(request);

            return Created(string.Empty, new
            {
                id = request.Id,
                publicKey = request.PublicKey,
                createdAt = request.CreatedAt
            });
        }

        // =========================
        // ADMIN: Get all land requests (paginated)
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> GetAll(
            int page = 1,
            int pageSize = 20,
            string sortBy = "createdAt",
            string sortOrder = "desc")
        {
            int pageNum = Math.Max(1, page);
            int pageSizeNum = Math.Clamp(pageSize, 1, 100);

            var query = _uow.LandRequests.Query().AsNoTracking()
                .Where(r => !r.IsDeleted);

            var total = await query.CountAsync();

            if (sortBy.ToLower() == "createdat")
            {
                query = sortOrder.ToLower() == "asc"
                    ? query.OrderBy(r => r.CreatedAt)
                    : query.OrderByDescending(r => r.CreatedAt);
            }
            else
            {
                query = sortOrder.ToLower() == "asc"
                    ? query.OrderBy(r => r.Id)
                    : query.OrderByDescending(r => r.Id);
            }

            var data = await query
                .Skip((pageNum - 1) * pageSizeNum)
                .Take(pageSizeNum)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Phone,
                    Location = r.Location,
                    r.MinPrice,
                    r.MaxPrice,
                    r.MinArea,
                    r.MaxArea,
                    r.Notes,
                    r.Source,
                    r.Medium,
                    r.Campaign,
                    r.Term,
                    r.Content,
                    r.LandingPage,
                    r.FirstVisitAt,
                    r.CurrentPage,
                    r.Referrer,
                    r.UserAgent,
                    r.PageViews,
                    r.SessionDuration,
                    r.LastReferrer,
                    r.VisitHistory,
                    r.CreatedAt
                })
                .ToListAsync();

            var totalPages = pageSizeNum > 0 ? (int)Math.Ceiling((double)total / pageSizeNum) : 0;

            return Ok(new
            {
                data,
                total,
                page = pageNum,
                pageSize = pageSizeNum,
                totalPages
            });
        }

        // =========================
        // ADMIN: Delete (Soft delete)
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> Delete(int id)
        {
            var request = await _uow.LandRequests.GetByIdAsync(id);

            if (request == null || request.IsDeleted)
                return NotFound();

            request.IsDeleted = true;

            _uow.LandRequests.Update(request);
            await _uow.CommitAsync();

            _logger.LogWarning("AbuseAudit: Admin delete land request Id={Id} Phone={Phone} Admin={Admin}",
                id, LogMask.Phone(request.Phone), User.Identity?.Name ?? "unknown");

            return NoContent();
        }

        private async Task SendLandRequestEmailAsync(Domain.Entities.LandRequest request)
        {
            try
            {
                var to = _config["AppSettings:AdminNotificationEmail"] ?? _config["Smtp:From"];
                if (string.IsNullOrWhiteSpace(to)) return;

                var subject = "New Land Request";

                var infoRows =
                    EmailTemplateService.Row("Name", request.Name) +
                    EmailTemplateService.Row("Phone", request.Phone, isPhone: true) +
                    EmailTemplateService.Row("Location", request.Location) +
                    EmailTemplateService.Row("Min Price", request.MinPrice.HasValue && request.MinPrice > 0 ? request.MinPrice.Value.ToString("N0") + " EGP" : null) +
                    EmailTemplateService.Row("Max Price", request.MaxPrice.HasValue && request.MaxPrice > 0 ? request.MaxPrice.Value.ToString("N0") + " EGP" : null) +
                    EmailTemplateService.Row("Min Area", request.MinArea.HasValue && request.MinArea > 0 ? request.MinArea.Value.ToString("N0") + " m²" : null) +
                    EmailTemplateService.Row("Max Area", request.MaxArea.HasValue && request.MaxArea > 0 ? request.MaxArea.Value.ToString("N0") + " m²" : null) +
                    EmailTemplateService.Row("Notes", request.Notes, isMultiline: true) +
                    EmailTemplateService.Row("Date", request.CreatedAt.ToString("MMM dd, yyyy HH:mm UTC"));

                var infoCard = EmailTemplateService.Card(infoRows);

                var tracking = EmailTemplateService.TrackingSection(
                    request.Source, request.Medium, request.Campaign, request.Term, request.Content,
                    request.LandingPage, request.CurrentPage, request.Referrer, request.UserAgent,
                    request.PageViews, request.SessionDuration, request.LastReferrer);

                var body = EmailTemplateService.BuildDocument(subject, infoCard + tracking, request.CreatedAt);

                await _notifier.SendEmailAsync(to, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send land request notification email");
            }
        }
    }
}