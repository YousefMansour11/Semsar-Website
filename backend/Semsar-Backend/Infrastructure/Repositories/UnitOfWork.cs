using System.Data;
using Application.Interfaces;
using System;
using System.Linq;
using Domain.Entities;
using Infrastructure.Data;
using System.Threading;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UnitOfWork>? _logger;
        private readonly Application.Interfaces.IAppMetrics? _metrics;
        private readonly Application.Interfaces.IConcurrencyValidator _concurrencyValidator;

        public UnitOfWork(AppDbContext context, Application.Interfaces.IConcurrencyValidator concurrencyValidator, Application.Interfaces.IReservationRepository reservations, ILogger<UnitOfWork>? logger = null, Application.Interfaces.IAppMetrics? metrics = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _concurrencyValidator = concurrencyValidator ?? throw new ArgumentNullException(nameof(concurrencyValidator));
            _logger = logger;
            _metrics = metrics;
            Properties = new Repository<Property>(_context);
            Units = new Repository<Unit>(_context);
            Projects = new Repository<Project>(_context);
            Leads = new Repository<Lead>(_context);
            Contacts = new Repository<ContactInfo>(_context);
            LandRequests = new LandRequestRepository(_context);
            Bookings = new Repository<Domain.Entities.BookingRequest>(_context);
            Locations = new Repository<Domain.Entities.Location>(_context);
            Features = new Repository<Domain.Entities.Feature>(_context);
            PropertyFeatures = new Repository<Domain.Entities.PropertyFeature>(_context);
            UnitFeatures = new Repository<Domain.Entities.UnitFeature>(_context);
            PropertyInstallmentPlans = new Repository<Domain.Entities.PropertyInstallmentPlan>(_context);
            UnitInstallmentPlans = new Repository<Domain.Entities.UnitInstallmentPlan>(_context);
            UnitVariants = new Repository<Domain.Entities.UnitVariant>(_context);
            Settings = new Repository<Domain.Entities.Setting>(_context);
            Users = new Repository<Domain.Entities.User>(_context);
            RefreshTokens = new Repository<Domain.Entities.RefreshToken>(_context);
            OrphanedUploads = new Repository<Domain.Entities.OrphanedUpload>(_context);
            Reservations = reservations ?? throw new ArgumentNullException(nameof(reservations));
        }

        public IRepository<Property> Properties { get; }
        public IRepository<Unit> Units { get; }
        public IRepository<Project> Projects { get; }
        public IRepository<Lead> Leads { get; }
        public IRepository<ContactInfo> Contacts { get; }
        public IRepository<LandRequest> LandRequests { get; }
        public IRepository<Domain.Entities.BookingRequest> Bookings { get; }
        public IRepository<Domain.Entities.Location> Locations { get; }
        public IRepository<Domain.Entities.Feature> Features { get; }
        public IRepository<Domain.Entities.PropertyFeature> PropertyFeatures { get; }
        public IRepository<Domain.Entities.UnitFeature> UnitFeatures { get; }
        public IRepository<Domain.Entities.PropertyInstallmentPlan> PropertyInstallmentPlans { get; }
        public IRepository<Domain.Entities.UnitInstallmentPlan> UnitInstallmentPlans { get; }
        public IRepository<Domain.Entities.UnitVariant> UnitVariants { get; }
        public IRepository<Domain.Entities.Setting> Settings { get; }
        public IRepository<Domain.Entities.User> Users { get; }
        public IRepository<Domain.Entities.RefreshToken> RefreshTokens { get; }
        public IRepository<Domain.Entities.OrphanedUpload> OrphanedUploads { get; }
        public Application.Interfaces.IReservationRepository Reservations { get; }
        public string? ConnectionString => _context.Database.GetConnectionString();

        public async Task CommitAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            if (Reservations.Context != null && !ReferenceEquals(Reservations.Context, _context))
            {
                _logger?.LogCritical("DI scope violation: ReservationRepository DbContext instance does not match UnitOfWork's DbContext");
                _metrics?.Increment("di.scope.violation");
                throw new InvalidOperationException("DbContext mismatch detected between UnitOfWork and ReservationRepository");
            }

            try
            {
                var cur = _context.Database.CurrentTransaction;
                if (cur != null && Infrastructure.Repositories.TimedDbTransaction.TryGet(cur.TransactionId, out var timed) && timed?.HasTimedOut == true)
                {
                    _metrics?.Increment("transaction.timeout");
                    _logger?.LogError("Transaction has already timed out before commit");
                    throw new TimeoutException("Transaction timeout exceeded before commit");
                }
            }
            catch (Exception ex) when (ex is not TimeoutException)
            {
                _logger?.LogError(ex, "Failed while validating current transaction state");
                throw;
            }

            var orphanSlug = _context.ChangeTracker.Entries<Domain.Entities.SlugReservation>()
                .Any(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added
                    && e.Entity != null
                    && e.Entity.EntityId == null
                    && e.Entity.Property == null
                    && e.Entity.Project == null
                    && e.Entity.Unit == null);

            var orphanCode = _context.ChangeTracker.Entries<Domain.Entities.CodeReservation>()
                .Any(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added
                    && e.Entity != null
                    && e.Entity.EntityId == null
                    && e.Entity.Property == null
                    && e.Entity.Project == null
                    && e.Entity.Unit == null);

            if (orphanSlug || orphanCode)
            {
                _logger?.LogError("Orphan reservations detected before commit. OrphanSlug={OrphanSlug}, OrphanCode={OrphanCode}", orphanSlug, orphanCode);
                throw new InvalidOperationException("Orphan reservations exist before commit");
            }

            var modifiedEntries = _context.ChangeTracker.Entries()
                .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Modified)
                .ToList();
            if (modifiedEntries.Any())
            {
                await _concurrencyValidator.ValidateNoExternalModificationAsync(modifiedEntries);
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException dbEx)
            {
                if (dbEx is DbUpdateConcurrencyException concurrencyEx)
                {
                    _metrics?.Increment("db.concurrency.conflict");
                    _logger?.LogError(concurrencyEx, "Concurrency conflict detected during SaveChanges: {Message}", concurrencyEx.GetBaseException()?.Message);
                    throw new InvalidOperationException("Concurrency conflict detected during SaveChanges.", concurrencyEx);
                }

                var baseEx = dbEx.GetBaseException();
                if (baseEx is SqlException sqlEx)
                {
                    if (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                    {
                        _logger?.LogInformation(dbEx, "Unique constraint violation during SaveChanges");
                        _metrics?.Increment("reservation.conflict");
                        throw new InvalidOperationException("Unique constraint violation during SaveChanges.", dbEx);
                    }

                    if (sqlEx.Number == 1205)
                    {
                        _metrics?.Increment("db.retry.deadlock");
                        _logger?.LogError(dbEx, "Deadlock detected during SaveChanges - failing fast");
                        throw new InvalidOperationException("Deadlock detected during SaveChanges.", dbEx);
                    }

                    if (sqlEx.Number == -2)
                    {
                        _metrics?.Increment("db.retry.timeout");
                        _logger?.LogError(dbEx, "Timeout detected during SaveChanges - failing fast");
                        throw new TimeoutException("Database timeout during SaveChanges.", dbEx);
                    }
                }

                _logger?.LogError(dbEx, "Unexpected DbUpdateException during SaveChanges");
                throw;
            }
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            var inner = await _context.Database.BeginTransactionAsync();
            var timeoutEnv = Environment.GetEnvironmentVariable("DB_TRANSACTION_TIMEOUT_MS");
            var timeoutMs = 30000;
            if (!string.IsNullOrEmpty(timeoutEnv) && int.TryParse(timeoutEnv, out var parsed)) timeoutMs = Math.Max(1000, parsed);
            return new TimedDbTransaction(inner, timeoutMs, _logger, _metrics);
        }

        public void DetachEntity(object entity)
        {
            _context.Entry(entity).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel)
        {
            // InMemory provider does not support transactions — return no-op wrapper
            if (_context.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger?.LogDebug("InMemory database detected — returning no-op transaction");
                return new NoopDbTransaction();
            }

            var inner = await _context.Database.BeginTransactionAsync(isolationLevel);
            var timeoutEnv = Environment.GetEnvironmentVariable("DB_TRANSACTION_TIMEOUT_MS");
            var timeoutMs = 30000;
            if (!string.IsNullOrEmpty(timeoutEnv) && int.TryParse(timeoutEnv, out var parsed)) timeoutMs = Math.Max(1000, parsed);
            return new TimedDbTransaction(inner, timeoutMs, _logger, _metrics);
        }
    }
}
