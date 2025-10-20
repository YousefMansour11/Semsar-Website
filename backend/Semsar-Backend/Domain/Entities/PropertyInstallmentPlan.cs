using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities
{
    public class PropertyInstallmentPlan : ISoftDelete
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public Property Property { get; set; } = null!;

        public PaymentType PaymentType { get; set; } = PaymentType.Installment;

        [Range(0, 100)]
        public int DownPaymentPercent { get; set; }

        [Range(0, 100)]
        public int? DiscountPercent { get; set; }

        [Range(0, 50)]
        public int Years { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MonthlyAmount { get; set; }

        public bool IsEnabled { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
