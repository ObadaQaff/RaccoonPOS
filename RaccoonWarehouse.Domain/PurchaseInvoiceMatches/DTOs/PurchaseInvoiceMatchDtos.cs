using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.PurchaseInvoiceMatches.DTOs;

public class PurchaseInvoiceMatchLineWriteDto
{
    public int InvoiceLineId { get; set; }
    public int PurchaseOrderLineId { get; set; }
}

public class PurchaseInvoiceMatchWriteDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public string? SupplierInvoiceNumber { get; set; }
    public decimal PriceTolerance { get; set; } = 0.01m;
    public decimal QuantityTolerance { get; set; }
    public ICollection<PurchaseInvoiceMatchLineWriteDto> Lines { get; set; } = new List<PurchaseInvoiceMatchLineWriteDto>();
}

public class PurchaseInvoiceMatchReadDto : PurchaseInvoiceMatchWriteDto
{
    public PurchaseInvoiceMatchingStatus Status { get; set; }
    public string? DiscrepancyReason { get; set; }
    public DateTime? MatchedDate { get; set; }
}
