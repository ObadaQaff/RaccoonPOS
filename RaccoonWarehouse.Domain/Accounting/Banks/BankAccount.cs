using RaccoonWarehouse.Domain.Accounting.Accounts;
using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Currencies;

namespace RaccoonWarehouse.Domain.Accounting.Banks
{
    public class BankAccount : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public int GlAccountId { get; set; }
        public Account GlAccount { get; set; } = null!;
        public int CurrencyId { get; set; }
        public Currency Currency { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        public ICollection<BankStatement> Statements { get; set; } = new List<BankStatement>();
    }
}
