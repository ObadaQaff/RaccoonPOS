using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse.Stocks
{
    public partial class CurrentStock : Window
    {
        private readonly IStockReportService _stockReportService;
        private readonly IServiceProvider _serviceProvider;

        public ObservableCollection<CurrentStockDto> CurrentStockItems { get; set; }
            = new ObservableCollection<CurrentStockDto>();

        public CurrentStock(IStockReportService stockReportService, IServiceProvider serviceProvider)
        {
            _stockReportService = stockReportService;
            _serviceProvider = serviceProvider;
            InitializeComponent();
            DataContext = this;
            UiText.ApplyWindow(this);

            Loaded += CurrentStock_Loaded;
        }

        private async void CurrentStock_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadStock();
        }

        private async Task LoadStock()
        {
            try
            {
                CurrentStockItems.Clear();

                var data = await _stockReportService.GetCurrentStockAsync();

                foreach (var item in data)
                    CurrentStockItems.Add(item);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("خطأ أثناء تحميل المخزون", "Error loading stock")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            await LoadStock();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AdjustBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = _serviceProvider.GetRequiredService<StockAdjustmentWindow>();
            if (StockGrid.SelectedItem is CurrentStockDto selected)
                window.InitialProductId = selected.ProductId;

            window.Owner = this;
            window.ShowDialog();
            if (window.SavedSuccessfully)
                _ = LoadStock();
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            string term = SearchText.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(term))
            {
                StockGrid.ItemsSource = CurrentStockItems;
                return;
            }

            var filtered = CurrentStockItems
                .Where(x =>
                    (x.ProductName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ITEMCODE?.Contains(term) ?? false))
                .ToList();

            StockGrid.ItemsSource = filtered;
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
                    FileName = "CurrentStock.xlsx"
                };

                if (dlg.ShowDialog() != true)
                    return;

                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var ws = workbook.Worksheets.Add(UiText.T("المخزون الحالي", "Current Stock"));

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
    }
}
