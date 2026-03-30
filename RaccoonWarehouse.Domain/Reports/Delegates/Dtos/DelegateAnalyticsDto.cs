namespace RaccoonWarehouse.Domain.Reports.Delegates.Dtos
{
    public class DelegateAnalyticsDto
    {
        public int DelegateId { get; set; }
        public string DelegateName { get; set; } = string.Empty;
        public int TotalInvoices { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public int InvoicesInRange { get; set; }
        public int UniqueCustomersServed { get; set; }
        public DateTime? LastInvoiceDate { get; set; }
        public int OpenInvoicesCount { get; set; }
    }
}
