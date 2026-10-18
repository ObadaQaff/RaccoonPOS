using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.PurchaseRequests.DTOs;

public class PurchaseRequestLineWriteDto
{
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

public class PurchaseRequestWriteDto
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public PurchaseRequestStatus Status { get; set; } = PurchaseRequestStatus.Draft;
    public DateTime RequestDate { get; set; }
    public int? RequesterId { get; set; }
    public int? BranchId { get; set; }
    public string? Department { get; set; }
    public string? Priority { get; set; }
    public string? Justification { get; set; }
    public string? Notes { get; set; }
    public ICollection<PurchaseRequestLineWriteDto> Lines { get; set; } = new List<PurchaseRequestLineWriteDto>();
}

public class PurchaseRequestReadDto : PurchaseRequestWriteDto
{
    public int LineCount { get; set; }
}
