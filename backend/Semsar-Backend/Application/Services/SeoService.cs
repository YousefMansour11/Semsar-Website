using Application.Interfaces;
using System.Threading.Tasks;

namespace Application.Services
{
    public class SeoService : ISeoService
    {
        public Task<(string TitleEn, string TitleAr, string DescriptionEn, string DescriptionAr, string KeywordsEn, string KeywordsAr)> GenerateSeoAsync(string? titleEn, string? titleAr, string? descriptionEn, string? descriptionAr, string? location)
        {
            var titleEnOut = !string.IsNullOrWhiteSpace(titleEn) ? titleEn : (titleAr ?? string.Empty);
            var titleArOut = !string.IsNullOrWhiteSpace(titleAr) ? titleAr : (titleEn ?? string.Empty);

            var descEn = !string.IsNullOrWhiteSpace(descriptionEn) ? (descriptionEn.Length <= 150 ? descriptionEn : descriptionEn.Substring(0, 150)) : string.Empty;
            var descAr = !string.IsNullOrWhiteSpace(descriptionAr) ? (descriptionAr.Length <= 150 ? descriptionAr : descriptionAr.Substring(0, 150)) : string.Empty;

            var keywordsEn = (!string.IsNullOrWhiteSpace(titleEn) ? titleEn + ", " : "") + (location ?? "") + ", real estate";
            var keywordsAr = (!string.IsNullOrWhiteSpace(titleAr) ? titleAr + ", " : "") + (location ?? "") + ", عقارات";

            return Task.FromResult((titleEnOut, titleArOut, descEn, descAr, keywordsEn, keywordsAr));
        }
    }
}
