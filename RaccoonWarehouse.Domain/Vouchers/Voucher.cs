using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Cashiers;
using RaccoonWarehouse.Domain.Checks;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Vouchers
{
    public class Voucher : BaseEntity
    {
        public string? VoucherNumber { get; set; }
        public VoucherType VoucherType { get; set; }
        public decimal Amount { get; set; }
        public PaymentType PaymentType { get; set; } 
        public int? CasherId { get; set; }
        public int? SupplierId { get; set; }
        public int? CustomerId { get; set; }
        public string? Notes { get; set; }
        public CashierSession? CashierSession { get; set; }
        public int? CashierSessionId { get; set; }
        public ICollection<Check>? Checks { get; set; } = new List<Check>();
        public User? User { get; set; }

    }
}
