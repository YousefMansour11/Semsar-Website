using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces;

namespace Application.Services;

public class InternalLinkingService : IInternalLinkingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public List<InternalLinkGroup> GenerateLinks(string? location, string? propertyType, string? listingType, string? slug, string? projectName)
    {
        var groups = new List<InternalLinkGroup>();

        var relatedGroup = new InternalLinkGroup { SectionTitle = "Related Properties" };
        var locPart = !string.IsNullOrWhiteSpace(location)
            ? location.ToLowerInvariant().Replace(" ", "-")
            : "egypt";

        relatedGroup.Links.Add(new InternalLink
        {
            Text = $"More properties in {location ?? "Egypt"}",
            Url = $"/properties/location/{locPart}",
            Type = "location"
        });

        if (!string.IsNullOrWhiteSpace(propertyType))
        {
            relatedGroup.Links.Add(new InternalLink
            {
                Text = $"All {propertyType}s for sale",
                Url = $"/properties/filter?propertyType={propertyType}",
                Type = "filter"
            });
        }

        if (!string.IsNullOrWhiteSpace(listingType))
        {
            relatedGroup.Links.Add(new InternalLink
            {
                Text = $"{listingType} properties in {location ?? "Egypt"}",
                Url = $"/properties/filter?listingType={listingType}&location={locPart}",
                Type = "filter"
            });
        }

        if (!string.IsNullOrWhiteSpace(projectName))
        {
            var projectSlug = projectName.ToLowerInvariant().Replace(" ", "-");
            relatedGroup.Links.Add(new InternalLink
            {
                Text = $"View {projectName} project details",
                Url = $"/projects/{projectSlug}",
                Type = "project"
            });
        }

        groups.Add(relatedGroup);

        var guideGroup = new InternalLinkGroup { SectionTitle = "Investment Guides" };
        guideGroup.Links.Add(new InternalLink
        {
            Text = "Complete guide to buying property in Egypt",
            Url = "/guides/buying-property-egypt",
            Type = "guide"
        });
        guideGroup.Links.Add(new InternalLink
        {
            Text = "Real estate investment tips for beginners",
            Url = "/guides/real-estate-investment-tips",
            Type = "guide"
        });
        guideGroup.Links.Add(new InternalLink
        {
            Text = "Understanding property prices in Egypt",
            Url = "/guides/property-prices-egypt",
            Type = "guide"
        });
        groups.Add(guideGroup);

        return groups;
    }

    public List<InternalLinkGroup> GetMissingLinks(string? location, string? propertyType, string? listingType, string? slug)
    {
        var missing = new List<InternalLinkGroup>();

        var current = GenerateLinks(location, propertyType, listingType, slug, null);
        var allTypes = new[] { "location", "filter", "project", "guide", "related" };
        var existingTypes = current.SelectMany(g => g.Links).Select(l => l.Type).Distinct().ToHashSet();

        var missingTypes = allTypes.Where(t => !existingTypes.Contains(t)).ToList();
        if (missingTypes.Count > 0)
        {
            var suggestions = new InternalLinkGroup { SectionTitle = "Suggested Links" };
            foreach (var type in missingTypes)
            {
                var locPart = !string.IsNullOrWhiteSpace(location)
                    ? location.ToLowerInvariant().Replace(" ", "-")
                    : "egypt";

                suggestions.Links.Add(type switch
                {
                    "location" => new InternalLink
                    {
                        Text = $"Explore {location ?? "Egypt"}",
                        Url = $"/properties/location/{locPart}",
                        Type = "location"
                    },
                    "project" => new InternalLink
                    {
                        Text = "View related projects",
                        Url = "/projects",
                        Type = "project"
                    },
                    _ => new InternalLink
                    {
                        Text = $"Browse all {propertyType ?? "properties"}",
                        Url = $"/properties/filter?{(propertyType != null ? $"propertyType={propertyType}" : "")}",
                        Type = "filter"
                    }
                });
            }
            missing.Add(suggestions);
        }

        return missing;
    }

    public List<InternalLinkGroup> GetOptimizedLinks(string? location, string? propertyType, string? listingType, string? slug, string? projectName)
    {
        var groups = GenerateLinks(location, propertyType, listingType, slug, projectName);

        foreach (var group in groups)
        {
            var unique = new HashSet<string>();
            group.Links = group.Links
                .Where(l => unique.Add(l.Url.ToLowerInvariant()))
                .Where(l => !string.IsNullOrWhiteSpace(l.Text) && !string.IsNullOrWhiteSpace(l.Url))
                .ToList();
        }

        var allLinks = groups.SelectMany(g => g.Links).ToList();
        if (allLinks.Count > 8)
        {
            var seenUrls = new HashSet<string>();
            var limited = new List<InternalLink>();
            foreach (var link in allLinks)
            {
                if (limited.Count >= 8) break;
                if (seenUrls.Add(link.Url.ToLowerInvariant()))
                    limited.Add(link);
            }

            foreach (var group in groups)
                group.Links = group.Links.Where(l => limited.Contains(l)).ToList();
        }

        groups = groups.Where(g => g.Links.Count > 0).ToList();

        return groups;
    }

    public bool MeetsMinimumRequirement(List<InternalLinkGroup> links)
    {
        var allLinks = links.SelectMany(g => g.Links).ToList();
        var totalLinks = allLinks.Count;
        if (totalLinks < 3) return false;

        var uniqueTypes = allLinks.Select(l => l.Type).Distinct().ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (uniqueTypes.Count < 2) return false;

        if (!uniqueTypes.Contains("location")) return false;
        if (!uniqueTypes.Contains("guide")) return false;

        return true;
    }

    public static string ToJson(List<InternalLinkGroup> groups)
    {
        return JsonSerializer.Serialize(groups, JsonOptions);
    }
}
