using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.ProductUnits
{
    public sealed class ProductUnitBarcode : BaseEntity
    {
        public int ProductUnitId { get; set; }
        public ProductUnit? ProductUnit { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string NormalizedBarcode { get; set; } = string.Empty;
    }
}
