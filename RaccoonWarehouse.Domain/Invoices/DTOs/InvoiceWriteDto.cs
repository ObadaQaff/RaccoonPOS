using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Cashiers;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Checks;
using RaccoonWarehouse.Domain.Checks.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Invoices.DTOs
{
    public class InvoiceWriteDto : IBaseDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public string? OriginalInvoiceId { get; set; }
        public InvoiceType InvoiceType { get; set; }
        public PaymentType? PaymentType { get; set; }
        public int? CasherId { get; set; }
        public int? SupplierId { get; set; }
        public int? CustomerId { get; set; }
        public UserWriteDto? User { get; set; }
        public int? VoucherId { get; set; }
        public VoucherWriteDto? Voucher { get; set; }
        public decimal TotalAmount { get; set; }
        public ICollection<InvoiceLineWriteDto>? InvoiceLines { get; set; } = new List<InvoiceLineWriteDto>();
        public ICollection<CheckWriteDto>? Checks { get; set; } = new List<CheckWriteDto>();
        public CashierSessionWriteDto? CashierSession { get; set; }
        public int? CashierSessionId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
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
        public decimal NetSales { get; set; }

    }
}
