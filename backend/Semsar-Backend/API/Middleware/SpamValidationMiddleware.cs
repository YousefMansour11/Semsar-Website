using System.Text.Json;
using System.Text.RegularExpressions;
using API.Services;

namespace API.Middleware;

public partial class SpamValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SpamValidationMiddleware> _logger;
    private readonly BotBehaviorDetector _botDetector;

    private static readonly Regex ScriptTagRegex = ScriptTagRegexGenerated();
    private static readonly Regex EncodedEntityRegex = EncodedEntityRegexGenerated();
    private static readonly Regex ExcessiveUrlRegex = ExcessiveUrlRegexGenerated();
    private static readonly Regex RepeatedCharsRegex = RepeatedCharsRegexGenerated();
    private static readonly Regex InvisibleCharsRegex = InvisibleCharsRegexGenerated();

    private static readonly HashSet<string> SpamKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "buy now", "click here", "subscribe", "subscribe now", "act now", "limited offer",
        "free money", "earn money", "work from home", "make money", "no deposit", "casino",
        "visit our", "check this out", "promotion"
    };

    private static readonly HashSet<string> AllowedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "name", "phone", "message", "notes", "location", "email",
        "minprice", "maxprice", "minarea", "maxarea",
        "propertyid", "unitid", "preferreddate",
        "source", "medium", "campaign", "term", "content",
        "landingpage", "firstvisitat", "currentpage", "referrer",
        "useragent", "pageviews", "sessionduration",
        "lastreferrer", "visithistory", "interactiontimestamp",
        "submittedat", "website"
    };

    private static bool IsAllowedField(string fieldName)
    {
        if (AllowedFields.Contains(fieldName))
            return true;
        if (fieldName.StartsWith("hp_", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private const int MaxMessageLength = 5000;
    private const int MaxUrlCount = 3;
    private const int MaxRepeatedCharSequences = 20;

    private static readonly Dictionary<string, EntityLimit> EntityLimits = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/api/bookings"] = new EntityLimit("booking", 3, 15),
        ["/api/land-requests"] = new EntityLimit("land_request", 5, 30),
        ["/api/leads"] = new EntityLimit("lead", 5, 10),
        ["/api/contacts"] = new EntityLimit("contact", 2, 10),
    };

    private static readonly int[] StageThresholds = [0, 40, 70, 90];

    [GeneratedRegex(@"<[^>]*>", RegexOptions.Compiled)]
    private static partial Regex ScriptTagRegexGenerated();

    [GeneratedRegex(@"&#\d{2,};|&#[xX][0-9a-fA-F]{2,};|&[a-z]{2,}(?:\s|;|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex EncodedEntityRegexGenerated();

    [GeneratedRegex(@"(https?://|www\.)\S+", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ExcessiveUrlRegexGenerated();

    [GeneratedRegex(@"(.)\1{9,}", RegexOptions.Compiled)]
    private static partial Regex RepeatedCharsRegexGenerated();

    [GeneratedRegex(@"[\u200B-\u200D\uFEFF\u00AD\u2060\u2061\u2062\u2063\u2064]", RegexOptions.Compiled)]
    private static partial Regex InvisibleCharsRegexGenerated();

    public SpamValidationMiddleware(RequestDelegate next, ILogger<SpamValidationMiddleware> logger, BotBehaviorDetector botDetector)
    {
        _next = next;
        _logger = logger;
        _botDetector = botDetector;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        if (!HttpMethods.IsPost(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (!EntityLimits.Keys.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        if (context.Request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) != true)
        {
            _logger.LogWarning("AbuseAudit: Non-JSON Content-Type rejected Path={Path} ContentType={CT}",
                path, context.Request.ContentType);
            context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
            await context.Response.WriteAsJsonAsync(
                new { message = "JSON content type required." },
                _jsonOptions);
            return;
        }

        context.Request.EnableBuffering();
        string body;
        using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync();
        }
        context.Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(body))
        {
            await _next(context);
            return;
        }

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var fingerprint = _botDetector.ComputeFingerprint(context);

        var entityLimit = new EntityLimit("unknown", 5, 15);
        foreach (var kvp in EntityLimits)
        {
            if (path.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                entityLimit = kvp.Value;
                break;
            }
        }

        var (entityId, honeypotFilled, hasViolations, malformedJson) = ValidateContent(body, context);

        if (malformedJson)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(
                new { message = "Invalid JSON format." },
                _jsonOptions);
            return;
        }

        var entityIdFinal = entityId ?? "unknown";

        if (_botDetector.IsInCooldown(clientIp, fingerprint, out var existingRetryAfter))
        {
            _logger.LogInformation("AbuseAudit: Active cooldown IP={IP} Path={Path} RetryAfter={RetryAfter}s",
                clientIp, path, existingRetryAfter);
            await WriteCooldownResponse(context, existingRetryAfter, "Please wait a moment before submitting another request.");
            return;
        }

        var currentScore = _botDetector.ComputeReputationScore(clientIp, fingerprint);
        var scoreDelta = _botDetector.CalculateScore(context, body, entityLimit.Type, entityIdFinal,
            clientIp, fingerprint, honeypotFilled, malformedJson, hasViolations);
        var projectedScore = Math.Clamp(currentScore + scoreDelta, 0, 100);

        if (hasViolations)
        {
            _botDetector.AddReputationEvent(clientIp, fingerprint, 10, serious: false);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(
                new { message = "Invalid request content. Please check your input and try again." },
                _jsonOptions);
            return;
        }

        if (projectedScore >= 90)
        {
            _logger.LogWarning("AbuseAudit: Stage 4 block IP={IP} Path={Path} Score={Score}", clientIp, path, projectedScore);
            _botDetector.AddReputationEvent(clientIp, fingerprint, scoreDelta, serious: true);
            _botDetector.ApplyCooldown(clientIp, fingerprint, 300);
            await WriteCooldownResponse(context, 300, "Too many requests. Please try again later.");
            return;
        }

        if (!_botDetector.CheckFingerprintConsistency(clientIp, fingerprint))
        {
            _logger.LogWarning("AbuseAudit: Fingerprint mismatch IP={IP} Path={Path}", clientIp, path);
            _botDetector.AddReputationEvent(clientIp, fingerprint, 15, serious: false);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(
                new { message = "Invalid request content. Please check your input and try again." },
                _jsonOptions);
            return;
        }

        var velocityAllowed = !_botDetector.IsEntityVelocityExceeded(
            clientIp, entityLimit.Type, entityIdFinal, entityLimit.MaxRequests, TimeSpan.FromMinutes(entityLimit.WindowMinutes));

        if (!velocityAllowed && projectedScore >= 70)
        {
            _logger.LogWarning("AbuseAudit: Stage 3 friction IP={IP} Path={Path} Score={Score}", clientIp, path, projectedScore);
            _botDetector.AddReputationEvent(clientIp, fingerprint, scoreDelta, serious: false);
            _botDetector.ApplyCooldown(clientIp, fingerprint, 120);
            await Task.Delay(2000);
            context.Response.Headers["X-RateLimit-Remaining"] = "0";
            context.Response.Headers["Retry-After"] = "120";
            context.Items["ReputationScore"] = projectedScore;
            context.Items["CooldownActive"] = true;
            await _next(context);
            return;
        }

        if (!velocityAllowed)
        {
            _logger.LogInformation("AbuseAudit: Velocity limit reached but score low, allowing IP={IP} Path={Path} Score={Score}",
                clientIp, path, projectedScore);
            _botDetector.AddReputationEvent(clientIp, fingerprint, scoreDelta, serious: false);
            if (projectedScore >= 40)
            {
                _botDetector.ApplyCooldown(clientIp, fingerprint, 120);
                await WriteCooldownResponse(context, 120, "Please wait a moment before submitting another request.");
                return;
            }
        }

        if (projectedScore >= 40)
        {
            _logger.LogInformation("AbuseAudit: Stage 2 cooldown applied IP={IP} Path={Path} Score={Score}", clientIp, path, projectedScore);
            _botDetector.AddReputationEvent(clientIp, fingerprint, scoreDelta, serious: false);
            _botDetector.ApplyCooldown(clientIp, fingerprint, 120);
            await WriteCooldownResponse(context, 120, "Please wait a moment before submitting another request.");
            return;
        }

        _botDetector.AddReputationEvent(clientIp, fingerprint, scoreDelta, serious: false);
        context.Items["ReputationScore"] = projectedScore;
        context.Items["EntityType"] = entityLimit.Type;
        context.Items["EntityId"] = entityIdFinal;

        await _next(context);
    }

    private (string? entityId, bool honeypotFilled, bool hasViolations, bool malformedJson) ValidateContent(string body, HttpContext context)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return (null, false, false, false);

            var violations = new List<string>();
            bool honeypotFilled = false;
            int? propertyId = null;
            int? unitId = null;
            string? location = null;

            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Name.Equals("propertyid", StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.Number)
                    propertyId = prop.Value.GetInt32();

                if (prop.Name.Equals("unitid", StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.Number)
                    unitId = prop.Value.GetInt32();

                if (prop.Name.Equals("location", StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.String)
                    location = prop.Value.GetString();

                if (prop.Value.ValueKind != JsonValueKind.String)
                    continue;

                var value = prop.Value.GetString() ?? "";
                var fieldName = prop.Name;

                if (!IsAllowedField(fieldName))
                {
                    violations.Add($"unexpected_field:{fieldName}");
                    continue;
                }

                if (fieldName.StartsWith("hp_", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        honeypotFilled = true;
                        violations.Add($"honeypot_filled:{fieldName}");
                    }
                    continue;
                }

                if (value.Length > MaxMessageLength)
                {
                    violations.Add($"field_too_long:{fieldName}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                if (ScriptTagRegex.IsMatch(value))
                {
                    violations.Add($"html_injection:{fieldName}");
                    continue;
                }

                if (EncodedEntityRegex.IsMatch(value))
                {
                    violations.Add($"encoded_entity:{fieldName}");
                    continue;
                }

                if (!string.Equals(fieldName, "visitHistory", StringComparison.OrdinalIgnoreCase))
                {
                    var urlCount = ExcessiveUrlRegex.Matches(value).Count;
                    if (urlCount > MaxUrlCount)
                    {
                        violations.Add($"excessive_urls:{fieldName}({urlCount})");
                        continue;
                    }
                }

                if (RepeatedCharsRegex.IsMatch(value))
                {
                    violations.Add($"repeated_chars:{fieldName}");
                    continue;
                }

                if (InvisibleCharsRegex.IsMatch(value))
                {
                    violations.Add($"invisible_chars:{fieldName}");
                    continue;
                }

                if (SpamKeywords.Any(k => value.Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    violations.Add($"spam_keyword:{fieldName}");
                    continue;
                }
            }

            var entityId = propertyId?.ToString() ?? unitId?.ToString() ?? location;
            var hasViolations = violations.Count > 0;

            var path = context.Request.Path.Value ?? "";
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (hasViolations)
            {
                _logger.LogWarning("AbuseAudit: Spam validation failed Path={Path} IP={IP} Violations={Violations}",
                    path, clientIp, string.Join("; ", violations));
            }

            return (entityId, honeypotFilled, hasViolations, false);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "AbuseAudit: Malformed JSON rejected Path={Path}",
                context.Request.Path.Value ?? "");
            return (null, false, true, true);
        }
    }

    private async Task WriteCooldownResponse(HttpContext context, int retryAfterSeconds, string message)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = "0";
        await context.Response.WriteAsJsonAsync(
            new
            {
                message,
                retryAfterMs = retryAfterSeconds * 1000,
                retryAfterSeconds
            },
            _jsonOptions);
    }

    private record EntityLimit(string Type, int MaxRequests, int WindowMinutes);
}
