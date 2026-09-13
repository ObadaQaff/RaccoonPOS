using System.Linq.Expressions;
using System.Windows.Controls;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Domain.Units.DTOs;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Invoices;

public partial class POS
{
    private void RecalculateLineFromCurrentValues(InvoiceLineWriteDto line)
    {
        if (line.Quantity == 0)
            return;

        var lineTotal = line.Quantity * line.UnitPrice;
        var costTotal = line.Quantity * line.UnitCost;
        var product = FindProductForLine(line);
        var taxExempt = product?.TaxExempt ?? line.TaxExempt;
        var taxRate = taxExempt ? 0m : Math.Max(0m, product?.TaxRate ?? line.TaxRate);
        var divisor = 1m + taxRate / 100m;
        var lineSubTotal = taxExempt || divisor <= 0m
            ? lineTotal
            : Math.Round(lineTotal / divisor, 3);

        line.TaxExempt = taxExempt;
        line.TaxRate = taxRate;
        line.LineSubTotal = lineSubTotal;
        line.TaxAmount = Math.Round(lineTotal - lineSubTotal, 3);
        line.ProfitBeforeTax = lineSubTotal - costTotal;
        line.Profit = line.ProfitBeforeTax;
        line.BaseQuantity = line.Quantity * (line.QuantityPerUnitSnapshot > 0 ? line.QuantityPerUnitSnapshot : 1m);
    }

    private ProductReadDto? FindProductForLine(InvoiceLineWriteDto line)
    {
        if (line.SelectedProduct != null)
            return line.SelectedProduct;

        return Products.FirstOrDefault(p => p.Id == line.ProductId);
    }

    private static InvoiceLineWriteDto CloneLineSnapshot(InvoiceLineWriteDto source, decimal quantity, string? originalInvoiceId = null)
    {
        var divisor = source.Quantity == 0 ? 1 : Math.Abs(source.Quantity);

        return new InvoiceLineWriteDto
        {
            ProductId = source.ProductId,
            ProductName = source.ProductName,
            ProductUnitId = source.ProductUnitId,
            QuantityPerUnitSnapshot = source.QuantityPerUnitSnapshot,
            BaseQuantity = (source.BaseQuantity / divisor) * quantity,
            UnitPrice = source.UnitPrice,
            UnitCost = source.UnitCost,
            AvailableQuantitySnapshot = source.AvailableQuantitySnapshot,
            UnitNameSnapshot = source.UnitNameSnapshot,
            TaxExempt = source.TaxExempt,
            TaxRate = source.TaxRate,
            Quantity = quantity,
            LineSubTotal = (source.LineSubTotal / divisor) * quantity,
            TaxAmount = (source.TaxAmount / divisor) * quantity,
            ProfitBeforeTax = (source.ProfitBeforeTax / divisor) * quantity,
            Profit = (source.Profit / divisor) * quantity,
            OriginalInvoiceId = originalInvoiceId ?? source.OriginalInvoiceId
        };
    }

    private decimal GetDefaultSalePrice(InvoiceLineWriteDto line)
    {
        var product = FindProductForLine(line);
        var unit = product?.ProductUnits?.FirstOrDefault(u => u.Id == line.ProductUnitId)
                   ?? ProductUnitSelector.GetDefaultSaleUnit(product?.ProductUnits);

        if (unit != null)
            return unit.SalePrice;

        return _stockLookup.TryGetValue((line.ProductId, line.ProductUnitId), out var stock)
            ? stock.SalePrice
            : line.UnitPrice;
    }

    private void ResetPriceBelowCost(InvoiceLineWriteDto line, decimal enteredPrice, TextBox? editor = null)
    {
        var defaultPrice = GetDefaultSalePrice(line);
        ShowPaymentValidationMessage(
            UiText.T(
                $"لا يمكن بيع الصنف {line.ProductName} بسعر أقل من التكلفة. السعر المدخل: {enteredPrice:0.00000}، التكلفة: {line.UnitCost:0.00000}. سيتم إعادة السعر الافتراضي: {defaultPrice:0.00000}.",
                $"Cannot sell {line.ProductName} below cost. Entered price: {enteredPrice:0.00000}, cost: {line.UnitCost:0.00000}. The default price will be restored: {defaultPrice:0.00000}."),
            UiText.T("تنبيه", "Notice"));

        line.UnitPrice = defaultPrice;
        if (editor != null)
            editor.Text = defaultPrice.ToString("0.00000");
    }

    private async Task<List<StockReadDto>> GetAvailableStocksForProductAsync(int productId)
    {
        var result = await _stockService.GetAllWithFilteringAndIncludeAsync(
            s => s.ProductId == productId && s.Quantity > 0,
            new Expression<Func<Stock, object>>[]
            {
                s => s.Product,
                s => s.Product.SubCategory,
                s => s.Product.Brand,
                s => s.Product.ProductUnits,
                s => s.ProductUnit,
                s => s.ProductUnit.Unit
            });

        var stocks = result?.Data?
            .Where(stock => stock.Product != null && stock.ProductUnit != null && stock.Quantity > 0)
            .ToList()
            ?? new List<StockReadDto>();

        CacheStocks(stocks);
        return stocks;
    }

