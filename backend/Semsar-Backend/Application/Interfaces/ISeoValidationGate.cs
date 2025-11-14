namespace Application.Interfaces;

public class SeoValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new();
}

public interface ISeoValidationGate
{
    SeoValidationResult ValidatePropertySeo(
        string? seoTitle,
        string? seoDescription,
        string? canonicalUrl,
        string? propertyType,
        string? location,
        string? faqJsonLd,
        string? breadcrumbJsonLd,
        string? entityGraphJson,
        string? internalLinksJson,
        string? listingType,
        decimal price);
}
