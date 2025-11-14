using System.Collections.Generic;

namespace Application.DTOs
{
    public class PropertyCreatedResponse
    {
        public int Id { get; set; }
        public string TitleEn { get; set; } = string.Empty;
        public string TitleAr { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? RentPerMonth { get; set; }
        public string? Code { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string SeoTitle { get; set; } = string.Empty;
        public string SeoDescription { get; set; } = string.Empty;
        public string SeoTitleAr { get; set; } = string.Empty;
        public string SeoDescriptionAr { get; set; } = string.Empty;
        public string SeoKeywords { get; set; } = string.Empty;
        public string SeoKeywordsAr { get; set; } = string.Empty;
        public string CanonicalUrl { get; set; } = string.Empty;
    }

    public class PropertyUpdatedResponse
    {
        public int Id { get; set; }
        public string TitleEn { get; set; } = string.Empty;
        public string TitleAr { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? RentPerMonth { get; set; }
        public string? Code { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string SeoTitle { get; set; } = string.Empty;
        public string SeoDescription { get; set; } = string.Empty;
        public string SeoTitleAr { get; set; } = string.Empty;
        public string SeoDescriptionAr { get; set; } = string.Empty;
        public string SeoKeywords { get; set; } = string.Empty;
        public string SeoKeywordsAr { get; set; } = string.Empty;
        public string CanonicalUrl { get; set; } = string.Empty;
    }
}
