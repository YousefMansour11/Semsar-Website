using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace API.Middleware;

public partial class InputSanitizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InputSanitizationMiddleware> _logger;
    private static readonly Regex HtmlTagRegex = HtmlTagRegexGenerated();
    private static readonly Regex ControlCharRegex = ControlCharRegexGenerated();

    [GeneratedRegex(@"<[^>]*>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegexGenerated();

    [GeneratedRegex(@"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", RegexOptions.Compiled)]
    private static partial Regex ControlCharRegexGenerated();

    public InputSanitizationMiddleware(RequestDelegate next, ILogger<InputSanitizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method) ||
            HttpMethods.IsPut(context.Request.Method) ||
            HttpMethods.IsPatch(context.Request.Method))
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (!string.IsNullOrWhiteSpace(body) &&
                context.Request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) == true)
            {
                var sanitized = SanitizeJsonBody(body, context);
                if (sanitized != body)
                {
                    var bytes = Encoding.UTF8.GetBytes(sanitized);
                    context.Request.Body = new MemoryStream(bytes);
                }
            }
        }

        await _next(context);
    }

    private string SanitizeJsonBody(string body, HttpContext context)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement.Clone();

            if (root.ValueKind != JsonValueKind.Object)
                return body;

            bool wasSanitized = false;
            using var ms = new MemoryStream();
            using var writer = new Utf8JsonWriter(ms);
            writer.WriteStartObject();

            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    var original = prop.Value.GetString() ?? "";
                    var sanitized = SanitizeString(original);
                    if (sanitized != original)
                        wasSanitized = true;
                    writer.WriteString(prop.Name, sanitized);
                }
                else
                {
                    prop.WriteTo(writer);
                }
            }

            writer.WriteEndObject();
            writer.Flush();

            if (wasSanitized)
            {
                var cid = context.Items["X-Correlation-Id"] ?? context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
                _logger.LogWarning("Input sanitized for {Method} {Path} [CorrelationId={CorrelationId}]",
                    context.Request.Method, context.Request.Path, cid);
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }
        catch (JsonException)
        {
            return body;
        }
    }

    private static readonly char[] InvisibleChars =
    [
        '\u200B', '\u200C', '\u200D', '\uFEFF', '\u00AD',
        '\u2060', '\u2061', '\u2062', '\u2063', '\u2064'
    ];

    private static string SanitizeString(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var result = HtmlTagRegex.Replace(input, "");
        result = ControlCharRegex.Replace(result, "");
        if (result.IndexOfAny(InvisibleChars) >= 0)
        {
            result = string.Create(result.Length, result, (span, s) =>
            {
                int writeIdx = 0;
                foreach (var c in s)
                {
                    if (Array.IndexOf(InvisibleChars, c) < 0)
                        span[writeIdx++] = c;
                }
                span = span[..writeIdx];
            });
        }
        return result.Trim();
    }
}
