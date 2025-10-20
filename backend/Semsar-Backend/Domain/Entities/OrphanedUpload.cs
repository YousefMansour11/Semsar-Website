using System;

namespace Domain.Entities
{
    public class OrphanedUpload
    {
        public int Id { get; set; }
        public string PublicId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending";
        public string? ErrorMessage { get; set; }

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
