using RaccoonWarehouse.Domain.POS.DTOs;
using RaccoonWarehouse.Domain.Products.DTOs;

namespace RaccoonWarehouse.Invoices;

internal static class PosProductCatalogMapper
{
    public static ProductReadDto MapBrowseItemToProduct(PosBrowseItemDto item)
    {
        return new ProductReadDto
        {
            Id = item.ProductId,
            Name = item.Name,
            ITEMCODE = item.ItemCode,
            SubCategoryId = item.SubCategoryId,
            TaxExempt = item.TaxExempt,
            TaxRate = item.TaxRate,
            CurrentStockQuantity = item.AvailableQuantity,
            CurrentSalePrice = item.CurrentSalePrice,
            LastCostIncludingTax = item.LastCostIncludingTax,
            AverageCostIncludingTax = item.AverageCostIncludingTax
        };
    }
}
