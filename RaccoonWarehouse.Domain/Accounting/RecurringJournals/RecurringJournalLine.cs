using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.CostCenters;

namespace RaccoonWarehouse.Domain.Accounting.RecurringJournals
{
    public class RecurringJournalLine : BaseEntity
    {
        public int RecurringJournalId { get; set; }
        public RecurringJournal RecurringJournal { get; set; } = null!;
        public int AccountId { get; set; }
        public Account Account { get; set; } = null!;
        public int? CostCenterId { get; set; }
        public CostCenter? CostCenter { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public string? Description { get; set; }
    }
}
