using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Accounting.Banks
{
    public class BankStatement : BaseEntity
    {
        public int BankAccountId { get; set; }
        public BankAccount BankAccount { get; set; } = null!;
        public DateTime StatementDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public DateTime ImportedAt { get; set; }
        public ICollection<BankStatementLine> Lines { get; set; } = new List<BankStatementLine>();
    }
}
