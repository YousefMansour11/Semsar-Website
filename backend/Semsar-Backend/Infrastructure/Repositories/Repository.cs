using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
            => await _dbSet.AsNoTracking().ToListAsync();

        public async Task<IEnumerable<T>> GetAllAsync(int maxResults)
            => await _dbSet.AsNoTracking().OrderBy(e => EF.Property<int>(e, "Id")).Take(maxResults).ToListAsync();

        public async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
        }

        public async Task AddAsync(T entity)
            => await _dbSet.AddAsync(entity);

        public void Update(T entity)
        {
            // Ensure entity is being tracked properly
            var entry = _context.Entry(entity);
            if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Detached)
            {
                _dbSet.Update(entity);
            }
        }

        public void Delete(T entity)
            => _dbSet.Remove(entity);

        public IQueryable<T> Query()
            => _dbSet.AsNoTracking().AsQueryable();

        /// <summary>
        /// Returns a queryable that tracks entities for update operations
        /// </summary>
        public IQueryable<T> QueryTracked()
            => _dbSet.AsQueryable();
    }
}
