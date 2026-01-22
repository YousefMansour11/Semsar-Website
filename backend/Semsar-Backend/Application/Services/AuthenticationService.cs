using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System;
using System.Linq;

namespace Application.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUnitOfWork _uow;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthenticationService> _logger;
        public AuthenticationService(IUnitOfWork uow, IJwtService jwtService, ILogger<AuthenticationService> logger)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<User> CreateUserAsync(string username, string password, string role = "Admin")
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("username required");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("password required");

            var user = new User { Username = username.Trim(), Role = role };
            user.PasswordHash = PasswordHelper.HashPassword(password);
            var usersRepo = _uow.Users;
            await usersRepo.AddAsync(user);
            await _uow.CommitAsync();
            return user;
        }

        public async Task<User?> ValidateCredentialsAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return null;
            var usersRepo = _uow.Users;
            var u = await usersRepo.Query().FirstOrDefaultAsync(x => x.Username == username);
            if (u == null) return null;

            if (u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTime.UtcNow)
            {
                return null;
            }

            if (u.LockoutEnd.HasValue && u.LockoutEnd.Value <= DateTime.UtcNow)
            {
                u.FailedLoginAttempts = 0;
                u.LockoutEnd = null;
            }

            if (PasswordHelper.VerifyHashedPassword(u.PasswordHash, password))
            {
                u.LastLoginAt = DateTime.UtcNow;
                u.FailedLoginAttempts = 0;
                u.LockoutEnd = null;
                usersRepo.Update(u);
                await _uow.CommitAsync();
                return u;
            }

            u.FailedLoginAttempts++;
            if (u.FailedLoginAttempts >= 5)
            {
                u.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
            }
            usersRepo.Update(u);
            await _uow.CommitAsync();
            return null;
        }

        public async Task<bool> AnyUsersExistAsync()
        {
            var usersRepo = _uow.Users;
            var any = await usersRepo.Query().AnyAsync();
            return any;
        }

        public async Task<RefreshToken> CreateRefreshTokenAsync(int userId, string ipAddress, string userAgent)
        {
            var token = new RefreshToken
            {
                Token = _jwtService.GenerateRefreshToken(),
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtService.GetRefreshTokenExpiryDays()),
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            await _uow.RefreshTokens.AddAsync(token);
            await _uow.CommitAsync();
            return token;
        }

        public async Task<RefreshToken?> ValidateRefreshTokenAsync(string token, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            if (string.IsNullOrWhiteSpace(ipAddress)) return null; // IP address is mandatory

            var rt = await _uow.RefreshTokens.Query()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == token);

            if (rt == null || !rt.IsActive || !rt.User.IsActive) return null;

            // Strict IP validation - token must have IP and it must match
            if (string.IsNullOrWhiteSpace(rt.IpAddress))
            {
                // First use - set the IP address
                rt.IpAddress = ipAddress;
                _uow.RefreshTokens.Update(rt);
                await _uow.CommitAsync();
            }
            else if (rt.IpAddress != ipAddress)
            {
                await RevokeTokenAsync(token, "IP address mismatch - potential token theft");
                _logger.LogWarning("Refresh token IP mismatch for user {UserId}. Old IP: {OldIp}, New IP: {NewIp}", rt.UserId, rt.IpAddress, ipAddress);
                return null;
            }

            return rt;
        }

        public async Task RevokeTokenAsync(string token, string reason = "User revocation")
        {
            var rt = await _uow.RefreshTokens.Query().FirstOrDefaultAsync(x => x.Token == token);
            if (rt != null)
            {
                rt.RevokedAt = DateTime.UtcNow;
                rt.ReasonRevoked = reason;
                await _uow.CommitAsync();
            }
        }

        public async Task<RefreshToken> ReplaceRefreshTokenAsync(string oldToken, int userId, string ipAddress, string userAgent)
        {
            await RevokeTokenAsync(oldToken, "Replaced by new token");
            return await CreateRefreshTokenAsync(userId, ipAddress, userAgent);
        }
    }
}
