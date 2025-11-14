using System.Collections.Generic;

namespace Application.DTOs
{
    public class UnitPublicDto
    {
        public int Id { get; set; }
        public string? PublicKey { get; set; }
        public string Code { get; set; } = null!;
        public string TitleEn { get; set; } = null!;
        public string TitleAr { get; set; } = null!;
        public string DescriptionEn { get; set; } = null!;
        public string DescriptionAr { get; set; } = null!;
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public double? MinArea { get; set; }
        public double? MaxArea { get; set; }
        public string? Location { get; set; }
        public string? LocationAr { get; set; }
        public string Currency { get; set; } = "EGP";
        public decimal? RentPerMonth { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsRecommended { get; set; }
        public string? PropertyType { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public int? Floor { get; set; }
        public bool IsFurnished { get; set; }
        public string? View { get; set; }
        public string? UnitNumber { get; set; }
        public string? BuildingNumber { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? FinishingType { get; set; }
        public bool HasBalcony { get; set; }
        public bool HasParking { get; set; }
        public string? ListingType { get; set; }
        public List<string> Features { get; set; } = new();
        public List<string> FeaturesAr { get; set; } = new();
        public List<string>? HighlightsAr { get; set; }
        public List<string>? NearbyPlaces { get; set; }
        public List<string>? NearbyPlacesAr { get; set; }
        public List<string> Images { get; set; } = new();
        public List<InstallmentDto> Installments { get; set; } = new();
        public string? DeliveryText { get; set; }
        public string? DeliveryTextAr { get; set; }
        public string? ConstructionStatus { get; set; }
        public string? AvailabilityStatus { get; set; }
        public string? OwnershipType { get; set; }
        public int ViewCount { get; set; }
        public int InquiryCount { get; set; }
        public int FavoriteCount { get; set; }
        public string? VirtualTourUrl { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public string? Slug { get; set; } = null;
        public string? SeoTitle { get; set; } = null;
        public string? SeoDescription { get; set; } = null;
        public string? SeoTitleAr { get; set; } = null;
        public string? SeoDescriptionAr { get; set; } = null;
        public string? SeoKeywords { get; set; } = null;
        public string? SeoKeywordsAr { get; set; } = null;
        public string? CanonicalUrl { get; set; } = null;
        public string? JsonLd { get; set; } = null;
        public string? FaqJsonLd { get; set; }
        public string? BreadcrumbJsonLd { get; set; }
        public string? InternalLinksJson { get; set; }
        public string? EntityGraphJson { get; set; }
        public List<ImageDto> ImagesMeta { get; set; } = new();
        public List<VideoDto> Videos { get; set; } = new();
        public List<UnitVariantDto> Variants { get; set; } = new();
    }
}
