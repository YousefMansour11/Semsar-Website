using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs
{
    public class UpdateLeadStatusDto
    {
        [Required]
        public LeadStatus Status { get; set; }
    }
}
