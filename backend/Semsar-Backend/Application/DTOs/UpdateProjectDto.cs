using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs
{
    public class UpdateProjectDto
    {
        public string? NameEn { get; set; }
        public string? NameAr { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public string? Location { get; set; }
        public string? LocationAr { get; set; }
        public string? Developer { get; set; }
        public string? Image { get; set; }
        public List<string>? Highlights { get; set; }
        public List<string>? HighlightsAr { get; set; }
        public decimal? StartingPrice { get; set; }
        public List<string>? NearbyPlaces { get; set; }

        public List<string>? NearbyPlacesAr { get; set; }
        public List<PropertyType>? PropertyTypes { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public decimal? TotalArea { get; set; }
        public OwnershipType? OwnershipType { get; set; }
        public int? UnitCount { get; set; }
        public string? Slug { get; set; }
        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
        public string? SeoKeywords { get; set; }
        public string? SeoTitleAr { get; set; }
        public string? SeoDescriptionAr { get; set; }
        public string? SeoKeywordsAr { get; set; }
        public string? CanonicalUrl { get; set; }
        public bool? IsRecommended { get; set; }
        public string? DeliveryText { get; set; }
        public string? DeliveryTextAr { get; set; }
        public ConstructionStatus? ConstructionStatus { get; set; }
        public string? AvailabilityStatus { get; set; }
        public string? VirtualTourUrl { get; set; }
    }
}
