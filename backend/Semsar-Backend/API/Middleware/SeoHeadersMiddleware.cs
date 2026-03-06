using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace API.Middleware;

public class SeoHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SeoHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var query = context.Request.QueryString.Value ?? "";

        if (!path.StartsWith("/api/", System.StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/swagger", System.StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/jobs", System.StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/healthz", System.StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/readyz", System.StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/metrics", System.StringComparison.OrdinalIgnoreCase))
        {
            var indexControl = context.RequestServices.GetRequiredService<IIndexControlService>();
            var fullUrl = $"{context.Request.Scheme}://{context.Request.Host}{path}{query}";

            var entityType = path switch
            {
                string p when p.Contains("/properties/") => "property",
                string p when p.Contains("/projects/") => "project",
                string p when p.Contains("/units/") => "unit",
                _ => "page"
            };

            var directive = indexControl.GetIndexDirective(fullUrl, entityType, 0.7);
            context.Response.Headers["X-Robots-Tag"] = directive.RobotsTag;

            if (path.Contains("/properties/") || path.Contains("/projects/") || path.Contains("/units/"))
            {
                context.Response.Headers["Content-Language"] = "en";
            }
        }

        await _next(context);
    }
}
