using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs
{
    public class PatchPropertyDto
    {
        public string? TitleEn { get; set; }
        public string? TitleAr { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public decimal? Price { get; set; }
        public decimal? RentPerMonth { get; set; }
        public string? Location { get; set; }
        public string? LocationAr { get; set; }
        public int? GovernorateId { get; set; }
        public int? CityId { get; set; }
        public int? AreaId { get; set; }
        public PropertyType? PropertyType { get; set; }
        public PropertyListingType? ListingType { get; set; }
        public double? Size { get; set; }
        public bool? IsFeatured { get; set; }
        public List<string>? Features { get; set; }
        public List<string>? FeaturesAr { get; set; }
        public List<CreateInstallmentDto>? Installments { get; set; }
        // Real estate details
        public int? Bedrooms { get; set; }
        public int? Bathrooms { get; set; }
        public int? Floor { get; set; }
        public int? TotalFloors { get; set; }
        public bool? IsFurnished { get; set; }
        public PropertyView? View { get; set; }
        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
        public string? SeoKeywords { get; set; }
        public string? SeoTitleAr { get; set; }
        public string? SeoDescriptionAr { get; set; }
        public string? SeoKeywordsAr { get; set; }
        public bool? SlugRegenerateRequested { get; set; }
        public int? SortOrder { get; set; }
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

        public CreateContactInfoDto? Contact { get; set; }
    }
}
