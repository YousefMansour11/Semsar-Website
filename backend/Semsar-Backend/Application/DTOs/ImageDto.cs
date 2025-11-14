namespace Application.DTOs
{
    public record ImageDto
    {
        public string Url { get; init; } = null!;
        public int Width { get; init; }
        public int Height { get; init; }
    }
}
