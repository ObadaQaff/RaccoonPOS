using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Cashiers;
using RaccoonWarehouse.Domain.Checks;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users;

namespace RaccoonWarehouse.Domain.Vouchers
{
    public class Voucher : BaseEntity
    {
        public string? VoucherNumber { get; set; }
        public DateTime? VoucherDate { get; set; }
        public VoucherType VoucherType { get; set; }
        public decimal Amount { get; set; }
        public PaymentType PaymentType { get; set; }
        public int? CasherId { get; set; }
        public int? SupplierId { get; set; }
        public int? CustomerId { get; set; }
        public int? BranchId { get; set; }
        public int? WarehouseId { get; set; }
        public int? CurrencyId { get; set; }
        public decimal ExchangeRate { get; set; } = 1m;
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
        public CashierSession? CashierSession { get; set; }
        public int? CashierSessionId { get; set; }
        public ICollection<Check>? Checks { get; set; } = new List<Check>();
        public User? User { get; set; }
        public AccountingPostingStatus PostingStatus { get; set; } = AccountingPostingStatus.NotPosted;
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
