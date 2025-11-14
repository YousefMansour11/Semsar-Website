namespace Application.Interfaces;

public class LocationSeoData
{
    public string Location { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string H1En { get; set; } = string.Empty;
    public string H1Ar { get; set; } = string.Empty;
    public string PrimaryKeyword { get; set; } = string.Empty;
    public List<string> SecondaryKeywords { get; set; } = new();
    public List<string> LongTailKeywords { get; set; } = new();
    public string LocationJsonLd { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
}

public interface ILocationSeoService
{
    Task<LocationSeoData> GenerateLocationSeoAsync(string location, string? propertyType = null);
    double CalculateLocationRelevance(string location, string searchQuery);
    List<string> GetRelatedLocations(string location, int maxCount = 5);
    string BuildLocationJsonLd(string location, double latitude = 0, double longitude = 0);
}
