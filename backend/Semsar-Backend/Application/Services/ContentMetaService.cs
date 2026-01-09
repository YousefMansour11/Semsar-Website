using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ContentMetaService : IContentMetaService
    {
        private readonly ISlugService _slugService;
        private readonly ISeoService _seoService;
        private readonly ICanonicalService _canonicalService;

        public ContentMetaService(ISlugService slugService, ISeoService seoService, ICanonicalService canonicalService)
        {
            _slugService = slugService;
            _seoService = seoService;
            _canonicalService = canonicalService;
        }

        public async Task<ContentMetaResult> GenerateAsync(string entityType, string? titleEn, string? titleAr, string? descriptionEn, string? descriptionAr, string? location)
        {
            var result = new ContentMetaResult();
            // Choose slug base using Arabic detection; SlugService provides Slugify/Normalize
            // Use slug service only to produce a base slug (not persisted)
            // Enforce English-only slug generation per product rules
            var baseText = !string.IsNullOrWhiteSpace(titleEn) ? titleEn : (titleAr ?? string.Empty);
            var input = (baseText ?? string.Empty) + " " + (location ?? string.Empty);
            var baseSlug = _slugService.Slugify(input.Trim(), "en");
            if (string.IsNullOrWhiteSpace(baseSlug)) baseSlug = _slugService.NormalizeSlug(entityType + "-" + ShortHash());

            result.BaseSlug = baseSlug;
            result.SlugLanguage = "en";
            result.MetaGeneratedAt = DateTime.UtcNow;
            result.MetaVersion = 1;

            // Delegate SEO generation to ISeoService
            var seo = await _seoService.GenerateSeoAsync(titleEn, titleAr, descriptionEn, descriptionAr, location);
            result.SeoTitleEn = seo.TitleEn;
            result.SeoTitleAr = seo.TitleAr;
            result.SeoDescriptionEn = seo.DescriptionEn;
            result.SeoDescriptionAr = seo.DescriptionAr;
            result.SeoKeywordsEn = seo.KeywordsEn;
            result.SeoKeywordsAr = seo.KeywordsAr;

            // canonical left empty until slug persisted
            result.CanonicalUrl = string.Empty;
            return result;
        }

        public async Task<ContentMetaResult> GenerateMeta(
            string entityType,
            Func<string, ContentMetaResult, Task> assignFunc,
            Func<Task> commitFunc,
            params string?[] inputs)
        {
            // inputs: titleEn, titleAr, descriptionEn, descriptionAr, location (optional)
            string? titleEn = inputs.Length > 0 ? inputs[0] : null;
            string? titleAr = inputs.Length > 1 ? inputs[1] : null;
            string? descriptionEn = inputs.Length > 2 ? inputs[2] : null;
            string? descriptionAr = inputs.Length > 3 ? inputs[3] : null;
            string? location = inputs.Length > 4 ? inputs[4] : null;

            var meta = await GenerateAsync(entityType, titleEn, titleAr, descriptionEn, descriptionAr, location);

            var candidate = _slugService.GenerateCandidateSlug(titleEn ?? string.Empty, titleAr ?? string.Empty, location ?? string.Empty);
            if (string.IsNullOrWhiteSpace(candidate))
                throw new InvalidOperationException("Generated slug candidate is empty");

            meta.BaseSlug = candidate;
            meta.SlugLanguage = SeoUtils.ContainsArabic(titleAr) ? "ar" : "en";
            var canonical = _canonicalService.BuildCanonical(entityType, candidate);
            if (string.IsNullOrWhiteSpace(canonical))
                throw new InvalidOperationException("Generated canonical is empty");

            meta.CanonicalUrl = canonical;

            await assignFunc(candidate, meta);

            // commitFunc is provided as part of signature but commit must only be performed by service layer; do not call it here
            return meta;
        }

        private static string ShortHash()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 6);
        }
    }
}
