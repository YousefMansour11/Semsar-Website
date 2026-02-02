using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Infrastructure.Repositories
{
    public static class PropertyRepositoryExtensions
    {
        public static IQueryable<Property> SearchByCode(this IQueryable<Property> query, string code)
        {
            return query.Include(p => p.Contact)
                        .Where(p => p.Code == code);
        }
    }
}