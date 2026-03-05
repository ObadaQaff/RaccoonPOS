using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.POS.VM;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.Units;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.ProductUnits
{
    public class ProductUnit: BaseEntity
    {
        public decimal SalePrice { get; set; }
        public decimal UnTaxedPrice { get; set; } 
        public decimal PurchasePrice { get; set; } //cost 
        public decimal QuantityPerUnit { get; set; }
        public bool IsBaseUnit { get; set; }
        public bool IsDefaultSaleUnit { get; set; }
        public bool IsDefaultPurchaseUnit { get; set; }
        public Product? Product { get; set; }
        public int ProductId { get; set; }
        public Unit? Unit { get; set; }
        public int UnitId { get; set; }
       // public bool IsMain { get; set; }=false;

    }
}
