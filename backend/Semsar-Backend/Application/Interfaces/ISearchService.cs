using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public class SearchResult
    {
        public int Id { get; set; }
        public string TitleEn { get; set; } = string.Empty;
        public string? TitleAr { get; set; }
        public decimal Price { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? LocationAr { get; set; }
        public string Slug { get; set; } = string.Empty;
        public List<string> Images { get; set; } = new();
        public double Rank { get; set; }
    }

    public interface ISearchService
    {
        Task<List<SearchResult>> SearchPropertiesAsync(string query, int maxResults = 20);
        bool IsFtsAvailable { get; }
        Task InitializeFtsAsync();
    }
}
