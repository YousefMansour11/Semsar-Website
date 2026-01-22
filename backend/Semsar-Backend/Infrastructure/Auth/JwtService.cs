using System;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Auth;

public class JwtService(IConfiguration config) : Application.Interfaces.IJwtService
{
    public string GenerateToken(string username, string? role = null)
    {
        var secret = config["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("JWT Key is missing in configuration");

        var keyBytes = Encoding.UTF8.GetBytes(secret);
        if (keyBytes.Length < 32)
            throw new InvalidOperationException("JWT Key must be at least 256 bits (32 bytes)");

        var key = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrWhiteSpace(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var issuer = config["Jwt:Issuer"];
        if (string.IsNullOrWhiteSpace(issuer))
            throw new InvalidOperationException("JWT Issuer is missing in configuration");
        var audience = config["Jwt:Audience"];
        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("JWT Audience is missing in configuration");

        var expiresHours = double.TryParse(config["Jwt:ExpireHours"], out var h) ? h : 2;

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiresHours),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public double GetRefreshTokenExpiryDays()
    {
        return double.TryParse(config["Jwt:RefreshTokenExpireDays"], out var days) ? days : 7;
    }
}
