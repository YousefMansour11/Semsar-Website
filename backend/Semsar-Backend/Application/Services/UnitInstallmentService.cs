using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Application.Services
{
    public class UnitInstallmentService
    {
        // UnitInstallmentService retained for legacy compatibility but not used for new property-centric installments.
        private readonly IUnitOfWork _uow;

        public UnitInstallmentService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task PropagateSoftDeleteAsync(int unitId)
        {
            var installments = _uow.UnitInstallmentPlans.Query().Where(x => x.UnitId == unitId && !x.IsDeleted);
            await foreach (var inst in installments.AsAsyncEnumerable())
            {
                inst.IsDeleted = true;
                _uow.UnitInstallmentPlans.Update(inst);
            }
        }
    }
}
