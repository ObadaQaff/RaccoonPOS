namespace RaccoonWarehouse.Domain.Reports.Delegates.Dtos
{
    public class DelegateReportRowDto
    {
        public int DelegateId { get; set; }
        public string DelegateName { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
        public decimal TotalSales { get; set; }
        public decimal AverageInvoiceValue { get; set; }
        public DateTime? LastActivityDate { get; set; }
    }
}
