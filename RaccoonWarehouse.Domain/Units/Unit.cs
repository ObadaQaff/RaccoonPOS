using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.ProductUnits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Units
{
    public class Unit : BaseEntity
    {
        public string Name { get; set; }
        public ICollection<ProductUnit>? ProductUnits { get; set; } = new List<ProductUnit>();

    }
}
