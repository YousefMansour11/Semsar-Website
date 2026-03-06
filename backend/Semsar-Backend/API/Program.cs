using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using API;
using API.Extensions;
using API.Middleware;
using API.Services;
using Application.Interfaces;
using Infrastructure.Data;
using Infrastructure.Health;
using Infrastructure.Middlewares;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;

try
{
    var startupMode = StartupModeProvider.GetCurrent();
    var isDiagnostics = startupMode == StartupMode.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// ----------------------
// Serilog — crash-safe
// ----------------------
try
{
    builder.Host.UseSerilog((context, config) =>
    {
        try
        {
            var logPath = context.Configuration["Serilog:LogPath"];
            if (string.IsNullOrWhiteSpace(logPath))
            {
                logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "log-.txt");
            }

            var logDir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            config.MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                  .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
                  .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
                  .WriteTo.Console()
                  .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
                  .Enrich.FromLogContext();

            var seqUrl = context.Configuration["Serilog:SeqUrl"];
            if (!string.IsNullOrWhiteSpace(seqUrl))
            {
                config.WriteTo.Seq(seqUrl);
            }
        }
        catch (Exception inner)
        {
            try { System.Console.Error.WriteLine($"Serilog init: {inner.Message}"); } catch { }
        }
    });
}
catch (Exception serilogEx)
{
    StartupDiagnostic.WriteFatal("SERILOG", serilogEx);
}

// ----------------------
// DI Registration
// ----------------------
try
{
builder.Services.ConfigureTelemetry(builder.Environment);
builder.Services.ConfigureDatabase(builder.Configuration, builder.Environment);
builder.Services.ConfigureDataProtection(builder.Configuration, builder.Environment);
builder.Services.ConfigureAuthentication(builder.Configuration);
    builder.Services.ConfigureApplicationServices(builder.Configuration);
    builder.Services.ConfigureIdempotency(builder.Configuration);
    builder.Services.ConfigureHealthChecks(builder.Configuration);
    builder.Services.ConfigureHangfire(builder.Configuration);
    builder.Services.ConfigureBackgroundServices(builder.Environment);
    builder.Services.ConfigureRateLimiting();
    builder.Services.ConfigureDistributedRateLimiting(builder.Configuration);
}
catch (Exception diEx)
{
    StartupDiagnostic.WriteFatal("DI_REGISTRATION", diEx, builder.Configuration, builder.Environment);
    throw;
}

// Bot behavior — never crashes
builder.Services.AddSingleton<IBotBehaviorStore>(sp =>
{
    try
    {
        var muxer = sp.GetService<ConnectionMultiplexer>();
        if (muxer?.IsConnected == true)
        {
            var logger = sp.GetRequiredService<ILogger<RedisBotBehaviorStore>>();
            return new RedisBotBehaviorStore(muxer, logger);
        }
    }
    catch { }
    return new MemoryBotBehaviorStore();
});
builder.Services.AddSingleton<BotBehaviorDetector>();

// CORS — never crashes
try
{
    builder.Services.ConfigureCors(builder.Configuration, builder.Environment);
}
catch (Exception corsEx)
{
    StartupDiagnostic.WriteFatal("CORS", corsEx, builder.Configuration, builder.Environment);
}

builder.Services.ConfigureApiVersioning();
builder.Services.ConfigureSwagger();

// Response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/json", "application/xml", "text/html", "text/plain"]);
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

