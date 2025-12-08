using System.Text.Json;
using Application.Interfaces;

namespace Application.Services;

public class LocationSeoService : ILocationSeoService
{
    private readonly Dictionary<string, (double Lat, double Lng)> _locationCoordinates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cairo"] = (30.0444, 31.2357),
        ["new cairo"] = (30.0169, 31.4706),
        ["el gouna"] = (27.3941, 33.6782),
        ["hurghada"] = (27.2574, 33.8116),
        ["north coast"] = (30.8508, 29.0316),
        ["alexandria"] = (31.2001, 29.9187),
        ["new administrative capital"] = (30.0083, 31.7167),
        ["sheikh zayed"] = (30.0185, 31.0015),
        ["6th october"] = (29.9361, 30.9197),
        ["rehab"] = (30.0917, 31.5767),
        ["madinty"] = (30.0894, 31.6228)
    };

    public Task<LocationSeoData> GenerateLocationSeoAsync(string location, string? propertyType = null)
    {
        var cityTitle = location.TitleCase();
        var cityAr = ArabicCityName(location);
        var relevance = CalculateLocationRelevance(location, propertyType ?? "");

        var data = new LocationSeoData
        {
            Location = location,
            TitleEn = $"{cityTitle} Real Estate — Properties for Sale & Rent",
            TitleAr = $"العقارات في {cityAr} — فلل وشقق للبيع والإيجار",
            DescriptionEn = $"Discover the best real estate properties in {cityTitle}. Find villas, apartments, and more.",
            DescriptionAr = $"اكتشف أفضل العقارات في {cityAr}. فيلا، شقة، وشاليه للبيع والإيجار.",
            H1En = $"Properties in {cityTitle}",
            H1Ar = $"العقارات في {cityAr}",
            PrimaryKeyword = $"real estate in {location}",
            SecondaryKeywords = new List<string>
            {
                $"properties in {location}",
                $"{location} real estate",
                $"buy property in {location}",
                $"{location} apartments",
                $"{location} villas"
            },
            LongTailKeywords = new List<string>
            {
                $"best real estate deals in {location}",
                $"luxury properties in {location} for sale",
                $"affordable apartments in {location}"
            },
            LocationJsonLd = BuildLocationJsonLd(location),
            RelevanceScore = relevance
        };

        if (!string.IsNullOrWhiteSpace(propertyType))
        {
            data.TitleEn = $"{propertyType.TitleCase()}s in {cityTitle} — Buy & Sell";
            data.TitleAr = $"{ArabicPropertyType(propertyType)} في {cityAr} — بيع وشراء";
            data.H1En = $"{propertyType.TitleCase()}s for Sale in {cityTitle}";
            data.H1Ar = $"{ArabicPropertyType(propertyType)} للبيع في {cityAr}";
            data.PrimaryKeyword = $"{propertyType}s in {location}";
        }

        return Task.FromResult(data);
    }

    public double CalculateLocationRelevance(string location, string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(location) || string.IsNullOrWhiteSpace(searchQuery))
            return 0.5;

        var loc = location.ToLowerInvariant();
        var query = searchQuery.ToLowerInvariant();

        if (query.Contains(loc)) return 1.0;
        if (loc.Contains(query)) return 0.9;

        var locParts = loc.Split(new[] { ',', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
        var queryParts = query.Split(new[] { ',', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
        var common = locParts.Intersect(queryParts).Count();
        var union = locParts.Union(queryParts).Count();

        return union > 0 ? (double)common / union : 0.5;
    }

    public List<string> GetRelatedLocations(string location, int maxCount = 5)
    {
        var allLocations = _locationCoordinates.Keys.ToList();
        var loc = location.ToLowerInvariant();

        var related = allLocations
            .Where(l => l != loc)
            .Select(l => new
            {
                Name = l,
                Distance = Math.Abs(
                    (_locationCoordinates.GetValueOrDefault(loc).Lat - _locationCoordinates[l].Lat) +
                    (_locationCoordinates.GetValueOrDefault(loc).Lng - _locationCoordinates[l].Lng))
            })
            .OrderBy(x => x.Distance)
            .Take(maxCount)
            .Select(x => x.Name.TitleCase())
            .ToList();

        return related;
    }

    public string BuildLocationJsonLd(string location, double latitude = 0, double longitude = 0)
    {
        try
        {
            var coords = _locationCoordinates.GetValueOrDefault(location.ToLowerInvariant());
            if (coords == default && latitude == 0 && longitude == 0)
                return string.Empty;

            var lat = latitude != 0 ? latitude : coords.Lat;
            var lng = longitude != 0 ? longitude : coords.Lng;

            var obj = new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@type"] = "Place",
                ["name"] = location.TitleCase(),
                ["address"] = new Dictionary<string, object?>
                {
                    ["@type"] = "PostalAddress",
                    ["addressLocality"] = location.TitleCase(),
                    ["addressCountry"] = "EG"
                },
                ["geo"] = new Dictionary<string, object?>
                {
                    ["@type"] = "GeoCoordinates",
                    ["latitude"] = lat,
                    ["longitude"] = lng
                }
            };

            return JsonSerializer.Serialize(obj);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ArabicCityName(string city)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["el gouna"] = "الجونة",
            ["gouna"] = "الجونة",
            ["hurghada"] = "الغردقة",
            ["cairo"] = "القاهرة",
            ["new cairo"] = "القاهرة الجديدة",
            ["north coast"] = "الساحل الشمالي",
            ["sahel"] = "الساحل الشمالي",
            ["alexandria"] = "الإسكندرية",
            ["new administrative capital"] = "العاصمة الإدارية الجديدة",
            ["sheikh zayed"] = "الشيخ زايد",
            ["6th october"] = "6 أكتوبر",
            ["rehab"] = "الرحاب",
            ["madinty"] = "مدينتي"
        };
        return map.TryGetValue(city, out var ar) ? ar : city;
    }

    private static string ArabicPropertyType(string? propertyType)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["villa"] = "فيلا",
            ["apartment"] = "شقة",
            ["chalet"] = "شاليه",
            ["penthouse"] = "بنتهاوس",
            ["twinhouse"] = "تويين هاوس",
            ["townhouse"] = "تاون هاوس",
            ["duplex"] = "دوبلكس",
            ["studio"] = "استوديو",
            ["land"] = "أرض"
        };
        return map.TryGetValue(propertyType ?? "", out var ar) ? ar : propertyType ?? "عقار";
    }
}
