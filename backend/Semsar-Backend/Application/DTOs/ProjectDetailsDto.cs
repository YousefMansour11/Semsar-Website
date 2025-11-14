using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs
{
    public class ProjectDetailsDto
    {
        public int Id { get; set; }
        public string? PublicKey { get; set; }
        public string NameEn { get; set; } = null!;
        public string NameAr { get; set; } = null!;
        public string DescriptionEn { get; set; } = null!;
        public string DescriptionAr { get; set; } = null!;
        public string Location { get; set; } = null!;
        public string? LocationAr { get; set; }
        public string Developer { get; set; } = null!;
        public string? Image { get; set; }
        public List<string> Images { get; set; } = new();
        public List<ImageInfoDto> AdminImages { get; set; } = new();
        public List<string> Highlights { get; set; } = new();
        public List<string>? HighlightsAr { get; set; }
        public decimal? StartingPrice { get; set; }
        public List<string>? NearbyPlaces { get; set; }

        public List<string>? NearbyPlacesAr { get; set; }
        public List<PropertyType>? PropertyTypes { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public decimal? TotalArea { get; set; }
        public OwnershipType? OwnershipType { get; set; }
        public int UnitCount { get; set; }
        public string? Slug { get; set; }
        public string? DeliveryText { get; set; }
        public string? DeliveryTextAr { get; set; }
        public bool IsRecommended { get; set; }
        public string? ConstructionStatus { get; set; }
        public string? AvailabilityStatus { get; set; }
        public int ViewCount { get; set; }
        public int InquiryCount { get; set; }
        public int FavoriteCount { get; set; }
        public string? VirtualTourUrl { get; set; }
        public string SeoTitle { get; set; } = string.Empty;
        public string SeoDescription { get; set; } = string.Empty;
        public string SeoTitleAr { get; set; } = string.Empty;
        public string SeoDescriptionAr { get; set; } = string.Empty;
        public string SeoKeywords { get; set; } = string.Empty;
        public string SeoKeywordsAr { get; set; } = string.Empty;
        public string CanonicalUrl { get; set; } = string.Empty;
        public string? JsonLd { get; set; }
        public string? FaqJsonLd { get; set; }
        public string? BreadcrumbJsonLd { get; set; }
        public string? InternalLinksJson { get; set; }
        public string? EntityGraphJson { get; set; }
        public List<VideoDto> Videos { get; set; } = new();
    }
}
