using System.Text.Json;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class SeoValidationGate : ISeoValidationGate
{
    private readonly ILogger<SeoValidationGate>? _logger;

    public SeoValidationGate(ILogger<SeoValidationGate>? logger = null)
    {
        _logger = logger;
    }

    public SeoValidationResult ValidatePropertySeo(
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
        decimal price)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(seoTitle))
            errors.Add("SeoTitle is empty");

        if (string.IsNullOrWhiteSpace(seoDescription))
            errors.Add("SeoDescription is empty");

        if (string.IsNullOrWhiteSpace(canonicalUrl))
            errors.Add("CanonicalUrl is empty");
        else if (!canonicalUrl.StartsWith("http://") && !canonicalUrl.StartsWith("https://"))
            errors.Add("CanonicalUrl is not a valid absolute URL");

        if (!string.IsNullOrWhiteSpace(propertyType))
        {
            var validTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "residential", "commercial", "land", "industrial", "mixeduse", "other",
                "villa", "apartment", "chalet", "penthouse", "twinhouse", "townhouse",
                "duplex", "studio", "office", "commercial unit"
            };
            if (!validTypes.Contains(propertyType.Trim()))
                errors.Add($"PropertyType '{propertyType}' is not a recognized type");
        }

        if (!string.IsNullOrWhiteSpace(faqJsonLd))
        {
            try
            {
                using var doc = JsonDocument.Parse(faqJsonLd);
                var root = doc.RootElement;
                if (!root.TryGetProperty("@type", out var type) || type.GetString() != "FAQPage")
                    errors.Add("faqJsonLd is not a valid FAQPage schema");
                if (!root.TryGetProperty("mainEntity", out _))
                    errors.Add("faqJsonLd missing mainEntity");
            }
            catch
            {
                errors.Add("faqJsonLd is not valid JSON");
            }
        }

        if (!string.IsNullOrWhiteSpace(breadcrumbJsonLd))
        {
            try
            {
                using var doc = JsonDocument.Parse(breadcrumbJsonLd);
                var root = doc.RootElement;
                if (!root.TryGetProperty("@type", out var type) || type.GetString() != "BreadcrumbList")
                    errors.Add("breadcrumbJsonLd is not a valid BreadcrumbList schema");
                if (!root.TryGetProperty("itemListElement", out var items) || items.GetArrayLength() < 2)
                    errors.Add("breadcrumbJsonLd must have at least 2 items");
            }
            catch
            {
                errors.Add("breadcrumbJsonLd is not valid JSON");
            }
        }

        if (!string.IsNullOrWhiteSpace(entityGraphJson))
        {
            try
            {
                JsonDocument.Parse(entityGraphJson);
            }
            catch
            {
                errors.Add("entityGraphJson is not valid JSON");
            }
        }

        if (!string.IsNullOrWhiteSpace(internalLinksJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(internalLinksJson);
                var groups = doc.RootElement.EnumerateArray().ToList();
                var totalLinks = groups.Sum(g =>
                {
                    if (g.TryGetProperty("links", out var links))
                        return links.GetArrayLength();
                    return 0;
                });

                if (totalLinks < 3)
                    errors.Add($"Internal links count ({totalLinks}) is below minimum (3)");
                if (totalLinks > 8)
                    errors.Add($"Internal links count ({totalLinks}) exceeds maximum (8)");

                var linkTypes = groups.SelectMany(g =>
                {
                    if (g.TryGetProperty("links", out var links))
                        return links.EnumerateArray().Select(l =>
                            l.TryGetProperty("type", out var t) ? t.GetString() : null);
                    return Enumerable.Empty<string?>();
                }).Where(t => t != null).Cast<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (!linkTypes.Contains("location"))
                    errors.Add("Internal links missing required 'location' type");
                if (!linkTypes.Contains("guide"))
                    errors.Add("Internal links missing required 'guide' type");
            }
            catch
            {
                errors.Add("internalLinksJson is not valid JSON");
            }
        }

        var result = new SeoValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };

        if (errors.Count > 0)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase))
            {
                var message = "SEO Validation failed:\n  - " + string.Join("\n  - ", errors);
                _logger?.LogError(message);
            }
            else
            {
                _logger?.LogWarning("SEO Validation issues (non-blocking in production): {Errors}", string.Join("; ", errors));
            }
        }

        return result;
    }
}
