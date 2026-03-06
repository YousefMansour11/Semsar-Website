using System.Security.Cryptography;
using Microsoft.Extensions.Primitives;

namespace API.Middleware;

public class ETagMiddleware
{
    private readonly RequestDelegate _next;

    public ETagMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method != HttpMethods.Get)
        {
            await _next(context);
            return;
        }

        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);

            if (context.Response.StatusCode == StatusCodes.Status200OK && buffer.Length > 0)
            {
                var body = buffer.ToArray();
                var hash = SHA256.HashData(body.AsSpan(0, Math.Min(body.Length, 64 * 1024)));
                var etag = Convert.ToBase64String(hash, 0, 12);

                context.Response.Headers.ETag = $"\"{etag}\"";

                var ifNoneMatch = context.Request.Headers.IfNoneMatch;
                if (ifNoneMatch.Count > 0)
                {
                    foreach (var candidate in ifNoneMatch)
                    {
                        if (candidate == $"\"{etag}\"" || candidate == $"W/\"{etag}\"")
                        {
                            context.Response.StatusCode = StatusCodes.Status304NotModified;
                            context.Response.ContentLength = 0;
                            return;
                        }
                    }
                }
            }

            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }
}
