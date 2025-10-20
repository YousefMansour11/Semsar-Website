using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities
{
    public class Property : ISoftDelete, IHasPublicKey
    {
        public int Id { get; set; }
        public Guid PublicId { get; set; } = Guid.CreateVersion7();
        public string PublicKey { get; set; } = string.Empty;

        // ===== Multilingual =====
        [Required]
        [MaxLength(200)]
        public string TitleEn { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string TitleAr { get; set; } = null!;

        [MaxLength(8000)]
        public string DescriptionEn { get; set; } = null!;

        [MaxLength(8000)]
        public string DescriptionAr { get; set; } = null!;

        // ===== Pricing =====
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public decimal? RentPerMonth { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "EGP";

        // ===== Classification =====
        public PropertyType PropertyType { get; set; }
        public PropertyListingType ListingType { get; set; }

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = null!;

        public string? LocationAr { get; set; }

        public int? LocationId { get; set; }
        public Location? LocationEntity { get; set; }

        [MaxLength(50)]
        public string Code { get; set; } = null!;

        // routing
        [MaxLength(450)]
        public string Slug { get; set; } = null!;

        public bool SlugIsAuto { get; set; } = true;

        [MaxLength(5)]
        public string SlugLanguage { get; set; } = "en";

        [MaxLength(200)]
        public string? SeoTitle { get; set; }

        [MaxLength(300)]
        public string? SeoDescription { get; set; }

        [MaxLength(200)]
        public string? SeoTitleAr { get; set; }

        [MaxLength(300)]
        public string? SeoDescriptionAr { get; set; }

        [MaxLength(500)]
        public string? SeoKeywords { get; set; }

        [MaxLength(500)]
        public string? SeoKeywordsAr { get; set; }

        [MaxLength(1000)]
        public string CanonicalUrl { get; set; } = string.Empty;

        public DateTime MetaGeneratedAt { get; set; } = DateTime.UtcNow;
        public int MetaVersion { get; set; } = 1;

        public bool IsFeatured { get; set; } = false;
        public bool IsRecommended { get; set; } = false;
        public int SortOrder { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public double Size { get; set; }

        public int Bedrooms { get; set; } = 0;
        public int Bathrooms { get; set; } = 0;
        public int? Floor { get; set; }
        public int? TotalFloors { get; set; }
        public bool IsFurnished { get; set; } = false;
        public PropertyView View { get; set; } = PropertyView.Unknown;

        public List<string> Features { get; set; } = new();
        public List<string> FeaturesAr { get; set; } = new();
        public ICollection<PropertyFeature> PropertyFeatures { get; set; } = new List<PropertyFeature>();

        public int? ContactId { get; set; }
        public ContactInfo? Contact { get; set; }

        public ICollection<PropertyImage>? Images { get; set; }
        public ICollection<PropertyVideo>? Videos { get; set; }
        public ICollection<PropertyInstallmentPlan>? Installments { get; set; } = new List<PropertyInstallmentPlan>();
        public RentalDetails? RentalDetails { get; set; }

        [MaxLength(200)]
        public string? DeliveryText { get; set; }
        [MaxLength(200)]
        public string? DeliveryTextAr { get; set; }
        public ConstructionStatus? ConstructionStatus { get; set; }
        [MaxLength(50)]
        public string? AvailabilityStatus { get; set; } = "Available";
        public OwnershipType? OwnershipType { get; set; }
        public int InquiryCount { get; set; } = 0;
        public int FavoriteCount { get; set; } = 0;
        [MaxLength(1000)]
        public string? VirtualTourUrl { get; set; }
        public List<string>? HighlightsAr { get; set; }
        public List<string>? NearbyPlaces { get; set; }
        public List<string>? NearbyPlacesAr { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public int ViewCount { get; set; } = 0;
        public int ContactClickCount { get; set; } = 0;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public override bool Equals(object? obj)
        {
            if (obj is not Property other) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id != 0 && Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
