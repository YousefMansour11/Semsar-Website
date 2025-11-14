using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs
{
    public class ProjectCardDto
    {
        public int Id { get; set; }
        public string? PublicKey { get; set; }
        public string NameEn { get; set; } = null!;
        public string NameAr { get; set; } = null!;
        public string Location { get; set; } = null!;
        public string? LocationAr { get; set; }
        public string Developer { get; set; } = null!;
        public string? Image { get; set; }
        public string? Slug { get; set; }
        public decimal? StartingPrice { get; set; }
        public List<PropertyType>? PropertyTypes { get; set; }
        public decimal? TotalArea { get; set; }
        public int UnitCount { get; set; }
        public List<string> Highlights { get; set; } = new();
        public List<string>? HighlightsAr { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public List<ImageInfoDto> AdminImages { get; set; } = new();
        public bool IsRecommended { get; set; }
    }
}