using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Application.Services
{
    public class PropertyInstallmentService
    {
        private readonly IUnitOfWork _uow;

        public PropertyInstallmentService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // Only used for propagation during deletes
        public async Task PropagateSoftDeleteAsync(int propertyId)
        {
            var installments = _uow.PropertyInstallmentPlans.Query().Where(x => x.PropertyId == propertyId && !x.IsDeleted);
            await foreach (var inst in installments.AsAsyncEnumerable())
            {
                inst.IsDeleted = true;
                _uow.PropertyInstallmentPlans.Update(inst);
            }
        }
    }
}
