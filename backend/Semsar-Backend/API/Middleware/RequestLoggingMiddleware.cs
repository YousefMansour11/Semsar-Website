using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace API.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = context.Request.Headers.UserAgent.FirstOrDefault() ?? "unknown";
            var path = context.Request.Path;

            var sensitivePaths = new[]
            {
                "/api/auth/login",
                "/api/auth/refresh",
                "/api/auth/register",
                "/api/auth/revoke",
                "/api/settings"
            };
            var isSensitive = sensitivePaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));

            if (HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method))
            {
                context.Request.EnableBuffering();
                string body;
                using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
                {
                    body = await reader.ReadToEndAsync();
                }
                context.Request.Body.Position = 0;

                var logBody = isSensitive ? "***REDACTED***" : SanitizeJsonBody(body);
                _logger.LogInformation("Request {Method} {Path} [IP={Ip}] [UA={UserAgent}] {Body}", context.Request.Method, path, ip, userAgent, logBody);
            }

            await _next(context);

            if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
            {
                _logger.LogWarning("Unauthorized access attempt {Method} {Path} [IP={Ip}] [UA={UserAgent}]", context.Request.Method, path, ip, userAgent);
            }
            else if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
            {
                _logger.LogWarning("Rate limit hit {Method} {Path} [IP={Ip}] [UA={UserAgent}]", context.Request.Method, path, ip, userAgent);
            }
        }

        private string SanitizeJsonBody(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement.Clone();
                using var ms = new MemoryStream();
                using var writer = new Utf8JsonWriter(ms);
                writer.WriteStartObject();
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Name.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                        prop.Name.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                        prop.Name.Contains("token", StringComparison.OrdinalIgnoreCase))
                    {
                        writer.WriteString(prop.Name, "***REDACTED***");
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.String && prop.Value.GetString()?.Contains("@") == true)
                    {
                        writer.WriteString(prop.Name, "***EMAIL***");
                    }
                    else if (prop.Name.Equals("phone", StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.String)
                    {
                        var phone = prop.Value.GetString() ?? "";
                        var masked = phone.Length >= 6
                            ? phone[..3] + new string('*', phone.Length - 6) + phone[^3..]
                            : "***MASKED***";
                        writer.WriteString(prop.Name, masked);
                    }
                    else
                    {
                        prop.WriteTo(writer);
                    }
                }
                writer.WriteEndObject();
                writer.Flush();
                return System.Text.Encoding.UTF8.GetString(ms.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sanitize JSON body");
                return "***INVALID JSON***";
            }
        }
    }
}
