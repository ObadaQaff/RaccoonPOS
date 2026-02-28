using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.StockDocuments.DTOs;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class StockItemWriteDto : IBaseDto
{
    public int Id { get; set; }

    public int StockId { get; set; }
    public StockDocumentWriteDto? Stock { get; set; }

    public int ProductId { get; set; }
    public ProductWriteDto? Product { get; set; }

    public int ProductUnitId { get; set; }
    public ProductUnitWriteDto? ProductUnit { get; set; }
    public decimal Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public ObservableCollection<ProductUnitWriteDto> Units { get; set; } = new();
    public string? ProductSearchText { get; set; } = string.Empty;
    public string ProductName { get; set; }
    public string UnitName { get; set; }

    public virtual DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}