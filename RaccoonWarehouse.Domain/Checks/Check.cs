using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Vouchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Checks
{
    public class Check : BaseEntity
    {
        public string CheckNumber { get; set; }
        public string BankName { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
        public int? VoucherId { get; set; }
        public Voucher? Voucher { get; set; }
        public int? InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }
    }

}
