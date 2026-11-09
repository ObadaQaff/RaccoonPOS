namespace RaccoonWarehouse.Domain.LostCustomers;

public class LostCustomerWriteDto
{
    public LostCustomerType Type { get; set; }
    public LostCustomerReason Reason { get; set; }
    public int? ProductId { get; set; }
    public int? ProductUnitId { get; set; }
    public string? ProductName { get; set; }
    public decimal? Quantity { get; set; }
    public string? Message { get; set; }
    public string? Notes { get; set; }
}

public sealed class LostCustomerDto : LostCustomerWriteDto
{
    public int Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
}
