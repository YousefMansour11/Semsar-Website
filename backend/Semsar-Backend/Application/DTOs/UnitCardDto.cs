namespace Application.DTOs
{
    public class UnitCardDto
    {
        public int Id { get; set; }
        public int? ProjectId { get; set; }
        public string? PublicKey { get; set; }
        public string TitleEn { get; set; } = null!;
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public double? MinArea { get; set; }
        public double? MaxArea { get; set; }
        public string? MainImage { get; set; }
        public string? Slug { get; set; }
        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
        public string? SeoKeywords { get; set; }
    }
}