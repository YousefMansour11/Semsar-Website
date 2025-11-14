namespace Application.Interfaces;

public class SerpVariant
{
    public string VariantId { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string H1En { get; set; } = string.Empty;
    public string H1Ar { get; set; } = string.Empty;
    public string PrimaryKeyword { get; set; } = string.Empty;
    public int PredictedCtrScore { get; set; }
}

public class SerpVariantRequest
{
    public SeoEntityType EntityType { get; set; }
    public string? TitleEn { get; set; }
    public string? TitleAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string? Location { get; set; }
    public string? PropertyType { get; set; }
    public string? ListingType { get; set; }
    public decimal Price { get; set; }
    public string? Currency { get; set; }
    public List<string>? Features { get; set; }
    public string? Intent { get; set; }
}

public interface ISERPVariantGenerator
{
    List<SerpVariant> GenerateVariants(SerpVariantRequest request);
    SerpVariant SelectBestVariant(List<SerpVariant> variants, string? deviceType = null, string? userLocation = null);
}
