using Application.DTOs;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ProjectQueryService : IProjectQueryService
    {
        private readonly IUnitOfWork _uow;
        private readonly IJsonLdService _jsonLdService;
        private readonly ICanonicalService _canonicalService;
        private readonly ISeoContentGenerator _seoContentGenerator;
        private readonly ISERPVariantGenerator _serpVariantGenerator;
        private readonly IEntityGraphService _entityGraphService;
        private readonly IInternalLinkingService _internalLinkingService;
        private readonly IClickBehaviorOptimizationService _clickBehavior;
        private readonly IPublicIdService _publicIdService;
        private readonly Application.Interfaces.ICloudinaryService? _cloud;
        private readonly ILogger<ProjectQueryService>? _logger;

        public ProjectQueryService(
            IUnitOfWork uow,
            IJsonLdService jsonLdService,
            ICanonicalService canonicalService,
            ISeoContentGenerator seoContentGenerator,
            ISERPVariantGenerator serpVariantGenerator,
            IEntityGraphService entityGraphService,
            IInternalLinkingService internalLinkingService,
            IClickBehaviorOptimizationService clickBehavior,
            IPublicIdService publicIdService,
            Application.Interfaces.ICloudinaryService? cloud = null,
            ILogger<ProjectQueryService>? logger = null)
        {
            _uow = uow;
            _jsonLdService = jsonLdService;
            _canonicalService = canonicalService;
            _seoContentGenerator = seoContentGenerator;
            _serpVariantGenerator = serpVariantGenerator;
            _entityGraphService = entityGraphService;
            _internalLinkingService = internalLinkingService;
            _clickBehavior = clickBehavior;
            _publicIdService = publicIdService;
            _cloud = cloud;
            _logger = logger;
        }

        private string ResolvePublicKey(string? publicKey, string entityTypePrefix)
        {
            return !string.IsNullOrEmpty(publicKey) ? publicKey : _publicIdService.GenerateId(entityTypePrefix);
        }

        public async Task<List<ProjectCardDto>> GetPublicCardsAsync(CancellationToken ct = default)
        {
            var cards = await _uow.Projects.Query().AsNoTracking()
                .Where(p => !p.IsDeleted)
                .Include(p => p.Images)
                .Select(p => new ProjectCardDto
                {
                    Id = p.Id,
                    PublicKey = p.PublicKey,
                    NameEn = p.NameEn,
                    NameAr = p.NameAr,
                    Location = p.Location,
                    LocationAr = p.LocationAr,
                    Developer = p.Developer,
                    Image = p.Image,
                    Slug = p.Slug,
                    Highlights = p.Highlights,
                    HighlightsAr = p.HighlightsAr,
                    StartingPrice = p.StartingPrice,
                    PropertyTypes = p.PropertyTypes,
                    TotalArea = p.TotalArea,
                    UnitCount = p.UnitCount,
                    IsRecommended = p.IsRecommended,
                    AdminImages = p.Images != null
                        ? p.Images.Where(i => !i.IsDeleted).OrderBy(i => i.SortOrder).Select(i => new ImageInfoDto
                        {
                            Id = i.Id,
                            Url = i.Url,
                            PublicId = i.PublicId
                        }).ToList()
                        : new List<ImageInfoDto>()
                }).ToListAsync(ct);

            if (_cloud != null)
            {
                foreach (var card in cards)
                {
                    if (!string.IsNullOrWhiteSpace(card.Image))
                        card.Image = _cloud.GetOptimizedUrl(card.Image);
                }
            }

            foreach (var card in cards)
            {
                if (string.IsNullOrEmpty(card.PublicKey))
                    card.PublicKey = _publicIdService.GenerateId(Application.Common.EntityType.Project);
            }

            return cards;
        }

        public async Task<ProjectDetailsDto?> GetBySlugOrIdAsync(string slugOrId, CancellationToken ct = default)
        {
            Domain.Entities.Project? project;

            if (int.TryParse(slugOrId, out var id))
            {
                project = await _uow.Projects.Query()
                    .Include(p => p.Images)
                    .Include(p => p.Videos)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
            }
            else
            {
                project = await _uow.Projects.Query()
                    .Include(p => p.Images)
                    .Include(p => p.Videos)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Slug == slugOrId && !p.IsDeleted, ct);
            }

            if (project == null) return null;

            _clickBehavior.RecordImpression($"/projects/{project.Slug}");

            var images = project.Images != null
                ? project.Images.Where(i => !i.IsDeleted).OrderBy(i => i.SortOrder).ToList()
                : new List<Domain.Entities.ProjectImage>();

            var imageUrls = images.Select(i => i.Url).ToList();
            if (project.Image != null && !imageUrls.Contains(project.Image))
                imageUrls.Insert(0, project.Image);

            if (_cloud != null)
                imageUrls = imageUrls.Select(u => _cloud.GetOptimizedUrl(u)).ToList();

            var dto = MapToDetailsDto(project, imageUrls);
            ApplySeoEnhancements(dto, project);

            return dto;
        }

        public async Task<ProjectDetailsDto?> GetByPublicKeyAsync(string publicKey, CancellationToken ct = default)
        {
            var project = await _uow.Projects.Query()
                .Include(p => p.Images)
                .Include(p => p.Videos)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PublicKey == publicKey && !p.IsDeleted, ct);

            if (project == null) return null;

            _clickBehavior.RecordImpression($"/projects/{project.Slug}");

            var images = project.Images != null
                ? project.Images.Where(i => !i.IsDeleted).OrderBy(i => i.SortOrder).ToList()
                : new List<Domain.Entities.ProjectImage>();

            var imageUrls = images.Select(i => i.Url).ToList();
            if (project.Image != null && !imageUrls.Contains(project.Image))
                imageUrls.Insert(0, project.Image);

            if (_cloud != null)
                imageUrls = imageUrls.Select(u => _cloud.GetOptimizedUrl(u)).ToList();

            var dto = MapToDetailsDto(project, imageUrls);
            ApplySeoEnhancements(dto, project);

            return dto;
        }

        private ProjectDetailsDto MapToDetailsDto(Domain.Entities.Project project, List<string> imageUrls)
        {
            var images = project.Images != null
                ? project.Images.Where(i => !i.IsDeleted).OrderBy(i => i.SortOrder).ToList()
                : new List<Domain.Entities.ProjectImage>();

            return new ProjectDetailsDto
            {
                Id = project.Id,
                PublicKey = ResolvePublicKey(project.PublicKey, Application.Common.EntityType.Project),
                NameEn = project.NameEn,
                NameAr = project.NameAr ?? string.Empty,
                DescriptionEn = project.DescriptionEn ?? string.Empty,
                DescriptionAr = project.DescriptionAr ?? string.Empty,
                Location = project.Location,
                LocationAr = project.LocationAr,
                Developer = project.Developer,
                Image = project.Image,
                Images = imageUrls,
                AdminImages = images.Select(i => new ImageInfoDto
                {
                    Id = i.Id,
                    Url = i.Url,
                    PublicId = i.PublicId
                }).ToList(),
                Highlights = project.Highlights ?? new List<string>(),
                HighlightsAr = project.HighlightsAr,
                StartingPrice = project.StartingPrice,
                NearbyPlaces = project.NearbyPlaces,
                NearbyPlacesAr = project.NearbyPlacesAr,
                PropertyTypes = project.PropertyTypes,
                Latitude = project.Latitude,
                Longitude = project.Longitude,
                TotalArea = project.TotalArea,
                OwnershipType = project.OwnershipType,
                UnitCount = project.UnitCount,
                Slug = project.Slug,
                SeoTitle = project.SeoTitle ?? string.Empty,
                SeoDescription = project.SeoDescription ?? string.Empty,
                SeoTitleAr = project.SeoTitleAr ?? string.Empty,
                SeoDescriptionAr = project.SeoDescriptionAr ?? string.Empty,
                SeoKeywords = project.SeoKeywords ?? string.Empty,
                SeoKeywordsAr = project.SeoKeywordsAr ?? string.Empty,
                CanonicalUrl = project.CanonicalUrl ?? string.Empty,
                DeliveryText = project.DeliveryText,
                DeliveryTextAr = project.DeliveryTextAr,
                IsRecommended = project.IsRecommended,
                ConstructionStatus = project.ConstructionStatus?.ToString(),
                AvailabilityStatus = project.AvailabilityStatus ?? "Available",
                ViewCount = project.ViewCount,
                InquiryCount = project.InquiryCount,
                FavoriteCount = project.FavoriteCount,
                VirtualTourUrl = project.VirtualTourUrl,
                JsonLd = BuildJsonLd(project, imageUrls),
                Videos = project.Videos?.Where(v => !v.IsDeleted).OrderBy(v => v.SortOrder).Select(v => new VideoDto
                {
                    Id = v.Id,
                    Url = v.Url,
                    PublicId = v.PublicId,
                    ThumbnailUrl = v.ThumbnailUrl,
                    Duration = v.Duration,
                    Width = v.Width,
                    Height = v.Height,
                    Title = v.Title,
                    SortOrder = v.SortOrder,
                    IsMain = v.IsMain,
                    CreatedAt = v.CreatedAt
                }).ToList() ?? new List<VideoDto>()
            };
        }

        private void ApplySeoEnhancements(ProjectDetailsDto dto, Domain.Entities.Project project)
        {
            try
            {
                var seoContent = _seoContentGenerator.Generate(
                    SeoEntityType.Project,
                    project.NameEn, project.NameAr,
                    project.DescriptionEn, project.DescriptionAr,
                    project.Location, null, null,
                    0, "EGP", null,
                    project.Developer, project.NameEn);

                if (string.IsNullOrWhiteSpace(dto.SeoTitle))
                    dto.SeoTitle = seoContent.TitleEn;
                if (string.IsNullOrWhiteSpace(dto.SeoDescription))
                    dto.SeoDescription = seoContent.DescriptionEn;
                if (string.IsNullOrWhiteSpace(dto.SeoKeywords))
                    dto.SeoKeywords = seoContent.PrimaryKeyword;

                var faqs = seoContent.Faqs
                    .Select(f => (f.QuestionEn, f.AnswerEn))
                    .ToList();
                if (faqs.Count > 0)
                    dto.FaqJsonLd = _jsonLdService.BuildFaqJsonLd(faqs);

                var canonicalUrl = dto.CanonicalUrl;
                if (!string.IsNullOrWhiteSpace(canonicalUrl))
                {
                    var breadcrumbItems = new List<(string, string)>
                    {
                        ("Home", "/"),
                        ("Projects", "/projects"),
                        (project.NameEn ?? project.Slug ?? "Details", canonicalUrl)
                    };
                    dto.BreadcrumbJsonLd = _jsonLdService.BuildBreadcrumbJsonLd(breadcrumbItems);
                }

                var internalLinks = _internalLinkingService.GenerateLinks(
                    project.Location, null, null, project.Slug, null);

                if (!_internalLinkingService.MeetsMinimumRequirement(internalLinks))
                {
                    var missing = _internalLinkingService.GetMissingLinks(
                        project.Location, null, null, project.Slug);
                    if (missing.Count > 0)
                        internalLinks.AddRange(missing);
                }

                dto.InternalLinksJson = InternalLinkingService.ToJson(internalLinks);

                var serpRequest = new SerpVariantRequest
                {
                    EntityType = SeoEntityType.Project,
                    TitleEn = project.NameEn,
                    TitleAr = project.NameAr,
                    DescriptionEn = project.DescriptionEn,
                    DescriptionAr = project.DescriptionAr,
                    Location = project.Location
                };
                var variants = _serpVariantGenerator.GenerateVariants(serpRequest);
                var bestVariant = _serpVariantGenerator.SelectBestVariant(variants);
                if (bestVariant.PredictedCtrScore > 75)
                {
                    dto.SeoTitle = dto.SeoTitle ?? bestVariant.TitleEn;
                    dto.SeoDescription = dto.SeoDescription ?? bestVariant.DescriptionEn;
                }

                var entityNode = _entityGraphService.BuildEntityNode(
                    "project", project.Slug ?? project.Id.ToString(),
                    project.NameEn ?? "", project.DescriptionEn);
                var entityGraph = _entityGraphService.BuildKnowledgeGraph(
                    "project", project.Slug ?? project.Id.ToString());
                if (!string.IsNullOrWhiteSpace(entityGraph.JsonLd))
                    dto.EntityGraphJson = entityGraph.JsonLd;

                _clickBehavior.RecordImpression($"/projects/{project.Slug}");
            }
            catch (System.Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to apply SEO enhancements for project {Id}", project.Id);
            }
        }

        private string BuildJsonLd(Domain.Entities.Project p, List<string> images)
        {
            return _jsonLdService.BuildProjectJsonLd(
                p.NameEn,
                p.DescriptionEn,
                p.SeoDescription,
                p.CanonicalUrl,
                p.Location,
                p.Developer,
                images);
        }
    }
}
