using System.Globalization;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [EnableRateLimiting("fixed")]
    public class LandingController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly ICacheService _cache;
        private readonly ICanonicalService _canonicalService;
        private readonly ILocationSeoService _locationSeo;
        private readonly IInternalLinkingService _internalLinkingService;
        private readonly IEntityGraphService _entityGraphService;
        private readonly ISlugService _slugService;

        public LandingController(
            IUnitOfWork uow,
            ICacheService cache,
            ICanonicalService canonicalService,
            ILocationSeoService locationSeo,
            IInternalLinkingService internalLinkingService,
            IEntityGraphService entityGraphService,
            ISlugService slugService)
        {
            _uow = uow;
            _cache = cache;
            _canonicalService = canonicalService;
            _locationSeo = locationSeo;
            _internalLinkingService = internalLinkingService;
            _entityGraphService = entityGraphService;
            _slugService = slugService;
        }

        [HttpGet("properties/{location}")]
        public async Task<IActionResult> PropertiesByLocation(string location, [FromQuery] string? lang, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(location))
                return BadRequest(new { message = "Location is required" });

            lang = string.IsNullOrWhiteSpace(lang) ? "en" : lang.ToLowerInvariant();
            var key = $"landing_{location}_{lang}";
            var cached = _cache.Get<string>(key);
            if (cached != null)
                return Content(cached, "text/html", Encoding.UTF8);

            var loc = location.Trim();
            var seoData = await _locationSeo.GenerateLocationSeoAsync(loc);
            var chosenTitle = lang == "ar" ? seoData.TitleAr : seoData.TitleEn;
            var chosenDesc = lang == "ar" ? seoData.DescriptionAr : seoData.DescriptionEn;

            var props = await _uow.Properties.Query().AsNoTracking()
                .Where(p => (p.Location == loc || (p.LocationAr != null && p.LocationAr == loc)) && !p.IsDeleted && !string.IsNullOrEmpty(p.Slug))
                .OrderBy(p => p.SortOrder).ThenByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Select(p => new
                {
                    p.Id,
                    p.Slug,
                    p.TitleEn,
                    p.TitleAr,
                    p.Price,
                    Image = p.Images != null && p.Images.Any() ? p.Images.OrderBy(i => i.Id).Select(i => i.Url).FirstOrDefault() : string.Empty,
                    p.IsFeatured
                }).Take(100).ToListAsync(ct);

            var projects = await _uow.Projects.Query().AsNoTracking()
                .Where(pr => pr.Location == loc && !pr.IsDeleted && !string.IsNullOrEmpty(pr.Slug))
                .OrderBy(pr => pr.UpdatedAt ?? pr.CreatedAt)
                .Select(pr => new { pr.Id, pr.Slug, NameEn = pr.NameEn, NameAr = pr.NameAr, Image = pr.Image })
                .Take(20).ToListAsync(ct);

            var otherLocations = await _uow.Properties.Query().AsNoTracking()
                .Where(p => !p.IsDeleted && !string.IsNullOrEmpty(p.Location))
                .Select(p => p.Location)
                .Distinct()
                .OrderBy(x => x)
                .Take(100)
                .ToListAsync(ct);

            var otherLocationsAr = await _uow.Properties.Query().AsNoTracking()
                .Where(p => !p.IsDeleted && !string.IsNullOrEmpty(p.LocationAr))
                .Select(p => p.LocationAr!)
                .Distinct()
                .OrderBy(x => x)
                .Take(100)
                .ToListAsync(ct);

            otherLocations = otherLocations.Union(otherLocationsAr, StringComparer.OrdinalIgnoreCase).Take(100).ToList();

            var canonical = _canonicalService.BuildCanonical("properties", _slugService.Slugify(loc, lang));
            var internalLinks = _internalLinkingService.GenerateLinks(loc, null, null, null, null);
            var locationNode = _entityGraphService.BuildEntityNode("location", loc, CultureInfo.CurrentCulture.TextInfo.ToTitleCase(loc), chosenDesc);
            var entityGraph = _entityGraphService.BuildKnowledgeGraph("location", loc);

            var sb = new StringBuilder();
            sb.AppendLine("<!doctype html>");
            sb.AppendLine("<html lang=\"" + WebUtility.HtmlEncode(lang) + "\">\n<head>");
            sb.AppendLine("<title>" + WebUtility.HtmlEncode(chosenTitle) + "</title>");
            sb.AppendLine("<meta name=\"description\" content=\"" + WebUtility.HtmlEncode(chosenDesc) + "\" />");
            sb.AppendLine("<link rel=\"canonical\" href=\"" + WebUtility.HtmlEncode(canonical) + "\" />");
            sb.AppendLine("<meta property=\"og:title\" content=\"" + WebUtility.HtmlEncode(chosenTitle) + "\" />");
            sb.AppendLine("<meta property=\"og:description\" content=\"" + WebUtility.HtmlEncode(chosenDesc) + "\" />");
            if (!string.IsNullOrWhiteSpace(seoData.LocationJsonLd))
                sb.AppendLine($"<script type=\"application/ld+json\">{seoData.LocationJsonLd}</script>");
            if (!string.IsNullOrWhiteSpace(entityGraph.JsonLd))
                sb.AppendLine($"<script type=\"application/ld+json\">{entityGraph.JsonLd}</script>");
            if (props.Any() && !string.IsNullOrWhiteSpace(props.First().Image))
                sb.AppendLine("<meta property=\"og:image\" content=\"" + WebUtility.HtmlEncode(props.First().Image) + "\" />");
            sb.AppendLine("</head><body>");
            sb.AppendLine("<h1>" + WebUtility.HtmlEncode(chosenTitle) + "</h1>");
            sb.AppendLine("<p>" + WebUtility.HtmlEncode(chosenDesc) + "</p>");

            sb.AppendLine("<section id=\"properties\"><h2>Listings</h2><ul>");
            foreach (var p in props)
            {
                var title = lang == "ar" ? p.TitleAr ?? p.TitleEn : p.TitleEn;
                var link = _canonicalService.BuildCanonical("property", p.Slug);
                sb.AppendLine("<li><a href=\"" + WebUtility.HtmlEncode(link) + "\">" + WebUtility.HtmlEncode(title) + "</a> - " + WebUtility.HtmlEncode(p.Price.ToString()) + "</li>");
            }
            sb.AppendLine("</ul></section>");

            sb.AppendLine("<section id=\"projects\"><h2>Projects</h2><ul>");
            foreach (var pr in projects)
            {
                var name = lang == "ar" ? pr.NameAr ?? pr.NameEn : pr.NameEn;
                var link = _canonicalService.BuildCanonical("projects", pr.Slug);
                sb.AppendLine("<li><a href=\"" + WebUtility.HtmlEncode(link) + "\">" + WebUtility.HtmlEncode(name) + "</a></li>");
            }
            sb.AppendLine("</ul></nav>");

            if (internalLinks.Count > 0)
            {
                sb.AppendLine("<nav id=\"internal-links\">");
                foreach (var group in internalLinks)
                {
                    sb.AppendLine($"<h3>{WebUtility.HtmlEncode(group.SectionTitle)}</h3><ul>");
                    foreach (var link in group.Links)
                    {
                        sb.AppendLine($"<li><a href=\"{WebUtility.HtmlEncode(link.Url)}\">{WebUtility.HtmlEncode(link.Text)}</a></li>");
                    }
                    sb.AppendLine("</ul>");
                }
                sb.AppendLine("</nav>");
            }

            sb.AppendLine("<section id=\"faq\"><h3>FAQ</h3>");
            if (lang == "ar")
            {
                sb.AppendLine("<p>الأسئلة الشائعة حول العقارات في " + WebUtility.HtmlEncode(loc) + "</p>");
                sb.AppendLine("<p>ابحث عن الوحدات، المشاريع، والأسعار بسهولة.</p>");
            }
            else
            {
                sb.AppendLine("<p>Frequently asked questions about properties in " + WebUtility.HtmlEncode(loc) + ".</p>");
                sb.AppendLine("<p>Find units, projects and pricing easily.</p>");
            }
            sb.AppendLine("</section>");

            sb.AppendLine("</body></html>");

            var html = sb.ToString();
            _cache.Set(key, html, TimeSpan.FromMinutes(5));
            _cache.RegisterKey(key);
            return Content(html, "text/html", Encoding.UTF8);
        }

        [HttpGet("projects/{location}")]
        public async Task<IActionResult> ProjectsByLocation(string location, [FromQuery] string? lang, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(location))
                return BadRequest(new { message = "Location is required" });

            lang = string.IsNullOrWhiteSpace(lang) ? "en" : lang.ToLowerInvariant();
            var key = $"landing_projects_{location}_{lang}";
            var cached = _cache.Get<string>(key);
            if (cached != null)
                return Content(cached, "text/html", Encoding.UTF8);

            var loc = location.Trim();
            var seoData = await _locationSeo.GenerateLocationSeoAsync(loc);
            var chosenTitle = lang == "ar" ? seoData.TitleAr : seoData.TitleEn;
            var chosenDesc = lang == "ar" ? seoData.DescriptionAr : seoData.DescriptionEn;

            var projects = await _uow.Projects.Query().AsNoTracking()
                .Where(pr => (pr.Location == loc || (pr.LocationAr != null && pr.LocationAr == loc)) && !pr.IsDeleted && !string.IsNullOrEmpty(pr.Slug))
                .OrderBy(pr => pr.UpdatedAt ?? pr.CreatedAt)
                .Select(pr => new { pr.Id, pr.Slug, pr.NameEn, pr.NameAr, pr.Image })
                .Take(100).ToListAsync(ct);

            var featuredProps = await _uow.Properties.Query().AsNoTracking()
                .Where(p => (p.Location == loc || (p.LocationAr != null && p.LocationAr == loc)) && !p.IsDeleted && p.IsFeatured && !string.IsNullOrEmpty(p.Slug))
                .OrderBy(p => p.SortOrder).ThenByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Select(p => new { p.Id, p.Slug, p.TitleEn, p.TitleAr, Image = p.Images != null && p.Images.Any() ? p.Images.OrderBy(i => i.Id).Select(i => i.Url).FirstOrDefault() : string.Empty })
                .Take(20).ToListAsync(ct);

            var otherLocations = await _uow.Projects.Query().AsNoTracking()
                .Where(p => !p.IsDeleted && !string.IsNullOrEmpty(p.Location))
                .Select(p => p.Location)
                .Distinct().OrderBy(x => x).Take(100).ToListAsync(ct);

            var otherLocationsAr = await _uow.Projects.Query().AsNoTracking()
                .Where(p => !p.IsDeleted && !string.IsNullOrEmpty(p.LocationAr))
                .Select(p => p.LocationAr!)
                .Distinct().OrderBy(x => x).Take(100)
                .ToListAsync(ct);

            otherLocations = otherLocations.Union(otherLocationsAr, StringComparer.OrdinalIgnoreCase).Take(100).ToList();

            var canonical = _canonicalService.BuildCanonical("projects", _slugService.Slugify(loc, lang));
            var internalLinks = _internalLinkingService.GenerateLinks(loc, null, null, null, null);
            var locationNode = _entityGraphService.BuildEntityNode("location", loc, CultureInfo.CurrentCulture.TextInfo.ToTitleCase(loc), chosenDesc);
            var entityGraph = _entityGraphService.BuildKnowledgeGraph("location", loc);

            var sb = new StringBuilder();
            sb.AppendLine("<!doctype html>");
            sb.AppendLine("<html lang=\"" + WebUtility.HtmlEncode(lang) + "\">\n<head>");
            sb.AppendLine("<title>" + WebUtility.HtmlEncode(chosenTitle) + "</title>");
            sb.AppendLine("<meta name=\"description\" content=\"" + WebUtility.HtmlEncode(chosenDesc) + "\" />");
            sb.AppendLine("<link rel=\"canonical\" href=\"" + WebUtility.HtmlEncode(canonical) + "\" />");
            sb.AppendLine("<meta property=\"og:title\" content=\"" + WebUtility.HtmlEncode(chosenTitle) + "\" />");
            sb.AppendLine("<meta property=\"og:description\" content=\"" + WebUtility.HtmlEncode(chosenDesc) + "\" />");
            if (!string.IsNullOrWhiteSpace(seoData.LocationJsonLd))
                sb.AppendLine($"<script type=\"application/ld+json\">{seoData.LocationJsonLd}</script>");
            if (!string.IsNullOrWhiteSpace(entityGraph.JsonLd))
                sb.AppendLine($"<script type=\"application/ld+json\">{entityGraph.JsonLd}</script>");
            if (projects.Any() && !string.IsNullOrWhiteSpace(projects.First().Image))
                sb.AppendLine("<meta property=\"og:image\" content=\"" + WebUtility.HtmlEncode(projects.First().Image) + "\" />");
            sb.AppendLine("</head><body>");
            sb.AppendLine("<h1>" + WebUtility.HtmlEncode(chosenTitle) + "</h1>");
            sb.AppendLine("<p>" + WebUtility.HtmlEncode(chosenDesc) + "</p>");

            sb.AppendLine("<section id=\"projects-list\"><h2>Projects</h2><ul>");
            foreach (var pr in projects)
            {
                var name = lang == "ar" ? pr.NameAr ?? pr.NameEn : pr.NameEn;
                var link = _canonicalService.BuildCanonical("projects", pr.Slug);
                sb.AppendLine("<li><a href=\"" + WebUtility.HtmlEncode(link) + "\">" + WebUtility.HtmlEncode(name) + "</a></li>");
            }
            sb.AppendLine("</ul></section>");

            sb.AppendLine("<section id=\"featured\"><h2>Featured Properties</h2><ul>");
            foreach (var p in featuredProps)
            {
                var title = lang == "ar" ? p.TitleAr ?? p.TitleEn : p.TitleEn;
                var link = _canonicalService.BuildCanonical("property", p.Slug);
                sb.AppendLine("<li><a href=\"" + WebUtility.HtmlEncode(link) + "\">" + WebUtility.HtmlEncode(title) + "</a></li>");
            }
            sb.AppendLine("</ul></section>");

            if (internalLinks.Count > 0)
            {
                sb.AppendLine("<nav id=\"internal-links\">");
                foreach (var group in internalLinks)
                {
                    sb.AppendLine($"<h3>{WebUtility.HtmlEncode(group.SectionTitle)}</h3><ul>");
                    foreach (var link in group.Links)
                    {
                        sb.AppendLine($"<li><a href=\"{WebUtility.HtmlEncode(link.Url)}\">{WebUtility.HtmlEncode(link.Text)}</a></li>");
                    }
                    sb.AppendLine("</ul>");
                }
                sb.AppendLine("</nav>");
            }

            sb.AppendLine("<nav><h3>Other locations</h3><ul>");
            foreach (var ol in otherLocations.Where(x => x != loc).Take(20))
            {
                var lslug = _slugService.Slugify(ol, lang);
                var projsUrl = _canonicalService.BuildCanonical("projects", lslug);
                sb.AppendLine("<li><a href=\"" + WebUtility.HtmlEncode(projsUrl) + "\">Explore " + WebUtility.HtmlEncode(ol) + " projects</a></li>");
            }
            sb.AppendLine("</ul></nav>");

            sb.AppendLine("</body></html>");

            var html = sb.ToString();
            _cache.Set(key, html, TimeSpan.FromMinutes(5));
            _cache.RegisterKey(key);
            return Content(html, "text/html", Encoding.UTF8);
        }
    }
}
