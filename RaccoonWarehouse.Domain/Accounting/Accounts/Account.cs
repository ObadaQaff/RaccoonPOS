using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Accounting.JournalEntries;
using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Accounting.Accounts
{
    public class Account : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public AccountType AccountType { get; set; }
        public bool IsPosting { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public int? ParentAccountId { get; set; }
        public Account? ParentAccount { get; set; }
        public ICollection<Account> Children { get; set; } = new List<Account>();
        public ICollection<JournalEntryLine> JournalEntryLines { get; set; } = new List<JournalEntryLine>();
    }
}
