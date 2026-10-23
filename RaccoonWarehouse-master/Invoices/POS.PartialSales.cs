using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.ProductUnits;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Globalization;
using System.Windows;

namespace RaccoonWarehouse.Invoices;

public partial class POS
{
    private readonly Dictionary<int, int> _partialSaleRefreshVersions = new();
    private readonly HashSet<int> _pendingPartialSaleProducts = new();
    private bool _partialSaleSetupOpen;
    private bool _partialSaleWindowClosed;
    private sealed record PartialSaleResolution(bool Success, decimal Quantity);

    private void PartialSaleUnitsChanged(int productId, IReadOnlyList<ProductUnitReadDto>? units)
    {
        if (_partialSaleWindowClosed)
            return;
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(() => PartialSaleUnitsChanged(productId, units)));
            return;
        }
        var version = _partialSaleRefreshVersions.GetValueOrDefault(productId) + 1;
        _partialSaleRefreshVersions[productId] = version;
        if (units != null)
        {
            ApplyPartialSaleCatalogUnits(productId, units);
            return;
        }

        // Refresh after a product edit, outside quantity entry and on an
        // independent DbContext. Until it completes, stale permissions are blocked.
        _pendingPartialSaleProducts.Add(productId);
        ProductSearchWindow.InvalidateCachedProductUnits(productId);
        _hydratedProducts.Remove(productId);
        _hydratedProductUnits.Remove(productId);
        _ = RefreshPartialSaleCatalogUnitsAsync(productId, version);
    }

    private async Task RefreshPartialSaleCatalogUnitsAsync(int productId, int version)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IProductService>();
            var result = await service.GetByIdWithUnitsAsync(productId);
            if (_partialSaleWindowClosed || _partialSaleRefreshVersions.GetValueOrDefault(productId) != version)
                return;
            if (result.Success && result.Data?.ProductUnits != null)
                ApplyPartialSaleCatalogUnits(productId, result.Data.ProductUnits.ToList());
            else
                ProductSearchWindow.InvalidateCatalogCache();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            if (!_partialSaleWindowClosed && _partialSaleRefreshVersions.GetValueOrDefault(productId) == version)
                ProductSearchWindow.InvalidateCatalogCache();
            // Keep this entry pending so a failed refresh cannot enable a sale
            // using an obsolete AllowFractionalSale flag.
        }
    }

    private void ApplyPartialSaleCatalogUnits(int productId, IReadOnlyList<ProductUnitReadDto> units)
    {
        ProductSearchWindow.UpdateCachedProductUnits(productId, units);
        _hydratedProductUnits[productId] = units.ToList();
        if (_hydratedProducts.TryGetValue(productId, out var product))
            product.ProductUnits = units.ToList();
        foreach (var row in _invoiceLines.Where(row => row.ProductId == productId))
        {
            if (row.SelectedProduct != null)
                row.SelectedProduct.ProductUnits = units.ToList();
        }
        foreach (var item in Products.Where(item => item.Id == productId))
            item.ProductUnits = units.ToList();
        _pendingPartialSaleProducts.Remove(productId);
    }

    private void ShowPartialSaleCatalogNotReady()
        => MessageBox.Show(this,
            UiText.T("إعدادات وحدات الصنف قيد التحديث أو غير متاحة. أعد فتح بحث الأصناف ثم حاول مجدداً.",
                "Product unit settings are updating or unavailable. Reopen product search and try again."),
            UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);

    private async Task<PartialSaleResolution> ResolvePartialSaleAsync(InvoiceLineWriteDto line, decimal input)
    {
        if (input <= 0m)
            return new(false, input);
        if (input == decimal.Truncate(input))
            return new(true, input);
        if (_partialSaleSetupOpen)
            return new(false, input);

        // The full catalog is indexed once when loaded. No product, unit or
        // stock query is made here to decide whether the shortcut is allowed.
        if (!ProductSearchWindow.TryGetCachedProductUnits(line.ProductId, out var units))
        {
            ShowPartialSaleCatalogNotReady();
            return new(false, input);
        }
        _pendingPartialSaleProducts.Remove(line.ProductId);
        var source = units.FirstOrDefault(unit => unit.Id == line.ProductUnitId);
        if (source == null)
        {
            ShowPartialSaleCatalogNotReady();
            return new(false, input);
        }
        if (!source.AllowFractionalSale || ProductUnitService.IsTapeUnitName(source.Unit?.Name))
        {
            MessageBox.Show(this,
                UiText.T("البيع الجزئي غير مسموح لهذه الوحدة. فعّل خيار بيع جزئي من بطاقة الصنف.",
                    "Partial sale is not allowed for this unit. Enable Fractional sale in the product editor."),
                UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return new(false, input);
        }
        if (source.QuantityPerUnit <= 0m)
        {
            ShowPartialSaleCatalogNotReady();
            return new(false, input);
        }
        var digit = (input - decimal.Truncate(input)) * 10m;
        if (digit != decimal.Truncate(digit))
        {
            ShowInvalidPartialSaleQuantity();
            return new(false, input);
        }

        var legacyTape = units.FirstOrDefault(unit => ProductUnitService.IsTapeUnitName(unit.Unit?.Name));
        var hasValidParts = source.FractionalPartsPerUnit >= 2m &&
                            source.FractionalPartsPerUnit == decimal.Truncate(source.FractionalPartsPerUnit);
        var fractionalSalePrice = source.FractionalSalePrice
            ?? (hasValidParts ? Math.Round(source.SalePrice / source.FractionalPartsPerUnit, 3, MidpointRounding.AwayFromZero) : legacyTape?.SalePrice);
        var fractionalSaleCost = source.FractionalSaleCost
            ?? (hasValidParts ? Math.Round(source.PurchasePrice / source.FractionalPartsPerUnit, 3, MidpointRounding.AwayFromZero) : legacyTape?.PurchasePrice);
        if (fractionalSalePrice == null || fractionalSaleCost == null)
        {
            var invoiceBeforeSetup = _currentInvoice;
            var resetVersionBeforeSetup = _posResetVersion;
            _partialSaleSetupOpen = true;
            try
            {
                var dialog = new FractionalSaleConfigurationWindow(line.ProductName ?? line.SelectedProduct?.Name ?? "",
                    source.Unit?.Name ?? line.UnitNameSnapshot ?? "", source.SalePrice, source.PurchasePrice,
                    source.FractionalPartsPerUnit)
                { Owner = this };
                if (dialog.ShowDialog() != true)
                    return new(false, input);

                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IProductUnitService>();
                RaccoonWarehouse.Core.Common.Result<List<ProductUnitReadDto>> result;
                var wasEnabled = IsEnabled;
                try
                {
                    IsEnabled = false;
                    _loading.Show();
                    result = await service.ConfigureFractionalSaleAsync(
                        source.Id, dialog.PartsPerUnit, dialog.FractionalSalePrice, dialog.FractionalSaleCost);
                }
                finally
                {
                    _loading.Hide();
                    if (!_partialSaleWindowClosed)
                        IsEnabled = wasEnabled;
                }
                if (_partialSaleWindowClosed || _posResetVersion != resetVersionBeforeSetup ||
                    !ReferenceEquals(_currentInvoice, invoiceBeforeSetup))
                    return new(false, input);
                if (!result.Success || result.Data == null)
                {
                    MessageBox.Show(this, result.Message ?? UiText.T("تعذر حفظ إعدادات الشريط.", "Could not save tape settings."),
                        UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return new(false, input);
                }
                ApplyPartialSaleCatalogUnits(line.ProductId, result.Data);
                units = result.Data;
                source = units.First(unit => unit.Id == source.Id);
                fractionalSalePrice = source.FractionalSalePrice;
                fractionalSaleCost = source.FractionalSaleCost;
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception);
                MessageBox.Show(this, UiText.T("تعذر حفظ إعدادات الشريط.", "Could not save tape settings."),
                    UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return new(false, input);
            }
            finally
            {
                _partialSaleSetupOpen = false;
            }
        }

        if (fractionalSalePrice == null || fractionalSaleCost == null ||
            fractionalSalePrice <= 0m || fractionalSaleCost < 0m ||
            source.FractionalPartsPerUnit < 2m ||
            source.FractionalPartsPerUnit != decimal.Truncate(source.FractionalPartsPerUnit))
        {
            ShowPartialSaleCatalogNotReady();
            return new(false, input);
        }
        var parts = source.FractionalPartsPerUnit;
        if (parts < 2m || !ProductUnitService.TryConvertPartialQuantity(input, parts, out var quantity))
        {
            ShowInvalidPartialSaleQuantity();
            return new(false, input);
        }

        // Keep the main unit on the invoice row. The decimal input is a shortcut
        // for a partial quantity, while the conversion metadata is used to
        // calculate base quantity, price, and cost. This keeps the POS editor
        // showing 0.1 / 0.2 instead of changing the row to Tape quantity 1 / 2.
        var oldPercentage = line.LineDiscountIsPercentage ? line.LineDiscountPercent : 0m;
        var product = line.SelectedProduct ?? new ProductReadDto { Id = line.ProductId, Name = line.ProductName };
        product.ProductUnits = units.ToList();
        line.SelectedProduct = product;
        line.ProductUnitId = source.Id;
        line.ProductUnit = MapProductUnit(source);
        line.UnitNameSnapshot = source.Unit?.Name ?? line.UnitNameSnapshot;
        line.QuantityPerUnitSnapshot = source.QuantityPerUnit;
        line.Quantity = input;
        line.BaseQuantity = input * source.QuantityPerUnit;
        // Tape.SalePrice and Tape.PurchasePrice are the saved prices for one
        // tape. Keep the row quantity in the main-unit decimal shortcut, but
        // derive the equivalent row price from the actual converted tape
        // quantity so the line total always uses the saved Tape price.
        var partialSalePrice = fractionalSalePrice.Value * quantity;
        var partialSaleCost = fractionalSaleCost.Value * quantity;
        line.UnitPrice = partialSalePrice / input;
        line.UnitCost = partialSaleCost / input;
        line.PartialSaleResolved = true;
        if (line.LineDiscountIsPercentage && !_invoiceDiscountRows.ContainsKey(line))
            line.LineDiscountAmount = Math.Round(input * line.UnitPrice * oldPercentage / 100m,
                3, MidpointRounding.AwayFromZero);
        _hydratedProducts[line.ProductId] = product;
        _hydratedProductUnits[line.ProductId] = units.ToList();
        RecalculateLineFromCurrentValues(line);
        return new(true, input);
    }

    private void ShowInvalidPartialSaleQuantity()
        => MessageBox.Show(this,
            UiText.T("أدخل 0.1 لشريط واحد أو 0.2 لشريطين. للكميات الأكبر اختر وحدة الشريط وأدخل عدداً صحيحاً.",
                "Enter 0.1 for one tape or 0.2 for two tapes. For larger quantities, select Tape and enter a whole number."),
            UiText.T("كمية غير صالحة", "Invalid quantity"), MessageBoxButton.OK, MessageBoxImage.Warning);
}
