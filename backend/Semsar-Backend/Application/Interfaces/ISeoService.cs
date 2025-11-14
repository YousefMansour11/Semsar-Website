using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ISeoService
    {
        // Deterministic SEO generation based on provided inputs. Returns SEO pieces but does not persist.
        Task<(string TitleEn, string TitleAr, string DescriptionEn, string DescriptionAr, string KeywordsEn, string KeywordsAr)> GenerateSeoAsync(string? titleEn, string? titleAr, string? descriptionEn, string? descriptionAr, string? location);
    }
}
