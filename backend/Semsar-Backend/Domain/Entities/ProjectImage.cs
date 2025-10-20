using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class ProjectImage : ISoftDelete
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        [Required]
        [MaxLength(1000)]
        public string Url { get; set; } = null!;

        [MaxLength(200)]
        public string? FileName { get; set; }

        public int SortOrder { get; set; }

        public bool IsMain { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [MaxLength(500)]
        public string? PublicId { get; set; }
    }
}
