using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/contacts")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("fixed")]
    public class ContactsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<ContactsController> _logger;

        public ContactsController(IUnitOfWork uow, ILogger<ContactsController> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        // =========================
        // ADMIN: Get all contacts (paginated)
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetAll(
            int page = 1,
            int pageSize = 20)
        {
            int pageNum = Math.Max(1, page);
            int pageSizeNum = Math.Clamp(pageSize, 1, 100);

            var query = _uow.Contacts.Query().AsNoTracking()
                .Where(c => !c.IsDeleted);

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(c => c.Id)
                .Skip((pageNum - 1) * pageSizeNum)
                .Take(pageSizeNum)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Phone,
                    c.Type
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
        // ADMIN: Get single contact
        // =========================
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var contact = await _uow.Contacts.Query().AsNoTracking()
                .Where(c => c.Id == id && !c.IsDeleted)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Phone,
                    c.Type
                })
                .FirstOrDefaultAsync();

            if (contact == null)
                return NotFound();

            return Ok(contact);
        }

        // =========================
        // ADMIN: Update contact
        // =========================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ContactUpdateDto dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Phone))
                return BadRequest(new { message = "Name and phone are required" });

            var contact = await _uow.Contacts.GetByIdAsync(id);

            if (contact == null || contact.IsDeleted)
                return NotFound();

            contact.Name = dto.Name;
            contact.Phone = dto.Phone;
            contact.Type = dto.Type;

            _uow.Contacts.Update(contact);
            await _uow.CommitAsync();

            return Ok(new
            {
                contact.Id,
                contact.Name,
                contact.Phone,
                contact.Type
            });
        }

        // =========================
        // ADMIN: Soft delete
        // =========================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var contact = await _uow.Contacts.GetByIdAsync(id);

            if (contact == null || contact.IsDeleted)
                return NotFound();

            contact.IsDeleted = true;

            _uow.Contacts.Update(contact);
            await _uow.CommitAsync();

            _logger.LogWarning("AbuseAudit: Admin delete contact Id={Id} Phone={Phone} Admin={Admin}",
                id, contact.Phone, User.Identity?.Name ?? "unknown");

            return NoContent();
        }
    }
}