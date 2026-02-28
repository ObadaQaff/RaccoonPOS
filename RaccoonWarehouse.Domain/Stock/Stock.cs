using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.StockItems;
using RaccoonWarehouse.Domain.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Stock
{
    public class Stock: BaseEntity
    {
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public int ProductUnitId { get; set; }
        public ProductUnit? ProductUnit { get; set; }
        public decimal Quantity { get; set; } 
    }
}

