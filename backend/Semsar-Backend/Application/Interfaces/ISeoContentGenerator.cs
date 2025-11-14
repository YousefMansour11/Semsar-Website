namespace Application.Interfaces;

public class SeoContentResult
{
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;

    public string H1En { get; set; } = string.Empty;
    public string H1Ar { get; set; } = string.Empty;

    public string PrimaryKeyword { get; set; } = string.Empty;
    public List<string> SecondaryKeywords { get; set; } = new();
    public List<string> LongTailKeywords { get; set; } = new();

    public List<string> H2SectionsEn { get; set; } = new();
    public List<string> H2SectionsAr { get; set; } = new();

    public List<SeoFaqItem> Faqs { get; set; } = new();

    public string Intent { get; set; } = "transactional";
}

public class SeoFaqItem
{
    public string QuestionEn { get; set; } = string.Empty;
    public string QuestionAr { get; set; } = string.Empty;
    public string AnswerEn { get; set; } = string.Empty;
    public string AnswerAr { get; set; } = string.Empty;
}

public enum SeoEntityType
{
    Property,
    Unit,
    Project,
    Location
}

public interface ISeoContentGenerator
{
    SeoContentResult Generate(
        SeoEntityType entityType,
        string? titleEn,
        string? titleAr,
        string? descriptionEn,
        string? descriptionAr,
        string? location,
        string? propertyType,
        string? listingType,
        decimal price,
        string? currency,
        List<string>? features,
        string? developer = null,
        string? projectName = null);
}
