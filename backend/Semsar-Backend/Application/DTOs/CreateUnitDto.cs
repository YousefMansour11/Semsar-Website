using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs
{
    public class CreateUnitDto
    {
        [Required]
        public string TitleEn { get; set; } = null!;

        [Required]
        public string TitleAr { get; set; } = null!;

        [Required]
        public string DescriptionEn { get; set; } = null!;

        [Required]
        public string DescriptionAr { get; set; } = null!;

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public decimal? RentPerMonth { get; set; }

        [Required]
        public string Location { get; set; } = null!;

        public string? LocationAr { get; set; }

        [Required]
        public PropertyType PropertyType { get; set; }

        [Required]
        public PropertyListingType ListingType { get; set; }

        public double? MinArea { get; set; }

        public double? MaxArea { get; set; }

        public bool IsFeatured { get; set; } = false;

        // Real estate details
        public int? Bedrooms { get; set; }
        public int? Bathrooms { get; set; }
        public int? Floor { get; set; }
        public bool? IsFurnished { get; set; }
        public PropertyView? View { get; set; }

        // Unit-specific
        public string? UnitNumber { get; set; }
        public string? BuildingNumber { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public FinishingType? FinishingType { get; set; }
        public bool HasBalcony { get; set; } = false;
        public bool HasParking { get; set; } = false;

        public List<string>? Features { get; set; }
        public List<string>? FeaturesAr { get; set; }

        public List<CreateInstallmentDto>? Installments { get; set; }
        public List<CreateUnitVariantDto>? Variants { get; set; }

        public CreateContactInfoDto? Contact { get; set; }

        public bool? IsRecommended { get; set; }
        public string? DeliveryText { get; set; }
        public string? DeliveryTextAr { get; set; }
        public ConstructionStatus? ConstructionStatus { get; set; }
        public string? AvailabilityStatus { get; set; }
        public OwnershipType? OwnershipType { get; set; }
        public string? VirtualTourUrl { get; set; }
        public List<string>? HighlightsAr { get; set; }
        public List<string>? NearbyPlaces { get; set; }
        public List<string>? NearbyPlacesAr { get; set; }

        [Required]
        public int ProjectId { get; set; }

        public string? Slug { get; set; }
        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
        public string? SeoKeywords { get; set; }
        public string? SeoTitleAr { get; set; }
        public string? SeoDescriptionAr { get; set; }
        public string? SeoKeywordsAr { get; set; }
    }
}
