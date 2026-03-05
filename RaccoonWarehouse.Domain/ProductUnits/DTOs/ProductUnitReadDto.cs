using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.Units;
using RaccoonWarehouse.Domain.Units.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.ProductUnits.DTOs
{
    public class ProductUnitReadDto:IBaseDto
    {
        public int Id { get; set; }
        public decimal SalePrice { get; set; }
        public decimal UnTaxedPrice { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal QuantityPerUnit { get; set; }
        public ProductReadDto? Product { get; set; }
        public int ProductId { get; set; }
        public UnitReadDto? Unit { get; set; }
        public int UnitId { get; set; }
        public bool IsBaseUnit { get; set; }
        public bool IsDefaultSaleUnit { get; set; }
        public bool IsDefaultPurchaseUnit { get; set; }
        public bool IsMain
        {
            get => IsBaseUnit;
            set => IsBaseUnit = value;
        }

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
