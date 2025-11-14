using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs
{
    // DTO used when creating or replacing installments as part of Property create/update.
    // Client MUST NOT provide any PropertyId or Entity identifiers; the server assigns PropertyId.
    public class CreateInstallmentDto
    {
        // Whether this installment is enabled (visible/active). Defaults to true.
        public bool IsEnabled { get; set; } = true;

        public PaymentType PaymentType { get; set; } = PaymentType.Installment;

        public int DownPaymentPercent { get; set; }

        [Range(0, 100)]
        public int? DiscountPercent { get; set; }

        public int Years { get; set; }
    }
}
