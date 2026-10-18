using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.PurchaseRequests;

public class PurchaseRequest : BaseEntity
{
    public string RequestNumber { get; set; } = string.Empty;
    public PurchaseRequestStatus Status { get; set; } = PurchaseRequestStatus.Draft;
    public DateTime RequestDate { get; set; }
    public int? RequesterId { get; set; }
    public int? BranchId { get; set; }
    public string? Department { get; set; }
    public string? Priority { get; set; }
    public string? Justification { get; set; }
    public string? Notes { get; set; }
    public int? SubmittedBy { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public int? RejectedBy { get; set; }
    public DateTime? RejectedDate { get; set; }
    public string? RejectionReason { get; set; }
    public ICollection<PurchaseRequestLine> Lines { get; set; } = new List<PurchaseRequestLine>();
}

public class PurchaseRequestLine : BaseEntity
{
    public int PurchaseRequestId { get; set; }
    public PurchaseRequest PurchaseRequest { get; set; } = null!;
    public int? ProductId { get; set; }
    public int? ProductUnitId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal ApprovedQuantity { get; set; }
    public decimal AvailableStockSnapshot { get; set; }
    public decimal ReservedStockSnapshot { get; set; }
    public decimal AlreadyOrderedQuantitySnapshot { get; set; }
    public decimal MinimumStockSnapshot { get; set; }
    public decimal AverageSalesSnapshot { get; set; }
    public DateTime? LastPurchaseDateSnapshot { get; set; }
    public DateTime? ExpectedStockoutDate { get; set; }
    public string? Notes { get; set; }
}
