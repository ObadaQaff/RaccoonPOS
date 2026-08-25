using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.StockDocuments;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaccoonWarehouse.Domain.StockItems
{
    public class StockItem : BaseEntity
    {
        [Column("StockId")]
        public int StockDocumentId { get; set; }
        public StockDocument? StockDocument { get; set; }

        [NotMapped]
        public int StockId
        {
            get => StockDocumentId;
            set => StockDocumentId = value;
        }

        [NotMapped]
        public StockDocument? Stock
        {
            get => StockDocument;
            set => StockDocument = value;
        }

        public int LineNumber { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public int ProductUnitId { get; set; }
        public ProductUnit? ProductUnit { get; set; }
        public int? StockLotId { get; set; }
        public decimal Quantity { get; set; }
        public decimal QuantityPerUnitSnapshot { get; set; } = 1m;
        public decimal BaseQuantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal LineDiscountAmount { get; set; }
        public decimal FreeQuantity { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
