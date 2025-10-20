using System;

namespace Domain.Entities
{
    public class CodeReservation
    {
        public int Id { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties for EF relationship resolution
        public Property? Property { get; set; }
        public Project? Project { get; set; }
        public Unit? Unit { get; set; }

        // Concurrency token for reservations
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
