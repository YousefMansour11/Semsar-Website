using System.Collections.Generic;

namespace Application.DTOs
{
    public class LocationResolutionResult
    {
        public string LocationString { get; set; } = string.Empty;
        public string LocationStringAr { get; set; } = string.Empty;
        public int DeepestId { get; set; }
    }


    public class LocationTreeNodeDto
    {
        public int Id { get; set; }
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int Level { get; set; }
        public string Path { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public List<LocationTreeNodeDto> Children { get; set; } = new();
    }

    public class LocationSearchResultDto
    {
        public int Id { get; set; }
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int Level { get; set; }
        public string FullPathEn { get; set; } = string.Empty;
        public string FullPathAr { get; set; } = string.Empty;
    }
}
