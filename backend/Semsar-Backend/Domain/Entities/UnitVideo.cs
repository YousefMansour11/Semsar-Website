using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class UnitVideo : ISoftDelete
    {
        public int Id { get; set; }

        public int UnitId { get; set; }
        public Unit Unit { get; set; } = null!;

        [Required]
        [MaxLength(1000)]
        public string Url { get; set; } = null!;

        [MaxLength(500)]
        public string? PublicId { get; set; }

        [MaxLength(500)]
        public string? ThumbnailUrl { get; set; }

        public double? Duration { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }

        [MaxLength(300)]
        public string? Title { get; set; }

        public int SortOrder { get; set; }
        public bool IsMain { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
