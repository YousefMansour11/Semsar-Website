using System.Collections.Generic;

namespace Application.DTOs
{
    public class HreflangTagDto
    {
        public string HrefLang { get; set; } = string.Empty;
        public string Href { get; set; } = string.Empty;
    }

    public class PropertyBaseDto
    {
        public int Id { get; set; }
        public int? ProjectId { get; set; }
        public string? PublicKey { get; set; }
        public string? TitleEn { get; set; }
        public string? TitleAr { get; set; }
        public decimal Price { get; set; }
        public string? Location { get; set; }
        public string? LocationAr { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public double Size { get; set; }
        public decimal? RentPerMonth { get; set; }
        public string Currency { get; set; } = "EGP";
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public int? Floor { get; set; }
        public int? TotalFloors { get; set; }
        public bool IsFurnished { get; set; }
        public string? View { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsRecommended { get; set; }
        public string? PropertyType { get; set; }
        public string? ListingType { get; set; }
        public List<string> Images { get; set; } = new();
        public List<string> Features { get; set; } = new();
        public List<string> FeaturesAr { get; set; } = new();
        public List<string>? HighlightsAr { get; set; }
        public List<string>? NearbyPlaces { get; set; }
        public List<string>? NearbyPlacesAr { get; set; }
        public List<InstallmentDto> Installments { get; set; } = new();
        public int SortOrder { get; set; }
        public string? DeliveryText { get; set; }
        public string? DeliveryTextAr { get; set; }
        public string? ConstructionStatus { get; set; }
        public string? AvailabilityStatus { get; set; }
        public string? OwnershipType { get; set; }
        public int ViewCount { get; set; }
        public int InquiryCount { get; set; }
        public int FavoriteCount { get; set; }
        public string? VirtualTourUrl { get; set; }
        public string? Slug { get; set; }
        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
        public string? SeoTitleAr { get; set; }
        public string? SeoDescriptionAr { get; set; }
        public string? SeoKeywords { get; set; }
        public string? SeoKeywordsAr { get; set; }
        public string? CanonicalUrl { get; set; }
        public string? JsonLd { get; set; }
        public string? FaqJsonLd { get; set; }
        public string? BreadcrumbJsonLd { get; set; }
        public string? InternalLinksJson { get; set; }
        public string? EntityGraphJson { get; set; }
        public List<ImageDto> ImagesMeta { get; set; } = new();
        public List<HreflangTagDto> HreflangTags { get; set; } = new();
        public List<VideoDto> Videos { get; set; } = new();
    }
}
