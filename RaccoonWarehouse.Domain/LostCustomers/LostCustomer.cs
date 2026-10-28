using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.LostCustomers;

public enum LostCustomerType { MissingProduct = 0, GeneralReason = 1 }
public enum LostCustomerReason { OutOfStock = 0, ProductNotRegistered = 1, Other = 2 }

public sealed class LostCustomer : BaseEntity
{
    public LostCustomerType Type { get; set; }
    public LostCustomerReason Reason { get; set; }
    public int? ProductId { get; set; }
    public int? ProductUnitId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string? Message { get; set; }
    public string? Notes { get; set; }
    public int? CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
}
