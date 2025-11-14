using System.Collections.Generic;

namespace Application.DTOs
{
    public class PropertyFilterResultDto
    {
        public int Id { get; set; }
        public string PublicKey { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string TitleEn { get; set; } = string.Empty;
        public string TitleAr { get; set; } = string.Empty;
        public string DescriptionEn { get; set; } = string.Empty;
        public string DescriptionAr { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? RentPerMonth { get; set; }
        public string Currency { get; set; } = "EGP";
        public string PropertyType { get; set; } = string.Empty;
        public string ListingType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? LocationAr { get; set; }
        public double Size { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsFurnished { get; set; }
        public bool HasInstallment { get; set; }
        public string? Image { get; set; }
        public List<string> Images { get; set; } = new();
        public List<string> Features { get; set; } = new();
        public List<string> FeaturesAr { get; set; } = new();
        public string? Code { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<InstallmentDto> Installments { get; set; } = new();
    }

    public class PropertyFilterResponseDto
    {
        public List<PropertyFilterResultDto> Data { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
