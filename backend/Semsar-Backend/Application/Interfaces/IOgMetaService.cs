namespace Application.Interfaces
{
    public class OgMeta
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Image { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Type { get; set; } = "website";
    }

    public interface IOgMetaService
    {
        OgMeta BuildPropertyOgMeta(
            string? titleEn,
            string? titleAr,
            string? descriptionEn,
            string? seoDescription,
            string? canonicalUrl,
            List<string>? images,
            string lang = "en");

        OgMeta BuildProjectOgMeta(
            string? nameEn,
            string? nameAr,
            string? descriptionEn,
            string? seoDescription,
            string? canonicalUrl,
            List<string>? images,
            string lang = "en");
    }
}
