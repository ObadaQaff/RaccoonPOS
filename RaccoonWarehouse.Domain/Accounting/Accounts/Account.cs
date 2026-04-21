using RaccoonWarehouse.Domain.Accounting.AccountOpeningBalances;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Accounting.JournalEntries;
using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Accounting.Accounts
{
    public class Account : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ArabicName { get; set; }
        public string? EnglishName { get; set; }
        public string? Description { get; set; }
        public AccountType AccountType { get; set; }
        public NormalBalanceType NormalBalanceType { get; set; } = NormalBalanceType.Debit;
        public bool IsPosting { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public bool IsSystemGenerated { get; set; }
        public bool AllowManualEntry { get; set; } = true;
        public int Level { get; set; }
        public int? CurrencyId { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public int? ParentAccountId { get; set; }
        public Account? ParentAccount { get; set; }
        public ICollection<Account> Children { get; set; } = new List<Account>();
        public ICollection<JournalEntryLine> JournalEntryLines { get; set; } = new List<JournalEntryLine>();
        public ICollection<AccountOpeningBalance> OpeningBalances { get; set; } = new List<AccountOpeningBalance>();
    }
}
