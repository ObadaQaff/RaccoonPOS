using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Stocks
{
    public partial class CurrentStock : Window
    {
        private const int SearchDelayMs = 2000;

        private readonly IStockReportService _stockReportService;
        private readonly ILoadingService _loadingService;
        private readonly IServiceProvider _serviceProvider;
        private readonly SemaphoreSlim _stockLoadLock = new(1, 1);

        private int _searchVersion;

        public ObservableCollection<CurrentStockDto> CurrentStockItems { get; set; }
            = new ObservableCollection<CurrentStockDto>();

        public CurrentStock(
            IStockReportService stockReportService,
            ILoadingService loadingService,
            IServiceProvider serviceProvider)
        {
            _stockReportService = stockReportService;
            _loadingService = loadingService;
            _serviceProvider = serviceProvider;
            InitializeComponent();
            DataContext = this;
            UiText.ApplyWindow(this);

            Loaded += CurrentStock_Loaded;
        }

        private async void CurrentStock_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadStockAsync(GetSearchText(), _searchVersion);
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

                var data = await _stockReportService.GetCurrentStockAsync(normalizedSearch);
                if (requestVersion != _searchVersion)
                    return;

                CurrentStockItems.Clear();

                foreach (var item in data)
                    CurrentStockItems.Add(item);
            }
            catch (Exception ex)
            {
                if (requestVersion != _searchVersion)
                    return;

                MessageBox.Show(
                    $"{UiText.T("Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ ØªØ­Ù… „ Ø§Ù„Ù…Ø®Ø²ÙˆÙ†", "Error loading stock")}: {ex.Message}",
                    UiText.T("Ø®Ø·Ø£", "Error"),
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
                    $"{UiText.T("Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ ØªØ­Ù…ÙŠÙ„ Ø§Ù„Ù…Ø®Ø²ÙˆÙ†", "Error loading stock")}: {ex.Message}",
                    UiText.T("Ø®Ø·Ø£", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CopyBarcode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { DataContext: CurrentStockDto item })
                return;

            if (string.IsNullOrWhiteSpace(item.ITEMCODE))
            {
                MessageBox.Show(
                    UiText.T("Ù„Ø§ ÙŠÙˆØ¬Ø¯ Ø¨Ø§Ø±ÙƒÙˆØ¯ Ù„Ù†Ø³Ø®Ù‡.", "There is no barcode to copy."),
                    UiText.T("ØªÙ†Ø¨ÙŠÙ‡", "Notice"),
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
                    FileName = "CurrentStock.xlsx"
                };

                if (dlg.ShowDialog() != true)
                    return;

                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var ws = workbook.Worksheets.Add(UiText.T("Ø§Ù„Ù…Ø®Ø²ÙˆÙ† Ø§Ù„Ø­Ø§Ù„ÙŠ", "Current Stock"));

                ws.Cell(1, 1).Value = UiText.T("Ø§Ù„Ø¨Ø§Ø±ÙƒÙˆØ¯", "Barcode");
                ws.Cell(1, 2).Value = UiText.T("Ø§Ù„Ù…Ù†ØªØ¬", "Product");
                ws.Cell(1, 3).Value = UiText.T("Ø§Ù„ÙˆØ­Ø¯Ø©", "Unit");
                ws.Cell(1, 4).Value = UiText.T("Ø§Ù„ÙƒÙ…ÙŠØ©", "Quantity");
                ws.Cell(1, 5).Value = UiText.T("ØªÙƒÙ„ÙØ© Ø§Ù„Ù…Ø®Ø²ÙˆÙ†", "Inventory Cost");
                ws.Cell(1, 6).Value = UiText.T("Ø³Ø¹Ø± Ø§Ù„Ø¨ÙŠØ¹ Ø§Ù„Ø­Ø§Ù„ÙŠ", "Current Sale Price");
                ws.Cell(1, 7).Value = UiText.T("Ø£Ù‚Ø±Ø¨ Ø§Ù†ØªÙ‡Ø§Ø¡", "Nearest Expiry");
                ws.Cell(1, 8).Value = UiText.T("Ø§Ù„Ø­Ø¯ Ø§Ù„Ø£Ø¯Ù†Ù‰", "Minimum Quantity");
                ws.Cell(1, 9).Value = UiText.T("ØªÙ†Ø¨ÙŠÙ‡", "Alert");

                ws.Row(1).Style.Font.Bold = true;
                ws.Row(1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

                int row = 2;
                foreach (var item in CurrentStockItems)
                {
                    ws.Cell(row, 1).Value = item.ITEMCODE;
                    ws.Cell(row, 2).Value = item.ProductName;
                    ws.Cell(row, 3).Value = item.UnitName;
                    ws.Cell(row, 4).Value = item.Quantity;
                    ws.Cell(row, 5).Value = item.PurchasePrice;
                    ws.Cell(row, 6).Value = item.SalePrice;
                    ws.Cell(row, 7).Value = item.NearestExpiryDate?.ToString("yyyy-MM-dd") ?? "-";
                    ws.Cell(row, 8).Value = item.MinimumQuantity;
                    ws.Cell(row, 9).Value = item.IsLowStock ? UiText.T("âš  Ù‚Ù„ÙŠÙ„", "âš  Low") : "";
                    row++;
                }

                ws.Columns().AdjustToContents();
                workbook.SaveAs(dlg.FileName);

                MessageBox.Show(
                    UiText.T("ØªÙ… Ø§Ø³ØªØ®Ø±Ø§Ø¬ Ù…Ù„Ù Excel Ø¨Ù†Ø¬Ø§Ø­!", "Excel file exported successfully!"),
                    UiText.T("Ù†Ø¬Ø§Ø­", "Success"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ Ø§Ù„ØªØµØ¯ÙŠØ±", "An error occurred during export")}: {ex.Message}",
                    UiText.T("Ø®Ø·Ø£", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private string? GetSearchText()
        {
            var searchText = SearchText.Text?.Trim();
            return string.IsNullOrWhiteSpace(searchText) ? null : searchText;
        }
    }
}
