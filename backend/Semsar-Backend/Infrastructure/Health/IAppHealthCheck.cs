using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Health
{
    public interface IAppHealthCheck
    {
        string Name { get; }
        Task<(bool Healthy, string? Description)> CheckAsync(CancellationToken cancellationToken = default);
    }
}