// Controllers
builder.Services.AddControllers(options =>
{
    options.Filters.Add<PaginationValidationFilter>();
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// ----------------------
// Build
// ----------------------
WebApplication app;
try
{
    app = builder.Build();
}
catch (Exception buildEx)
{
    StartupDiagnostic.WriteFatal("BUILD", buildEx, builder.Configuration, builder.Environment);
    throw;
}

// ----------------------
// Configuration Validation — logs but NEVER crashes
// ----------------------
try
{
    var health = app.Services.GetRequiredService<IInfrastructureHealthCheck>();
    health.ValidateScopedDependencies();
    ValidateConfiguration(app.Services.GetRequiredService<IConfiguration>(), app.Environment);
}
catch (Exception valEx)
{
    StartupDiagnostic.WriteFatal("CONFIG_VALIDATION", valEx,
        app.Services.GetRequiredService<IConfiguration>(), app.Environment);
    Log.Warning(valEx, "Configuration validation failed — continuing in degraded mode");
}

// ----------------------
// Database Init — never crashes
// ----------------------
try
{
    using var ftsScope = app.Services.CreateScope();
    var ftsService = ftsScope.ServiceProvider.GetRequiredService<ISearchService>();
    await ftsService.InitializeFtsAsync();
}
catch (Exception ex)
{
    Log.Warning(ex, "FTS init failed; search uses LIKE fallback");
}

try
{
    await DbSeeder.EnsureSeedDataAsync(app.Services);
}
catch (Exception ex)
{
    Log.Warning(ex, "Data seeding failed");
}

try
{
    using var scope = app.Services.CreateScope();
    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var pending = (await ctx.Database.GetPendingMigrationsAsync()).ToList();
    if (pending.Count > 0)
    {
        Log.Information("Applying {Count} pending migrations", pending.Count);
        await ctx.Database.MigrateAsync();
        Log.Information("Migrations applied successfully");
    }

    // Verify tables actually exist — if not, reset history and reapply
    try
    {
        await ctx.Users.AnyAsync();
    }
    catch when (pending.Count == 0)
    {
        Log.Warning("Tables missing despite migration history — resetting and reapplying");
        await ctx.Database.ExecuteSqlRawAsync(
            "DELETE FROM [__EFMigrationsHistory] WHERE MigrationId IN ('20260524080315_InitialCreate', '20260524103847_AddVideoEntities')");
        await ctx.Database.MigrateAsync();
        Log.Information("Migrations reapplied after history reset");
    }
}
catch (Exception ex)
{
    Log.Warning(ex, "Migration auto-apply failed — continuing in degraded mode");
}

// ----------------------
// Graceful Shutdown
// ----------------------
app.Lifetime.ApplicationStopping.Register(() =>
{
    Log.Information("Application shutting down. Draining in-flight requests...");
});

// ----------------------
// Middleware Pipeline
// ----------------------
if (!isDiagnostics)
{
    var idempotencyRetentionHours = double.TryParse(builder.Configuration["Idempotency:RetentionHours"], out var ih) ? ih : 24;

    app.UseForwardedHeaders();
    app.UseResponseCompression();
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseMiddleware<SeoHeadersMiddleware>();
    app.UseMiddleware<IpAbuseMiddleware>();
    app.UseMiddleware<InputSanitizationMiddleware>();
    app.UseMiddleware<SpamValidationMiddleware>();
    app.UseMiddleware<ETagMiddleware>();
    app.UseMiddleware<IdempotencyMiddleware>(TimeSpan.FromHours(idempotencyRetentionHours));
    app.UseSerilogRequestLogging();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseCors();
    app.UseMiddleware<DistributedRateLimitingMiddleware>();
    app.UseRateLimiter();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    if (startupMode != StartupMode.Safe)
    {
        app.UseHangfireDashboard(builder.Configuration);
        if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
        {
            API.Extensions.HangfireExtensions.RegisterRecurringJobs();
        }
    }
}

// ----------------------
// Always-on endpoints
// ----------------------
app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true
});
app.MapHealthChecks("/readyz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true
});

