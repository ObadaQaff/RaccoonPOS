using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Helpers;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Products;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RaccoonWarehouse.Stocks
{
    public partial class CurrentStock : Window
    {
        private const int SearchDelayMs = 2000;

        private readonly IStockReportService _stockReportService;
        private readonly ILoadingService _loadingService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IUserService _userService;
        private readonly SemaphoreSlim _stockLoadLock = new(1, 1);

        private int _searchVersion;

        public ObservableCollection<CurrentStockDto> CurrentStockItems { get; set; }
            = new ObservableCollection<CurrentStockDto>();
        public ObservableCollection<CurrentStockDto> FilteredStockItems { get; set; }
            = new ObservableCollection<CurrentStockDto>();

        public CurrentStock(
            IStockReportService stockReportService,
            ILoadingService loadingService,
            IServiceProvider serviceProvider,
            IUserService userService)
        {
            _stockReportService = stockReportService;
            _loadingService = loadingService;
            _serviceProvider = serviceProvider;
            _userService = userService;
            RaccoonWarehouse.Security.PermissionWindowGuard.Demand(GetType());
            InitializeComponent();
            SupplierComboBoxSearch.Attach(SupplierComboBox);
            DataContext = this;
            UiText.ApplyWindow(this);

            Loaded += CurrentStock_Loaded;
        }

        private async void CurrentStock_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSupplierFilterAsync();
            await LoadStockAsync(GetSearchText(), _searchVersion);
        }

        private async Task LoadSupplierFilterAsync()
        {
            var suppliers = (await _userService.GetAllAsync()).Data?
                .Where(x => x.Role == UserRole.Supplier).OrderBy(x => x.Name).ToList() ?? new();
            suppliers.Insert(0, new RaccoonWarehouse.Domain.Users.DTOs.UserReadDto { Id = 0, Name = UiText.T("الكل", "All suppliers") });
            SupplierComboBoxSearch.SetItems(SupplierComboBox, suppliers);
            SupplierComboBox.SelectedIndex = 0;
        }

        private async Task LoadStockAsync(string? searchText, int requestVersion)
        {
            var normalizedSearch = string.IsNullOrWhiteSpace(searchText)
                ? null
                : searchText.Trim();

            await _stockLoadLock.WaitAsync();
            try
            {
                if (requestVersion != _searchVersion)
                    return;

                var supplierId = SupplierComboBox.SelectedValue is int selectedSupplierId && selectedSupplierId > 0 ? selectedSupplierId : (int?)null;
                var data = await _stockReportService.GetCurrentStockAsync(normalizedSearch, AsOfDatePicker.SelectedDate?.Date, supplierId);
                if (requestVersion != _searchVersion)
                    return;

                CurrentStockItems.Clear();

                foreach (var item in data)
                    CurrentStockItems.Add(item);

                ApplyQuantityFilter();
            }
            catch (Exception ex)
            {
                if (requestVersion != _searchVersion)
                    return;

                MessageBox.Show(
                    $"{UiText.T("خطأ أثناء تحميل المخزون", "Error loading stock")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _stockLoadLock.Release();
            }
        }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            var searchVersion = Interlocked.Increment(ref _searchVersion);
            await LoadStockAsync(GetSearchText(), searchVersion);
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void AdjustBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = _serviceProvider.GetRequiredService<StockAdjustmentWindow>();
            if (StockGrid.SelectedItem is CurrentStockDto selected)
                window.InitialProductId = selected.ProductId;

            window.Owner = this;
            window.ShowDialog();
            if (window.SavedSuccessfully)
            {
                var searchVersion = Interlocked.Increment(ref _searchVersion);
                await LoadStockAsync(GetSearchText(), searchVersion);
            }
        }

        private async void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            var searchVersion = Interlocked.Increment(ref _searchVersion);
            await LoadStockAsync(GetSearchText(), searchVersion);
        }

        private void SearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchVersion = Interlocked.Increment(ref _searchVersion);
            _ = DebouncedLoadStockAsync(GetSearchText(), searchVersion);
        }

        private async Task DebouncedLoadStockAsync(string? searchText, int searchVersion)
        {
            try
            {
                await Task.Delay(SearchDelayMs);
                if (searchVersion != _searchVersion)
                    return;

                await LoadStockAsync(searchText, searchVersion);
            }
            catch (Exception ex)
            {
                if (searchVersion != _searchVersion)
                    return;

                MessageBox.Show(
                    $"{UiText.T("خطأ أثناء تحميل المخزون", "Error loading stock")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void StockGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (StockGrid.SelectedItem is CurrentStockDto selected && selected.ProductId > 0)
                WindowManager.ShowDialog<UpdateProduct>(WindowSizeType.MediumRectangle, window => window.Initialize(selected.ProductId));
        }
        private void CopyBarcode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { DataContext: CurrentStockDto item })
                return;

            if (string.IsNullOrWhiteSpace(item.ITEMCODE))
            {
                MessageBox.Show(
                    UiText.T("لا يوجد باركود لنسخه.", "There is no barcode to copy."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            Clipboard.SetText(item.ITEMCODE);
        }

        private void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel File (*.xlsx)|*.xlsx",
                    FileName = "InventoryReport.xlsx"
                };

                if (dlg.ShowDialog() != true)
                    return;

                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var ws = workbook.Worksheets.Add(UiText.T("المخزون الحالي", "Inventory Report"));

                ws.Cell(1, 1).Value = UiText.T("الباركود", "Barcode");
                ws.Cell(1, 2).Value = UiText.T("المنتج", "Product");
                ws.Cell(1, 3).Value = UiText.T("الوحدة", "Unit");
                ws.Cell(1, 4).Value = UiText.T("الكمية", "Quantity");
                ws.Cell(1, 5).Value = UiText.T("تكلفة المخزون", "Inventory Cost");
                ws.Cell(1, 6).Value = UiText.T("سعر البيع الحالي", "Current Sale Price");
                ws.Cell(1, 7).Value = UiText.T("أقرب انتهاء", "Nearest Expiry");
                ws.Cell(1, 8).Value = UiText.T("الحد الأدنى", "Minimum Quantity");
                ws.Cell(1, 9).Value = UiText.T("تنبيه", "Alert");

                ws.Row(1).Style.Font.Bold = true;
                ws.Row(1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

                int row = 2;
                foreach (var item in FilteredStockItems)
                {
                    ws.Cell(row, 1).Value = item.ITEMCODE;
                    ws.Cell(row, 2).Value = item.ProductName;
                    ws.Cell(row, 3).Value = item.UnitName;
                    ws.Cell(row, 4).Value = item.Quantity;
                    ws.Cell(row, 5).Value = item.PurchasePrice;
                    ws.Cell(row, 6).Value = item.SalePrice;
                    ws.Cell(row, 7).Value = item.NearestExpiryDate?.ToString("yyyy-MM-dd") ?? "-";
                    ws.Cell(row, 8).Value = item.MinimumQuantity;
                    ws.Cell(row, 9).Value = item.IsLowStock ? UiText.T("⚠ قليل", "⚠ Low") : "";
                    row++;
                }

                ws.Columns().AdjustToContents();
                workbook.SaveAs(dlg.FileName);

                MessageBox.Show(
                    UiText.T("تم استخراج ملف Excel بنجاح!", "Excel file exported successfully!"),
                    UiText.T("نجاح", "Success"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("حدث خطأ أثناء التصدير", "An error occurred during export")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void QuantityFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsInitialized)
                ApplyQuantityFilter();
        }


        private async void AsOfDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsInitialized)
                return;

            var searchVersion = Interlocked.Increment(ref _searchVersion);
            await LoadStockAsync(GetSearchText(), searchVersion);
        }

        private void ApplyQuantityFilter()
        {
            var filter = (QuantityFilterComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "all";
            var filtered = filter switch
            {
                "greater" => CurrentStockItems.Where(item => item.Quantity > 0),
                "equal" => CurrentStockItems.Where(item => item.Quantity == 0),
                "less" => CurrentStockItems.Where(item => item.Quantity < 0),
                _ => CurrentStockItems
            };

            FilteredStockItems.Clear();
            foreach (var item in filtered)
                FilteredStockItems.Add(item);
            DisplayedQuantityTotalTextBlock.Text = FilteredStockItems.Sum(item => item.Quantity).ToString("N3");
        }
        private string? GetSearchText()
        {
            var searchText = SearchText.Text?.Trim();
            return string.IsNullOrWhiteSpace(searchText) ? null : searchText;
        }
    }
}
