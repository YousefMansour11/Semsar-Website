using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IAuthenticationService
    {
        Task<User?> ValidateCredentialsAsync(string username, string password);
        Task<User> CreateUserAsync(string username, string password, string role = "Admin");
        Task<bool> AnyUsersExistAsync();
        Task<RefreshToken> CreateRefreshTokenAsync(int userId, string ipAddress, string userAgent);
        Task<RefreshToken?> ValidateRefreshTokenAsync(string token, string ipAddress);
        Task RevokeTokenAsync(string token, string reason = "User revocation");
        Task<RefreshToken> ReplaceRefreshTokenAsync(string oldToken, int userId, string ipAddress, string userAgent);
    }
}
