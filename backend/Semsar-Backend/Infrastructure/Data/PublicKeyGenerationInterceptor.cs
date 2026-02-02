using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Data
{
    public sealed class PublicKeyGenerationInterceptor : ISaveChangesInterceptor
    {
        private readonly IPublicIdService _publicIdService;

        public PublicKeyGenerationInterceptor(IPublicIdService publicIdService)
        {
            _publicIdService = publicIdService;
        }

        public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            SetPublicKeys(eventData);
            return result;
        }

        public ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            SetPublicKeys(eventData);
            return ValueTask.FromResult(result);
        }

        private void SetPublicKeys(DbContextEventData eventData)
        {
            var context = eventData.Context;
            if (context == null) return;

            var entries = context.ChangeTracker.Entries<IHasPublicKey>()
                .Where(e => (e.State == EntityState.Added || e.State == EntityState.Modified) && string.IsNullOrEmpty(e.Entity.PublicKey));

            foreach (var entry in entries)
            {
                var entity = entry.Entity;
                var prefix = GetPrefix(entity);
                if (!string.IsNullOrEmpty(prefix))
                {
                    entity.PublicKey = _publicIdService.GenerateId(prefix);
                }
            }
        }

        private static string GetPrefix(IHasPublicKey entity)
        {
            return entity switch
            {
                Property => EntityType.Property,
                Project => EntityType.Project,
                Unit => EntityType.Unit,
                ContactInfo => EntityType.Contact,
                Domain.Entities.User => EntityType.User,
                Lead => EntityType.Lead,
                BookingRequest => EntityType.BookingRequest,
                LandRequest => EntityType.LandRequest,
                _ => string.Empty
            };
        }
    }
}
