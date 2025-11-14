using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class CreateLandRequestDto
    {
        public string? Honeypot { get; set; }
        public DateTime? SubmittedAt { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        [Required]
        [Phone]
        public string Phone { get; set; } = null!;

        [Required]
        public string Location { get; set; } = null!;

        [Range(0, double.MaxValue)]
        public decimal? MinPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MaxPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MinArea { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MaxArea { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

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

        [MaxLength(4000)]
        public string? VisitHistory { get; set; }
    }

    public class LandRequestDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Location { get; set; } = null!;
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public decimal? MinArea { get; set; }
        public decimal? MaxArea { get; set; }
        public string? Notes { get; set; }
        public string Source { get; set; } = "direct";
        public string? Medium { get; set; }
        public string? Campaign { get; set; }
        public string? Term { get; set; }
        public string? Content { get; set; }
        public string? LandingPage { get; set; }
        public DateTime? FirstVisitAt { get; set; }
        public string? CurrentPage { get; set; }
        public string? Referrer { get; set; }
        public string? UserAgent { get; set; }
        public int PageViews { get; set; }
        public int? SessionDuration { get; set; }
        public string? LastReferrer { get; set; }
        public string? VisitHistory { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
