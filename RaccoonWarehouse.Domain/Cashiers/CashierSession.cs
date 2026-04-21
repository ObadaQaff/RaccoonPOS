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
        public SessionStatus Status { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
