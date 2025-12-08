using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{
    public class DashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DashboardService>? _logger;

        public DashboardService(IUnitOfWork unitOfWork, ILogger<DashboardService>? logger = null)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger;
        }

        public async Task<object> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            var propQuery = _unitOfWork.Properties.Query().Where(p => !p.IsDeleted);
            var projQuery = _unitOfWork.Projects.Query().Where(p => !p.IsDeleted);
            var leadQuery = _unitOfWork.Leads.Query().Where(l => !l.IsDeleted);
            var unitQuery = _unitOfWork.Units.Query().Where(u => !u.IsDeleted);

            var totalProperties = await propQuery.CountAsync(cancellationToken);
            var totalProjects = await projQuery.CountAsync(cancellationToken);
            var rentals = await propQuery.CountAsync(x => x.ListingType == PropertyListingType.Rental || x.RentPerMonth > 0, cancellationToken);
            var resale = await propQuery.CountAsync(x => x.ListingType == PropertyListingType.Resale, cancellationToken);
            var projectUnits = await unitQuery.CountAsync(cancellationToken);
            var totalUnits = projectUnits;
            var totalLeads = await leadQuery.CountAsync(cancellationToken);

            _logger?.LogInformation("Dashboard stats retrieved: {TotalProperties} properties, {TotalProjects} projects, {TotalUnits} units, {TotalLeads} leads",
                totalProperties, totalProjects, totalUnits, totalLeads);

            return new
            {
                TotalProperties = totalProperties,
                TotalProjects = totalProjects,
                Rentals = rentals,
                Resale = resale,
                ProjectUnits = projectUnits,
                TotalUnits = totalUnits,
                TotalLeads = totalLeads
            };
        }
    }
}