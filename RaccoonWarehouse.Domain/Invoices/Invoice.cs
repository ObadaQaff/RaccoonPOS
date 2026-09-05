using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Cashiers;
using RaccoonWarehouse.Domain.Checks;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Vouchers;
using System.ComponentModel.DataAnnotations.Schema;
using DelegateEntity = RaccoonWarehouse.Domain.Delegates.Delegate;

namespace RaccoonWarehouse.Domain.Invoices
{
    public class Invoice : BaseEntity
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public string? FalconInvoiceNumber { get; set; }
        public string? OriginalInvoiceId { get; set; }
        public InvoiceType InvoiceType { get; set; }
        public PaymentType? PaymentType { get; set; }
        public int? CasherId { get; set; }
        public int? SupplierId { get; set; }
        public int? CustomerId { get; set; }
        public User? User { get; set; }
        public int? DelegateId { get; set; }
        public DelegateEntity? Delegate { get; set; }
        public int? VoucherId { get; set; }
        public Voucher? Voucher { get; set; }
        public int? BranchId { get; set; }
        public int? WarehouseId { get; set; }
        public int? CurrencyId { get; set; }
        public decimal ExchangeRate { get; set; } = 1m;
        public DateTime? DocumentDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string? ReferenceNumber { get; set; }
        public AccountingPostingStatus PostingStatus { get; set; } = AccountingPostingStatus.NotPosted;
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
        public ICollection<InvoiceLine>? InvoiceLines { get; set; } = new List<InvoiceLine>();
        public ICollection<InvoicePayment>? Payments { get; set; } = new List<InvoicePayment>();
        public ICollection<Check>? Checks { get; set; } = new List<Check>();
        public CashierSession? CashierSession { get; set; }
        public int? CashierSessionId { get; set; }

        public InvoiceStatus? Status { get; set; }
        public bool? IsPOS { get; set; }
        public DateTime? OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string? HeldColor { get; set; }
        public decimal? DiscountAmount { get; set; }
        [NotMapped]
        public bool CostPriceIncludesTax { get; set; } = true;
        public decimal SubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal? NetTotal => SubTotal - DiscountAmount + TotalTax;
        public decimal TotalCOGS { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal NetSales { get; set; }
    }
}
