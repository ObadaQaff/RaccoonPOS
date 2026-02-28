using RaccoonWarehouse.Domain.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Taxs
{
    public class Tax : BaseEntity
    {
        public string Name { get; set; }
        public decimal Rate { get; set; }   
        public bool IsActive { get; set; }  // only ONE true
    }

}
