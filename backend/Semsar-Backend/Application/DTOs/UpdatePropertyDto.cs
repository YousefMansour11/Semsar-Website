using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs
{
    public class UpdatePropertyDto
    {
        public string? TitleEn { get; set; }
        public string? TitleAr { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public decimal? Price { get; set; }
        public decimal? RentPerMonth { get; set; }
        public string? Location { get; set; }
        public string? LocationAr { get; set; }
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

        // Images handled via multipart/form-data endpoints; not part of DTO

        // SEO (slug controlled server-side)
        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
        public string? SeoKeywords { get; set; }
        // Allow admin to request slug regeneration explicitly (boolean). Default false.
        public bool? SlugRegenerateRequested { get; set; }
    }
}
