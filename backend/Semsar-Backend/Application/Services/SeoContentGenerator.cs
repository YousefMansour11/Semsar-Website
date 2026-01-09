using Application.Interfaces;

namespace Application.Services;

public class SeoContentGenerator : ISeoContentGenerator
{
    private static readonly HashSet<string> LuxuryMarkers = new(StringComparer.OrdinalIgnoreCase)
    {
        "luxury", "vip", "premium", "elite", "exclusive", "high-end"
    };

    private static readonly HashSet<string> BeachMarkers = new(StringComparer.OrdinalIgnoreCase)
    {
        "beach", "sea", "coast", "north coast", "red sea", "mediterranean", "lagoon", "waterfront"
    };

    private static readonly HashSet<string> InvestmentMarkers = new(StringComparer.OrdinalIgnoreCase)
    {
        "investment", "roi", "yield", "appreciation", "capital gain", "compound"
    };

    public SeoContentResult Generate(
        SeoEntityType entityType,
        string? titleEn,
        string? titleAr,
        string? descriptionEn,
        string? descriptionAr,
        string? location,
        string? propertyType,
        string? listingType,
        decimal price,
        string? currency,
        List<string>? features,
        string? developer = null,
        string? projectName = null)
    {
        var result = new SeoContentResult();

        var entities = ExtractEntities(location, propertyType, listingType, features, price, developer, projectName);
        var intent = ClassifyIntent(listingType, price);

        result.Intent = intent;
        result.PrimaryKeyword = BuildPrimaryKeyword(entities, intent, listingType);
        result.SecondaryKeywords = BuildSecondaryKeywords(entities, listingType);
        result.LongTailKeywords = BuildLongTailKeywords(entities, listingType, features);

        result.TitleEn = BuildTitle(entities, intent, listingType, price, currency, 60);
        result.TitleAr = BuildArabicTitle(entities, intent, listingType, price, currency, 60);

        result.H1En = BuildH1(entities, intent, listingType);
        result.H1Ar = BuildArabicH1(entities, intent, listingType);

        result.DescriptionEn = BuildDescription(entities, intent, listingType, price, currency, 155);
        result.DescriptionAr = BuildArabicDescription(entities, intent, listingType, price, currency, 155);

        result.H2SectionsEn = BuildH2Sections(entities, intent);
        result.H2SectionsAr = BuildArabicH2Sections(entities, intent);

        result.Faqs = BuildFaqs(entities, intent, listingType);

        return result;
    }

    private static (string City, string? District, string? Landmark, string PropertyType, string Lifestyle, string? Developer, string? Project) ExtractEntities(
        string? location, string? propertyType, string? listingType,
        List<string>? features, decimal price, string? developer, string? projectName)
    {
        var city = ExtractCity(location);
        var district = ExtractDistrict(location);
        var landmark = ExtractLandmark(location);
        var propType = NormalizePropertyType(propertyType);
        var lifestyle = InferLifestyle(features, price, location);
        return (city, district, landmark, propType, lifestyle, developer, projectName);
    }

    private static string ExtractCity(string? location)
    {
        if (string.IsNullOrWhiteSpace(location)) return "Egypt";
        var knownCities = new[] { "cairo", "new cairo", "el gouna", "gouna", "hurghada", "north coast",
            "sahel", "alexandria", "new administrative capital", "nace", "sheikh zayed", "october",
            "6th october", "rehab", "tagamoa", "tagamoe", "madinty", "mostakbal" };
        var loc = location.ToLowerInvariant();
        foreach (var city in knownCities)
        {
            if (loc.Contains(city)) return city;
        }
        return location;
    }

    private static string? ExtractDistrict(string? location)
    {
        if (string.IsNullOrWhiteSpace(location)) return null;
        var knownDistricts = new[] { "abu tig", "downtown", "marina", "touristic center",
            "mousa coast", "porto", "hacienda", "sidi abd el rahman", "marassi", "alamein",
            "katameya", "westown", "arjan", "sodic" };
        var loc = location.ToLowerInvariant();
        foreach (var d in knownDistricts)
        {
            if (loc.Contains(d)) return d;
        }
        return null;
    }

    private static string? ExtractLandmark(string? location)
    {
        if (string.IsNullOrWhiteSpace(location)) return null;
        var knownLandmarks = new[] { "lagoon", "marina", "golf course", "abu tig marina",
            "sahl hasheesh", "makadi bay", "soma bay", "down town" };
        var loc = location.ToLowerInvariant();
        foreach (var l in knownLandmarks)
        {
            if (loc.Contains(l)) return l;
        }
        return null;
    }

