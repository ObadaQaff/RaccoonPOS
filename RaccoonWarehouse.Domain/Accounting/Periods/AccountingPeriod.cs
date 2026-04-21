using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Accounting.Periods
{
    public class AccountingPeriod : BaseEntity
    {
        public int FiscalYearId { get; set; }
        public FiscalYear FiscalYear { get; set; } = null!;
        public int PeriodNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public AccountingPeriodStatus Status { get; set; } = AccountingPeriodStatus.Draft;
        public bool IsClosed { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
