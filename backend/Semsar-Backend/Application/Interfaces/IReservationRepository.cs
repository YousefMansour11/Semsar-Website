using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Application.Interfaces
{
    public interface IReservationRepository
    {
        // Create a reservation entity in the current DbContext. Must be called inside an active transaction.
        // These methods only add the reservation to the DbContext; the caller must call SaveChanges once.
        Task<Domain.Entities.SlugReservation?> TryCreateSlugReservationAsync(string entityType, string slug);
        Task<Domain.Entities.CodeReservation?> TryCreateCodeReservationAsync(string entityType, string prefix, string code);

        // Link a reservation to an entity id (must be called within same transaction before commit)
        // These methods modify the tracked reservation but do NOT call SaveChanges.
        Task LinkSlugReservationAsync(int reservationId, int entityId);
        Task LinkCodeReservationAsync(int reservationId, int entityId);

        // Remove a reservation (for cleanup)
        Task<bool> ReleaseSlugReservationAsync(int reservationId);
        Task<bool> ReleaseCodeReservationAsync(int reservationId);
        // Cleanup any pending (tracked but unsaved) reservations from the DbContext change tracker
        Task CleanupPendingReservationsAsync();
        // Expose the underlying DbContext instance for runtime validation (read-only)
        DbContext? Context { get; }
        IAppMetrics Metrics { get; }
    }
}
