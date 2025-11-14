using System.Collections.Generic;

namespace Application.DTOs
{
    public class PropertyListDto
    {
        public int Id { get; set; }
        public int? ProjectId { get; set; }
        public string? PublicKey { get; set; }
        public string Title { get; set; } = null!;
        public string TitleAr { get; set; } = null!;
        public string Type { get; set; } = null!;
        public decimal Price { get; set; }
        public decimal? RentPerMonth { get; set; } = null;
        public string Location { get; set; } = null!;
        public string? LocationAr { get; set; }
        // frontend expects propertyCode
        public string PropertyCode { get; set; } = null!;
        public string Image { get; set; } = null!;
        // All images for gallery
        public List<string> Images { get; set; } = new();
        public double Size { get; set; }
        public string Status { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string DescriptionAr { get; set; } = null!;
        public string Currency { get; set; } = "EGP";
        public List<string> Features { get; set; } = new();
        public List<string> FeaturesAr { get; set; } = new();
        public string ListingType { get; set; } = null!;
        public bool IsFeatured { get; set; }
        public int SortOrder { get; set; }
        public InstallmentDto? Installment { get; set; }
    }
}
