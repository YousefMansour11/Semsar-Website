using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs
{
    public class CreatePropertyDto
    {
        [Required]
        public string TitleEn { get; set; } = null!;

        [Required]
        public string TitleAr { get; set; } = null!;

        [Required]
        public string DescriptionEn { get; set; } = null!;

        [Required]
        public string DescriptionAr { get; set; } = null!;

        public decimal Price { get; set; }

        public decimal? RentPerMonth { get; set; }

        public string Location { get; set; } = null!;

        public string? LocationAr { get; set; }

        public int? GovernorateId { get; set; }
        public int? CityId { get; set; }
        public int? AreaId { get; set; }

        [Required]
        public PropertyType PropertyType { get; set; }

        [Required]
        public PropertyListingType ListingType { get; set; }

        public double Size { get; set; }
        public bool IsFeatured { get; set; } = false;

        // Real estate details
        public int? Bedrooms { get; set; }
        public int? Bathrooms { get; set; }
        public int? Floor { get; set; }
        public int? TotalFloors { get; set; }
        public bool? IsFurnished { get; set; }
        public PropertyView? View { get; set; }

        public List<string>? Features { get; set; }
        public List<string>? FeaturesAr { get; set; }

        public List<CreateInstallmentDto>? Installments { get; set; }

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
    }
}
