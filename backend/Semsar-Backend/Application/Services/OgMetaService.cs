using Application.Interfaces;

namespace Application.Services
{
    public class OgMetaService : IOgMetaService
    {
        public OgMeta BuildPropertyOgMeta(
            string? titleEn,
            string? titleAr,
            string? descriptionEn,
            string? seoDescription,
            string? canonicalUrl,
            List<string>? images,
            string lang = "en")
        {
            var title = lang == "ar" && !string.IsNullOrWhiteSpace(titleAr)
                ? titleAr
                : (titleEn ?? string.Empty);

            var description = Truncate(
                seoDescription ?? descriptionEn ?? string.Empty, 200);

            return new OgMeta
            {
                Title = title,
                Description = description,
                Image = images?.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u)),
                Url = canonicalUrl ?? string.Empty,
                Type = "website"
            };
        }

        public OgMeta BuildProjectOgMeta(
            string? nameEn,
            string? nameAr,
            string? descriptionEn,
            string? seoDescription,
            string? canonicalUrl,
            List<string>? images,
            string lang = "en")
        {
            var title = lang == "ar" && !string.IsNullOrWhiteSpace(nameAr)
                ? nameAr
                : (nameEn ?? string.Empty);

            var description = Truncate(
                seoDescription ?? descriptionEn ?? string.Empty, 200);

            return new OgMeta
            {
                Title = title,
                Description = description,
                Image = images?.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u)),
                Url = canonicalUrl ?? string.Empty,
                Type = "website"
            };
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}
