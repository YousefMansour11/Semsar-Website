using System.Linq;
using Application.DTOs;
using API.Services;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Controllers
{
    [ApiController]
    [Route("api/leads")]
    public class LeadsController : ControllerBase
    {
        private readonly ILeadService _leadService;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<LeadsController> _logger;
        private readonly INotificationService _notifier;
        private readonly IConfiguration _config;

        public LeadsController(
            ILeadService leadService,
            IUnitOfWork uow,
            ILogger<LeadsController> logger,
            INotificationService notifier,
            IConfiguration config)
        {
            _leadService = leadService;
            _uow = uow;
            _logger = logger;
            _notifier = notifier;
            _config = config;
        }

        [HttpPost]
        [EnableRateLimiting("form")]
        public async Task<IActionResult> Create([FromBody] LeadCreateDto dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.PropertyId.HasValue)
            {
                var property = await _uow.Properties.GetByIdAsync(dto.PropertyId.Value);
                if (property == null || property.IsDeleted)
                    return NotFound(new { message = "Property not found" });
            }

            var normalizedPhone = LeadNormalizer.NormalizePhone(dto.Phone);
            var normalizedName = LeadNormalizer.NormalizeText(dto.Name);
            var normalizedMessage = dto.Message != null ? LeadNormalizer.NormalizeText(dto.Message) : null;

            if (string.IsNullOrWhiteSpace(normalizedPhone) || string.IsNullOrWhiteSpace(normalizedName))
                return BadRequest(new { message = "Name and phone are required." });

            static string? N(string? v) => string.IsNullOrWhiteSpace(v) ? null : LeadNormalizer.NormalizeText(v).ToLowerInvariant();
            static string? P(string? v) => string.IsNullOrWhiteSpace(v) ? null : LeadNormalizer.NormalizeText(v);

            var lead = new Domain.Entities.Lead
            {
                PropertyId = dto.PropertyId,
                Name = normalizedName,
                Phone = normalizedPhone,
                Message = normalizedMessage,
                Source = N(dto.Source) ?? "direct",
                Medium = N(dto.Medium),
                Campaign = N(dto.Campaign),
                Term = N(dto.Term),
                Content = N(dto.Content),
                LandingPage = P(dto.LandingPage),
                FirstVisitAt = dto.FirstVisitAt,
                CurrentPage = P(dto.CurrentPage),
                IsPaid = string.Equals(N(dto.Medium), "cpc", StringComparison.OrdinalIgnoreCase),
                Referrer = P(dto.Referrer),
                UserAgent = P(dto.UserAgent),
                PageViews = dto.PageViews,
                SessionDuration = dto.SessionDuration,
                LastReferrer = P(dto.LastReferrer),
                VisitHistory = P(dto.VisitHistory),
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Leads.AddAsync(lead);
            await _uow.CommitAsync();

            // Fire-and-forget email notification — non-blocking, errors logged internally
            _ = SendLeadEmailAsync(lead);

            return Ok(new { lead.Id, lead.PublicKey, message = "Lead created successfully" });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> GetAll(int page = 1, int pageSize = 20, string? type = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            IQueryable<Lead> q = _uow.Leads.Query().AsNoTracking()
                .Where(l => !l.IsDeleted)
                .Include(l => l.Property)
                .Include(l => l.BookingRequest)
                .Include(l => l.LandRequest);

            if (!string.IsNullOrWhiteSpace(type))
            {
                q = type.ToLower() switch
                {
                    "contact" => q.Where(l => l.BookingRequestId == null && l.LandRequestId == null),
                    "booking" => q.Where(l => l.BookingRequestId != null),
                    "land" => q.Where(l => l.LandRequestId != null),
                    _ => q
                };
            }

            q = q.OrderByDescending(l => l.CreatedAt);

            var total = await q.CountAsync();

            var data = await q.Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    l.Id,
                    l.Name,
                    l.Phone,
                    l.Message,
                    l.CreatedAt,
                    l.Source,
                    l.Medium,
                    l.Campaign,
                    l.Term,
                    l.Content,
                    l.LandingPage,
                    l.FirstVisitAt,
                    l.CurrentPage,
                    l.IsPaid,
                    l.Referrer,
                    l.UserAgent,
                    l.PageViews,
                    l.SessionDuration,
                    l.LastReferrer,
                    l.VisitHistory,
                    Status = l.Status.ToString(),
                    PropertyCode = l.Property != null ? l.Property.Code : null,
                    BookingInfo = l.BookingRequest != null
                        ? new
                        {
                            PropertyCode = l.BookingRequest.PropertyCode,
                            Message = l.BookingRequest.Message,
                            PreferredDate = l.BookingRequest.PreferredDate
                        }
                        : null,
                    LandRequestInfo = l.LandRequest != null
                        ? new
                        {
                            Location = l.LandRequest.Location,
                            MinPrice = l.LandRequest.MinPrice,
                            MaxPrice = l.LandRequest.MaxPrice,
                            MinArea = l.LandRequest.MinArea,
                            MaxArea = l.LandRequest.MaxArea,
                            Notes = l.LandRequest.Notes
                        }
                        : null
                })
                .ToListAsync();

            return Ok(new { total, page, pageSize, data });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id:int}")]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> Get(int id)
        {
            var lead = await _uow.Leads.Query().AsNoTracking()
                .Where(l => l.Id == id && !l.IsDeleted)
                .Include(l => l.Property)
                .Include(l => l.BookingRequest)
                .Include(l => l.LandRequest)
                .Select(l => new
                {
                    l.Id,
                    l.Name,
                    l.Phone,
                    l.Message,
                    l.CreatedAt,
                    l.Source,
                    l.Medium,
                    l.Campaign,
                    l.Term,
                    l.Content,
                    l.LandingPage,
                    l.FirstVisitAt,
                    l.CurrentPage,
                    l.IsPaid,
                    l.Referrer,
                    l.UserAgent,
                    l.PageViews,
                    l.SessionDuration,
                    l.LastReferrer,
                    l.VisitHistory,
                    Status = l.Status.ToString(),
                    PropertyCode = l.Property != null ? l.Property.Code : null,
                    BookingInfo = l.BookingRequest != null
                        ? new
                        {
                            PropertyCode = l.BookingRequest.PropertyCode,
                            Message = l.BookingRequest.Message,
                            PreferredDate = l.BookingRequest.PreferredDate
                        }
                        : null,
                    LandRequestInfo = l.LandRequest != null
                        ? new
                        {
                            Location = l.LandRequest.Location,
                            MinPrice = l.LandRequest.MinPrice,
                            MaxPrice = l.LandRequest.MaxPrice,
                            MinArea = l.LandRequest.MinArea,
                            MaxArea = l.LandRequest.MaxArea,
                            Notes = l.LandRequest.Notes
                        }
                        : null
                })
                .FirstOrDefaultAsync();

            if (lead == null) return NotFound();

            return Ok(lead);
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/status")]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateLeadStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var lead = await _uow.Leads.GetByIdAsync(id);
            if (lead == null || lead.IsDeleted)
                return NotFound();

            lead.Status = dto.Status;
            _uow.Leads.Update(lead);
            await _uow.CommitAsync();

            _logger.LogInformation("Lead {Id} status updated to {Status} by {Admin}", id, dto.Status, User.Identity?.Name ?? "unknown");
            return Ok(new { message = "Lead status updated", status = dto.Status.ToString() });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> Delete(int id)
        {
            var lead = await _uow.Leads.GetByIdAsync(id);
            if (lead == null || lead.IsDeleted) return NotFound();

            lead.IsDeleted = true;
            _uow.Leads.Update(lead);
            await _uow.CommitAsync();

            _logger.LogWarning("AbuseAudit: Admin delete lead Id={Id} Phone={Phone} Admin={Admin}",
                id, LogMask.Phone(lead.Phone), User.Identity?.Name ?? "unknown");

            return NoContent();
        }

        private async Task SendLeadEmailAsync(Domain.Entities.Lead lead)
        {
            try
            {
                var to = _config["AppSettings:AdminNotificationEmail"] ?? _config["Smtp:From"];
                if (string.IsNullOrWhiteSpace(to)) return;

                var subject = "New Contact Message";

                var infoRows =
                    EmailTemplateService.Row("Name", lead.Name) +
                    EmailTemplateService.Row("Phone", lead.Phone, isPhone: true) +
                    EmailTemplateService.Row("Message", lead.Message, isMultiline: true) +
                    EmailTemplateService.Row("Date", lead.CreatedAt.ToString("MMM dd, yyyy HH:mm UTC"));

                var infoCard = EmailTemplateService.Card(infoRows);

                var tracking = EmailTemplateService.TrackingSection(
                    lead.Source, lead.Medium, lead.Campaign, lead.Term, lead.Content,
                    lead.LandingPage, lead.CurrentPage, lead.Referrer, lead.UserAgent,
                    lead.PageViews, lead.SessionDuration, lead.LastReferrer);

                var body = EmailTemplateService.BuildDocument(subject, infoCard + tracking, lead.CreatedAt);

                await _notifier.SendEmailAsync(to, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send lead notification email");
            }
        }
    }
}
