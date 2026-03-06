using API.Services;

namespace API.Middleware;

public class DistributedRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDistributedRateLimitStore _store;
    private readonly ILogger<DistributedRateLimitingMiddleware> _logger;

    private static readonly Dictionary<string, (int MaxRequests, TimeSpan Window)> _policies = new(StringComparer.OrdinalIgnoreCase)
    {
        ["form"] = (20, TimeSpan.FromHours(1)),
        ["auth"] = (50, TimeSpan.FromHours(1)),
        ["api"] = (500, TimeSpan.FromMinutes(1)),
    };

    private static readonly HashSet<string> _formPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/bookings",
        "/api/land-requests",
        "/api/leads",
        "/api/contacts"
    };

    public DistributedRateLimitingMiddleware(RequestDelegate next, IDistributedRateLimitStore store, ILogger<DistributedRateLimitingMiddleware> logger)
    {
        _next = next;
        _store = store;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var method = context.Request.Method;

        if (!HttpMethods.IsPost(method))
        {
            await _next(context);
            return;
        }

        var ip = GetClientIp(context);
        if (string.IsNullOrEmpty(ip) || ip == "unknown")
        {
            await _next(context);
            return;
        }

        string policy;
        int maxRequests;
        TimeSpan window;

        if (_formPaths.Contains(path))
        {
            policy = "form";
            (maxRequests, window) = _policies["form"];
        }
        else if (path.StartsWith("/api/auth/", StringComparison.OrdinalIgnoreCase))
        {
            policy = "auth";
            (maxRequests, window) = _policies["auth"];
        }
        else
        {
            await _next(context);
            return;
        }

        var key = $"{policy}:{ip}";
        var allowed = await _store.CheckAndIncrementAsync(key, maxRequests, window);

        if (!allowed)
        {
            _logger.LogWarning("AbuseAudit: Distributed rate limit exceeded Policy={Policy} IP={IP} Path={Path}", policy, ip, path);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = ((int)window.TotalSeconds).ToString();
            var origin = context.Request.Headers.Origin.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(origin))
                context.Response.Headers.AccessControlAllowOrigin = origin;
            await context.Response.WriteAsJsonAsync(
                new { message = "Too many requests. Please try again later." },
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            return;
        }

        await _next(context);
    }

    private static string? GetClientIp(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(ip) && ip != "unknown" && ip != "::1")
            return ip;

        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
            return forwarded.Split(',')[0].Trim();

        return ip;
    }
}
