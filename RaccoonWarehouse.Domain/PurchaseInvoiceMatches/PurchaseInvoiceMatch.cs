using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.PurchaseInvoiceMatches;

public class PurchaseInvoiceMatch : BaseEntity
{
    public int InvoiceId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierInvoiceNumber { get; set; }
    public PurchaseInvoiceMatchingStatus Status { get; set; } = PurchaseInvoiceMatchingStatus.Draft;
    public decimal PriceTolerance { get; set; }
    public decimal QuantityTolerance { get; set; }
    public string? DiscrepancyReason { get; set; }
    public DateTime? MatchedDate { get; set; }
    public int? MatchedBy { get; set; }
    public ICollection<PurchaseInvoiceMatchLine> Lines { get; set; } = new List<PurchaseInvoiceMatchLine>();
}

public class PurchaseInvoiceMatchLine : BaseEntity
{
    public int PurchaseInvoiceMatchId { get; set; }
    public PurchaseInvoiceMatch PurchaseInvoiceMatch { get; set; } = null!;
    public int InvoiceLineId { get; set; }
    public int PurchaseOrderLineId { get; set; }
    public decimal InvoicedQuantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal InvoicedUnitPrice { get; set; }
    public decimal OrderedUnitPrice { get; set; }
    public decimal PriceVariance { get; set; }
    public decimal QuantityVariance { get; set; }
}