    private static string NormalizePropertyType(string? propertyType)
    {
        if (string.IsNullOrWhiteSpace(propertyType)) return "property";
        var type = propertyType.ToLowerInvariant();
        var typeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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
            ["commercial"] = "commercial unit",
            ["office"] = "office",
            ["residential"] = "residential",
            ["industrial"] = "industrial",
            ["mixeduse"] = "mixed use"
        };
        return typeMap.TryGetValue(type, out var mapped) ? mapped : type;
    }

    private static string InferLifestyle(List<string>? features, decimal price, string? location)
    {
        if (features != null)
        {
            foreach (var f in features)
            {
                if (LuxuryMarkers.Any(m => f.Contains(m, StringComparison.OrdinalIgnoreCase)))
                    return "luxury";
                if (BeachMarkers.Any(m => f.Contains(m, StringComparison.OrdinalIgnoreCase)))
                    return "beachfront";
                if (InvestmentMarkers.Any(m => f.Contains(m, StringComparison.OrdinalIgnoreCase)))
                    return "investment";
            }
        }

        if (location != null && BeachMarkers.Any(m => location.Contains(m, StringComparison.OrdinalIgnoreCase)))
            return "beachfront";

        if (price > 10_000_000)
            return "luxury";
        if (price > 3_000_000)
            return "premium";

        return "family";
    }

    private static string ClassifyIntent(string? listingType, decimal price)
    {
        if (string.IsNullOrWhiteSpace(listingType))
            return "transactional";
        if (listingType.Equals("rental", StringComparison.OrdinalIgnoreCase))
            return "transactional";
        if (price > 5_000_000)
            return "commercial";
        return "transactional";
    }

    private static string BuildPrimaryKeyword(
        (string City, string? District, string? Landmark, string PropertyType, string Lifestyle, string? Developer, string? Project) entities,
        string intent, string? listingType)
    {
        var verb = listingType?.Equals("rental", StringComparison.OrdinalIgnoreCase) == true ? "for rent" : "for sale";
        var parts = new List<string> { entities.Lifestyle, entities.PropertyType, verb, "in", entities.City };

        if (!string.IsNullOrWhiteSpace(entities.District))
            parts.Insert(4, entities.District);

        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static List<string> BuildSecondaryKeywords(
        (string City, string? District, string? Landmark, string PropertyType, string Lifestyle, string? Developer, string? Project) entities,
        string? listingType)
    {
        var keywords = new List<string>
        {
            $"{entities.City} real estate",
            $"{entities.PropertyType}s in {entities.City}",
            $"best {entities.PropertyType}s {entities.City}"
        };

        if (!string.IsNullOrWhiteSpace(entities.Landmark))
        {
            keywords.Add($"{entities.PropertyType}s near {entities.Landmark}");
            keywords.Add($"{entities.Landmark} {entities.City} real estate");
        }

        if (entities.Lifestyle == "luxury")
        {
            keywords.Add($"luxury {entities.PropertyType}s {entities.City}");
        }

        if (!string.IsNullOrWhiteSpace(entities.Developer))
        {
            keywords.Add($"{entities.Developer} {entities.City}");
        }

        return keywords;
    }

    private static List<string> BuildLongTailKeywords(
        (string City, string? District, string? Landmark, string PropertyType, string Lifestyle, string? Developer, string? Project) entities,
        string? listingType, List<string>? features)
    {
        var verb = listingType?.Equals("rental", StringComparison.OrdinalIgnoreCase) == true ? "for rent" : "for sale";
        var keywords = new List<string>
        {
            $"best {entities.Lifestyle} {entities.PropertyType}s {verb} in {entities.City}"
        };

        if (!string.IsNullOrWhiteSpace(entities.District))
        {
            keywords.Add($"{entities.PropertyType}s {verb} in {entities.District} {entities.City}");
        }

        if (!string.IsNullOrWhiteSpace(entities.Landmark))
        {
            keywords.Add($"{entities.PropertyType} near {entities.Landmark} {entities.City}");
        }

        if (features != null && features.Count > 0)
        {
            var topFeatures = features.Take(3).Select(f => f.ToLowerInvariant()).ToList();
            keywords.Add($"{string.Join(" ", topFeatures)} {entities.PropertyType} {entities.City}");
        }

        if (!string.IsNullOrWhiteSpace(entities.Project))
        {
            keywords.Add($"{entities.Project} {entities.City} {entities.PropertyType}");
        }

        return keywords;
    }

    private static string BuildTitle(
        (string City, string? District, string? Landmark, string PropertyType, string Lifestyle, string? Developer, string? Project) entities,
        string intent, string? listingType, decimal price, string? currency, int maxLen)
    {
        var verb = listingType?.Equals("rental", StringComparison.OrdinalIgnoreCase) == true ? "for Rent" : "for Sale";

        var title = $"{entities.PropertyType.TitleCase()} in {entities.City.TitleCase()} {verb}";

        if (!string.IsNullOrWhiteSpace(entities.Landmark))
        {
            title = $"{entities.PropertyType.TitleCase()} Near {entities.Landmark.TitleCase()} — {entities.City.TitleCase()} {verb}";
        }
        else if (!string.IsNullOrWhiteSpace(entities.District))
        {
            title = $"{entities.PropertyType.TitleCase()} in {entities.District.TitleCase()}, {entities.City.TitleCase()} {verb}";
        }

        if (title.Length > maxLen)
        {
            title = title.Length > maxLen + 5
                ? $"{entities.PropertyType.TitleCase()} in {entities.City.TitleCase()} {verb}"
                : title;
        }

        if (title.Length > maxLen)
        {
            title = $"{entities.PropertyType.TitleCase()} {entities.City.TitleCase()} {verb}";
        }

        if (title.Length > maxLen)
        {
            title = title.Substring(0, maxLen - 3) + "...";
        }

        return title;
    }

    private static string BuildArabicTitle(
        (string City, string? District, string? Landmark, string PropertyType, string Lifestyle, string? Developer, string? Project) entities,
        string intent, string? listingType, decimal price, string? currency, int maxLen)
    {
        var typeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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
            ["residential"] = "سكني",
            ["commercial"] = "تجاري",
            ["industrial"] = "صناعي",
            ["mixed use"] = "متعدد الاستخدامات"
        };
        var typeAr = typeMap.TryGetValue(entities.PropertyType, out var t) ? t : entities.PropertyType;
        var verb = listingType?.Equals("rental", StringComparison.OrdinalIgnoreCase) == true ? "للإيجار" : "للبيع";
        var cityAr = ArabicCityName(entities.City);

        var title = $"{typeAr} {verb} في {cityAr}";

        if (!string.IsNullOrWhiteSpace(entities.District))
        {
            title = $"{typeAr} {verb} في {entities.District} {cityAr}";
        }

        if (title.Length > maxLen && title.Length > 30)
        {
            title = $"{typeAr} {verb} {cityAr}";
        }

        if (title.Length > maxLen)
        {
            title = title.Substring(0, maxLen - 3) + "...";
        }

        return title;
    }

    private static string BuildH1(
        (string City, string? District, string? Landmark, string PropertyType, string Lifestyle, string? Developer, string? Project) entities,
        string intent, string? listingType)
    {
        if (!string.IsNullOrWhiteSpace(entities.Project))
            return $"{entities.Project} — {entities.PropertyType.TitleCase()}s in {entities.City.TitleCase()}";

        if (!string.IsNullOrWhiteSpace(entities.Landmark))
            return $"{entities.PropertyType.TitleCase()} Near {entities.Landmark.TitleCase()}, {entities.City.TitleCase()}";

        if (!string.IsNullOrWhiteSpace(entities.District))
            return $"{entities.PropertyType.TitleCase()}s in {entities.District.TitleCase()}, {entities.City.TitleCase()}";

        return $"{entities.Lifestyle.TitleCase()} {entities.PropertyType.TitleCase()}s in {entities.City.TitleCase()} — Your Dream Home Awaits";
    }

    private static string BuildArabicH1(
        (string City, string? District, string? Landmark, string PropertyType, string Lifestyle, string? Developer, string? Project) entities,
        string intent, string? listingType)
    {
        var cityAr = ArabicCityName(entities.City);
        var typeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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
            ["residential"] = "سكني",
            ["commercial"] = "تجاري",
            ["industrial"] = "صناعي",
            ["mixed use"] = "متعدد الاستخدامات"
        };
        var typeAr = typeMap.TryGetValue(entities.PropertyType, out var t) ? t : entities.PropertyType;

        if (!string.IsNullOrWhiteSpace(entities.Project))
            return $"مشروع {entities.Project} — {typeAr} في {cityAr}";

        if (!string.IsNullOrWhiteSpace(entities.Landmark))
            return $"{typeAr} بالقرب من {entities.Landmark}، {cityAr}";

        return $"{typeAr} في {cityAr} — منزل أحلامك في انتظارك";
    }

    private static string BuildDescription(
        (string City, string? District, string? Landmark, string PropertyType, string Lifestyle, string? Developer, string? Project) entities,
        string intent, string? listingType, decimal price, string? currency, int maxLen)
    {
        var verb = listingType?.Equals("rental", StringComparison.OrdinalIgnoreCase) == true ? "rent" : "buy";

        var desc = $"Looking to {verb} a {entities.Lifestyle} {entities.PropertyType} in {entities.City}?";

        if (!string.IsNullOrWhiteSpace(entities.Landmark))
            desc = $"Discover {entities.PropertyType}s near {entities.Landmark} in {entities.City}. {entities.Lifestyle.TitleCase()} living with premium amenities.";

        if (!string.IsNullOrWhiteSpace(entities.District))
            desc = $"Explore {entities.PropertyType}s in {entities.District}, {entities.City}. {entities.Lifestyle.TitleCase()} properties at competitive prices.";

        if (price > 0)
        {
            var formattedPrice = FormatPrice(price, currency);
            if (desc.Length + formattedPrice.Length + 10 < maxLen)
                desc += $" From {formattedPrice}.";
        }

        desc += " Book a tour today!";

        if (desc.Length > maxLen)
        {
            desc = desc.Substring(0, maxLen - 3) + "...";
        }

        return desc;
    }

    private static string BuildArabicDescription(
        (string City, string? District, string? Landmark, string PropertyType, string Lifestyle, string? Developer, string? Project) entities,
        string intent, string? listingType, decimal price, string? currency, int maxLen)
    {
        var cityAr = ArabicCityName(entities.City);
        var typeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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
            ["residential"] = "سكني",
            ["commercial"] = "تجاري",
            ["industrial"] = "صناعي",
            ["mixed use"] = "متعدد الاستخدامات"
        };
        var typeAr = typeMap.TryGetValue(entities.PropertyType, out var t) ? t : entities.PropertyType;

        var desc = $"ابحث عن {typeAr} في {cityAr}. ";
        if (!string.IsNullOrWhiteSpace(entities.Landmark))
            desc = $"{typeAr} بالقرب من {entities.Landmark} في {cityAr}. ";

        desc += "أسعار تنافسية وموقع متميز. احجز جولة اليوم!";

        if (desc.Length > maxLen)
        {
            desc = desc.Substring(0, maxLen - 3) + "...";
        }

        return desc;
    }

    private static List<string> BuildH2Sections(
        (string City, string? District, string? Landmark, string PropertyType, string Lifestyle, string? Developer, string? Project) entities,
        string intent)
    {
        var sections = new List<string>
        {
            "Overview",
            "Location & Accessibility",
            "Property Features & Amenities"
        };

        if (intent == "commercial" || intent == "transactional")
        {
            sections.Add("Investment Potential & ROI Analysis");
            sections.Add("Nearby Landmarks & Attractions");
        }

        sections.Add("Why Choose This Property");
        sections.Add("Frequently Asked Questions");

        return sections;
    }

    private static List<string> BuildArabicH2Sections(
        (string City, string? District, string? Landmark, string PropertyType, string Lifestyle, string? Developer, string? Project) entities,
        string intent)
    {
        var sections = new List<string>
        {
            "نظرة عامة",
            "الموقع وسهولة الوصول",
            "مميزات العقار ووسائل الراحة"
        };

        if (intent == "commercial" || intent == "transactional")
        {
            sections.Add("إمكانات الاستثمار وتحليل العائد");
            sections.Add("المعالم القريبة ومناطق الجذب");
        }

        sections.Add("لماذا تختار هذا العقار");
        sections.Add("الأسئلة الشائعة");

        return sections;
    }

    private static List<SeoFaqItem> BuildFaqs(
        (string City, string? District, string? Landmark, string PropertyType, string Lifestyle, string? Developer, string? Project) entities,
        string intent, string? listingType)
    {
        var faqs = new List<SeoFaqItem>();
        var city = entities.City.TitleCase();
        var type = entities.PropertyType;
        var verb = listingType?.Equals("rental", StringComparison.OrdinalIgnoreCase) == true ? "rent" : "buy";

        faqs.Add(new SeoFaqItem
        {
            QuestionEn = $"What is the average price of {type}s in {city}?",
            AnswerEn = $"Prices for {type}s in {city} vary based on location, size, and amenities. " +
                       $"Factors like proximity to landmarks, view quality, and compound amenities " +
                       $"significantly influence pricing. Contact us for current market rates.",
            QuestionAr = $"ما هو متوسط سعر {type} في {ArabicCityName(entities.City)}؟",
            AnswerAr = $"تختلف أسعار {type} في {ArabicCityName(entities.City)} حسب الموقع والحجم والمرافق."
        });

        faqs.Add(new SeoFaqItem
        {
            QuestionEn = $"Is {city} a good area to {verb} property?",
            AnswerEn = $"{city} is one of Egypt's most desirable locations for real estate investment. " +
                       "With growing infrastructure, new developments, and strong rental demand, " +
                       "it offers excellent potential for both living and investment.",
            QuestionAr = $"هل {ArabicCityName(entities.City)} منطقة جيدة لشراء العقارات؟",
            AnswerAr = $"تعتبر {ArabicCityName(entities.City)} من أفضل المناطق في مصر للاستثمار العقاري مع بنية تحتية متطورة وطلب متزايد."
        });

        if (!string.IsNullOrWhiteSpace(entities.Landmark))
        {
            faqs.Add(new SeoFaqItem
            {
                QuestionEn = $"How far is this {type} from {entities.Landmark.TitleCase()}?",
                AnswerEn = $"The {type} is conveniently located near {entities.Landmark.TitleCase()}, " +
                           "offering easy access to this popular destination. Exact distance " +
                           "varies by property — contact us for specific location details.",
                QuestionAr = $"كم يبعد هذا {type} عن {entities.Landmark}؟",
                AnswerAr = $"يقع {type} على مسافة قريبة من {entities.Landmark} مع سهولة الوصول."
            });
        }

        faqs.Add(new SeoFaqItem
        {
            QuestionEn = $"What amenities are included with {type}s in {city}?",
            AnswerEn = $"Most {type}s in {city} come with modern amenities including " +
                       "24/7 security, private parking, swimming pools, landscaped gardens, " +
                       "and maintenance services. Premium properties may include " +
                       "private beaches, clubhouses, and concierge services.",
            QuestionAr = $"ما هي المرافق المتضمنة مع {type} في {ArabicCityName(entities.City)}؟",
            AnswerAr = $"معظم {type} في {ArabicCityName(entities.City)} تشمل أمن على مدار الساعة ومواقف خاصة وحمامات سباحة وحدائق."
        });

        faqs.Add(new SeoFaqItem
        {
            QuestionEn = $"What is the {verb}ing process for properties in {city}?",
            AnswerEn = $"The process involves property selection, reservation with refundable deposit, " +
                       "due diligence, contract signing, and payment/transfer. " +
                       "For {verb}ing, we guide you through every step including " +
                       "legal verification, registration, and handover.",
            QuestionAr = $"ما هي عملية شراء العقارات في {ArabicCityName(entities.City)}؟",
            AnswerAr = $"تتضمن العملية اختيار العقار والحجز بعيد عن استرداد والفحص القانوني وتوقيع العقد والدفع."
        });

        if (!string.IsNullOrWhiteSpace(entities.Project))
        {
            faqs.Add(new SeoFaqItem
            {
                QuestionEn = $"What is the delivery timeline for {entities.Project}?",
                AnswerEn = $"Delivery timelines for {entities.Project} vary by phase. " +
                           "Contact our team for the latest handover schedules and available units.",
                QuestionAr = $"ما هو الجدول الزمني لتسليم {entities.Project}؟",
                AnswerAr = $"تختلف مواعيد التسليم لمشروع {entities.Project} حسب المرحلة. تواصل معنا لمعرفة最新的 المواعيد."
            });
        }

        return faqs;
    }

    private static string FormatPrice(decimal price, string? currency)
    {
        if (price >= 1_000_000)
            return $"{currency ?? "EGP"} {price / 1_000_000:0.#}M";
        if (price >= 1_000)
            return $"{currency ?? "EGP"} {price / 1_000:0.#}K";
        return $"{currency ?? "EGP"} {price:N0}";
    }

    private static string ArabicCityName(string city)
    {
        var cityMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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
        return cityMap.TryGetValue(city, out var ar) ? ar : city;
    }
}

internal static class StringExtensions
{
    public static string TitleCase(this string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.ToLower());
    }
}
