namespace Domain.Entities
{
    public class Setting : ISoftDelete
    {
        public int Id { get; set; }

        public string Key { get; set; } = null!;
        public string? Value { get; set; }
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
