using Application.Interfaces;

namespace Application.Services;

public class SERPVariantGenerator : ISERPVariantGenerator
{
    public List<SerpVariant> GenerateVariants(SerpVariantRequest request)
    {
        var variants = new List<SerpVariant>();

        var entity = ExtractEntityInfo(request);
        var verb = IsRental(request.ListingType) ? "for Rent" : "for Sale";

        variants.Add(new SerpVariant
        {
            VariantId = "default",
            TitleEn = $"{entity.PropertyTypeTitle} in {entity.City} {verb}",
            TitleAr = BuildArabicTitle(entity, verb),
            DescriptionEn = $"Looking to {verb.ToLower()} a {entity.Lifestyle} {entity.PropertyType} in {entity.City}? Browse our selection.",
            DescriptionAr = $"ابحث عن {entity.PropertyTypeAr} {verb.ToLower()} في {entity.CityAr}",
            H1En = $"{entity.PropertyTypeTitle}s in {entity.City}",
            H1Ar = $"{entity.PropertyTypeAr} في {entity.CityAr}",
            PrimaryKeyword = $"{entity.PropertyType} {verb.ToLower()} in {entity.City}",
            PredictedCtrScore = 75
        });

        variants.Add(new SerpVariant
        {
            VariantId = "question",
            TitleEn = $"Best {entity.Lifestyle} {entity.PropertyType}s {verb} in {entity.City}?",
            TitleAr = $"أفضل {entity.PropertyTypeAr} {verb.ToLower()} في {entity.CityAr}؟",
            DescriptionEn = $"Find the best {entity.Lifestyle} {entity.PropertyType}s {verb.ToLower()} in {entity.City}. Great prices!",
            DescriptionAr = $"ابحث عن أفضل {entity.PropertyTypeAr} {verb.ToLower()} في {entity.CityAr}",
            H1En = $"Best {entity.PropertyTypeTitle}s in {entity.City}",
            H1Ar = $"أفضل {entity.PropertyTypeAr} في {entity.CityAr}",
            PrimaryKeyword = $"best {entity.PropertyType}s {verb.ToLower()} in {entity.City}",
            PredictedCtrScore = 82
        });

        variants.Add(new SerpVariant
        {
            VariantId = "location-first",
            TitleEn = $"{entity.City} {entity.PropertyTypeTitle}s {verb} — Premium Selection",
            TitleAr = $"{entity.CityAr} {entity.PropertyTypeAr} {verb.ToLower()} — تشكيلة ممتازة",
            DescriptionEn = $"Discover premium {entity.PropertyType}s {verb.ToLower()} in {entity.City}. Expert guidance.",
            DescriptionAr = $"اكتشف {entity.PropertyTypeAr} ممتازة {verb.ToLower()} في {entity.CityAr}",
            H1En = $"{entity.City} {entity.PropertyTypeTitle}s",
            H1Ar = $"{entity.CityAr} {entity.PropertyTypeAr}",
            PrimaryKeyword = $"{entity.City} {entity.PropertyType}s {verb.ToLower()}",
            PredictedCtrScore = 78
        });

        if (request.Price > 0)
        {
            variants.Add(new SerpVariant
            {
                VariantId = "price-first",
                TitleEn = $"{entity.PropertyTypeTitle}s {verb} in {entity.City} from {FormatPrice(request.Price, request.Currency)}",
                TitleAr = $"{entity.PropertyTypeAr} {verb.ToLower()} في {entity.CityAr} من {FormatPrice(request.Price, request.Currency)}",
                DescriptionEn = $"{entity.PropertyTypeTitle}s starting at {FormatPrice(request.Price, request.Currency)} in {entity.City}.",
                DescriptionAr = $"{entity.PropertyTypeAr} تبدأ من {FormatPrice(request.Price, request.Currency)} في {entity.CityAr}",
                H1En = $"{entity.PropertyTypeTitle}s from {FormatPrice(request.Price, request.Currency)}",
                H1Ar = $"{entity.PropertyTypeAr} من {FormatPrice(request.Price, request.Currency)}",
                PrimaryKeyword = $"{FormatPrice(request.Price, request.Currency)} {entity.PropertyType}s {entity.City}",
                PredictedCtrScore = 85
            });
        }

        if (!string.IsNullOrWhiteSpace(entity.District))
        {
            variants.Add(new SerpVariant
            {
                VariantId = "district",
                TitleEn = $"{entity.PropertyTypeTitle}s in {entity.District}, {entity.City} {verb}",
                TitleAr = $"{entity.PropertyTypeAr} في {entity.District}، {entity.CityAr} {verb.ToLower()}",
                DescriptionEn = $"Find {entity.PropertyType}s in {entity.District}, {entity.City}. Prime location!",
                DescriptionAr = $"ابحث عن {entity.PropertyTypeAr} في {entity.District}، {entity.CityAr}",
                H1En = $"{entity.PropertyTypeTitle}s in {entity.District}, {entity.City}",
                H1Ar = $"{entity.PropertyTypeAr} في {entity.District}، {entity.CityAr}",
                PrimaryKeyword = $"{entity.PropertyType}s in {entity.District} {entity.City}",
                PredictedCtrScore = 80
            });
        }

        return variants;
    }

