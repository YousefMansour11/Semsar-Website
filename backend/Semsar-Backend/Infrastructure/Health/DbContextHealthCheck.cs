using System;
using System.Threading;
using System.Threading.Tasks;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Health
{
    public class DbContextHealthCheck : IAppHealthCheck
    {
        private readonly AppDbContext _context;
        public string Name => "db";
        public DbContextHealthCheck(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Healthy, string? Description)> CheckAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // simple lightweight query
                await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
                return (true, "Db ok");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