app.MapGet("/diagnostics/startup", (IConfiguration? config) =>
{
    var diag = new Dictionary<string, object?>
    {
        ["status"] = "running",
        ["mode"] = startupMode.ToString(),
        ["environment"] = app.Environment.EnvironmentName,
        ["timestamp"] = DateTime.UtcNow,
        ["runtime"] = Environment.Version.ToString()
    };

    if (config != null)
    {
        var cfg = new Dictionary<string, string?>();
        cfg["DefaultConnection"] = string.IsNullOrWhiteSpace(config.GetConnectionString("DefaultConnection")) ? "MISSING" : "SET";
        cfg["Jwt:Key"] = string.IsNullOrWhiteSpace(config["Jwt:Key"]) ? "MISSING" : "SET";
        cfg["Jwt:Issuer"] = config["Jwt:Issuer"];
        cfg["Jwt:Audience"] = config["Jwt:Audience"];
        cfg["Cloudinary:ApiKey"] = string.IsNullOrWhiteSpace(config["Cloudinary:ApiKey"]) ? "MISSING" : "SET";
        cfg["Cloudinary:ApiSecret"] = string.IsNullOrWhiteSpace(config["Cloudinary:ApiSecret"]) ? "MISSING" : "SET";
        cfg["AppSettings:BaseUrl"] = string.IsNullOrWhiteSpace(config["AppSettings:BaseUrl"]) ? "MISSING" : "SET";
        cfg["Smtp:Host"] = config["Smtp:Host"];
        diag["configuration"] = cfg;
    }

    var missing = new List<string>();
    if (string.IsNullOrWhiteSpace(config?.GetConnectionString("DefaultConnection")))
        missing.Add("DATABASE_CONNECTION_STRING");
    if (string.IsNullOrWhiteSpace(config?["Jwt:Key"]) ||
        System.Text.Encoding.UTF8.GetByteCount(config?["Jwt:Key"] ?? "") < 32)
        missing.Add("JWT_KEY (32+ bytes)");

    if (missing.Count > 0)
    {
        diag["status"] = "misconfigured";
        diag["missing_critical"] = missing;
    }

    return Results.Json(diag, new JsonSerializerOptions { WriteIndented = true });
})
.WithName("StartupDiagnostics");

if (!isDiagnostics)
{
    app.MapPrometheusScrapingEndpoint().RequireAuthorization();
    app.MapGet("/metrics/snapshot", (IAppMetrics metrics) => Results.Json(metrics.Snapshot()))
        .RequireAuthorization();
}

app.Run();
}
catch (Exception ex)
{
    StartupDiagnostic.WriteFatal("TOP_LEVEL", ex);
    Log.Fatal(ex, "Fatal startup error");
    throw;
}

// ----------------------
// Configuration Validation — logs only, never crashes startup
// ----------------------
static void ValidateConfiguration(IConfiguration configuration, IWebHostEnvironment environment)
{
    var critical = new List<string>();
    var warnings = new List<string>();

    var connStr = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connStr))
        critical.Add("ConnectionStrings:DefaultConnection — database disabled");

    var jwtKey = configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey))
        critical.Add("Jwt:Key — authentication disabled");
    else if (System.Text.Encoding.UTF8.GetByteCount(jwtKey) < 32)
        critical.Add("Jwt:Key too short — must be 32+ bytes");

    if (string.IsNullOrWhiteSpace(configuration["Jwt:Issuer"]))
        warnings.Add("Jwt:Issuer not set");
    if (string.IsNullOrWhiteSpace(configuration["Jwt:Audience"]))
        warnings.Add("Jwt:Audience not set");

    if (!environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(configuration["Cloudinary:ApiKey"]))
            warnings.Add("Cloudinary:ApiKey — image uploads will fail");
        if (string.IsNullOrWhiteSpace(configuration["Cloudinary:ApiSecret"]))
            warnings.Add("Cloudinary:ApiSecret — image uploads will fail");
    }

    if (string.IsNullOrWhiteSpace(configuration["AppSettings:BaseUrl"]))
        warnings.Add("AppSettings:BaseUrl — SEO canonical URLs degraded");

    if (string.IsNullOrWhiteSpace(configuration["Smtp:Host"]))
        warnings.Add("Smtp:Host — email notifications disabled");
    if (string.IsNullOrWhiteSpace(configuration["Smtp:From"]))
        warnings.Add("Smtp:From — email notifications disabled");

    if (critical.Count > 0)
        Log.Fatal("STARTUP DEGRADED: {Errors}", string.Join("; ", critical));
    if (warnings.Count > 0)
        Log.Warning("STARTUP WARNINGS: {Warnings}", string.Join("; ", warnings));
}

namespace API
{
    public partial class Program { }
}
