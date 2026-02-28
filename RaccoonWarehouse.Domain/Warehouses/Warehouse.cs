using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Warehouses
{
    public  class Warehouse:BaseEntity
    {
        public string Name { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public int? PhoneNumber { get; set; }
        public WarehouseStatus Status { get; set; }
}
}
