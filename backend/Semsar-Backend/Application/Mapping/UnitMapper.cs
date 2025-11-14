using Application.DTOs;
using System.Collections.Generic;
using System.Linq;

namespace Application.Mapping
{
    public class UnitMapper : IUnitMapper
    {
        public UnitCardDto ToCardDto(Domain.Entities.Unit u)
        {
            return new UnitCardDto
            {
                Id = u.Id,
                TitleEn = u.TitleEn,
                MinPrice = u.MinPrice,
                MaxPrice = u.MaxPrice,
                MinArea = u.MinArea,
                MaxArea = u.MaxArea,
                MainImage = u.Images == null ? string.Empty : u.Images.Select(i => i.Url).FirstOrDefault() ?? string.Empty,
                Slug = u.Slug
            };
        }

        public UnitDetailsDto ToDetailsDto(dynamic u)
        {
            return new UnitDetailsDto
            {
                Id = u.Id,
                TitleEn = u.TitleEn,
                TitleAr = u.TitleAr ?? string.Empty,
                DescriptionEn = u.DescriptionEn ?? string.Empty,
                DescriptionAr = u.DescriptionAr ?? string.Empty,
                MinPrice = u.MinPrice,
                MaxPrice = u.MaxPrice,
                MinArea = u.MinArea,
                MaxArea = u.MaxArea,
                LocationAr = u.LocationAr,
                Features = u.Features ?? new System.Collections.Generic.List<string>(),
                FeaturesAr = u.FeaturesAr ?? new System.Collections.Generic.List<string>(),
                Images = u.Images ?? new System.Collections.Generic.List<string>(),
                ProjectId = u.ProjectId,
                ProjectName = u.ProjectName ?? string.Empty,
                Slug = u.Slug,
                SeoTitle = u.SeoTitle ?? string.Empty,
                SeoDescription = u.SeoDescription ?? string.Empty,
                SeoTitleAr = u.SeoTitleAr ?? string.Empty,
                SeoDescriptionAr = u.SeoDescriptionAr ?? string.Empty,
                SeoKeywords = u.SeoKeywords ?? string.Empty,
                SeoKeywordsAr = u.SeoKeywordsAr ?? string.Empty,
                CanonicalUrl = u.CanonicalUrl ?? string.Empty,
                JsonLd = null,
                ImagesMeta = new System.Collections.Generic.List<Application.DTOs.ImageDto>()
            };
        }

        public List<UnitCardDto> ToCardDtoList(IEnumerable<dynamic> list)
        {
            return list.Select(u => new UnitCardDto
            {
                Id = u.Id,
                TitleEn = u.TitleEn,
                MinPrice = u.MinPrice,
                MaxPrice = u.MaxPrice,
                MinArea = u.MinArea,
                MaxArea = u.MaxArea,
                MainImage = u.MainImage ?? string.Empty,
                Slug = u.Slug
            }).ToList();
        }
    }
}
