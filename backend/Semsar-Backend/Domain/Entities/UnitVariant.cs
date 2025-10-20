using System;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities
{
    public class UnitVariant : ISoftDelete
    {
        public int Id { get; set; }
        public Guid PublicId { get; set; } = Guid.CreateVersion7();
        public string PublicKey { get; set; } = string.Empty;

        public int UnitId { get; set; }
        public Unit Unit { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(100)]
        public string? NameAr { get; set; }

        [Range(0, double.MaxValue)]
        public double Size { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "EGP";

        [Range(0, double.MaxValue)]
        public decimal? RentPerMonth { get; set; }

        public int Bedrooms { get; set; } = 0;
        public int Bathrooms { get; set; } = 0;
        public int? Floor { get; set; }
        public bool IsFurnished { get; set; } = false;
        public PropertyView View { get; set; } = PropertyView.Unknown;

        [MaxLength(50)]
        public string? UnitNumber { get; set; }
        [MaxLength(50)]
        public string? BuildingNumber { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public FinishingType? FinishingType { get; set; }
        public bool HasBalcony { get; set; } = false;
        public bool HasParking { get; set; } = false;

        [MaxLength(500)]
        public string? FloorPlanUrl { get; set; }

        [MaxLength(50)]
        public string? AvailabilityStatus { get; set; } = "Available";

        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        public bool IsRecommended { get; set; } = false;
        public int ViewCount { get; set; } = 0;
        public int InquiryCount { get; set; } = 0;
        public int FavoriteCount { get; set; } = 0;
        [MaxLength(200)]
        public string? DeliveryText { get; set; }
        [MaxLength(200)]
        public string? DeliveryTextAr { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
