using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class JsonLdService : IJsonLdService
    {
        private readonly ILogger<JsonLdService>? _logger;

        public JsonLdService(ILogger<JsonLdService>? logger = null)
        {
            _logger = logger;
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public string BuildPropertyJsonLd(
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
            double? longitude = null)
        {
            try
            {
                var displayPrice = IsRental(listingType) && rentPerMonth.HasValue && rentPerMonth.Value > 0
                    ? rentPerMonth.Value
                    : price;

                var imageList = images?.Where(u => !string.IsNullOrWhiteSpace(u)).ToList();

                var obj = new Dictionary<string, object?>
                {
                    ["@context"] = "https://schema.org",
                    ["@type"] = "RealEstateListing",
                    ["name"] = titleEn ?? string.Empty,
                    ["description"] = Truncate(descriptionSeo ?? descriptionEn ?? string.Empty, 200),
                    ["url"] = !string.IsNullOrWhiteSpace(canonicalUrl) ? canonicalUrl : null,
                    ["sku"] = sku ?? code ?? string.Empty,
                    ["offers"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "Offer",
                        ["price"] = displayPrice.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["priceCurrency"] = currency ?? "EGP",
                        ["availability"] = "https://schema.org/InStock"
                    },
                    ["address"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "PostalAddress",
                        ["addressLocality"] = location ?? string.Empty
                    }
                };

                if (size.HasValue && size.Value > 0)
                {
                    obj["floorSize"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "QuantitativeValue",
                        ["value"] = size.Value,
                        ["unitText"] = "SQM"
                    };
                }

                if (!string.IsNullOrWhiteSpace(propertyType))
                {
                    obj["category"] = propertyType;
                    obj["type"] = propertyType;
                }

                if (latitude.HasValue && longitude.HasValue)
                {
                    obj["geo"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "GeoCoordinates",
                        ["latitude"] = latitude.Value,
                        ["longitude"] = longitude.Value
                    };
                }

                if (imageList != null && imageList.Count > 0)
                    obj["image"] = imageList;

                ValidateSchema(obj, "RealEstateListing");
                return JsonSerializer.Serialize(obj, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to build property JSON-LD");
                return string.Empty;
            }
        }

        public string BuildProjectJsonLd(
            string? nameEn,
            string? descriptionEn,
            string? descriptionSeo,
            string? canonicalUrl,
            string? location,
            string? developer,
            List<string>? images)
        {
            try
            {
                var imageList = images?.Where(u => !string.IsNullOrWhiteSpace(u)).ToList();

                var obj = new Dictionary<string, object?>
                {
                    ["@context"] = "https://schema.org",
                    ["@type"] = "RealEstateListing",
                    ["name"] = nameEn ?? string.Empty,
                    ["description"] = Truncate(descriptionSeo ?? descriptionEn ?? string.Empty, 200),
                    ["url"] = !string.IsNullOrWhiteSpace(canonicalUrl) ? canonicalUrl : null,
                    ["address"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "PostalAddress",
                        ["addressLocality"] = location ?? string.Empty
                    }
                };

                if (!string.IsNullOrWhiteSpace(developer))
                {
                    obj["builder"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "Organization",
                        ["name"] = developer
                    };
                }

                if (imageList != null && imageList.Count > 0)
                    obj["image"] = imageList;

                ValidateSchema(obj, "RealEstateListing");

                return JsonSerializer.Serialize(obj, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to build project JSON-LD");
                return string.Empty;
            }
        }

        public string BuildFaqJsonLd(List<(string QuestionEn, string AnswerEn)> faqs)
        {
            try
            {
                var mainEntity = new List<object>();
                foreach (var faq in faqs)
                {
                    mainEntity.Add(new Dictionary<string, object?>
                    {
                        ["@type"] = "Question",
                        ["name"] = faq.QuestionEn,
                        ["acceptedAnswer"] = new Dictionary<string, object?>
                        {
                            ["@type"] = "Answer",
                            ["text"] = faq.AnswerEn
                        }
                    });
                }

                var obj = new Dictionary<string, object?>
                {
                    ["@context"] = "https://schema.org",
                    ["@type"] = "FAQPage",
                    ["mainEntity"] = mainEntity
                };

                ValidateSchema(obj, "FAQPage");
                return JsonSerializer.Serialize(obj, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to build FAQ JSON-LD");
                return string.Empty;
            }
        }

        public string BuildBreadcrumbJsonLd(List<(string Name, string Url)> items)
        {
            try
            {
                var itemList = new List<object>();
                for (int i = 0; i < items.Count; i++)
                {
                    itemList.Add(new Dictionary<string, object?>
                    {
                        ["@type"] = "ListItem",
                        ["position"] = i + 1,
                        ["name"] = items[i].Name,
                        ["item"] = items[i].Url
                    });
                }

                var obj = new Dictionary<string, object?>
                {
                    ["@context"] = "https://schema.org",
                    ["@type"] = "BreadcrumbList",
                    ["itemListElement"] = itemList
                };

                ValidateSchema(obj, "BreadcrumbList");
                return JsonSerializer.Serialize(obj, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to build breadcrumb JSON-LD");
                return string.Empty;
            }
        }

        private static void ValidateSchema(Dictionary<string, object?> obj, string expectedType)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isDev = string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase)
                || string.Equals(env, "Testing", StringComparison.OrdinalIgnoreCase);

            if (!isDev) return;

            // Only RealEstateListing requires name/description/url;
            // FAQPage and BreadcrumbList have different required fields.
            if (expectedType == "RealEstateListing")
            {
                if (!obj.TryGetValue("name", out var name) || string.IsNullOrWhiteSpace(name?.ToString()))
                    throw new InvalidOperationException($"JSON-LD {expectedType}: 'name' is required");
                if (!obj.TryGetValue("description", out var desc) || string.IsNullOrWhiteSpace(desc?.ToString()))
                    throw new InvalidOperationException($"JSON-LD {expectedType}: 'description' is required");
                if (!obj.TryGetValue("url", out var url) || string.IsNullOrWhiteSpace(url?.ToString()))
                    throw new InvalidOperationException($"JSON-LD {expectedType}: 'url' is required");
            }
        }

        private static bool IsRental(string? listingType)
        {
            return string.Equals(listingType, "Rental", System.StringComparison.OrdinalIgnoreCase);
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
