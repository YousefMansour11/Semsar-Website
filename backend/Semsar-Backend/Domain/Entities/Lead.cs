using System;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities
{
    public class Lead : ISoftDelete, IHasPublicKey
    {
        public int Id { get; set; }
        public Guid PublicId { get; set; } = Guid.CreateVersion7();
        public string PublicKey { get; set; } = string.Empty;

        public int? PropertyId { get; set; }
        public Property? Property { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [Phone]
        [MaxLength(20)]
        public string Phone { get; set; } = null!;

        [MaxLength(500)]
        public string? Message { get; set; }

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

        public bool IsPaid { get; set; }

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

        public int? BookingRequestId { get; set; }
        public BookingRequest? BookingRequest { get; set; }

        public int? LandRequestId { get; set; }
        public LandRequest? LandRequest { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public LeadStatus Status { get; set; } = LeadStatus.New;

        public bool IsDeleted { get; set; } = false;

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public override bool Equals(object? obj)
        {
            if (obj is not Lead other) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id != 0 && Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
