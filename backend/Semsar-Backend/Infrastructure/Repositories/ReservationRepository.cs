using Infrastructure.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories
{
    public class ReservationRepository : Application.Interfaces.IReservationRepository
    {
        private readonly AppDbContext _context;
        private readonly Microsoft.Extensions.Logging.ILogger<ReservationRepository>? _logger;
        private readonly Application.Interfaces.IAppMetrics? _metrics;
        private static readonly Application.Interfaces.IAppMetrics _noop = new Infrastructure.Services.NoopMetrics();

        public ReservationRepository(AppDbContext context, Microsoft.Extensions.Logging.ILogger<ReservationRepository>? logger, Application.Interfaces.IAppMetrics? metrics = null)
        {
            _context = context;
            _logger = logger;
            _metrics = metrics;
        }

        public Microsoft.EntityFrameworkCore.DbContext? Context => _context;
        public Application.Interfaces.IAppMetrics Metrics => _metrics ?? _noop;

        private void EnsureInTransaction()
        {
            if (_context.Database.CurrentTransaction == null)
                throw new InvalidOperationException("Reservation operations must be executed inside an active DB transaction.");
        }

        public Task<Domain.Entities.SlugReservation?> TryCreateSlugReservationAsync(string entityType, string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return Task.FromResult<Domain.Entities.SlugReservation?>(null);
            EnsureInTransaction();
            var reservation = new Domain.Entities.SlugReservation { EntityType = entityType, Slug = slug, CreatedAt = DateTime.UtcNow };
            _logger?.LogInformation("Creating slug reservation (in-context) for {Slug} and {EntityType}", slug, entityType);
            _context.SlugReservations.Add(reservation);
            return Task.FromResult<Domain.Entities.SlugReservation?>(reservation);
        }

        public Task<Domain.Entities.CodeReservation?> TryCreateCodeReservationAsync(string entityType, string prefix, string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return Task.FromResult<Domain.Entities.CodeReservation?>(null);
            EnsureInTransaction();
            var reservation = new CodeReservation { EntityType = entityType, Prefix = prefix, Code = code, CreatedAt = DateTime.UtcNow };
            _logger?.LogInformation("Creating code reservation (in-context) for {Code} and {EntityType}", code, entityType);
            _context.CodeReservations.Add(reservation);
            return Task.FromResult<Domain.Entities.CodeReservation?>(reservation);
        }

        public async Task LinkSlugReservationAsync(int reservationId, int entityId)
        {
            EnsureInTransaction();
            _logger?.LogInformation("Linking slug reservation {ResId} to entity {EntityId} (in-context)", reservationId, entityId);
            var res = await _context.SlugReservations.FindAsync(reservationId);
            if (res == null) throw new KeyNotFoundException("Reservation not found");

            var entityTypeNav = (res.EntityType ?? string.Empty).ToLowerInvariant();
            if (entityTypeNav == "property")
            {
                var ent = await _context.Properties.FindAsync(entityId);
                if (ent == null) throw new InvalidOperationException("Reservation linking failed: target property not found");
                res.Property = ent;
            }
            else if (entityTypeNav == "project")
            {
                var ent = await _context.Projects.FindAsync(entityId);
                if (ent == null) throw new InvalidOperationException("Reservation linking failed: target project not found");
                res.Project = ent;
            }
            else if (entityTypeNav == "unit")
            {
                var ent = await _context.Units.FindAsync(entityId);
                if (ent == null) throw new InvalidOperationException("Reservation linking failed: target unit not found");
                res.Unit = ent;
            }
            else
            {
                throw new InvalidOperationException($"Reservation linking failed: unknown entity type {res.EntityType}");
            }
            _context.SlugReservations.Update(res);
        }

        public async Task LinkCodeReservationAsync(int reservationId, int entityId)
        {
            EnsureInTransaction();
            _logger?.LogInformation("Linking code reservation {ResId} to entity {EntityId} (in-context)", reservationId, entityId);
            var res = await _context.CodeReservations.FindAsync(reservationId);
            if (res == null) throw new KeyNotFoundException("Reservation not found");

            var entityTypeNav = (res.EntityType ?? string.Empty).ToLowerInvariant();
            if (entityTypeNav == "property")
            {
                var ent = await _context.Properties.FindAsync(entityId);
                if (ent == null) throw new InvalidOperationException("Reservation linking failed: target property not found");
                res.Property = ent;
            }
            else if (entityTypeNav == "project")
            {
                var ent = await _context.Projects.FindAsync(entityId);
                if (ent == null) throw new InvalidOperationException("Reservation linking failed: target project not found");
                res.Project = ent;
            }
            else if (entityTypeNav == "unit")
            {
                var ent = await _context.Units.FindAsync(entityId);
                if (ent == null) throw new InvalidOperationException("Reservation linking failed: target unit not found");
                res.Unit = ent;
            }
            else
            {
                throw new InvalidOperationException($"Reservation linking failed: unknown entity type {res.EntityType}");
            }
            _context.CodeReservations.Update(res);
        }

        public async Task<bool> ReleaseSlugReservationAsync(int reservationId)
        {
            EnsureInTransaction();
            _logger?.LogInformation("Releasing slug reservation (in-context) {ResId}", reservationId);
            var res = await _context.SlugReservations.FindAsync(reservationId);
            if (res == null) return false;
            _context.SlugReservations.Remove(res);
            return true;
        }

        public async Task<bool> ReleaseCodeReservationAsync(int reservationId)
        {
            EnsureInTransaction();
            _logger?.LogInformation("Releasing code reservation (in-context) {ResId}", reservationId);
            var res = await _context.CodeReservations.FindAsync(reservationId);
            if (res == null) return false;
            _context.CodeReservations.Remove(res);
            return true;
        }

        public Task CleanupPendingReservationsAsync()
        {
            try
            {
                var entries = _context.ChangeTracker.Entries<Domain.Entities.SlugReservation>();
                foreach (var e in entries)
                {
                    if (e.Entity != null && e.State == Microsoft.EntityFrameworkCore.EntityState.Added && e.Entity.EntityId == null)
                    {
                        e.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                }

                var codeEntries = _context.ChangeTracker.Entries<Domain.Entities.CodeReservation>();
                foreach (var e in codeEntries)
                {
                    if (e.Entity != null && e.State == Microsoft.EntityFrameworkCore.EntityState.Added && e.Entity.EntityId == null)
                    {
                        e.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to cleanup pending reservations from ChangeTracker");
                throw;
            }
            return Task.CompletedTask;
        }
    }
}
