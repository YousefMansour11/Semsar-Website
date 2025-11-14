using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class BookingSubmitDto
    {
        public int? PropertyId { get; set; }
        public int? UnitId { get; set; }
        public string? Honeypot { get; set; }
        public DateTime? SubmittedAt { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [Phone]
        [MaxLength(20)]
        public string Phone { get; set; } = null!;

        [MaxLength(500)]
        public string? Message { get; set; }

        public DateTime? PreferredDate { get; set; }

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
