using Application.Interfaces;
using Application.Settings;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace Application.Services
{
    public class CanonicalService : ICanonicalService
    {
        private readonly AppSettings _settings;

        public CanonicalService(IOptions<AppSettings> settings)
        {
            _settings = settings.Value;
        }

        public string BuildCanonical(string entityType, string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return string.Empty;

            var relativePath = $"/{entityType}/{slug}".Replace("//", "/");
            var baseUrl = ResolveBaseUrl();

            if (string.IsNullOrWhiteSpace(baseUrl))
                return relativePath;

            var cleanedBase = baseUrl.TrimEnd('/');
            var combined = $"{cleanedBase}{relativePath}";
            return combined;
        }

        public List<HreflangTag> BuildHreflangTags(string entityType, string slugEn, string? slugAr, string? titleAr, string? location)
        {
            var tags = new List<HreflangTag>();
            var baseUrl = ResolveBaseUrl();

            if (!string.IsNullOrWhiteSpace(slugEn))
            {
                tags.Add(new HreflangTag
                {
                    HrefLang = "en",
                    Href = $"{baseUrl.TrimEnd('/')}/{entityType}/{slugEn}"
                });
            }

            if (!string.IsNullOrWhiteSpace(slugAr))
            {
                tags.Add(new HreflangTag
                {
                    HrefLang = "ar",
                    Href = $"{baseUrl.TrimEnd('/')}/{entityType}/{slugAr}"
                });
            }

            if (!string.IsNullOrWhiteSpace(slugEn))
            {
                tags.Add(new HreflangTag
                {
                    HrefLang = "x-default",
                    Href = $"{baseUrl.TrimEnd('/')}/{entityType}/{slugEn}"
                });
            }

            return tags;
        }

        private string ResolveBaseUrl()
        {
            var baseUrl = _settings?.BaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = Environment.GetEnvironmentVariable("APP_BASE_URL");
            }
            return baseUrl ?? string.Empty;
        }
    }
}
