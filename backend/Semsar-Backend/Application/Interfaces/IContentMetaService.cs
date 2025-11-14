using System;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public class ContentMetaResult
    {
        public string BaseSlug { get; set; } = string.Empty;
        public string SlugLanguage { get; set; } = "en";

        public string SeoTitleEn { get; set; } = string.Empty;
        public string SeoTitleAr { get; set; } = string.Empty;
        public string SeoDescriptionEn { get; set; } = string.Empty;
        public string SeoDescriptionAr { get; set; } = string.Empty;
        public string SeoKeywordsEn { get; set; } = string.Empty;
        public string SeoKeywordsAr { get; set; } = string.Empty;

        // canonical will be set when slug is finalized
        public string CanonicalUrl { get; set; } = string.Empty;
        // metadata tracking
        public DateTime MetaGeneratedAt { get; set; }
        public int MetaVersion { get; set; } = 1;
    }

    public interface IContentMetaService
    {
        // Generate meta suggestions (slug candidate + seo) without persisting slug uniqueness
        Task<ContentMetaResult> GenerateAsync(string entityType, string? titleEn, string? titleAr, string? descriptionEn, string? descriptionAr, string? location);

        // Generate meta and call assignFunc to apply meta values to the entity. This method does not commit changes.
        Task<ContentMetaResult> GenerateMeta(
            string entityType,
            Func<string, ContentMetaResult, Task> assignFunc,
            Func<Task> commitFunc,
            params string?[] inputs
        );
    }
}
