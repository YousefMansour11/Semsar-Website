namespace Application.Interfaces
{
    public class HreflangTag
    {
        public string HrefLang { get; set; } = string.Empty;
        public string Href { get; set; } = string.Empty;
    }

    public interface ICanonicalService
    {
        string BuildCanonical(string entityType, string slug);
        List<HreflangTag> BuildHreflangTags(string entityType, string slugEn, string? slugAr, string? titleAr, string? location);
    }
}
