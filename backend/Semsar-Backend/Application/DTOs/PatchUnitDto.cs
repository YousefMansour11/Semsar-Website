using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs
{
    public class PatchUnitDto
    {
        public string? TitleEn { get; set; }
        public string? TitleAr { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public decimal? RentPerMonth { get; set; }
        public string? Location { get; set; }
        public string? LocationAr { get; set; }
        public PropertyType? PropertyType { get; set; }
        public PropertyListingType? ListingType { get; set; }
        public double? MinArea { get; set; }
        public double? MaxArea { get; set; }
        public bool? IsFeatured { get; set; }
        public List<string>? Features { get; set; }
        public List<string>? FeaturesAr { get; set; }
        public List<CreateInstallmentDto>? Installments { get; set; }
        public List<CreateUnitVariantDto>? Variants { get; set; }
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
        public bool? HasBalcony { get; set; }
        public bool? HasParking { get; set; }
        public string? Slug { get; set; }
        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
        public string? SeoKeywords { get; set; }
        public string? SeoTitleAr { get; set; }
        public string? SeoDescriptionAr { get; set; }
        public string? SeoKeywordsAr { get; set; }
        public bool? SlugRegenerateRequested { get; set; }
        public int? ProjectId { get; set; }

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
