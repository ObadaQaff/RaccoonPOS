namespace RaccoonWarehouse.Domain.PurchaseControl.DTOs;

public sealed class PurchaseControlEntryDto
{
    public int Id { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public DateTime? PurchaseOrderDate { get; set; }
    public string? PurchaseRequestNumber { get; set; }
    public string? SupplierName { get; set; }
    public decimal? PurchaseOrderAmount { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal? PaidAmount { get; set; }
    public decimal? DueAmount { get; set; }
    public decimal? ReceiptAmount { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime? DueDate { get; set; }
    public string? LinkCheck { get; set; }
    public string? Notes { get; set; }
    public int? OverdueDays { get; set; }
    public decimal? ReceivedValue { get; set; }
}
