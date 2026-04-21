using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.ProductUnits;

namespace RaccoonWarehouse.Domain.Stock
{
    public class Stock : BaseEntity
    {
        public int? WarehouseId { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public int ProductUnitId { get; set; }
        public ProductUnit? ProductUnit { get; set; }
        public decimal Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public DateTime? LastMovementDate { get; set; }
    }
}
