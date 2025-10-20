using System;

namespace Domain.Entities
{
    public class SlugReservation
    {
        public int Id { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties for EF relationship resolution
        // These are configured in AppDbContext with separate FK columns
        public Property? Property { get; set; }
        public Project? Project { get; set; }
        public Unit? Unit { get; set; }

        // Concurrency token for reservations
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
