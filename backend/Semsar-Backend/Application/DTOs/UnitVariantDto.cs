namespace Application.DTOs
{
    public class UnitVariantDto
    {
        public int Id { get; set; }
        public string? PublicKey { get; set; }
        public string Name { get; set; } = null!;
        public string? NameAr { get; set; }
        public double Size { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "EGP";
        public decimal? RentPerMonth { get; set; }
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
        public string? FloorPlanUrl { get; set; }
        public string? AvailabilityStatus { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsRecommended { get; set; }
        public int ViewCount { get; set; }
        public int InquiryCount { get; set; }
        public int FavoriteCount { get; set; }
        public string? DeliveryText { get; set; }
        public string? DeliveryTextAr { get; set; }
    }

    public class CreateUnitVariantDto
    {
        public string Name { get; set; } = null!;
        public string? NameAr { get; set; }
        public double Size { get; set; }
        public decimal Price { get; set; }
        public string? Currency { get; set; }
        public decimal? RentPerMonth { get; set; }
        public int Bedrooms { get; set; } = 0;
        public int Bathrooms { get; set; } = 0;
        public int? Floor { get; set; }
        public bool IsFurnished { get; set; } = false;
        public string? View { get; set; }
        public string? UnitNumber { get; set; }
        public string? BuildingNumber { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? FinishingType { get; set; }
        public bool HasBalcony { get; set; } = false;
        public bool HasParking { get; set; } = false;
        public string? FloorPlanUrl { get; set; }
        public string? AvailabilityStatus { get; set; }
        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public bool? IsFeatured { get; set; }
        public bool? IsRecommended { get; set; }
        public string? DeliveryText { get; set; }
        public string? DeliveryTextAr { get; set; }
    }

    public class UpdateUnitVariantDto
    {
        public string? Name { get; set; }
        public string? NameAr { get; set; }
        public double? Size { get; set; }
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public decimal? RentPerMonth { get; set; }
        public int? Bedrooms { get; set; }
        public int? Bathrooms { get; set; }
        public int? Floor { get; set; }
        public bool? IsFurnished { get; set; }
        public string? View { get; set; }
        public string? UnitNumber { get; set; }
        public string? BuildingNumber { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? FinishingType { get; set; }
        public bool? HasBalcony { get; set; }
        public bool? HasParking { get; set; }
        public string? FloorPlanUrl { get; set; }
        public string? AvailabilityStatus { get; set; }
        public int? SortOrder { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsFeatured { get; set; }
        public bool? IsRecommended { get; set; }
        public string? DeliveryText { get; set; }
        public string? DeliveryTextAr { get; set; }
    }
}
