using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;

namespace API.Middleware
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware>? _logger;
        public const string HeaderName = "X-Correlation-Id";

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware>? logger = null)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                string? cid = null;
                if (context.Request.Headers.ContainsKey(HeaderName))
                {
                    cid = context.Request.Headers[HeaderName].ToString();
                }
                if (string.IsNullOrWhiteSpace(cid)) cid = Guid.NewGuid().ToString();
                // attach to response
                context.Response.OnStarting(() =>
                {
                    if (!context.Response.Headers.ContainsKey(HeaderName)) context.Response.Headers[HeaderName] = cid;
                    return Task.CompletedTask;
                });
                // include in log scope
                using (_logger?.BeginScope(new System.Collections.Generic.Dictionary<string, object> { { "CorrelationId", cid } }) ?? null)
                {
                    context.Items[HeaderName] = cid;
                    context.Request.Headers[HeaderName] = cid; // normalize
                    await _next(context);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Correlation middleware failed");
                throw;
            }
        }
    }
}
