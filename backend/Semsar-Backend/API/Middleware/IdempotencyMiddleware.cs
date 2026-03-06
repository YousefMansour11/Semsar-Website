using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace API.Middleware
{
    public class IdempotencyMiddleware
    {
        private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(30);
        private const int MaxResponseBodyBytes = 1 * 1024 * 1024; // 1 MB limit
        private readonly RequestDelegate _next;
        private readonly IIdempotencyStore _store;
        private readonly TimeSpan _retention;

        public IdempotencyMiddleware(RequestDelegate next, IIdempotencyStore store, TimeSpan retention)
        {
            _next = next;
            _store = store;
            _retention = retention;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!HttpMethods.IsPost(context.Request.Method) && !HttpMethods.IsPut(context.Request.Method) && !HttpMethods.IsPatch(context.Request.Method))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var keyValues) || string.IsNullOrWhiteSpace(keyValues))
            {
                await _next(context);
                return;
            }

            var key = keyValues.ToString().Trim();

            var existing = await _store.GetAsync(key);
            if (existing != null)
            {
                context.Response.StatusCode = existing.StatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(existing.ResponseBody);
                return;
            }

            if (!await _store.TryAcquireAsync(key, LockTimeout))
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await context.Response.WriteAsync("{\"message\":\"Request in progress, retry after completion\"}");
                return;
            }

            var originalBody = context.Response.Body;
            using var memStream = new MemoryStream();
            context.Response.Body = memStream;

            try
            {
                await _next(context);
            }
            finally
            {
                memStream.Position = 0;

                if (memStream.Length > MaxResponseBodyBytes)
                {
                    await memStream.CopyToAsync(originalBody);
                    context.Response.Body = originalBody;
                    await _store.ReleaseLockAsync(key);
                }
                else
                {
                    using var responseReader = new StreamReader(memStream);
                    var responseBody = await responseReader.ReadToEndAsync();
                    memStream.Position = 0;
                    await memStream.CopyToAsync(originalBody);
                    context.Response.Body = originalBody;

                    if (context.Response.StatusCode < 500)
                    {
                        context.Response.ContentType = context.Response.ContentType ?? "application/json";
                        await _store.StoreAsync(key, new IdempotencyRecord
                        {
                            StatusCode = context.Response.StatusCode,
                            ResponseBody = responseBody,
                            ContentType = context.Response.ContentType
                        }, _retention);
                    }
                    else
                    {
                        await _store.ReleaseLockAsync(key);
                    }
                }
            }
        }
    }

    public class IdempotencyRecord
    {
        public int StatusCode { get; set; }
        public string ResponseBody { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/json";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public interface IIdempotencyStore
    {
        Task<IdempotencyRecord?> GetAsync(string key);
        Task<bool> TryAcquireAsync(string key, TimeSpan lockTimeout);
        Task StoreAsync(string key, IdempotencyRecord record, TimeSpan retention);
        Task ReleaseLockAsync(string key);
        Task CleanupAsync(TimeSpan retention);
    }

    public class InMemoryIdempotencyStore : IIdempotencyStore
    {
        private readonly ConcurrentDictionary<string, IdempotencyRecord> _store = new ConcurrentDictionary<string, IdempotencyRecord>();
        private readonly ConcurrentDictionary<string, DateTime> _locks = new ConcurrentDictionary<string, DateTime>();

        public Task<IdempotencyRecord?> GetAsync(string key)
        {
            _store.TryGetValue(key, out var record);
            return Task.FromResult(record);
        }

        public Task<bool> TryAcquireAsync(string key, TimeSpan lockTimeout)
        {
            var expires = DateTime.UtcNow.Add(lockTimeout);
            return Task.FromResult(_locks.TryAdd(key, expires));
        }

        public Task StoreAsync(string key, IdempotencyRecord record, TimeSpan retention)
        {
            _locks.TryRemove(key, out _);
            _store[key] = record;
            return Task.CompletedTask;
        }

        public Task ReleaseLockAsync(string key)
        {
            _locks.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task CleanupAsync(TimeSpan retention)
        {
            var cutoff = DateTime.UtcNow.Subtract(retention);
            foreach (var kvp in _store)
            {
                if (kvp.Value.CreatedAt < cutoff)
                {
                    _store.TryRemove(kvp.Key, out _);
                }
            }
            foreach (var kvp in _locks)
            {
                if (kvp.Value < DateTime.UtcNow)
                {
                    _locks.TryRemove(kvp.Key, out _);
                }
            }
            return Task.CompletedTask;
        }
    }
}
