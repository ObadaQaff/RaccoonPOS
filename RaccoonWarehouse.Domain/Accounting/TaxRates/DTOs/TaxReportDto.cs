namespace RaccoonWarehouse.Domain.Accounting.TaxRates.DTOs
{
    public class TaxReportDto
    {
        public decimal InputVAT { get; set; }
        public decimal OutputVAT { get; set; }
        public decimal NetVATPayable { get; set; }
    }
}
