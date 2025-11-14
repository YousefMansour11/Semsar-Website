namespace Application.DTOs
{
    public class VideoLibraryItemDto
    {
        public string Url { get; set; } = null!;
        public string PublicId { get; set; } = null!;
        public string? ThumbnailUrl { get; set; }
        public string? FileName { get; set; }
        public int ReferenceCount { get; set; }
    }
}