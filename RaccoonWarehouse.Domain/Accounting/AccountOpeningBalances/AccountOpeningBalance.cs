using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Accounting.Periods;
using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Accounting.AccountOpeningBalances
{
    public class AccountOpeningBalance : BaseEntity
    {
        public int FiscalYearId { get; set; }
        public FiscalYear FiscalYear { get; set; } = null!;
        public int AccountId { get; set; }
        public Account Account { get; set; } = null!;
        public int? BranchId { get; set; }
        public int? CostCenterId { get; set; }
        public int? WarehouseId { get; set; }
        public int? PartyUserId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
