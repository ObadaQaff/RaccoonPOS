using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Reports.Stocks.Dtos;
using RaccoonWarehouse.Helpers.Localization;
using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Reports
{
    public partial class StockBalancesReport : Window
    {
        private readonly IStockReportService _stockReportService;
        private readonly IUserService _userService;
        private List<StockVarianceRowDto> _allRows = new();
        private bool _isReady;

        public StockBalancesReport(IStockReportService stockReportService, IUserService userService)
        {
            
            InitializeComponent();
            _stockReportService = stockReportService;
            _userService = userService;
            UiText.ApplyWindow(this);
            Loaded += StockBalancesReport_Loaded;
        }

        private async void StockBalancesReport_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSuppliersAsync();
            await LoadReportAsync();
            _isReady = true;
        }

        private async System.Threading.Tasks.Task LoadSuppliersAsync()
        {
            var users = (await _userService.GetAllAsync()).Data ?? new();
            var suppliers = users.Where(x => x.Role == UserRole.Supplier).OrderBy(x => x.Name).ToList();
            suppliers.Insert(0, new RaccoonWarehouse.Domain.Users.DTOs.UserReadDto { Id = 0, Name = UiText.T("الكل", "All suppliers") });
            SupplierFilter.ItemsSource = suppliers;
            SupplierFilter.SelectedIndex = 0;
        }

        private async System.Threading.Tasks.Task LoadReportAsync()
        {
            try
            {
                _allRows = await _stockReportService.GetStockVarianceAsync();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ", "Error")}: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            var search = SearchTextBox?.Text?.Trim();
            var selectedSupplierId = SupplierFilter?.SelectedValue is int supplierId && supplierId > 0 ? supplierId : (int?)null;
            var hasQuantity = decimal.TryParse(QuantityFilterText?.Text?.Trim(), out var quantity);
            var comparison = (QuantityComparison?.SelectedItem as ComboBoxItem)?.Tag?.ToString();

            var rows = _allRows.Where(row =>
                (string.IsNullOrWhiteSpace(search)
                    || (row.ProductName?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)
                    || row.ITEMCODE.Contains(search, StringComparison.CurrentCultureIgnoreCase))
                && (!selectedSupplierId.HasValue || row.SupplierId == selectedSupplierId.Value)
                && (!hasQuantity || comparison == "all"
                    || comparison == "gt" && row.CurrentQuantity > quantity
                    || comparison == "lt" && row.CurrentQuantity < quantity
                    || comparison == "eq" && row.CurrentQuantity == quantity))
                .ToList();

            StockBalancesGrid.ItemsSource = rows;
            VarianceCountTextBlock.Text = rows.Count(x => x.VarianceQuantity != 0).ToString();
            DisplayedQuantityTotalTextBlock.Text = rows.Sum(x => x.CurrentQuantity).ToString("N5");
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (_isReady) ApplyFilters();
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var rows = StockBalancesGrid.Items.OfType<StockVarianceRowDto>().ToList();
            if (rows.Count == 0)
            {
                MessageBox.Show(this, UiText.T("لا توجد بيانات لتصديرها.", "There is no data to export."), UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    AddExtension = true,
                    FileName = $"StockBalances_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };
                if (dialog.ShowDialog(this) != true) return;

                using var workbook = new XLWorkbook();
                var sheet = workbook.Worksheets.Add(UiText.T("الجرد والفرق", "Stock Balance"));
                sheet.RightToLeft = !UiText.IsEnglish;
                sheet.Cell(1, 1).Value = UiText.T("تقرير الجرد والفرق", "Stock Balance Report");
                sheet.Cell(2, 1).Value = UiText.T("تاريخ التصدير", "Exported at");
                sheet.Cell(2, 2).Value = DateTime.Now;
                sheet.Cell(2, 2).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";

                var headers = new[]
                {
                    UiText.T("كود الصنف", "Product code"), UiText.T("اسم الصنف", "Product name"),
                    UiText.T("المورد", "Supplier"), UiText.T("الوحدة", "Unit"),
                    UiText.T("الرصيد الحالي", "Current quantity"), UiText.T("الحد الأدنى", "Minimum"),
                    UiText.T("الفرق", "Difference"), UiText.T("الحالة", "Status")
                };
                for (var column = 0; column < headers.Length; column++) sheet.Cell(4, column + 1).Value = headers[column];

                var rowNumber = 5;
                foreach (var row in rows)
                {
                    sheet.Cell(rowNumber, 1).Style.NumberFormat.Format = "@";
                    sheet.Cell(rowNumber, 1).Value = row.ITEMCODE;
                    sheet.Cell(rowNumber, 2).Value = row.ProductName ?? string.Empty;
                    sheet.Cell(rowNumber, 3).Value = row.SupplierName ?? string.Empty;
                    sheet.Cell(rowNumber, 4).Value = row.UnitName ?? string.Empty;
                    sheet.Cell(rowNumber, 5).Value = row.CurrentQuantity;
                    sheet.Cell(rowNumber, 6).Value = row.MinimumQuantity;
                    sheet.Cell(rowNumber, 7).Value = row.VarianceQuantity;
                    sheet.Cell(rowNumber, 8).Value = row.StatusText;
                    rowNumber++;
                }

                sheet.Range(4, 1, rowNumber - 1, headers.Length).CreateTable();
                sheet.Range(5, 5, rowNumber - 1, 7).Style.NumberFormat.Format = "#,##0.00000";
                sheet.Columns(1, headers.Length).AdjustToContents();
                sheet.SheetView.FreezeRows(4);
                workbook.SaveAs(dialog.FileName);
                MessageBox.Show(this, UiText.T("تم تصدير التقرير بنجاح.", "The report was exported successfully."), UiText.T("نجاح", "Success"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"{UiText.T("تعذر تصدير التقرير", "Could not export the report")}: {ex.Message}", UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
            await LoadReportAsync();
        }
    }
}
