using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Services;
using Application.Interfaces;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;

namespace API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IJwtService _jwtService;
        private readonly IAuthenticationService _authService;
        private readonly Microsoft.Extensions.Logging.ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, FailedLoginRecord> _failedLogins = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _loginLocks = new();
        private static readonly object _cleanupLock = new();
        private static DateTime _lastCleanup = DateTime.MinValue;

        public AuthController(IJwtService jwtService, IAuthenticationService authService, Microsoft.Extensions.Logging.ILogger<AuthController> logger, IConfiguration configuration)
        {
            _jwtService = jwtService;
            _authService = authService;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new { message = "Username and password are required" });
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            TrimFallback();

            // Progressive delay based on failed attempts
            var ipKey = $"{dto.Username}@{ip}";
            var ipSemaphore = _loginLocks.GetOrAdd(ipKey, _ => new SemaphoreSlim(1, 1));
            Domain.Entities.User? user;
            try
            {
                await ipSemaphore.WaitAsync();
                var failedRecord = _failedLogins.GetOrAdd(ipKey, _ => new FailedLoginRecord());
                if (failedRecord.Attempts > 0 && DateTime.UtcNow - failedRecord.LastAttempt < TimeSpan.FromMinutes(15))
                {
                    var delay = Math.Min(failedRecord.Attempts * 500, 5000);
                    await Task.Delay(delay);
                }

                user = await _authService.ValidateCredentialsAsync(dto.Username, dto.Password);
                if (user == null)
                {
                    failedRecord.Attempts++;
                    failedRecord.LastAttempt = DateTime.UtcNow;
                    _logger.LogWarning("AbuseAudit: Failed login attempt Username={Username} IP={IP}", dto.Username, ip);
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                // Successful login — reset failure counter
                _failedLogins.TryRemove(ipKey, out _);
            }
            finally
            {
                ipSemaphore.Release();
            }

            var userAgent = Request.Headers["User-Agent"].ToString();

            _logger.LogInformation("AbuseAudit: Successful login Username={Username} IP={IP}", dto.Username, ip);

            var token = _jwtService.GenerateToken(user.Username, user.Role);
            var refreshToken = await _authService.CreateRefreshTokenAsync(user.Id, ip, userAgent);

            return Ok(new
            {
                token,
                refreshToken = refreshToken.Token,
                expiresInHours = double.TryParse(_configuration["Jwt:ExpireHours"], out var h) ? h : 2,
                user = new
                {
                    id = user.Id,
                    username = user.Username,
                    role = user.Role
                }
            });
        }

        private class FailedLoginRecord
        {
            public int Attempts { get; set; }
            public DateTime LastAttempt { get; set; }
        }

        private static void TrimFallback()
        {
            var now = DateTime.UtcNow;
            if (now - _lastCleanup < TimeSpan.FromMinutes(1))
                return;

            lock (_cleanupLock)
            {
                if (now - _lastCleanup < TimeSpan.FromMinutes(1))
                    return;
                _lastCleanup = now;
            }

            var cutoff = now.AddMinutes(-15);
            foreach (var key in _failedLogins.Keys)
            {
                if (_failedLogins.TryGetValue(key, out var record) && record.LastAttempt < cutoff)
                {
                    if (_failedLogins.TryRemove(key, out _) && _loginLocks.TryRemove(key, out var sem))
                        sem.Dispose();
                }
            }
        }

        [HttpPost("refresh")]
        [Authorize]
        public async Task<IActionResult> Refresh([FromBody] RefreshDto dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            {
                return BadRequest(new { message = "Refresh token is required" });
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var rt = await _authService.ValidateRefreshTokenAsync(dto.RefreshToken, ip);
            if (rt == null)
            {
                return Unauthorized(new { message = "Invalid or expired refresh token" });
            }

            var newToken = _jwtService.GenerateToken(rt.User.Username, rt.User.Role);
            var newRt = await _authService.ReplaceRefreshTokenAsync(dto.RefreshToken, rt.UserId, ip, Request.Headers["User-Agent"].ToString());

            return Ok(new
            {
                token = newToken,
                refreshToken = newRt.Token,
                expiresInHours = double.TryParse(_configuration["Jwt:ExpireHours"], out var h) ? h : 2
            });
        }

        [HttpPost("revoke")]
        [Authorize]
        public async Task<IActionResult> Revoke([FromBody] RevokeDto dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            {
                return BadRequest(new { message = "Refresh token is required" });
            }

            await _authService.RevokeTokenAsync(dto.RefreshToken, "User revocation");
            return NoContent();
        }
    }

    public class LoginDto
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    public class RefreshDto
    {
        public required string RefreshToken { get; set; }
    }

    public class RevokeDto
    {
        public required string RefreshToken { get; set; }
    }
}
