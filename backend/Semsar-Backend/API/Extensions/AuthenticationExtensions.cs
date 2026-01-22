using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Application.Interfaces;
using Infrastructure.Auth;
using Microsoft.Extensions.Logging;

namespace API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var secret = configuration["Jwt:Key"];
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32
            || string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
        {
            LogCriticalConfig(services, secret, issuer, audience);
            RegisterDegradedAuth(services);
            return services;
        }

        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var key = new SymmetricSecurityKey(keyBytes);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };
            });

        RegisterStandardServices(services);
        return services;
    }

    private static void LogCriticalConfig(IServiceCollection services, string? secret, string? issuer, string? audience)
    {
        try
        {
            var sp = services.BuildServiceProvider();
            var loggerFactory = sp.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("Semsar.Auth");
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret ?? "") < 32)
                missing.Add("Jwt:Key (32+ bytes required)");
            if (string.IsNullOrWhiteSpace(issuer))
                missing.Add("Jwt:Issuer");
            if (string.IsNullOrWhiteSpace(audience))
                missing.Add("Jwt:Audience");
            logger?.LogCritical("JWT authentication is MISSING critical configuration: {Missing}. " +
                "Authentication will be disabled. Set these via environment variables Jwt__Key, Jwt__Issuer, Jwt__Audience. " +
                "The application will start in degraded mode.",
                string.Join(", ", missing));
        }
        catch
        {
        }
    }

    private static void RegisterDegradedAuth(IServiceCollection services)
    {
        RegisterStandardServices(services);
    }

    private static void RegisterStandardServices(IServiceCollection services)
    {
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthenticationService, Application.Services.AuthenticationService>();
    }
}
