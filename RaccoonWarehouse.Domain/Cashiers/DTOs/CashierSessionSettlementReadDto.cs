namespace RaccoonWarehouse.Domain.Cashiers.DTOs;

public sealed class CashierSessionSettlementReadDto
{
    public int SessionId { get; init; }
    public decimal OpeningCash { get; init; }
    public decimal ExpectedCash { get; init; }
    public decimal ExpectedVisa { get; init; }
    public decimal CountedCash { get; init; }
    public decimal CountedVisa { get; init; }
    public decimal CashDifference => CountedCash - ExpectedCash;
    public decimal VisaDifference => CountedVisa - ExpectedVisa;
    public bool IsCashMatched => CashDifference == 0m;
    public bool IsVisaMatched => VisaDifference == 0m;
    public IReadOnlyList<CashierSessionActionReadDto> Actions { get; init; } = Array.Empty<CashierSessionActionReadDto>();
}

public sealed class CashierSessionActionReadDto
{
    public DateTime Date { get; init; }
    public string ActionType { get; init; } = string.Empty;
    public string DocumentNumber { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Direction { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public bool CountedInSettlement { get; init; }
}
