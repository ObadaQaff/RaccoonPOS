using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Accounting.Periods
{
    public class FiscalYear : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public FiscalYearStatus Status { get; set; } = FiscalYearStatus.Draft;
        public bool IsClosed { get; set; }
        public bool IsLegacy { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public ICollection<AccountingPeriod> AccountingPeriods { get; set; } = new List<AccountingPeriod>();
    }
}
