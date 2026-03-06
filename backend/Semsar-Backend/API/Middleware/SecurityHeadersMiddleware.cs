using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace API.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _cspPolicy;
    private readonly string _cspPolicySwagger;

    public SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;

        var baseUrl = configuration["AppSettings:BaseUrl"] ?? "";
        var cloudinaryHost = "https://res.cloudinary.com";
        var cloudinaryApiHost = "https://api.cloudinary.com";
        var frontendOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        var fontOrigins = configuration.GetSection("Csp:FontOrigins").Get<string[]>() ?? ["https://fonts.googleapis.com", "https://fonts.gstatic.com"];

        var imgSrc = new List<string> { "'self'", cloudinaryHost, "data:", "blob:" };
        var scriptSrc = new List<string> { "'self'" };
        var styleSrc = new List<string> { "'self'", "'unsafe-inline'" };
        var connectSrc = new List<string> { "'self'", cloudinaryHost, cloudinaryApiHost };
        var fontSrc = new List<string> { "'self'", "data:" };

        foreach (var origin in frontendOrigins)
        {
            if (!string.IsNullOrWhiteSpace(origin))
            {
                connectSrc.Add(origin.TrimEnd('/'));
            }
        }

        foreach (var origin in fontOrigins)
        {
            if (!string.IsNullOrWhiteSpace(origin))
            {
                fontSrc.Add(origin.TrimEnd('/'));
            }
        }

        var baseCsp = $"default-src 'self'; " +
            $"img-src {string.Join(" ", imgSrc)}; " +
            $"style-src {string.Join(" ", styleSrc)}; " +
            $"media-src 'self' {cloudinaryHost}; " +
            $"connect-src {string.Join(" ", connectSrc)}; " +
            $"font-src {string.Join(" ", fontSrc)}; " +
            "frame-ancestors 'none'; " +
            "form-action 'self'; " +
            "base-uri 'self'; " +
            "object-src 'none'; " +
            "worker-src 'self'; " +
            "upgrade-insecure-requests";

        _cspPolicy = $"script-src {string.Join(" ", scriptSrc)}; {baseCsp}";

        // Swagger UI needs 'unsafe-inline' for its inline scripts
        _cspPolicySwagger = $"script-src 'self' 'unsafe-inline'; {baseCsp}";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var response = context.Response;
        var request = context.Request;
        var path = request.Path.Value ?? "";

        // Relax CSP for Swagger UI (inline scripts required by Swagger.js)
        var isSwagger = path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);

        // Security headers
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["X-Frame-Options"] = "DENY";
        response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), display-capture=(), fullscreen=(self), payment=(), usb=(), magnetometer=(), accelerometer=(), gyroscope=(), clipboard-write=(self), clipboard-read=()";

        // Cross-Origin opener policy (security + OAuth compatibility)
        response.Headers["Cross-Origin-Opener-Policy"] = "same-origin-allow-popups";

        // Content Security Policy
        response.Headers["Content-Security-Policy"] = isSwagger ? _cspPolicySwagger : _cspPolicy;

        // HSTS
        if (request.IsHttps)
        {
            response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
        }

        // Cache-Control for API responses (prevent browser caching of dynamic data)
        if (!response.Headers.ContainsKey("Cache-Control"))
        {
            response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        }

        await _next(context);
    }
}
