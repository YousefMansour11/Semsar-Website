using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities
{
    public class Project : ISoftDelete, IHasPublicKey
    {
        public int Id { get; set; }
        public Guid PublicId { get; set; } = Guid.CreateVersion7();
        public string PublicKey { get; set; } = string.Empty;

        // routing
        [MaxLength(450)]
        public string Slug { get; set; } = null!;

        public bool SlugIsAuto { get; set; } = true;

        [MaxLength(5)]
        public string SlugLanguage { get; set; } = "en";

        // persisted SEO
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

        // Names
        [Required]
        [MaxLength(200)]
        public string NameEn { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string NameAr { get; set; } = null!;

        // Description
        [MaxLength(8000)]
        public string DescriptionEn { get; set; } = null!;

        [MaxLength(8000)]
        public string DescriptionAr { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = null!;

        public string? LocationAr { get; set; }

        [Required]
        [MaxLength(200)]
        public string Developer { get; set; } = null!;

        [MaxLength(500)]
        public string? Image { get; set; }

        public List<string> Highlights { get; set; } = new();

        public List<string>? HighlightsAr { get; set; }

        public int UnitCount { get; set; }

        public decimal? StartingPrice { get; set; }

        public List<string>? NearbyPlaces { get; set; }

        public List<string>? NearbyPlacesAr { get; set; }

        public List<PropertyType>? PropertyTypes { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public decimal? TotalArea { get; set; }

        public OwnershipType? OwnershipType { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Relations
        public ProjectDetails? Details { get; set; }
        public ICollection<Unit> Units { get; set; } = new List<Unit>();
        public ICollection<ProjectImage>? Images { get; set; }
        public ICollection<ProjectVideo>? Videos { get; set; }

        [MaxLength(200)]
        public string? DeliveryText { get; set; }
        [MaxLength(200)]
        public string? DeliveryTextAr { get; set; }
        public bool IsRecommended { get; set; } = false;
        public ConstructionStatus? ConstructionStatus { get; set; }
        [MaxLength(50)]
        public string? AvailabilityStatus { get; set; } = "Available";
        public int ViewCount { get; set; } = 0;
        public int InquiryCount { get; set; } = 0;
        public int FavoriteCount { get; set; } = 0;
        [MaxLength(1000)]
        public string? VirtualTourUrl { get; set; }

        // Concurrency token
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Determines whether the specified object is equal to the current object based on Id
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is not Project other) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id != 0 && Id == other.Id;
        }

        /// <summary>
        /// Serves as the default hash function
        /// </summary>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
