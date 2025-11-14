namespace Application.DTOs
{
    public class CloudinaryUploadResult
    {
        public bool Success { get; set; }
        public string? Url { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public string? PublicId { get; set; }
        public string? FileHash { get; set; }
    }
}
