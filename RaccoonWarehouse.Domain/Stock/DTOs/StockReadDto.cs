using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Stock.DTOs
{
    public class StockReadDto :IBaseDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public ProductReadDto? Product { get; set; }
        public int ProductUnitId { get; set; }
        public ProductUnit? ProductUnit { get; set; }
        public decimal Quantity { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
