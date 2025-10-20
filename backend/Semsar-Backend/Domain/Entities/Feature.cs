using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class Feature
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Key { get; set; } = null!;

        [MaxLength(200)]
        public string? NameEn { get; set; }

        [MaxLength(200)]
        public string? NameAr { get; set; }

        public ICollection<PropertyFeature> PropertyFeatures { get; set; } = new List<PropertyFeature>();
        public ICollection<UnitFeature> UnitFeatures { get; set; } = new List<UnitFeature>();
    }
}
