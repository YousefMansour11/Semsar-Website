using Application.DTOs;
using API.Services;
using Application.Interfaces;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    public class BookingsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly INotificationService _notifier;
        private readonly IConfiguration _config;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(IUnitOfWork uow, INotificationService notifier, IConfiguration config, ILogger<BookingsController> logger)
        {
            _uow = uow;
            _notifier = notifier;
            _config = config;
            _logger = logger;
        }

        [HttpPost]
        [EnableRateLimiting("form")]
        public async Task<IActionResult> Create([FromBody] BookingSubmitDto dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            string? propertyCode = null;
            int? resolvedPropertyId = null;

            if (dto.PropertyId.GetValueOrDefault() > 0)
            {
                var property = await _uow.Properties.Query()
                    .FirstOrDefaultAsync(p => p.Id == dto.PropertyId!.Value);

                if (property != null)
                {
                    propertyCode = property.Code ?? property.PublicKey ?? property.Id.ToString();
                    resolvedPropertyId = property.Id;
                }
            }
            else if (dto.UnitId.GetValueOrDefault() > 0)
            {
                var unit = await _uow.Units.Query()
                    .FirstOrDefaultAsync(u => u.Id == dto.UnitId!.Value);

                if (unit != null)
                {
                    propertyCode = unit.Code ?? unit.PublicKey ?? unit.Id.ToString();
                    resolvedPropertyId = null;
                }
            }

            if (string.IsNullOrWhiteSpace(propertyCode))
                return BadRequest(new { message = "A valid PropertyId or UnitId is required." });

            // Normalize inputs
            var normalizedPhone = LeadNormalizer.NormalizePhone(dto.Phone);
            var normalizedName = LeadNormalizer.NormalizeText(dto.Name);
            var normalizedMessage = dto.Message != null ? LeadNormalizer.NormalizeText(dto.Message) : null;

            if (string.IsNullOrWhiteSpace(normalizedPhone) || string.IsNullOrWhiteSpace(normalizedName))
                return BadRequest(new { message = "Name and phone are required." });

            // Existing booking for same property
            var phoneExists = await _uow.Bookings.Query()
                .AnyAsync(b => b.Phone == normalizedPhone && b.PropertyCode == propertyCode && !b.IsDeleted);
            if (phoneExists)
                return Conflict(new { message = "A booking with this phone number already exists for this property." });

            static string? N(string? v) => string.IsNullOrWhiteSpace(v) ? null : LeadNormalizer.NormalizeText(v).ToLowerInvariant();
            static string? P(string? v) => string.IsNullOrWhiteSpace(v) ? null : LeadNormalizer.NormalizeText(v);

            var booking = new Domain.Entities.BookingRequest
            {
                PropertyCode = propertyCode,
                Name = normalizedName,
                Phone = normalizedPhone,
                Message = dto.Message,
                PreferredDate = dto.PreferredDate,
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

            await _uow.Bookings.AddAsync(booking);

            var existingLead = await _uow.Leads.Query()
                .FirstOrDefaultAsync(l => l.Phone == normalizedPhone && !l.IsDeleted);

            if (existingLead != null)
            {
                existingLead.BookingRequest = booking;
                existingLead.Message = dto.Message ?? existingLead.Message;
                _uow.Leads.Update(existingLead);
            }
            else
            {
                var lead = new Domain.Entities.Lead
                {
                    PropertyId = resolvedPropertyId,
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
                    Referrer = P(dto.Referrer),
                    UserAgent = P(dto.UserAgent),
                    PageViews = dto.PageViews,
                    SessionDuration = dto.SessionDuration,
                    LastReferrer = P(dto.LastReferrer),
                    VisitHistory = P(dto.VisitHistory),
                    IsPaid = string.Equals(N(dto.Medium), "cpc", StringComparison.OrdinalIgnoreCase),
                    BookingRequest = booking,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.Leads.AddAsync(lead);
            }

            await _uow.CommitAsync();

            _ = SendBookingEmailAsync(booking);

            return Ok(new { booking.Id, publicKey = booking.PublicKey, message = "Booking request submitted successfully" });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> GetAll(int page = 1, int pageSize = 20)
        {
            int pageNum = Math.Max(1, page);
            int pageSizeNum = Math.Clamp(pageSize, 1, 100);

            var q = _uow.Bookings.Query().AsNoTracking()
                .Where(b => !b.IsDeleted)
                .OrderByDescending(b => b.CreatedAt);

            var total = await q.CountAsync();

            var bookings = await q.Skip((pageNum - 1) * pageSizeNum)
                .Take(pageSizeNum)
                .ToListAsync();

            var codes = bookings
                .Where(b => !string.IsNullOrEmpty(b.PropertyCode))
                .Select(b => b.PropertyCode)
                .Distinct()
                .ToList();

            var propertyMap = await _uow.Properties.Query().AsNoTracking()
                .Where(p => codes.Contains(p.Code) && !p.IsDeleted)
                .ToDictionaryAsync(p => p.Code, p => new { p.TitleEn, p.Location });

            var unitMap = await _uow.Units.Query().AsNoTracking()
                .Where(u => codes.Contains(u.Code) && !u.IsDeleted)
                .ToDictionaryAsync(u => u.Code, u => new { u.TitleEn, u.Location });

            var data = bookings.Select(b =>
            {
                var match = b.PropertyCode != null && propertyMap.TryGetValue(b.PropertyCode, out var m)
                    ? m
                    : b.PropertyCode != null && unitMap.TryGetValue(b.PropertyCode, out var u)
                        ? u
                        : null;
                return new
                {
                    b.Id,
                    b.PropertyCode,
                    PropertyTitle = match?.TitleEn,
                    PropertyLocation = match?.Location,
                    b.Name,
                    b.Phone,
                    b.Message,
                    b.PreferredDate,
                    b.Source,
                    b.Medium,
                    b.Campaign,
                    b.Term,
                    b.Content,
                    b.LandingPage,
                    b.FirstVisitAt,
                    b.CurrentPage,
                    b.Referrer,
                    b.UserAgent,
                    b.PageViews,
                    b.SessionDuration,
                    b.LastReferrer,
                    b.VisitHistory,
                    b.CreatedAt
                };
            }).ToList();

            return Ok(new { total, page = pageNum, pageSize = pageSizeNum, data });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> Delete(int id)
        {
            var b = await _uow.Bookings.GetByIdAsync(id);
            if (b == null || b.IsDeleted) return NotFound();

            b.IsDeleted = true;
            _uow.Bookings.Update(b);
            await _uow.CommitAsync();

            _logger.LogWarning("AbuseAudit: Admin delete booking Id={Id} Phone={Phone} Admin={Admin}",
                id, LogMask.Phone(b.Phone), User.Identity?.Name ?? "unknown");

            return NoContent();
        }

        private async Task SendBookingEmailAsync(Domain.Entities.BookingRequest booking)
        {
            try
            {
                var to = _config["AppSettings:AdminNotificationEmail"] ?? _config["Smtp:From"];
                if (string.IsNullOrWhiteSpace(to)) return;

                var subject = $"New Booking Request — {booking.PropertyCode}".Replace("\r", "").Replace("\n", "");

                var infoRows =
                    EmailTemplateService.Row("Name", booking.Name) +
                    EmailTemplateService.Row("Phone", booking.Phone, isPhone: true) +
                    EmailTemplateService.Row("Property", booking.PropertyCode) +
                    EmailTemplateService.Row("Message", booking.Message, isMultiline: true) +
                    EmailTemplateService.Row("Date", booking.CreatedAt.ToString("MMM dd, yyyy HH:mm UTC"));

                var infoCard = EmailTemplateService.Card(infoRows);

                var tracking = EmailTemplateService.TrackingSection(
                    booking.Source, booking.Medium, booking.Campaign, booking.Term, booking.Content,
                    booking.LandingPage, booking.CurrentPage, booking.Referrer, booking.UserAgent,
                    booking.PageViews, booking.SessionDuration, booking.LastReferrer);

                var body = EmailTemplateService.BuildDocument(subject, infoCard + tracking, booking.CreatedAt);

                await _notifier.SendEmailAsync(to, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send booking notification email");
            }
        }
    }
}
