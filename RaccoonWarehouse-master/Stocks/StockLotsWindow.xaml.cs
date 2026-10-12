using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Stocks;

public partial class StockLotsWindow : Window
{
    private readonly IStockService _stockService;
    private readonly ILoadingService _loadingService;
    private List<StockBatchLookupDto> _allLots = new();

    private sealed record StatusOption(BatchStatus? Value, string Arabic, string English)
    {
        public override string ToString() => UiText.IsEnglish ? English : Arabic;
    }

    public StockLotsWindow(IStockService stockService, ILoadingService loadingService)
    {
        InitializeComponent();
        _stockService = stockService;
        _loadingService = loadingService;
        StatusComboBox.ItemsSource = new[]
        {
            new StatusOption(null, "الكل", "All"),
            new StatusOption(BatchStatus.Active, "نشطة", "Active"),
            new StatusOption(BatchStatus.Closed, "مغلقة", "Closed"),
            new StatusOption(BatchStatus.Replaced, "مستبدلة", "Replaced")
        };
        StatusComboBox.SelectedIndex = 0;
        ApplyTexts();
        Loaded += async (_, _) => await LoadLotsAsync();
    }

    private async Task LoadLotsAsync()
    {
        try
        {
            _loadingService.Show();
            var result = await _stockService.GetBatchLookupAsync();
            if (!result.Success || result.Data == null)
                throw new InvalidOperationException(result.Message ?? UiText.T("فشل تحميل الدفعات.", "Failed to load lots."));
            _allLots = result.Data;
            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _loadingService.Hide();
        }
    }

    private void ApplyFilters()
    {
        var search = SearchTextBox.Text?.Trim();
        var selectedStatus = StatusComboBox.SelectedValue as BatchStatus?;
        var rows = _allLots.Where(lot =>
            string.IsNullOrWhiteSpace(search)
            || lot.StockLotId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
            || (lot.ProductName ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase)
            || (lot.UnitName ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase));

        if (selectedStatus.HasValue)
            rows = rows.Where(lot => lot.Status == selectedStatus.Value);

        var result = rows.OrderBy(lot => lot.Status == BatchStatus.Active ? 0 : 1)
                         .ThenBy(lot => lot.ExpiryDate ?? DateTime.MaxValue)
                         .ThenBy(lot => lot.ProductName)
                         .ToList();
        LotsGrid.ItemsSource = result;
        CountText.Text = UiText.IsEnglish ? $"{result.Count} lot(s)" : $"عدد الدفعات: {result.Count}";
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadLotsAsync();
    private void FiltersChanged(object sender, RoutedEventArgs e) => ApplyFilters();
    private void LotsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DetailsButton.IsEnabled = LotsGrid.SelectedItem is StockBatchLookupDto;
        AdjustmentButton.IsEnabled = LotsGrid.SelectedItem is StockBatchLookupDto;
    }

    private void DetailsButton_Click(object sender, RoutedEventArgs e)
    {
        if (LotsGrid.SelectedItem is not StockBatchLookupDto lot)
            return;

        var details = UiText.IsEnglish
            ? $"Lot: {lot.StockLotId}\nProduct: {lot.ProductName}\nUnit: {lot.UnitName}\nStatus: {lot.Status}\nUsed: {lot.IsUsed}\nOriginal quantity: {lot.OriginalQuantity:N3}\nRemaining quantity: {lot.RemainingQuantity:N3}\nPurchase cost: {lot.PurchasePrice:N4}\nSale price: {lot.SalePrice:N4}\nExpiry: {lot.ExpiryDate:d}\nCreated: {lot.CreatedDate:g}"
            : $"الدفعة: {lot.StockLotId}\nالمنتج: {lot.ProductName}\nالوحدة: {lot.UnitName}\nالحالة: {lot.Status}\nمستخدمة: {lot.IsUsed}\nالكمية الأصلية: {lot.OriginalQuantity:N3}\nالمتبقي: {lot.RemainingQuantity:N3}\nتكلفة الشراء: {lot.PurchasePrice:N4}\nسعر البيع: {lot.SalePrice:N4}\nالانتهاء: {lot.ExpiryDate:d}\nتاريخ الإنشاء: {lot.CreatedDate:g}";
        MessageBox.Show(details, UiText.T("تفاصيل الدفعة", "Lot details"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void AdjustmentButton_Click(object sender, RoutedEventArgs e)
    {
        if (LotsGrid.SelectedItem is not StockBatchLookupDto lot)
            return;

        WindowManager.Show<StockAdjustmentWindow>(WindowSizeType.LargeRectangle, window =>
        {
            window.InitialProductId = lot.ProductId;
        });
    }

    private void ApplyTexts()
    {
        if (UiText.IsEnglish)
            return;
        TitleText.Text = "إدارة دفعات المخزون / Lot Management";
        DescriptionText.Text = "مراجعة الكميات والتكاليف وتواريخ الانتهاء وفتح تعديلات المخزون بأمان.";
        SearchLabel.Text = "بحث";
        StatusLabel.Text = "الحالة";
        RefreshButton.Content = "تحديث / Refresh";
        LotColumn.Header = "الدفعة";
        ProductColumn.Header = "المنتج";
        UnitColumn.Header = "الوحدة";
        OriginalColumn.Header = "الأصلية";
        RemainingColumn.Header = "المتبقية";
        PurchaseCostColumn.Header = "تكلفة الشراء";
        SalePriceColumn.Header = "سعر البيع";
        ExpiryColumn.Header = "الانتهاء";
        StatusColumn.Header = "الحالة";
        UsedColumn.Header = "مستخدمة";
        DetailsButton.Content = "التفاصيل / View details";
        AdjustmentButton.Content = "تعديل المخزون / Open adjustment";
    }
}
