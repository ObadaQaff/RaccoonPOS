using RaccoonWarehouse.Application.Service.LostCustomers;
using RaccoonWarehouse.Domain.LostCustomers;
using ClosedXML.Excel;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.LostCustomers;

public partial class LostCustomersReportWindow : Window
{
    private readonly ILostCustomerService _service; private List<LostCustomerDto> _all = new();
    public LostCustomersReportWindow(ILostCustomerService service) { _service = service; InitializeComponent(); TypeFilter.SelectedIndex = 0; ReasonFilter.SelectedIndex = 0; Loaded += async (_, _) => { var result = await _service.GetAllAsync(); _all = result.Data ?? new(); Apply(); }; }
    private void Apply_Click(object sender, RoutedEventArgs e) => Apply();
    private void Apply()
    { var from = FromDate.SelectedDate?.Date; var to = ToDate.SelectedDate?.Date.AddDays(1).AddTicks(-1); var type = (TypeFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString(); var reason = (ReasonFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString(); var search = SearchBox.Text.Trim(); var rows = _all.Where(x => (!from.HasValue || x.CreatedDate >= from) && (!to.HasValue || x.CreatedDate <= to) && (type == "-1" || string.IsNullOrEmpty(type) || (int)x.Type == int.Parse(type)) && (reason == "-1" || string.IsNullOrEmpty(reason) || (int)x.Reason == int.Parse(reason)) && (string.IsNullOrWhiteSpace(search) || $"{x.ProductName} {x.Message} {x.Notes} {x.CreatedByName}".Contains(search, StringComparison.OrdinalIgnoreCase))).ToList(); Grid.ItemsSource = rows.Select(x => new ReportRow(x)).ToList(); var missing = rows.Where(x => x.Type == LostCustomerType.MissingProduct).ToList(); var most = missing.GroupBy(x => x.ProductName).OrderByDescending(g => g.Sum(x => x.Quantity ?? 0)).FirstOrDefault(); var reasonBreakdown = string.Join("، ", rows.GroupBy(x => x.Reason).Select(g => $"{ReasonLabel(g.Key)}: {g.Count()}")); SummaryText.Text = $"إجمالي السجلات: {rows.Count}   |   المنتجات غير المتوفرة: {missing.Count}   |   إجمالي الكميات: {missing.Sum(x => x.Quantity ?? 0):0.###}   |   الأكثر طلباً: {most?.Key ?? "—"}\nالتوزيع حسب السبب: {reasonBreakdown}"; }
    private static string ReasonLabel(LostCustomerReason reason) => reason == LostCustomerReason.OutOfStock ? "غير متوفر بالمخزون" : reason == LostCustomerReason.ProductNotRegistered ? "غير مسجل بالنظام" : "سبب آخر";
    private void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        var rows = Grid.Items.OfType<ReportRow>().ToList();
        if (rows.Count == 0)
        {
            MessageBox.Show(this, "لا توجد بيانات لتصديرها.", "تنبيه");
            return;
        }

        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                AddExtension = true,
                FileName = $"LostCustomers_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };
            if (dialog.ShowDialog(this) != true) return;

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Lost Customers");
            sheet.RightToLeft = true;
            sheet.Cell(1, 1).Value = "تقرير العملاء المفقودين / Lost Customers Report";
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(2, 1).Value = "من تاريخ / From";
            sheet.Cell(2, 2).Value = FromDate.SelectedDate?.Date;
            sheet.Cell(3, 1).Value = "إلى تاريخ / To";
            sheet.Cell(3, 2).Value = ToDate.SelectedDate?.Date;
            sheet.Cell(4, 1).Value = "البحث / Search";
            sheet.Cell(4, 2).Value = SearchBox.Text.Trim();
            sheet.Cell(5, 1).Value = "تاريخ التصدير / Exported at";
            sheet.Cell(5, 2).Value = DateTime.Now;
            sheet.Cell(2, 2).Style.DateFormat.Format = "yyyy-mm-dd";
            sheet.Cell(3, 2).Style.DateFormat.Format = "yyyy-mm-dd";
            sheet.Cell(5, 2).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";

            var headers = new[]
            {
                "النوع / Type", "السبب / Reason", "المنتج / Product", "الكمية / Quantity",
                "الرسالة / Message", "الملاحظات / Notes", "الموظف / Employee", "التاريخ / Date"
            };
            for (var column = 0; column < headers.Length; column++)
                sheet.Cell(7, column + 1).Value = headers[column];

            var rowNumber = 8;
            foreach (var row in rows)
            {
                sheet.Cell(rowNumber, 1).Value = row.TypeLabel;
                sheet.Cell(rowNumber, 2).Value = row.ReasonLabel;
                sheet.Cell(rowNumber, 3).Value = row.ProductName ?? string.Empty;
                sheet.Cell(rowNumber, 4).Value = row.Quantity;
                sheet.Cell(rowNumber, 5).Value = row.Message ?? string.Empty;
                sheet.Cell(rowNumber, 6).Value = row.Notes ?? string.Empty;
                sheet.Cell(rowNumber, 7).Value = row.CreatedByName ?? string.Empty;
                sheet.Cell(rowNumber, 8).Value = row.CreatedDate;
                rowNumber++;
            }

            var lastRow = rowNumber - 1;
            sheet.Range(7, 1, lastRow, headers.Length).CreateTable();
            sheet.Range(8, 4, lastRow, 4).Style.NumberFormat.Format = "#,##0.###";
            sheet.Range(8, 8, lastRow, 8).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
            sheet.Columns(1, headers.Length).AdjustToContents(7, lastRow);
            foreach (var column in sheet.Columns(1, headers.Length))
                column.Width = Math.Min(60, Math.Max(15, column.Width));
            sheet.SheetView.FreezeRows(7);
            workbook.SaveAs(dialog.FileName);

            MessageBox.Show(this, "تم تصدير ملف Excel بنجاح.\n" + dialog.FileName, "نجاح");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "تعذر تصدير ملف Excel.\n" + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void Print_Click(object sender, RoutedEventArgs e) { var dialog = new PrintDialog(); if (dialog.ShowDialog() == true) dialog.PrintVisual(Grid, Title); }
    private sealed class ReportRow { private readonly LostCustomerDto D; public string TypeLabel => D.Type == LostCustomerType.MissingProduct ? "منتج غير متوفر" : "سبب آخر"; public string ReasonLabel => D.Reason == LostCustomerReason.OutOfStock ? "غير متوفر بالمخزون" : D.Reason == LostCustomerReason.ProductNotRegistered ? "غير مسجل بالنظام" : "سبب آخر"; public string ProductName => D.ProductName; public decimal? Quantity => D.Quantity; public string? Message => D.Message; public string? Notes => D.Notes; public string CreatedByName => D.CreatedByName; public DateTime CreatedDate => D.CreatedDate; public ReportRow(LostCustomerDto d) => D = d; }
}
