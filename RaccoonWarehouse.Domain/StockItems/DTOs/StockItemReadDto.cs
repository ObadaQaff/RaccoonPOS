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

        public int StockDocumentId { get; set; }
        public StockDocumentReadDto? StockDocument { get; set; }

        public int StockId
        {
            get => StockDocumentId;
            set => StockDocumentId = value;
        }

        public StockDocumentReadDto? Stock
        {
            get => StockDocument;
            set => StockDocument = value;
        }

        public int ProductId { get; set; }
        public ProductReadDto? Product { get; set; }

        public int ProductUnitId { get; set; }
        public ProductUnitReadDto? ProductUnit { get; set; }
        public decimal Quantity { get; set; }
        public decimal QuantityPerUnitSnapshot { get; set; } = 1m;
        public decimal BaseQuantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public DateTime? ExpiryDate { get; set; }


        public virtual DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
