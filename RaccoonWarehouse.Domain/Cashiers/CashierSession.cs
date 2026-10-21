using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users;

namespace RaccoonWarehouse.Domain.Cashiers
{
    public class CashierSession : BaseEntity
    {
        public int CashierId { get; set; }
        public User Cashier { get; set; } = null!;
        public string? SessionNumber { get; set; }
        public int? BranchId { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal StatrBalance { get; set; }
        public decimal EndingBalance { get; set; }
        public decimal? ExpectedClosingBalance { get; set; }
        public decimal? DifferenceAmount { get; set; }
        public decimal? ExpectedVisa { get; set; }
        public decimal? CountedVisa { get; set; }
        public decimal? VisaDifference { get; set; }
        public string? ClosingNotes { get; set; }
        public int? ClosedBy { get; set; }
        public DateTime? SettlementCalculatedAt { get; set; }
        public decimal? SettlementCountedCash { get; set; }
        public decimal? SettlementCountedVisa { get; set; }
        public decimal? SettlementExpectedCash { get; set; }
        public decimal? SettlementExpectedVisa { get; set; }
        public decimal? SettlementCashDifference { get; set; }
        public decimal? SettlementVisaDifference { get; set; }
        public bool SettlementLocked { get; set; }
        public SessionStatus Status { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
