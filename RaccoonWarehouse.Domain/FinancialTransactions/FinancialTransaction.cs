using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Cashiers;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaccoonWarehouse.Domain.FinancialTransactions
{
    public class FinancialTransaction : BaseEntity
    {
        public string TransactionNumber { get; set; } = null!;
        [Column("Type")]
        public FinancialTransactionType LegacyType { get; set; } = FinancialTransactionType.Receipt;
        public TransactionDirection Direction { get; set; }
        public PaymentMethod Method { get; set; }
        public decimal Amount { get; set; }

        [Column("Date")]
        public DateTime TransactionDate { get; set; }

        public int? InvoiceId { get; set; }
        public int? VoucherId { get; set; }
        public FinancialSourceType SourceType { get; set; }
        public int? SourceId { get; set; }
        public string? ReferenceNumber { get; set; }
        public int? CashierSessionId { get; set; }
        public CashierSession? CashierSession { get; set; }

        [Column("CasherId")]
        public int? CashierId { get; set; }

        public User? Cashier { get; set; }
        public int? BranchId { get; set; }
        public int? WarehouseId { get; set; }
        public int? CurrencyId { get; set; }
        public decimal ExchangeRate { get; set; } = 1m;
        public string? Notes { get; set; }
        public FinancialTransactionStatus Status { get; set; }
        public AccountingPostingStatus PostingStatus { get; set; } = AccountingPostingStatus.NotPosted;
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
