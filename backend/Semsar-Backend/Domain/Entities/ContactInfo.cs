using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities
{
    public class ContactInfo : ISoftDelete, IHasPublicKey
    {
        public int Id { get; set; }
        public Guid PublicId { get; set; } = Guid.CreateVersion7();
        public string PublicKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [Phone]
        [MaxLength(20)]
        public string Phone { get; set; } = null!;

        public ContactType Type { get; set; }

        public bool IsDeleted { get; set; } = false;

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<Property> Properties { get; set; } = new List<Property>();
    }
}
