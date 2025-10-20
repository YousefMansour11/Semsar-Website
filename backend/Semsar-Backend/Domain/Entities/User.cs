using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class User : IHasPublicKey
    {
        public int Id { get; set; }
        public Guid PublicId { get; set; } = Guid.CreateVersion7();
        public string PublicKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Role { get; set; } = "User";

        public bool IsActive { get; set; } = true;

        [EmailAddress]
        public string? Email { get; set; }

        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public override bool Equals(object? obj)
        {
            if (obj is not User other) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id != 0 && Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
