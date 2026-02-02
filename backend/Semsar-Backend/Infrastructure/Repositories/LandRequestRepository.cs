using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class LandRequestRepository : IRepository<LandRequest>
    {
        private readonly AppDbContext _context;
        public LandRequestRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<LandRequest>> GetAllAsync()
            => await _context.LandRequests
                             .AsNoTracking()
                             .OrderByDescending(lr => lr.CreatedAt)
                             .ToListAsync();

        public async Task<LandRequest?> GetByIdAsync(int id)
            => await _context.LandRequests.FindAsync(id);

        public async Task AddAsync(LandRequest entity)
            => await _context.LandRequests.AddAsync(entity);

        public void Update(LandRequest entity)
            => _context.LandRequests.Update(entity);

        public void Delete(LandRequest entity)
            => _context.LandRequests.Remove(entity);

        public IQueryable<LandRequest> Query()
            => _context.LandRequests.AsNoTracking().AsQueryable();

        public IQueryable<LandRequest> QueryTracked()
            => _context.LandRequests.AsQueryable();

        public async Task<IEnumerable<LandRequest>> GetAllAsync(int maxResults)
            => await _context.LandRequests
                             .AsNoTracking()
                             .OrderByDescending(lr => lr.CreatedAt)
                             .Take(maxResults)
                             .ToListAsync();
    }
}