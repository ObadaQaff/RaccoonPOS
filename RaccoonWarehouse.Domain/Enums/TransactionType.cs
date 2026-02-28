using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Enums
{
    public enum TransactionType
    {
        Sale = 0,
        Return = 1,
        Purchase = 2,
        Adjustment = 3,
        Damage = 4
    }
}
