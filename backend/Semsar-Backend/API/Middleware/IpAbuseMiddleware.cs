using System.Collections.Concurrent;
using System.Text.Json;

namespace API.Middleware;

public class IpAbuseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IpAbuseMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, IpRecord> _ipTracker = new();
    private static readonly TimeSpan _blockDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan _windowDuration = TimeSpan.FromMinutes(5);
    private const int MaxRequestsPerWindow = 50;
    private const int MaxViolationsBeforeBlock = 3;
    private static readonly ConcurrentDictionary<string, DateTime> _blockedIps = new();
    private static readonly System.Threading.Timer _cleanupTimer;
    private static readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(10);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    static IpAbuseMiddleware()
    {
        _cleanupTimer = new System.Threading.Timer(_ => PerformCleanup(), null, _cleanupInterval, _cleanupInterval);
    }

    public IpAbuseMiddleware(RequestDelegate next, ILogger<IpAbuseMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (_blockedIps.ContainsKey(ip))
        {
            _logger.LogWarning("AbuseAudit: Blocked IP attempted request {Method} {Path} IP={IP}", context.Request.Method, context.Request.Path, ip);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(
                new { message = "Too many requests. Please try again later." },
                _jsonOptions);
            return;
        }

        bool shouldReject = false;
        var record = _ipTracker.GetOrAdd(ip, _ => new IpRecord());
        lock (record)
        {
            var now = DateTime.UtcNow;
            record.Timestamps.RemoveAll(ts => now - ts > _windowDuration);

            if (record.Timestamps.Count >= MaxRequestsPerWindow)
            {
                record.Violations++;
                record.Timestamps.Clear();

                if (record.Violations >= MaxViolationsBeforeBlock)
                {
                    _blockedIps.TryAdd(ip, DateTime.UtcNow);
                    _logger.LogWarning("AbuseAudit: IP blocked after {Violations} violations IP={IP}", record.Violations, ip);
                }
                else
                {
                    _logger.LogWarning("AbuseAudit: IP rate violation {Count}/{Max} IP={IP}", record.Violations, MaxViolationsBeforeBlock, ip);
                }

                shouldReject = true;
            }
            else
            {
                record.Timestamps.Add(now);
            }
        }

        if (shouldReject)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(
                new { message = "Too many requests. Please try again later." },
                _jsonOptions);
            return;
        }

        context.Items["X-Client-IP"] = ip;

        await _next(context);
    }

    private static void PerformCleanup()
    {
        try
        {
            var now = DateTime.UtcNow;

            foreach (var kvp in _blockedIps)
            {
                if (now - kvp.Value > _blockDuration)
                {
                    _blockedIps.TryRemove(kvp.Key, out _);
                    _ipTracker.TryRemove(kvp.Key, out _);
                }
            }

            var staleIps = _ipTracker
                .Where(kvp => now - kvp.Value.LastActivity > TimeSpan.FromHours(1))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var ip in staleIps)
            {
                _ipTracker.TryRemove(ip, out _);
            }
        }
        catch
        {
            // Silently handle cleanup errors — never throw from timer callback
        }
    }

    private class IpRecord
    {
        public List<DateTime> Timestamps { get; set; } = new();
        public int Violations { get; set; }
        public DateTime LastActivity => Timestamps.Count > 0 ? Timestamps[^1] : DateTime.MinValue;
    }
}
