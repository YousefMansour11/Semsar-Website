using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace API.Controllers
{
    [ApiController]
    [Route("api/settings")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("fixed")]
    public class SettingsAdminController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public SettingsAdminController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        private async Task<string?> GetSettingValueAsync(string key)
        {
            var setting = await _uow.Settings.Query().FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted);
            return setting?.Value;
        }

        private async Task SetSettingAsync(string key, string value, string? description = null)
        {
            var setting = await _uow.Settings.Query().FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted);
            if (setting == null)
            {
                setting = new Setting { Key = key, Value = value, Description = description };
                await _uow.Settings.AddAsync(setting);
            }
            else
            {
                setting.Value = value;
                setting.Description = description ?? setting.Description;
                _uow.Settings.Update(setting);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Get()
        {
            var whatsappNumber = await GetSettingValueAsync("whatsapp_number") ?? "+201234567890";
            var phoneNumber = await GetSettingValueAsync("phone_number") ?? "+201234567890";
            var companyName = await GetSettingValueAsync("company_name") ?? "Semsar";
            var facebook = await GetSettingValueAsync("facebook") ?? "";
            var instagram = await GetSettingValueAsync("instagram") ?? "";
            var tiktok = await GetSettingValueAsync("tiktok") ?? "";

            return Ok(new
            {
                whatsappNumber,
                phoneNumber,
                companyName,
                socialLinks = new { facebook, instagram, tiktok }
            });
        }

        public class UpdateSettingsDto
        {
            [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Company name is required")]
            [System.ComponentModel.DataAnnotations.MaxLength(200, ErrorMessage = "Company name must not exceed 200 characters")]
            public required string CompanyName { get; set; }

            [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "WhatsApp number is required")]
            [System.ComponentModel.DataAnnotations.Phone(ErrorMessage = "WhatsApp number must be a valid phone number")]
            public required string WhatsappNumber { get; set; }

            [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Phone number is required")]
            [System.ComponentModel.DataAnnotations.Phone(ErrorMessage = "Phone number must be a valid phone number")]
            public required string PhoneNumber { get; set; }

            [System.ComponentModel.DataAnnotations.Url(ErrorMessage = "Facebook must be a valid URL")]
            [System.ComponentModel.DataAnnotations.MaxLength(500, ErrorMessage = "Facebook URL must not exceed 500 characters")]
            public string? Facebook { get; set; }

            [System.ComponentModel.DataAnnotations.Url(ErrorMessage = "Instagram must be a valid URL")]
            [System.ComponentModel.DataAnnotations.MaxLength(500, ErrorMessage = "Instagram URL must not exceed 500 characters")]
            public string? Instagram { get; set; }

            [System.ComponentModel.DataAnnotations.Url(ErrorMessage = "TikTok must be a valid URL")]
            [System.ComponentModel.DataAnnotations.MaxLength(500, ErrorMessage = "TikTok URL must not exceed 500 characters")]
            public string? TikTok { get; set; }
        }

        [HttpPut]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update([FromBody] UpdateSettingsDto dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            await SetSettingAsync("company_name", dto.CompanyName ?? string.Empty, "Company display name");
            await SetSettingAsync("whatsapp_number", dto.WhatsappNumber ?? string.Empty, "WhatsApp contact number");
            await SetSettingAsync("phone_number", dto.PhoneNumber ?? string.Empty, "Phone contact number");
            await SetSettingAsync("facebook", dto.Facebook ?? string.Empty, "Facebook URL");
            await SetSettingAsync("instagram", dto.Instagram ?? string.Empty, "Instagram URL");
            await SetSettingAsync("tiktok", dto.TikTok ?? string.Empty, "TikTok URL");

            await _uow.CommitAsync();

            return Ok(new { message = "Settings updated" });
        }
    }
}
