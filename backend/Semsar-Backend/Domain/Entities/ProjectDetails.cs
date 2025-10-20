using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class ProjectDetails
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        // Finance
        [Range(0, 100)]
        public decimal CashDiscountPercentage { get; set; }

        [Range(0, 100)]
        public decimal DownPaymentPercentage { get; set; }

        [Range(1, 50)]
        public int MinInstallmentYears { get; set; }

        [Range(1, 50)]
        public int MaxInstallmentYears { get; set; }

        [MaxLength(500)]
        public string? PaymentNotes { get; set; }

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
