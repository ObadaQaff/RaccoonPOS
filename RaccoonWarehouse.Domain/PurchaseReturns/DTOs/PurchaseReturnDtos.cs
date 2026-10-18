namespace RaccoonWarehouse.Domain.PurchaseReturns.DTOs;

public class PurchaseReturnLineWriteDto
{
    public int GoodsReceiptLineId { get; set; }
    public int ProductId { get; set; }
    public int ProductUnitId { get; set; }
    public decimal Quantity { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class PurchaseReturnWriteDto
{
    public int Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public int? SupplierId { get; set; }
    public int? OriginalInvoiceId { get; set; }
    public int? GoodsReceiptId { get; set; }
    public string? Reason { get; set; }
    public DateTime ReturnDate { get; set; }
    public ICollection<PurchaseReturnLineWriteDto> Lines { get; set; } = new List<PurchaseReturnLineWriteDto>();
}
