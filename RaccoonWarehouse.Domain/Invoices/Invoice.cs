using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Cashiers;
using RaccoonWarehouse.Domain.Checks;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Vouchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Invoices
{
    public class Invoice : BaseEntity
    {
          public string InvoiceNumber { get; set; }
          public string? OriginalInvoiceId {get; set;}
          public InvoiceType InvoiceType { get; set; }
          public PaymentType? PaymentType { get; set; }
          public int? CasherId { get; set; }
          public int? SupplierId { get; set; }
          public int? CustomerId { get; set; }
          public User? User { get; set; }
          public int? VoucherId { get; set; }
          public Voucher? Voucher { get; set; }
          public decimal TotalAmount { get; set; }
          public ICollection<InvoiceLine>? InvoiceLines { get; set; } = new List<InvoiceLine>();
          public ICollection<Check>? Checks { get; set; } = new List<Check>();
        public CashierSession? CashierSession { get; set; }
        public int? CashierSessionId { get; set; }

        // POS addons
        public InvoiceStatus? Status { get; set; }
          public bool? IsPOS { get; set; }
          public DateTime? OpenedAt { get; set; }
          public DateTime? ClosedAt { get; set; }
          public decimal? DiscountAmount { get; set; }
            public decimal SubTotal { get; set; }          // مجموع السطور قبل الخصم/الضريبة
            public decimal TotalTax { get; set; }          // إجمالي الضريبة
        public decimal? NetTotal => SubTotal - DiscountAmount + TotalTax; // optional (computed)
        public decimal TotalCOGS { get; set; }          // مجموع تكلفة السطور
        public decimal GrossProfit { get; set; }        // (SubTotal - Discount) - TotalCOGS
        public decimal NetSales { get; set; }           // (SubTotal - Discount)  (قبل الضريبة)


    }
}