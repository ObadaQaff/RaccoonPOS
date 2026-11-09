namespace RaccoonWarehouse.Domain.CustomerRequests;

public sealed class CustomerRequestLineDto
{
    public int Id { get; set; }
    public int? ProductId { get; set; }
    public int? ProductUnitId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? UnitName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal SoldQuantity { get; set; }
    public string? Notes { get; set; }
}

public sealed class CustomerRequestDto
{
    public int Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public CustomerRequestStatus Status { get; set; }
    public CustomerRequestReceivingStatus ReceivingStatus { get; set; }
    public CustomerRequestSalesStatus SalesStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string? PaymentNote { get; set; }
    public string? Notes { get; set; }
    public int? PurchaseRequestId { get; set; }
    public List<CustomerRequestLineDto> Lines { get; set; } = new();
    public string PaymentStatus => PaidAmount <= 0 ? "غير مدفوع" : PaidAmount >= TotalAmount ? "مدفوع بالكامل" : "مدفوع جزئياً";
    public string StatusText => Status switch { CustomerRequestStatus.Submitted => "مُرسل", CustomerRequestStatus.Approved => "معتمد", _ => "ملغى" };
    public string ReceivingStatusText => ReceivingStatus switch { CustomerRequestReceivingStatus.Pending => "قيد الانتظار", CustomerRequestReceivingStatus.PartiallyReceived => "مستلم جزئياً", _ => "مستلم بالكامل" };
    public string SalesStatusText => SalesStatus switch { CustomerRequestSalesStatus.NotSold => "غير مباع", CustomerRequestSalesStatus.PartiallySold => "مباع جزئياً", _ => "مباع بالكامل" };
}

public sealed class CustomerRequestWriteDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public decimal PaidAmount { get; set; }
    public string? PaymentNote { get; set; }
    public string? Notes { get; set; }
    public List<CustomerRequestLineDto> Lines { get; set; } = new();
}
