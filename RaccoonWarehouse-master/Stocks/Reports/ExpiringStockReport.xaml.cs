using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Reports.Stocks.Dtos;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Helpers;
using ClosedXML.Excel;
using Microsoft.Win32;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Stocks.Reports
{
    public partial class ExpiringStockReport : Window
    {
        private readonly IStockReportService _reports;
        private readonly IUserService _users;
        private bool _isReady;
        public ExpiringStockReport(IStockReportService reports, IUserService users)
        {
            
            InitializeComponent(); _reports = reports; _users = users; SupplierComboBoxSearch.Attach(SupplierComboBox); UiText.ApplyWindow(this); Loaded += LoadedAsync;
        }
        private async void LoadedAsync(object sender, RoutedEventArgs e)
        {
            var suppliers = (await _users.GetAllAsync()).Data?.Where(x => x.Role == UserRole.Supplier).OrderBy(x => x.Name).ToList() ?? new();
            suppliers.Insert(0, new RaccoonWarehouse.Domain.Users.DTOs.UserReadDto { Id = 0, Name = "All suppliers" });
            SupplierComboBoxSearch.SetItems(SupplierComboBox, suppliers); SupplierComboBox.SelectedIndex = 0;
            _isReady = true;
            await LoadAsync();
        }
        private async Task LoadAsync()
        {
            var days = ((DaysComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() is string value && int.TryParse(value, out var parsed)) ? parsed : 90;
            var supplier = SupplierComboBox.SelectedItem is RaccoonWarehouse.Domain.Users.DTOs.UserReadDto selectedSupplier && selectedSupplier.Id > 0
                ? selectedSupplier.Id
                : SupplierComboBox.SelectedValue is int id && id > 0 ? id : (int?)null;
            var rows = await _reports.GetExpiringStockAsync(days, supplier); ResultsGrid.ItemsSource = rows; TotalCostText.Text = rows.Sum(x => x.TotalCost).ToString("N3");
        }
        private async void SupplierComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isReady || e.AddedItems.Count == 0)
                return;

            await LoadAsync();
        }
        private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();
        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var rows = ResultsGrid.Items.OfType<ExpiringStockRowDto>().ToList();
            if (rows.Count == 0)
            {
                MessageBox.Show(this,
                    UiText.T("لا توجد بيانات لتصديرها.", "There is no data to export."),
                    UiText.T("تنبيه", "Notice"));
                return;
            }

            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    AddExtension = true,
                    FileName = $"ExpiringStock_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };
                if (dialog.ShowDialog(this) != true) return;

                using var workbook = new XLWorkbook();
                var sheet = workbook.Worksheets.Add(UiText.T("قرب الانتهاء", "Expiring Stock"));
                sheet.RightToLeft = !UiText.IsEnglish;
                sheet.Cell(1, 1).Value = UiText.T("المخزون قريب الانتهاء", "Expiring and Near-Expiry Stock");
                sheet.Cell(1, 1).Style.Font.Bold = true;
                sheet.Cell(2, 1).Value = UiText.T("الأيام القادمة", "Days ahead");
                sheet.Cell(2, 2).Value = (DaysComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
                sheet.Cell(3, 1).Value = UiText.T("المورد", "Supplier");
                sheet.Cell(3, 2).Value = SupplierComboBox.SelectedItem is RaccoonWarehouse.Domain.Users.DTOs.UserReadDto supplier
                    ? supplier.Name
                    : SupplierComboBox.Text;
                sheet.Cell(4, 1).Value = UiText.T("تاريخ التصدير", "Exported at");
                sheet.Cell(4, 2).Value = DateTime.Now;
                sheet.Cell(4, 2).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";

                var headers = new[]
                {
                    UiText.T("الباركود", "Barcode"),
                    UiText.T("الصنف", "Product"),
                    UiText.T("المورد", "Supplier"),
                    UiText.T("الوحدة", "Unit"),
                    UiText.T("الكمية المتبقية", "Remaining quantity"),
                    UiText.T("تكلفة الشراء", "Purchase cost"),
                    UiText.T("إجمالي التكلفة", "Total cost"),
                    UiText.T("تاريخ الانتهاء", "Expiry date"),
                    UiText.T("الأيام المتبقية", "Days remaining")
                };

                for (var column = 0; column < headers.Length; column++)
                    sheet.Cell(6, column + 1).Value = headers[column];

                var rowNumber = 7;
                foreach (var row in rows)
                {
                    sheet.Cell(rowNumber, 1).Style.NumberFormat.Format = "@";
                    sheet.Cell(rowNumber, 1).Value = row.ITEMCODE ?? string.Empty;
                    sheet.Cell(rowNumber, 2).Value = row.ProductName ?? string.Empty;
                    sheet.Cell(rowNumber, 3).Value = row.SupplierName ?? string.Empty;
                    sheet.Cell(rowNumber, 4).Value = row.UnitName ?? string.Empty;
                    sheet.Cell(rowNumber, 5).Value = row.RemainingQuantity;
                    sheet.Cell(rowNumber, 6).Value = row.PurchasePrice;
                    sheet.Cell(rowNumber, 7).Value = row.TotalCost;
                    sheet.Cell(rowNumber, 8).Value = row.ExpiryDate;
                    sheet.Cell(rowNumber, 9).Value = row.DaysRemaining;
                    rowNumber++;
                }

                var lastRow = rowNumber - 1;
                sheet.Range(6, 1, lastRow, headers.Length).CreateTable();
                sheet.Range(7, 5, lastRow, 7).Style.NumberFormat.Format = "#,##0.000";
                sheet.Range(7, 8, lastRow, 8).Style.DateFormat.Format = "yyyy-mm-dd";
                sheet.Columns(1, headers.Length).AdjustToContents(6, lastRow);
                foreach (var column in sheet.Columns(1, headers.Length))
                    column.Width = Math.Min(60, Math.Max(15, column.Width));
                sheet.SheetView.FreezeRows(6);
                workbook.SaveAs(dialog.FileName);

                MessageBox.Show(this,
                    UiText.T("تم تصدير ملف Excel بنجاح.", "Excel file exported successfully.")
                    + Environment.NewLine + dialog.FileName,
                    UiText.T("نجاح", "Success"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    UiText.T("تعذر تصدير ملف Excel.", "Failed to export the Excel file.")
                    + Environment.NewLine + ex.Message,
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private void Back_Click(object sender, RoutedEventArgs e) => Close();
    }
}
