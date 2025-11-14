using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs
{
    public class InstallmentDto
    {
        public string PaymentType { get; set; } = "Installment";

        [Range(0, 100)]
        public int DownPaymentPercent { get; set; }

        [Range(0, 100)]
        public int? DiscountPercent { get; set; }

        [Range(0, 50)]
        public int Years { get; set; }

        public bool IsEnabled { get; set; }

        public bool IsDeleted { get; set; }
    }
}