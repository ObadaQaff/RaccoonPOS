using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.CustomerRequests;

public enum CustomerRequestStatus { Submitted = 0, Approved = 1, Cancelled = 2 }
public enum CustomerRequestReceivingStatus { Pending = 0, PartiallyReceived = 1, Received = 2 }
public enum CustomerRequestSalesStatus { NotSold = 0, PartiallySold = 1, Sold = 2 }

public sealed class CustomerRequest : BaseEntity
{
    public string RequestNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public CustomerRequestStatus Status { get; set; } = CustomerRequestStatus.Submitted;
    public CustomerRequestReceivingStatus ReceivingStatus { get; set; } = CustomerRequestReceivingStatus.Pending;
    public CustomerRequestSalesStatus SalesStatus { get; set; } = CustomerRequestSalesStatus.NotSold;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string? PaymentNote { get; set; }
    public string? Notes { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public int? CancelledBy { get; set; }
    public DateTime? CancelledDate { get; set; }
    public int? PurchaseRequestId { get; set; }
    public ICollection<CustomerRequestLine> Lines { get; set; } = new List<CustomerRequestLine>();
}

public sealed class CustomerRequestLine : BaseEntity
{
    public int CustomerRequestId { get; set; }
    public CustomerRequest CustomerRequest { get; set; } = null!;
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
