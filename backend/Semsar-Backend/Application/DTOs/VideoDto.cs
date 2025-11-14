using System;

namespace Application.DTOs
{
    public class VideoDto
    {
        public int Id { get; set; }
        public string Url { get; set; } = null!;
        public string? PublicId { get; set; }
        public string? ThumbnailUrl { get; set; }
        public double? Duration { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string? Title { get; set; }
        public int SortOrder { get; set; }
        public bool IsMain { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
