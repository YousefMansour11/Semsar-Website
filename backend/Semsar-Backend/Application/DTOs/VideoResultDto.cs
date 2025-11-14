namespace Application.DTOs
{
    public class VideoResultDto
    {
        public int Id { get; set; }
        public string Url { get; set; } = null!;
        public string? PublicId { get; set; }
        public string? ThumbnailUrl { get; set; }
    }

    public class ConfirmVideoRequest
    {
        public string Url { get; set; } = null!;
        public string PublicId { get; set; } = null!;
        public string? ThumbnailUrl { get; set; }
        public string? FileName { get; set; }
    }
}
