namespace Application.Interfaces;

public interface IInternalLinkingService
{
    List<InternalLinkGroup> GenerateLinks(string? location, string? propertyType, string? listingType, string? slug, string? projectName);
    List<InternalLinkGroup> GetMissingLinks(string? location, string? propertyType, string? listingType, string? slug);
    List<InternalLinkGroup> GetOptimizedLinks(string? location, string? propertyType, string? listingType, string? slug, string? projectName);
    bool MeetsMinimumRequirement(List<InternalLinkGroup> links);
}

public class InternalLink
{
    public string Text { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = "related";
    public string? Language { get; set; }
}

public class InternalLinkGroup
{
    public string SectionTitle { get; set; } = string.Empty;
    public List<InternalLink> Links { get; set; } = new();
}
