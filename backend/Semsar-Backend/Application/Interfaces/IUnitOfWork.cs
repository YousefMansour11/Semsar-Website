using System.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Interfaces
{
    public interface IUnitOfWork
    {
        IRepository<Property> Properties { get; }
        IRepository<Unit> Units { get; }
        IRepository<Project> Projects { get; }
        IRepository<Lead> Leads { get; }
        IRepository<ContactInfo> Contacts { get; }
        IRepository<LandRequest> LandRequests { get; }
        IRepository<Domain.Entities.BookingRequest> Bookings { get; }
        IRepository<Domain.Entities.Location> Locations { get; }
        IRepository<Domain.Entities.Feature> Features { get; }
        IRepository<Domain.Entities.PropertyFeature> PropertyFeatures { get; }
        IRepository<Domain.Entities.UnitFeature> UnitFeatures { get; }
        // Installments handled via specific entities
        IRepository<Domain.Entities.PropertyInstallmentPlan> PropertyInstallmentPlans { get; }
        IRepository<Domain.Entities.UnitInstallmentPlan> UnitInstallmentPlans { get; }
        IRepository<Domain.Entities.UnitVariant> UnitVariants { get; }
        IRepository<Domain.Entities.Setting> Settings { get; }
        IRepository<Domain.Entities.User> Users { get; }
        IRepository<Domain.Entities.RefreshToken> RefreshTokens { get; }
        IRepository<Domain.Entities.OrphanedUpload> OrphanedUploads { get; }
        IReservationRepository Reservations { get; }
        string? ConnectionString { get; }
        Task CommitAsync(System.Threading.CancellationToken cancellationToken = default);
        void DetachEntity(object entity);

        // Begin a database transaction. Caller is responsible for committing/rolling back.
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel);
    }
}