    private StockReadDto? ResolvePreferredStock(IEnumerable<StockReadDto> stocks, int? preferredUnitId = null)
    {
        var stockList = stocks?.Where(stock => stock.Quantity > 0).ToList() ?? new List<StockReadDto>();
        if (stockList.Count == 0)
            return null;

        if (preferredUnitId.HasValue)
        {
            var preferred = stockList.FirstOrDefault(stock => stock.ProductUnitId == preferredUnitId.Value);
            if (preferred != null)
                return preferred;
        }

        var product = stockList.First().Product;
        var defaultUnitId = ProductUnitSelector.GetDefaultSaleUnit(product?.ProductUnits)?.Id;
        if (defaultUnitId.HasValue)
        {
            var defaultStock = stockList.FirstOrDefault(stock => stock.ProductUnitId == defaultUnitId.Value);
            if (defaultStock != null)
                return defaultStock;
        }

        return stockList
            .OrderByDescending(stock => stock.Quantity)
            .ThenBy(stock => stock.ProductUnit?.Unit?.Name)
            .FirstOrDefault();
    }

    private void CacheStocks(IEnumerable<StockReadDto> stocks)
    {
        var groupedStocks = stocks
            .Where(s => s != null && s.ProductId > 0 && s.ProductUnitId > 0)
            .GroupBy(s => s.ProductId);

        foreach (var group in groupedStocks)
        {
            foreach (var stock in group)
            {
                _stockLookup[(stock.ProductId, stock.ProductUnitId)] = stock;

                var productUnit = stock.Product?.ProductUnits?.FirstOrDefault(unit => unit.Id == stock.ProductUnitId);
                if (productUnit != null)
                    productUnit.PurchasePrice = stock.PurchasePrice;
            }

            var firstStock = ResolvePreferredStock(group);
            if (firstStock?.Product != null)
            {
                firstStock.Product.CurrentSalePrice = firstStock.Product.DefaultSalePrice;
                firstStock.Product.CurrentPurchasePrice = firstStock.PurchasePrice;
            }
        }
    }

    private static bool HasHydratedUnits(ProductReadDto? product)
    {
        return product?.ProductUnits?.Any(unit => unit.Unit != null && !string.IsNullOrWhiteSpace(unit.Unit.Name)) == true;
    }

    private ProductReadDto? ResolveProductForUnits(int productId)
    {
        var product = Products.FirstOrDefault(p => p.Id == productId);
        if (HasHydratedUnits(product))
            return product;

        return null;
    }

    private async Task<ProductReadDto?> ResolveProductForUnitsAsync(int productId)
    {
        if (_hydratedProducts.TryGetValue(productId, out var hydratedProduct))
            return hydratedProduct;

        var cachedProduct = ResolveProductForUnits(productId);
        if (cachedProduct != null)
        {
            _hydratedProducts[productId] = cachedProduct;
            return cachedProduct;
        }

        if (productId <= 0)
            return null;

        try
        {
            var result = await _productService.GetByIdWithUnitsAsync(productId);
            var product = result?.Data;
            if (product == null)
                return null;

            _hydratedProducts[productId] = product;
            if (product.ProductUnits != null)
                _hydratedProductUnits[productId] = product.ProductUnits.ToList();
            return product;
        }
        catch
        {
            return null;
        }
    }

    private async Task<ProductReadDto?> ResolveProductWithUnitsAsync(ProductReadDto product)
    {
        if (product.Id <= 0)
            return product;

        return await ResolveProductForUnitsAsync(product.Id) ?? product;
    }

    private async Task<List<ProductUnitWriteDto>> GetAvailableUnitsForProductAsync(int productId)
    {
        var product = await ResolveProductForUnitsAsync(productId);
        return product?.ProductUnits?
            .Select(MapProductUnit)
            .OrderByDescending(unit => unit.IsDefaultSaleUnit)
            .ThenBy(unit => unit.DisplayName)
            .ToList()
            ?? new List<ProductUnitWriteDto>();
    }

    private static ProductUnitWriteDto MapAvailableUnit(StockReadDto stock)
    {
        return new ProductUnitWriteDto
        {
            Id = stock.ProductUnitId,
            ProductId = stock.ProductId,
            UnitId = stock.ProductUnit?.UnitId ?? 0,
            Unit = stock.ProductUnit?.Unit == null
                ? null
                : new UnitWriteDto
                {
                    Id = stock.ProductUnit.Unit.Id,
                    Name = stock.ProductUnit.Unit.Name,
                    CreatedDate = stock.ProductUnit.Unit.CreatedDate,
                    UpdatedDate = stock.ProductUnit.Unit.UpdatedDate
                },
            QuantityPerUnit = stock.ProductUnit?.QuantityPerUnit ?? 1m,
            PurchasePrice = stock.PurchasePrice,
            SalePrice = stock.SalePrice,
            IsDefaultSaleUnit = stock.ProductUnit?.IsDefaultSaleUnit ?? false,
            IsDefaultPurchaseUnit = stock.ProductUnit?.IsDefaultPurchaseUnit ?? false
        };
    }

    private static ProductUnitWriteDto MapProductUnit(ProductUnitReadDto unit)
    {
        return new ProductUnitWriteDto
        {
            Id = unit.Id,
            ProductId = unit.ProductId,
            UnitId = unit.UnitId,
            Unit = unit.Unit == null
                ? null
                : new UnitWriteDto
                {
                    Id = unit.Unit.Id,
                    Name = unit.Unit.Name ?? string.Empty,
                    CreatedDate = unit.Unit.CreatedDate,
                    UpdatedDate = unit.Unit.UpdatedDate
                },
            QuantityPerUnit = unit.QuantityPerUnit,
            PurchasePrice = unit.PurchasePrice,
            SalePrice = unit.SalePrice,
            IsBaseUnit = unit.IsBaseUnit,
            IsDefaultSaleUnit = unit.IsDefaultSaleUnit,
            IsDefaultPurchaseUnit = unit.IsDefaultPurchaseUnit
        };
    }
}
