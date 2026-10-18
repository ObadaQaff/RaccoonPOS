using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.GoodsReceipts;

public class GoodsReceipt : BaseEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public int PurchaseOrderId { get; set; }
    public int? SupplierId { get; set; }
    public int? WarehouseId { get; set; }
    public int? BranchId { get; set; }
    public int? ReceivedBy { get; set; }
    public DateTime ReceiptDate { get; set; }
    public GoodsReceiptStatus Status { get; set; } = GoodsReceiptStatus.Draft;
    public DateTime? ConfirmedDate { get; set; }
    public string? Notes { get; set; }
    public ICollection<GoodsReceiptLine> Lines { get; set; } = new List<GoodsReceiptLine>();
}

public class GoodsReceiptLine : BaseEntity
{
    public int GoodsReceiptId { get; set; }
    public GoodsReceipt GoodsReceipt { get; set; } = null!;
    public int PurchaseOrderLineId { get; set; }
    public int ProductId { get; set; }
    public int ProductUnitId { get; set; }
    public decimal QuantityShipped { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityAccepted { get; set; }
    public decimal QuantityRejected { get; set; }
    public decimal QuantityDamaged { get; set; }
    public decimal FreeQuantity { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? StorageLocation { get; set; }
    public string? RejectionReason { get; set; }
}
