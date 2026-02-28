using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Domain.StockDocuments.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.StockItems.DTOs
{
    public class StockItemReadDto :IBaseDto
    {
        public int Id { get; set; }

        public int StockId { get; set; }
        public StockDocumentReadDto? Stock { get; set; }

        public int ProductId { get; set; }
        public ProductReadDto? Product { get; set; }

        public int ProductUnitId { get; set; }
        public ProductUnitReadDto? ProductUnit { get; set; }
        public decimal Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public DateTime? ExpiryDate { get; set; }


        public virtual DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
