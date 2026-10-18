namespace RaccoonWarehouse.Domain.SupplierPayments.DTOs;

public class SupplierPaymentAllocationWriteDto
{
    public int VoucherId { get; set; }
    public int SupplierId { get; set; }
    public ICollection<SupplierPaymentAllocationLineDto> Lines { get; set; } = new List<SupplierPaymentAllocationLineDto>();
}

public class SupplierPaymentAllocationLineDto
{
    public int InvoiceId { get; set; }
    public decimal Amount { get; set; }
}
