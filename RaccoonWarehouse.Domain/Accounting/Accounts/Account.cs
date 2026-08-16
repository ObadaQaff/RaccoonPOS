using RaccoonWarehouse.Domain.Accounting.AccountOpeningBalances;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Accounting.JournalEntries;
using RaccoonWarehouse.Domain.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaccoonWarehouse.Domain.Accounting.Accounts
{
    public class Account : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public string? AccountCode { get; set; }
        public int? AccountLevel { get; set; }
        public string? AccountNature { get; set; }
        public string? AccountCategory { get; set; }
        public string? AccountTypeCode { get; set; }
        public CashFlowCategory? CashFlowCategory { get; set; }
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

        [NotMapped]
        public int? ParentId
        {
            get => ParentAccountId;
            set => ParentAccountId = value;
        }

        [NotMapped]
        public Account? Parent
        {
            get => ParentAccount;
            set => ParentAccount = value;
        }

        [NotMapped]
        public string NameAr
        {
            get => ArabicName ?? Name;
            set
            {
                Name = value ?? string.Empty;
                ArabicName = value;
            }
        }

        [NotMapped]
        public string NameEn
        {
            get => EnglishName ?? string.Empty;
            set => EnglishName = value;
        }

        [NotMapped]
        public bool IsGroup
        {
            get => !IsPosting;
            set => IsPosting = !value;
        }

        [NotMapped]
        public NormalBalanceType NormalBalance
        {
            get => NormalBalanceType;
            set => NormalBalanceType = value;
        }

        [NotMapped]
        public DateTime CreatedAt
        {
            get => CreatedDate;
            set => CreatedDate = value;
        }

        [NotMapped]
        public DateTime UpdatedAt
        {
            get => UpdatedDate;
            set => UpdatedDate = value;
        }
    }
}
