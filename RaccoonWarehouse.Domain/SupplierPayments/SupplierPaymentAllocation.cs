using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.SupplierPayments;

public class SupplierPaymentAllocation : BaseEntity
{
    public int VoucherId { get; set; }
    public int InvoiceId { get; set; }
    public int SupplierId { get; set; }
    public decimal Amount { get; set; }
    public DateTime AllocationDate { get; set; }
    public int? CreatedBy { get; set; }
}

public class SupplierAdvance : BaseEntity
{
    public int VoucherId { get; set; }
    public int SupplierId { get; set; }
    public decimal Amount { get; set; }
    public decimal AllocatedAmount { get; set; }
    public DateTime AdvanceDate { get; set; }
    public int? CreatedBy { get; set; }
    public string? Notes { get; set; }
}
