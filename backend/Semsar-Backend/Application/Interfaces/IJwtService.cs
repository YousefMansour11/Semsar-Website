using Domain.Entities;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(string username, string? role = null);
        string GenerateRefreshToken();
        double GetRefreshTokenExpiryDays();
    }
}
