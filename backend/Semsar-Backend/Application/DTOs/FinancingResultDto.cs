namespace Application.DTOs
{
    public class FinancingResultDto
    {
        public decimal VariantPrice { get; set; }
        public string Currency { get; set; } = "EGP";
        public decimal DownPaymentPercent { get; set; }
        public int Years { get; set; }
        public decimal DownPaymentAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal MonthlyInstallment { get; set; }
    }
}
