using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Enums
{
    public enum ProductStatus 
    {
        InStock = 1,
        OutOfStock = 2,
        Discontinued = 3,
        BackOrder = 4,
        unDiscontinued = 5,
        UnDisplayed = 6
    }
}
