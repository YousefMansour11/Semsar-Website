using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Infrastructure.Health
{
    public class InfrastructureHealthCheck : IInfrastructureHealthCheck
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<InfrastructureHealthCheck>? _logger;
        private readonly Application.Interfaces.IAppMetrics? _metrics;

        public InfrastructureHealthCheck(IServiceProvider sp, ILogger<InfrastructureHealthCheck>? logger = null, Application.Interfaces.IAppMetrics? metrics = null)
        {
            _sp = sp;
            _logger = logger;
            _metrics = metrics;
        }

        public void ValidateScopedDependencies()
        {
            try
            {
                // Resolve a scope and compare DbContext instances
                using var scope = _sp.CreateScope();
                var uow = scope.ServiceProvider.GetService<Application.Interfaces.IUnitOfWork>();
                var repo = scope.ServiceProvider.GetService<Application.Interfaces.IReservationRepository>();
                if (uow == null || repo == null) return;

                // Attempt to access concrete contexts if exposed
                var uowType = uow.GetType();
                var repoType = repo.GetType();
                // Use reflection to try to get context properties
                var uowContextProp = uowType.GetProperty("_context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var repoContextProp = repoType.GetProperty("_context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (uowContextProp != null && repoContextProp != null)
                {
                    var uCtx = uowContextProp.GetValue(uow);
                    var rCtx = repoContextProp.GetValue(repo);
                    if (!ReferenceEquals(uCtx, rCtx))
                    {
                        _logger?.LogCritical("Invalid DI scope: multiple DbContext instances detected");
                        _metrics?.Increment("di.scope.violation");
                        throw new InvalidOperationException("Invalid DI scope: multiple DbContext instances detected");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Scoped dependency validation failed");
                throw;
            }
        }
    }
}
