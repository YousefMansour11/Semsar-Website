namespace Application.DTOs
{
    public class UploadResult
    {
        public string Url { get; set; } = null!;
        public string PublicId { get; set; } = null!;
        public int Width { get; set; }
        public int Height { get; set; }
        public List<string> Warnings { get; set; } = new();
    }
}
