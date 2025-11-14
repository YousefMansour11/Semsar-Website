namespace Application.DTOs
{
    public class PropertyCardDto
    {
        public int Id { get; set; }
        public string? PublicKey { get; set; }
        public string TitleEn { get; set; } = string.Empty;
        public string? TitleAr { get; set; }
        public decimal Price { get; set; }
        public decimal? RentPerMonth { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? LocationAr { get; set; }
        public double Size { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsRecommended { get; set; }
        public string? PropertyType { get; set; }
        public string? ListingType { get; set; }
        public string? Slug { get; set; }
        public string? Image { get; set; }
        public int SortOrder { get; set; }
        public InstallmentDto? Installment { get; set; }
        public int ViewCount { get; set; }
        public string? AvailabilityStatus { get; set; }
        public string? DeliveryText { get; set; }
        public string? DeliveryTextAr { get; set; }
    }
}
