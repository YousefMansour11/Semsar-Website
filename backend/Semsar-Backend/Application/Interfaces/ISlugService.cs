namespace Application.Interfaces
{
    public interface ISlugService
    {
        string Slugify(string? input, string? lang = null);
        string NormalizeSlug(string slug);
        string DeduplicateSlugTokens(string slug);
        string GenerateCandidateSlug(string titleEn, string? titleAr, string? location, string? preferredLang = null);
    }
}
