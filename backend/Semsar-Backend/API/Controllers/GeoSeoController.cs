using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers;

[EnableRateLimiting("fixed")]

[ApiController]
public class GeoSeoController : ControllerBase
{
    private readonly ILocationSeoService _locationSeo;
    private readonly IPropertyQueryService _propertyQuery;
    private readonly IInternalLinkingService _internalLinkingService;
    private readonly IEntityGraphService _entityGraphService;
    private readonly ICanonicalService _canonicalService;
    private readonly IOgMetaService _ogMetaService;

    public GeoSeoController(
        ILocationSeoService locationSeo,
        IPropertyQueryService propertyQuery,
        IInternalLinkingService internalLinkingService,
        IEntityGraphService entityGraphService,
        ICanonicalService canonicalService,
        IOgMetaService ogMetaService)
    {
        _locationSeo = locationSeo;
        _propertyQuery = propertyQuery;
        _internalLinkingService = internalLinkingService;
        _entityGraphService = entityGraphService;
        _canonicalService = canonicalService;
        _ogMetaService = ogMetaService;
    }

    [HttpGet("geo/{*geoPath}")]
    public async Task<IActionResult> PropertiesByLocation(string geoPath, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(geoPath))
            return RedirectPermanent("/");

        try
        {
            var location = geoPath.Replace("-", " ").Replace("/", " ").Trim();
            var seoData = await _locationSeo.GenerateLocationSeoAsync(location);

            var (data, total, currentPage, currentPageSize, totalPages, seoTitle, seoDescription) =
                await _propertyQuery.GetByLocationAsync(location, page, pageSize, ct);

            var canonicalUrl = _canonicalService.BuildCanonical("geo", geoPath);
            var internalLinks = _internalLinkingService.GenerateLinks(location, null, null, null, null);
            var ogMeta = _ogMetaService.BuildPropertyOgMeta(
            seoData.TitleEn, seoData.TitleAr, seoData.DescriptionEn, seoData.DescriptionAr,
            canonicalUrl, data.Select(p => p.Images?.FirstOrDefault() ?? "").ToList());
            var locationNode = _entityGraphService.BuildEntityNode("location", geoPath, CultureInfo.CurrentCulture.TextInfo.ToTitleCase(location), seoData.DescriptionEn);
            var entityGraph = _entityGraphService.BuildKnowledgeGraph("location", geoPath);

            var html = BuildGeoHtml(seoData, data, location, page, totalPages, canonicalUrl, internalLinks, entityGraph.JsonLd);

            return Content(html, "text/html", System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to generate geo page", detail = ex.Message });
        }
    }

    private string BuildGeoHtml(
        LocationSeoData seoData,
        System.Collections.Generic.List<Application.DTOs.PropertyPublicDto> properties,
        string location,
        int page,
        int totalPages,
        string canonicalUrl,
        System.Collections.Generic.List<InternalLinkGroup> internalLinks,
        string entityGraphJson)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine($"<title>{System.Net.WebUtility.HtmlEncode(seoData.TitleEn)}</title>");
        sb.AppendLine($"<meta name=\"description\" content=\"{System.Net.WebUtility.HtmlEncode(seoData.DescriptionEn)}\">");
        sb.AppendLine($"<meta name=\"keywords\" content=\"{System.Net.WebUtility.HtmlEncode(seoData.PrimaryKeyword)}\">");
        sb.AppendLine($"<link rel=\"canonical\" href=\"{System.Net.WebUtility.HtmlEncode(canonicalUrl)}\">");
        sb.AppendLine($"<script type=\"application/ld+json\">{seoData.LocationJsonLd}</script>");
        if (!string.IsNullOrWhiteSpace(entityGraphJson))
            sb.AppendLine($"<script type=\"application/ld+json\">{entityGraphJson}</script>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine($"<h1>{System.Net.WebUtility.HtmlEncode(seoData.H1En)}</h1>");

        sb.AppendLine("<section id=\"properties\">");
        foreach (var prop in properties)
        {
            sb.AppendLine($"<article>");
            sb.AppendLine($"<h2><a href=\"/property/{System.Net.WebUtility.HtmlEncode(prop.Slug)}\">{System.Net.WebUtility.HtmlEncode(prop.TitleEn)}</a></h2>");
            sb.AppendLine($"<p>{System.Net.WebUtility.HtmlEncode(prop.DescriptionEn)}</p>");
            sb.AppendLine($"<p>Price: {prop.Price:N0} {prop.Currency}</p>");
            sb.AppendLine($"</article>");
        }
        sb.AppendLine("</section>");

        if (totalPages > 1)
        {
            sb.AppendLine("<nav>");
            for (int i = 1; i <= totalPages; i++)
            {
                if (i == page)
                    sb.AppendLine($"<span>{i}</span>");
                else
                    sb.AppendLine($"<a href=\"/geo/{location.ToLowerInvariant().Replace(" ", "-")}?page={i}\">{i}</a>");
            }
            sb.AppendLine("</nav>");
        }

        if (internalLinks.Count > 0)
        {
            sb.AppendLine("<nav id=\"internal-links\">");
            foreach (var group in internalLinks)
            {
                sb.AppendLine($"<h3>{System.Net.WebUtility.HtmlEncode(group.SectionTitle)}</h3><ul>");
                foreach (var link in group.Links)
                {
                    sb.AppendLine($"<li><a href=\"{System.Net.WebUtility.HtmlEncode(link.Url)}\">{System.Net.WebUtility.HtmlEncode(link.Text)}</a></li>");
                }
                sb.AppendLine("</ul>");
            }
            sb.AppendLine("</nav>");
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }
}
