using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities
{
    public class Location
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string NameEn { get; set; } = null!;

        [Required, MaxLength(200)]
        public string NameAr { get; set; } = null!;

        [MaxLength(300)]
        public string Slug { get; set; } = null!;

        public int? ParentId { get; set; }
        public Location? Parent { get; set; }

        public ICollection<Location> Children { get; set; } = new List<Location>();

        public LocationLevel Level { get; set; }

        [MaxLength(500)]
        public string Path { get; set; } = null!;

        public int Depth { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Property> Properties { get; set; } = new List<Property>();
        public ICollection<Unit> Units { get; set; } = new List<Unit>();
    }
}
