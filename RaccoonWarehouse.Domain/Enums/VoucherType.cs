using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Enums
{
    public enum VoucherType
    {
        Purchase = 1,
        Sales = 2,
        ReturnPurchase = 3,
        ReturnSales = 4,
        Adjustment = 5, 
        Receipt = 7, 
        Payment = 8
    }
}
