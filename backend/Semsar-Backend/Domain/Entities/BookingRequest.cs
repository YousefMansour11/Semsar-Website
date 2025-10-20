using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class BookingRequest : ISoftDelete, IHasPublicKey
    {
        public int Id { get; set; }
        public Guid PublicId { get; set; } = Guid.CreateVersion7();
        public string PublicKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string PropertyCode { get; set; } = null!;

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
        public string Source { get; set; } = "direct";

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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; }

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
