using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class InstallmentQueryService : IInstallmentQueryService
    {
        private readonly IUnitOfWork _uow;

        public InstallmentQueryService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<InstallmentDto>> GetPublicByPropertyIdAsync(int propertyId)
        {
            return await _uow.PropertyInstallmentPlans.Query()
                .Where(i => i.PropertyId == propertyId && !i.IsDeleted && i.IsEnabled)
                .Select(i => new InstallmentDto
                {
                    DownPaymentPercent = i.DownPaymentPercent,
                    DiscountPercent = i.DiscountPercent,
                    Years = i.Years,
                    IsEnabled = i.IsEnabled,
                    IsDeleted = i.IsDeleted,
                    PaymentType = i.PaymentType.ToString()
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Dictionary<int, List<InstallmentDto>>> GetPublicByPropertyIdsAsync(List<int> propertyIds)
        {
            var list = await _uow.PropertyInstallmentPlans.Query()
                .Where(i => propertyIds.Contains(i.PropertyId) && !i.IsDeleted && i.IsEnabled)
                .Select(i => new
                {
                    i.PropertyId,
                    dto = new InstallmentDto
                    {
                        DownPaymentPercent = i.DownPaymentPercent,
                        DiscountPercent = i.DiscountPercent,
                        Years = i.Years,
                        IsEnabled = i.IsEnabled,
                        IsDeleted = i.IsDeleted,
                        PaymentType = i.PaymentType.ToString()
                    }
                })
                .ToListAsync();

            return list.GroupBy(x => x.PropertyId).ToDictionary(g => g.Key, g => g.Select(x => x.dto).ToList());
        }

        public async Task<List<InstallmentDto>> GetAdminByPropertyIdAsync(int propertyId)
        {
            return await _uow.PropertyInstallmentPlans.Query()
                .Where(i => i.PropertyId == propertyId && !i.IsDeleted)
                .Select(i => new InstallmentDto
                {
                    DownPaymentPercent = i.DownPaymentPercent,
                    DiscountPercent = i.DiscountPercent,
                    Years = i.Years,
                    IsEnabled = i.IsEnabled,
                    IsDeleted = i.IsDeleted,
                    PaymentType = i.PaymentType.ToString()
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Dictionary<int, List<InstallmentDto>>> GetAdminByPropertyIdsAsync(List<int> propertyIds)
        {
            var list = await _uow.PropertyInstallmentPlans.Query()
                .Where(i => propertyIds.Contains(i.PropertyId) && !i.IsDeleted)
                .Select(i => new
                {
                    i.PropertyId,
                    dto = new InstallmentDto
                    {
                        DownPaymentPercent = i.DownPaymentPercent,
                        DiscountPercent = i.DiscountPercent,
                        Years = i.Years,
                        IsEnabled = i.IsEnabled,
                        IsDeleted = i.IsDeleted,
                        PaymentType = i.PaymentType.ToString()
                    }
                })
                .ToListAsync();

            return list.GroupBy(x => x.PropertyId).ToDictionary(g => g.Key, g => g.Select(x => x.dto).ToList());
        }
    }
}
