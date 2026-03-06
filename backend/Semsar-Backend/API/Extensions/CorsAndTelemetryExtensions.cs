using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Microsoft.Extensions.Logging;

namespace API.Extensions;

public static class CorsAndTelemetryExtensions
{
    public static IServiceCollection ConfigureCors(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            if (allowedOrigins.Length > 0)
            {
                options.AddDefaultPolicy(policy =>
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials());
            }
            else
            {
                try
                {
                    var sp = services.BuildServiceProvider();
                    var loggerFactory = sp.GetService<ILoggerFactory>();
                    var logger = loggerFactory?.CreateLogger("Semsar.Cors");
                    logger?.LogWarning("Cors:AllowedOrigins is not configured. Falling back to AllowAny. " +
                        "Set Cors:AllowedOrigins in appsettings or environment variables for production security.");
                }
                catch { }

                options.AddDefaultPolicy(policy =>
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
            }
        });

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownProxies.Clear();

            options.KnownIPNetworks.Clear();

            if (configuration.GetValue<bool>("Edge:UseCloudflare"))
            {
                options.KnownProxies.Clear();
            }
        });

        return services;
    }

    public static IServiceCollection ConfigureTelemetry(this IServiceCollection services, IWebHostEnvironment environment)
    {
        if (environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
            return services;

        try
        {
            var otlpEndpoint = Environment.GetEnvironmentVariable("OTLP_ENDPOINT_URL");

            services.AddOpenTelemetry()
                .WithMetrics(m =>
                {
                    m.AddAspNetCoreInstrumentation()
                     .AddHttpClientInstrumentation()
                     .AddRuntimeInstrumentation()
                     .AddMeter("Semsar");

                    m.AddPrometheusExporter();

                    if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                    {
                        m.AddOtlpExporter(opt =>
                        {
                            opt.Endpoint = new Uri(otlpEndpoint);
                        });
                    }
                })
                .WithTracing(t =>
                {
                    t.AddAspNetCoreInstrumentation()
                     .AddHttpClientInstrumentation()
                     .AddEntityFrameworkCoreInstrumentation()
                     .AddSource("SemsarAPI");

                    if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                    {
                        t.AddOtlpExporter(opt =>
                        {
                            opt.Endpoint = new Uri(otlpEndpoint);
                        });
                    }
                });
        }
        catch (Exception ex)
        {
            try
            {
                var sp = services.BuildServiceProvider();
                var loggerFactory = sp.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger("Semsar.Telemetry");
                logger?.LogWarning(ex, "OpenTelemetry initialization failed. Telemetry is disabled.");
            }
            catch { }
        }

        return services;
    }
}
