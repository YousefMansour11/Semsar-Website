using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Domain.Exceptions;
using FluentValidation;
using System.Linq;

namespace Infrastructure.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException)
            {
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
                }
            }
            catch (TimeoutException ex)
            {
                var cid = GetCorrelationId(context);
                LogException(cid, ex, "Request timeout");
                await HandleExceptionAsync(context, ex, (int)HttpStatusCode.GatewayTimeout, "gateway_timeout", "The request timed out. Please try again later.");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency", System.StringComparison.OrdinalIgnoreCase))
            {
                var cid = GetCorrelationId(context);
                LogException(cid, ex, "Concurrency conflict");
                await HandleExceptionAsync(context, ex, (int)HttpStatusCode.Conflict, "concurrency_conflict", "The resource was modified by another request. Please refresh and try again.");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Unique constraint", System.StringComparison.OrdinalIgnoreCase))
            {
                var cid = GetCorrelationId(context);
                LogException(cid, ex, "Unique constraint violation");
                await HandleExceptionAsync(context, ex, (int)HttpStatusCode.Conflict, "constraint_violation", "A duplicate resource already exists.");
            }
            catch (Exception ex)
            {
                var cid = GetCorrelationId(context);
                LogException(cid, ex, "Unhandled exception");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static string? GetCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var header))
                return header.ToString();
            return context.Items.TryGetValue("X-Correlation-Id", out var item) ? item?.ToString() : null;
        }

        private void LogException(string? correlationId, Exception ex, string context)
        {
            if (!string.IsNullOrEmpty(correlationId))
            {
                using (_logger.BeginScope(new System.Collections.Generic.Dictionary<string, object> { { "CorrelationId", correlationId } }))
                {
                    _logger.LogError(ex, "{Context} [CorrelationId={CorrelationId}]", context, correlationId);
                }
            }
            else
            {
                _logger.LogError(ex, "{Context}", context);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception, int status, string errorCode, string message)
        {
            context.Response.ContentType = "application/json";
            var response = new
            {
                success = false,
                message,
                errorCode,
                details = Array.Empty<string>() as System.Collections.Generic.IEnumerable<string>
            };
            context.Response.StatusCode = status;
            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            int status = (int)HttpStatusCode.InternalServerError;
            var response = new
            {
                success = false,
                message = "An unexpected error occurred",
                errorCode = "internal_error",
                details = Array.Empty<string>() as System.Collections.Generic.IEnumerable<string>
            };

            if (exception is ValidationException vex)
            {
                status = (int)HttpStatusCode.BadRequest;
                response = new
                {
                    success = false,
                    message = "Validation failed",
                    errorCode = "validation_error",
                    details = vex.Errors.Select(e => e.ErrorMessage) as System.Collections.Generic.IEnumerable<string>
                };
            }

            if (exception is Application.Services.SlugConflictException)
            {
                status = (int)HttpStatusCode.Conflict;
                response = new
                {
                    success = false,
                    message = "Slug conflict - please retry",
                    errorCode = "slug_conflict",
                    details = Array.Empty<string>() as System.Collections.Generic.IEnumerable<string>
                };
            }

            if (exception is Application.Services.ExternalDataModificationException)
            {
                status = (int)HttpStatusCode.Conflict;
                response = new
                {
                    success = false,
                    message = "External data modification detected",
                    errorCode = "external_modification",
                    details = Array.Empty<string>() as System.Collections.Generic.IEnumerable<string>
                };
            }

            if (exception is ImageUploadException imgEx)
            {
                status = (int)HttpStatusCode.BadRequest;
                response = new
                {
                    success = false,
                    message = imgEx.Message,
                    errorCode = "image_upload_error",
                    details = Array.Empty<string>() as System.Collections.Generic.IEnumerable<string>
                };
            }

            if (exception is ArgumentException argEx)
            {
                status = (int)HttpStatusCode.BadRequest;
                response = new
                {
                    success = false,
                    message = "Invalid argument provided",
                    errorCode = "invalid_argument",
                    details = Array.Empty<string>() as System.Collections.Generic.IEnumerable<string>
                };
            }

            if (exception is UnauthorizedAccessException)
            {
                status = (int)HttpStatusCode.Unauthorized;
                response = new
                {
                    success = false,
                    message = "Unauthorized access",
                    errorCode = "unauthorized",
                    details = Array.Empty<string>() as System.Collections.Generic.IEnumerable<string>
                };
            }

            context.Response.StatusCode = status;
            var json = JsonSerializer.Serialize(response);
            return context.Response.WriteAsync(json);
        }
    }
}
