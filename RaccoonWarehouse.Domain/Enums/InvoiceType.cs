using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Enums
{
    public enum InvoiceType
    { 
        Sale,
        Return,
        Purchase,
        PurchaseReturn,
        Letter,
        Exchange
    }
}