    public SerpVariant SelectBestVariant(List<SerpVariant> variants, string? deviceType = null, string? userLocation = null)
    {
        if (variants.Count == 0)
            throw new ArgumentException("No variants provided");

        var scored = variants.Select(v => new
        {
            Variant = v,
            Score = v.PredictedCtrScore +
                    (v.VariantId == "question" ? 5 : 0),
            VariantId = v.VariantId
        });

        return scored
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.VariantId)
            .First().Variant;
    }

    private static (string City, string CityAr, string? District, string PropertyType, string PropertyTypeTitle, string PropertyTypeAr, string Lifestyle, string? Developer) ExtractEntityInfo(SerpVariantRequest request)
    {
        var city = !string.IsNullOrWhiteSpace(request.Location) ? request.Location.TitleCase() : "Egypt";
        var cityAr = ArabicCityName(request.Location ?? "");
        var district = ExtractDistrict(request.Location);
        var propType = NormalizePropertyType(request.PropertyType);
        var propTypeTitle = propType.TitleCase();
        var propTypeAr = ArabicPropertyType(request.PropertyType);
        var lifestyle = InferLifestyle(request.Features, request.Price, request.Location);

        return (city, cityAr, district, propType, propTypeTitle, propTypeAr, lifestyle, null);
    }

    private static string? ExtractDistrict(string? location)
    {
        if (string.IsNullOrWhiteSpace(location)) return null;
        var known = new[] { "abu tig", "downtown", "marina", "touristic center", "mousa coast",
            "porto", "hacienda", "sidi abd el rahman", "marassi", "alamein",
            "katameya", "westown", "arjan", "sodic" };
        var loc = location.ToLowerInvariant();
        foreach (var d in known)
            if (loc.Contains(d)) return d.TitleCase();
        return null;
    }

    private static string NormalizePropertyType(string? propertyType)
    {
        if (string.IsNullOrWhiteSpace(propertyType)) return "property";
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["villa"] = "villa",
            ["apartment"] = "apartment",
            ["chalet"] = "chalet",
            ["penthouse"] = "penthouse",
            ["twinhouse"] = "twinhouse",
            ["townhouse"] = "townhouse",
            ["duplex"] = "duplex",
            ["studio"] = "studio",
            ["land"] = "land",
            ["office"] = "office",
            ["shop"] = "shop",
            ["building"] = "building"
        };
        return map.TryGetValue(propertyType, out var m) ? m : propertyType;
    }

    private static string InferLifestyle(List<string>? features, decimal price, string? location)
    {
        var luxury = new[] { "luxury", "vip", "premium", "elite", "exclusive", "high-end" };
        var beach = new[] { "beach", "sea", "coast", "north coast", "red sea", "waterfront" };

        if (features != null)
        {
            foreach (var f in features)
            {
                if (luxury.Any(m => f.Contains(m, StringComparison.OrdinalIgnoreCase)))
                    return "luxury";
                if (beach.Any(m => f.Contains(m, StringComparison.OrdinalIgnoreCase)))
                    return "beachfront";
            }
        }

        if (location != null && beach.Any(m => location.Contains(m, StringComparison.OrdinalIgnoreCase)))
            return "beachfront";
        if (price > 10_000_000) return "luxury";
        if (price > 3_000_000) return "premium";
        return "family";
    }

    private static string BuildArabicTitle(
        (string City, string CityAr, string? District, string PropertyType, string PropertyTypeTitle, string PropertyTypeAr, string Lifestyle, string? Developer) entities,
        string verb)
    {
        var verbAr = verb.Contains("Rent", StringComparison.OrdinalIgnoreCase) ? "للإيجار" : "للبيع";
        if (!string.IsNullOrWhiteSpace(entities.District))
            return $"{entities.PropertyTypeAr} {verbAr} في {entities.District} {entities.CityAr}";
        return $"{entities.PropertyTypeAr} {verbAr} في {entities.CityAr}";
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
            ["land"] = "أرض",
            ["office"] = "مكتب",
            ["shop"] = "محل",
            ["building"] = "مبنى"
        };
        return map.TryGetValue(propertyType ?? "", out var ar) ? ar : propertyType ?? "عقار";
    }

    private static string FormatPrice(decimal price, string? currency)
    {
        if (price >= 1_000_000) return $"{currency ?? "EGP"} {price / 1_000_000:0.#}M";
        if (price >= 1_000) return $"{currency ?? "EGP"} {price / 1_000:0.#}K";
        return $"{currency ?? "EGP"} {price:N0}";
    }

    private static bool IsRental(string? listingType)
    {
        return string.Equals(listingType, "Rental", StringComparison.OrdinalIgnoreCase);
    }
}
