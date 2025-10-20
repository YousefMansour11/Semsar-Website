using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities
{
    public class RentalDetails
    {
        public int Id { get; set; }

        public int PropertyId { get; set; }
        public Property Property { get; set; } = null!;

        // ===== Rental terms =====
        public RentalPeriod Period { get; set; }

        public bool Furnished { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? SecurityDeposit { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MaintenanceFee { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
