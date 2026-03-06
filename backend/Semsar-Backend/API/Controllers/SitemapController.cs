using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace API.Controllers
{
    [ApiController]
    [EnableRateLimiting("sitemap")]
    public class SitemapController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly string _baseUrl;
        private readonly ICrawlBudgetOptimizer _crawlOptimizer;
        private readonly IIndexControlService _indexControl;
        private readonly ILogger<SitemapController> _logger;

        public SitemapController(
            IUnitOfWork uow,
            IOptions<AppSettings> settings,
            ICrawlBudgetOptimizer crawlOptimizer,
            IIndexControlService indexControl,
            ILogger<SitemapController> logger)
        {
            _uow = uow;
            _baseUrl = (settings.Value.BaseUrl ?? "").TrimEnd('/');
            _crawlOptimizer = crawlOptimizer;
            _indexControl = indexControl;
            _logger = logger;
        }

        [HttpGet("sitemap.xml")]
        public IActionResult SitemapIndex()
        {
            if (string.IsNullOrWhiteSpace(_baseUrl))
                return BadRequest("AppSettings:BaseUrl is not configured");

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<sitemapindex xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
            sb.AppendLine($"  <sitemap><loc>{_baseUrl}/sitemap-properties.xml</loc></sitemap>");
            sb.AppendLine($"  <sitemap><loc>{_baseUrl}/sitemap-projects.xml</loc></sitemap>");
            sb.AppendLine($"  <sitemap><loc>{_baseUrl}/sitemap-units.xml</loc></sitemap>");
            sb.AppendLine($"  <sitemap><loc>{_baseUrl}/sitemap-locations.xml</loc></sitemap>");
            sb.AppendLine($"  <sitemap><loc>{_baseUrl}/sitemap-static.xml</loc></sitemap>");
            sb.AppendLine("</sitemapindex>");
            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }

        [HttpGet("sitemap-properties.xml")]
        public async Task<IActionResult> SitemapProperties(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_baseUrl))
                return BadRequest("AppSettings:BaseUrl is not configured");

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" xmlns:xhtml=\"http://www.w3.org/1999/xhtml\">");

            try
            {
                var propertyUrls = await _uow.Properties.Query()
                    .Where(p => !p.IsDeleted && !string.IsNullOrEmpty(p.Slug))
                    .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                    .Select(p => new { p.Slug, p.UpdatedAt, p.CreatedAt, p.ViewCount })
                    .AsNoTracking()
                    .ToListAsync(ct);

                foreach (var p in propertyUrls)
                {
                    var url = $"{_baseUrl}/properties/{p.Slug}";
                    if (_indexControl.ShouldBlockFromSitemap(url, "property"))
                        continue;

                    var lastMod = (p.UpdatedAt ?? p.CreatedAt).ToString("yyyy-MM-dd");
                    var changeFreq = _crawlOptimizer.SuggestChangeFrequency("property", p.UpdatedAt ?? p.CreatedAt, p.ViewCount);
                    var priority = p.ViewCount > 1000 ? "0.9" : p.ViewCount > 100 ? "0.8" : p.ViewCount > 10 ? "0.7" : "0.6";
                    var enUrl = $"{_baseUrl}/en/properties/{p.Slug}";
                    var arUrl = $"{_baseUrl}/ar/properties/{p.Slug}";
                    sb.AppendLine($"  <url><loc>{enUrl}</loc><xhtml:link rel=\"alternate\" hreflang=\"en\" href=\"{enUrl}\"/><xhtml:link rel=\"alternate\" hreflang=\"ar\" href=\"{arUrl}\"/><xhtml:link rel=\"alternate\" hreflang=\"x-default\" href=\"{enUrl}\"/><lastmod>{lastMod}</lastmod><changefreq>{changeFreq}</changefreq><priority>{priority}</priority></url>");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch property URLs for sitemap");
            }

            sb.AppendLine("</urlset>");
            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }

        [HttpGet("sitemap-projects.xml")]
        public async Task<IActionResult> SitemapProjects(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_baseUrl))
                return BadRequest("AppSettings:BaseUrl is not configured");

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" xmlns:xhtml=\"http://www.w3.org/1999/xhtml\">");

            try
            {
                var projectUrls = await _uow.Projects.Query()
                    .Where(p => !p.IsDeleted && !string.IsNullOrEmpty(p.Slug))
                    .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                    .Select(p => new { p.Slug, p.UpdatedAt, p.CreatedAt })
                    .AsNoTracking()
                    .ToListAsync(ct);

                foreach (var p in projectUrls)
                {
                    var url = $"{_baseUrl}/projects/{p.Slug}";
                    if (_indexControl.ShouldBlockFromSitemap(url, "project"))
                        continue;

                    var lastMod = (p.UpdatedAt ?? p.CreatedAt).ToString("yyyy-MM-dd");
                    var enUrl = $"{_baseUrl}/en/projects/{p.Slug}";
                    var arUrl = $"{_baseUrl}/ar/projects/{p.Slug}";
                    sb.AppendLine($"  <url><loc>{enUrl}</loc><xhtml:link rel=\"alternate\" hreflang=\"en\" href=\"{enUrl}\"/><xhtml:link rel=\"alternate\" hreflang=\"ar\" href=\"{arUrl}\"/><xhtml:link rel=\"alternate\" hreflang=\"x-default\" href=\"{enUrl}\"/><lastmod>{lastMod}</lastmod><changefreq>weekly</changefreq><priority>0.7</priority></url>");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch project URLs for sitemap");
            }

            sb.AppendLine("</urlset>");
            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }

        [HttpGet("sitemap-units.xml")]
        public async Task<IActionResult> SitemapUnits(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_baseUrl))
                return BadRequest("AppSettings:BaseUrl is not configured");

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" xmlns:xhtml=\"http://www.w3.org/1999/xhtml\">");

            try
            {
                var unitUrls = await _uow.Units.Query()
                    .Where(u => !u.IsDeleted && !string.IsNullOrEmpty(u.Slug))
                    .OrderByDescending(u => u.UpdatedAt ?? u.CreatedAt)
                    .Select(u => new { u.Slug, u.UpdatedAt, u.CreatedAt })
                    .AsNoTracking()
                    .ToListAsync(ct);

                foreach (var u in unitUrls)
                {
                    var url = $"{_baseUrl}/units/{u.Slug}";
                    if (_indexControl.ShouldBlockFromSitemap(url, "unit"))
                        continue;

                    var lastMod = (u.UpdatedAt ?? u.CreatedAt).ToString("yyyy-MM-dd");
                    var enUrl = $"{_baseUrl}/en/units/{u.Slug}";
                    var arUrl = $"{_baseUrl}/ar/units/{u.Slug}";
                    sb.AppendLine($"  <url><loc>{enUrl}</loc><xhtml:link rel=\"alternate\" hreflang=\"en\" href=\"{enUrl}\"/><xhtml:link rel=\"alternate\" hreflang=\"ar\" href=\"{arUrl}\"/><xhtml:link rel=\"alternate\" hreflang=\"x-default\" href=\"{enUrl}\"/><lastmod>{lastMod}</lastmod><changefreq>weekly</changefreq><priority>0.6</priority></url>");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch unit URLs for sitemap");
            }

            sb.AppendLine("</urlset>");
            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }

        [HttpGet("sitemap-locations.xml")]
        public IActionResult SitemapLocations()
        {
            if (string.IsNullOrWhiteSpace(_baseUrl))
                return BadRequest("AppSettings:BaseUrl is not configured");

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
            sb.AppendLine("</urlset>");
            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }

        [HttpGet("sitemap-static.xml")]
        public IActionResult SitemapStatic()
        {
            if (string.IsNullOrWhiteSpace(_baseUrl))
                return BadRequest("AppSettings:BaseUrl is not configured");

            var pages = new[]
            {
                ("/", "daily", "1.0"),
                ("/projects", "daily", "0.8"),
                ("/about", "monthly", "0.5"),
                ("/contact", "monthly", "0.5")
            };

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" xmlns:xhtml=\"http://www.w3.org/1999/xhtml\">");

            foreach (var (path, freq, priority) in pages)
            {
                var enUrl = $"{_baseUrl}/en{path}";
                var arUrl = $"{_baseUrl}/ar{path}";
                sb.AppendLine($"  <url><loc>{enUrl}</loc><xhtml:link rel=\"alternate\" hreflang=\"en\" href=\"{enUrl}\"/><xhtml:link rel=\"alternate\" hreflang=\"ar\" href=\"{arUrl}\"/><xhtml:link rel=\"alternate\" hreflang=\"x-default\" href=\"{enUrl}\"/><changefreq>{freq}</changefreq><priority>{priority}</priority></url>");
            }

            sb.AppendLine("</urlset>");
            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }

        [HttpGet("robots.txt")]
        public IActionResult Robots()
        {
            if (string.IsNullOrWhiteSpace(_baseUrl))
                return BadRequest("AppSettings:BaseUrl is not configured");

            var robots = $"""
                User-agent: *
                Allow: /
                Disallow: /admin/
                Disallow: /api/auth/
                Disallow: /api/seo/
                Disallow: /swagger/
                Disallow: /jobs/

                Sitemap: {_baseUrl}/sitemap.xml
                """;

            return Content(robots, "text/plain", Encoding.UTF8);
        }
    }
}
