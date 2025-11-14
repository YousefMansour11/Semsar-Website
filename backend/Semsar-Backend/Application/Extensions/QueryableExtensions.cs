using System.Linq;
using System.Linq.Expressions;

namespace Application.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> Paginate<T, TKey>(this IQueryable<T> query, Expression<Func<T, TKey>> orderBy, bool descending, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
            return query.Skip((page - 1) * pageSize).Take(pageSize);
        }
    }
}

