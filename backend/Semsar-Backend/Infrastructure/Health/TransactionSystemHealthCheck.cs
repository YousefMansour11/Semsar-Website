using System.Threading.Tasks;
using System.Threading;
using System;
using Infrastructure.Data;

namespace Infrastructure.Health
{
    public class TransactionSystemHealthCheck : IAppHealthCheck
    {
        private readonly AppDbContext _context;

        public TransactionSystemHealthCheck(AppDbContext context)
        {
            _context = context;
        }

        public string Name => "transaction_system";

        public async Task<(bool Healthy, string? Description)> CheckAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                return (true, "Transaction system healthy");
            }
            catch (Exception ex)
            {
                return (false, $"Transaction system unhealthy: {ex.Message}");
            }
        }
    }
}
