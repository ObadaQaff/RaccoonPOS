using RaccoonWarehouse.Application.Service.CustomerRequests;
using RaccoonWarehouse.Domain.CustomerRequests;
using ClosedXML.Excel;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.CustomerRequests;

public partial class CustomerRequestsReportWindow : Window
{
    private readonly ICustomerRequestService _service;
    private List<CustomerRequestDto> _all = new();
    private bool _isInitialized;
    public CustomerRequestsReportWindow(ICustomerRequestService service) { _service = service; InitializeComponent(); _isInitialized = true; Loaded += async (_, _) => { var result = await _service.GetAllAsync(); _all = result.Data ?? new(); ApplyFilter(); }; }
    private void ApplyFilter() { if (!_isInitialized || ReportGrid is null) return; var text = SearchText?.Text?.Trim() ?? ""; var tag = (StatusFilter?.SelectedItem as ComboBoxItem)?.Tag?.ToString(); int.TryParse(tag, out var status); ReportGrid.ItemsSource = _all.Where(x => (string.IsNullOrWhiteSpace(text) || x.RequestNumber.Contains(text, StringComparison.CurrentCultureIgnoreCase) || x.CustomerName.Contains(text, StringComparison.CurrentCultureIgnoreCase) || (x.CustomerPhone?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false)) && (status < 0 || (int)x.Status == status)).ToList(); }
    private void SearchText_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilter();
    private void Print_Click(object sender, RoutedEventArgs e) { var dialog = new PrintDialog(); if (dialog.ShowDialog() == true) dialog.PrintVisual(ReportGrid, Title); }
    private void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        var rows = ReportGrid.Items.OfType<CustomerRequestDto>().ToList();
        if (rows.Count == 0)
        {
            MessageBox.Show(this, "لا توجد بيانات لتصديرها.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                AddExtension = true,
                FileName = $"CustomerRequests_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };
            if (dialog.ShowDialog(this) != true) return;

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Customer Requests");
            sheet.RightToLeft = true;
            sheet.Cell(1, 1).Value = "تقرير طلبات العملاء / Customer Requests Report";
            sheet.Cell(2, 1).Value = "تاريخ التصدير / Exported at";
            sheet.Cell(2, 2).Value = DateTime.Now;
            sheet.Cell(2, 2).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";

            var headers = new[] { "رقم الطلب", "العميل", "الهاتف", "الإجمالي", "الدفع", "الحالة", "الاستلام", "المبيعات", "ملاحظات" };
            for (var column = 0; column < headers.Length; column++) sheet.Cell(4, column + 1).Value = headers[column];

            var rowNumber = 5;
            foreach (var row in rows)
            {
                sheet.Cell(rowNumber, 1).Value = row.RequestNumber;
                sheet.Cell(rowNumber, 2).Value = row.CustomerName;
                sheet.Cell(rowNumber, 3).Value = row.CustomerPhone ?? string.Empty;
                sheet.Cell(rowNumber, 4).Value = row.TotalAmount;
                sheet.Cell(rowNumber, 5).Value = row.PaymentStatus;
                sheet.Cell(rowNumber, 6).Value = row.StatusText;
                sheet.Cell(rowNumber, 7).Value = row.ReceivingStatusText;
                sheet.Cell(rowNumber, 8).Value = row.SalesStatusText;
                sheet.Cell(rowNumber, 9).Value = row.Notes ?? string.Empty;
                rowNumber++;
            }

            sheet.Range(4, 1, rowNumber - 1, headers.Length).CreateTable();
            sheet.Range(5, 4, rowNumber - 1, 4).Style.NumberFormat.Format = "#,##0.000";
            sheet.Columns(1, headers.Length).AdjustToContents();
            sheet.SheetView.FreezeRows(4);
            workbook.SaveAs(dialog.FileName);
            MessageBox.Show(this, "تم تصدير ملف Excel بنجاح.\n" + dialog.FileName, "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "تعذر تصدير ملف Excel.\n" + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
