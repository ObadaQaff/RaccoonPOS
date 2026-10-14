using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Invoices.DTOs
{
    /// <summary>
    /// Header-only invoice data used to select an invoice before loading its lines.
    /// </summary>
    public sealed class ReturnInvoiceLookupDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public List<PaymentType> PaymentTypes { get; set; } = new();
        public string ReturnStatus { get; set; } = string.Empty;
        public bool IsFullyReturned { get; set; }
    }
}
