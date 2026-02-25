using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Health
{
    public class DependencyScopeHealthCheck : IAppHealthCheck
    {
        private readonly IServiceProvider _sp;
        public string Name => "dependency_scope";
        public DependencyScopeHealthCheck(IServiceProvider sp)
        {
            _sp = sp;
        }

        public Task<(bool Healthy, string? Description)> CheckAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var scope = _sp.CreateScope();
                // attempt to resolve scoped services used in request handling
                var _ = scope.ServiceProvider.GetService<Application.Interfaces.IUnitOfWork>();
                var __ = scope.ServiceProvider.GetService<Application.Interfaces.IReservationRepository>();
                return Task.FromResult<(bool, string?)>((true, "Scoped DI resolvable"));
            }
            catch (Exception ex)
            {
                return Task.FromResult<(bool, string?)>((false, ex.Message));
            }
        }
    }
}
