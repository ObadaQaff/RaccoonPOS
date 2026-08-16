using RaccoonWarehouse.Domain.Accounting.JournalEntries;
using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Accounting.Banks
{
    public class BankStatementLine : BaseEntity
    {
        public int BankStatementId { get; set; }
        public BankStatement BankStatement { get; set; } = null!;
        public DateTime TransactionDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
        public bool IsReconciled { get; set; }
        public int? MatchedJournalEntryLineId { get; set; }
        public JournalEntryLine? MatchedJournalEntryLine { get; set; }
    }
}
