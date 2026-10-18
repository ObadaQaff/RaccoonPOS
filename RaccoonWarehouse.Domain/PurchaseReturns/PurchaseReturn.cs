using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.PurchaseReturns;

public class PurchaseReturn : BaseEntity
{
    public string ReturnNumber { get; set; } = string.Empty;
    public int? SupplierId { get; set; }
    public int? OriginalInvoiceId { get; set; }
    public int? GoodsReceiptId { get; set; }
    public PurchaseReturnStatus Status { get; set; } = PurchaseReturnStatus.Draft;
    public string? Reason { get; set; }
    public DateTime ReturnDate { get; set; }
    public int? CreatedBy { get; set; }
    public int? ConfirmedBy { get; set; }
    public DateTime? ConfirmedDate { get; set; }
    public ICollection<PurchaseReturnLine> Lines { get; set; } = new List<PurchaseReturnLine>();
}

public class PurchaseReturnLine : BaseEntity
{
    public int PurchaseReturnId { get; set; }
    public PurchaseReturn PurchaseReturn { get; set; } = null!;
    public int GoodsReceiptLineId { get; set; }
    public int ProductId { get; set; }
    public int ProductUnitId { get; set; }
    public decimal Quantity { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public enum PurchaseReturnStatus
{
    Draft = 0,
    Approved = 1,
    Confirmed = 2,
    Reversed = 3
}
