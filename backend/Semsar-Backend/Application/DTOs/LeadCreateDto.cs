using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs
{
    public class LeadCreateDto
    {
        public int? PropertyId { get; set; }
        public string? Honeypot { get; set; }
        public DateTime? SubmittedAt { get; set; }
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = null!;
        [Required(ErrorMessage = "Phone is required")]
        public string Phone { get; set; } = null!;
        public string? Message { get; set; }

        [MaxLength(50)]
        public string? Source { get; set; }

        [MaxLength(50)]
        public string? Medium { get; set; }

        [MaxLength(100)]
        public string? Campaign { get; set; }

        [MaxLength(100)]
        public string? Term { get; set; }

        [MaxLength(100)]
        public string? Content { get; set; }

        [MaxLength(500)]
        public string? LandingPage { get; set; }

        public DateTime? FirstVisitAt { get; set; }

        [MaxLength(500)]
        public string? CurrentPage { get; set; }

        [MaxLength(500)]
        public string? Referrer { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public int PageViews { get; set; }

        public int? SessionDuration { get; set; }

        [MaxLength(500)]
        public string? LastReferrer { get; set; }

        [MaxLength(8000)]
        public string? VisitHistory { get; set; }
    }
}
