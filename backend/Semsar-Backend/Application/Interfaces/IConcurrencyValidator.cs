using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IConcurrencyValidator
    {
        Task ValidateNoExternalModificationAsync(IEnumerable<EntityEntry> entries);
        Task<bool> IsHealthyAsync(System.Threading.CancellationToken cancellationToken);
        Task LogMetricsAsync();
    }
}
