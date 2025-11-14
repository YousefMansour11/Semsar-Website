namespace Application.Interfaces
{
    public interface IJsonLdService
    {
        string BuildPropertyJsonLd(
            string? titleEn,
            string? descriptionEn,
            string? descriptionSeo,
            string? canonicalUrl,
            string? code,
            string? location,
            string? currency,
            string? listingType,
            decimal price,
            decimal? rentPerMonth,
            List<string>? images,
            string? sku = null,
            double? size = null,
            string? propertyType = null,
            double? latitude = null,
            double? longitude = null);

        string BuildProjectJsonLd(
            string? nameEn,
            string? descriptionEn,
            string? descriptionSeo,
            string? canonicalUrl,
            string? location,
            string? developer,
            List<string>? images);

        string BuildFaqJsonLd(List<(string QuestionEn, string AnswerEn)> faqs);

        string BuildBreadcrumbJsonLd(List<(string Name, string Url)> items);
    }
}
