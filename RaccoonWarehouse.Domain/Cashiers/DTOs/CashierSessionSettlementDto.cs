namespace RaccoonWarehouse.Domain.Cashiers.DTOs;

public sealed class CashierSessionSettlementDto
{
    public decimal CountedCash { get; set; }
    public decimal CountedVisa { get; set; }
    public string? ClosingNotes { get; set; }
    public int? ClosedBy { get; set; }
}
