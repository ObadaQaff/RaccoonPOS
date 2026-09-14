using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Reports.Financial.Filters;
using RaccoonWarehouse.Domain.Reports.Sales.Dtos;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace RaccoonWarehouse.FinancialTransactions.Reports
{
    public partial class InvoicePaymentMethodsReport : Window
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ILoadingService _loadingService;
        private readonly SourceDocumentNavigationService _sourceDocumentNavigationService;
        private List<InvoicePaymentMethodRow> _currentRows = new();

        public InvoicePaymentMethodsReport(IInvoiceService invoiceService, ILoadingService loadingService, SourceDocumentNavigationService sourceDocumentNavigationService)
        {
            InitializeComponent();
            _invoiceService = invoiceService;
            _loadingService = loadingService;
            _sourceDocumentNavigationService = sourceDocumentNavigationService;
            UiText.ApplyWindow(this);
            ApplyTranslations();
            Loaded += (_, _) => { FromDatePicker.SelectedDate = DateTime.Today; ToDatePicker.SelectedDate = DateTime.Today; };
        }

        private void ApplyTranslations()
        {
            Title = UiText.T("تقرير طرق دفع الفواتير", "Invoice Payment Methods");
            TitleText.Text = Title;
            FromLabel.Text = UiText.T("من تاريخ", "From date"); ToLabel.Text = UiText.T("إلى تاريخ", "To date");
            CountLabel.Text = UiText.T("الفواتير:", "Invoices:"); TotalLabel.Text = UiText.T("الإجمالي:", "Total:");
            GenerateButton.Content = UiText.T("عرض التقرير", "Show report"); BackButton.Content = UiText.T("رجوع", "Back");
            InvoiceNumberColumn.Header = UiText.T("رقم الفاتورة", "Invoice"); DateColumn.Header = UiText.T("التاريخ", "Date");
            CustomerColumn.Header = UiText.T("العميل", "Customer"); CashierColumn.Header = UiText.T("الكاشير", "Cashier");
            InvoiceTotalColumn.Header = UiText.T("إجمالي الفاتورة", "Invoice total"); CashColumn.Header = UiText.T("نقدي", "Cash");
            VisaColumn.Header = UiText.T("فيزا", "Visa"); MasterColumn.Header = UiText.T("ماستر", "Master");
            DebitColumn.Header = UiText.T("تحويل", "Debit"); CheckColumn.Header = UiText.T("شيك", "Check");
            MobileColumn.Header = UiText.T("موبايل", "Mobile"); CreditColumn.Header = UiText.T("آجل", "Credit");
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FromDatePicker.SelectedDate is not DateTime from || ToDatePicker.SelectedDate is not DateTime to)
                { MessageBox.Show(UiText.T("يرجى اختيار التاريخ.", "Please choose the dates.")); return; }
                if (from.Date > to.Date)
                { MessageBox.Show(UiText.T("تاريخ البداية يجب أن يسبق تاريخ النهاية.", "The start date must be before the end date.")); return; }

                _loadingService.Show();
                var result = await _invoiceService.GetSalesReportAsync(new FinancialSummaryFilterDto { From = from.Date, To = to.Date.AddDays(1).AddTicks(-1), IncludeReturns = true });
                if (!result.Success)
                { MessageBox.Show(result.Message ?? UiText.T("تعذر تحميل التقرير.", "Could not load the report.")); return; }

                var rows = (result.Data.rows ?? new List<SalesReportRowDto>()).Select(x => new InvoicePaymentMethodRow(x)).ToList();
                _currentRows = rows;
                InvoicePaymentsGrid.ItemsSource = rows;
                CountText.Text = rows.Count.ToString(); TotalText.Text = rows.Sum(x => x.InvoiceTotal).ToString("0.00000");
                CashTotalText.Text = rows.Sum(x => x.CashAmount).ToString("0.00000");
                VisaTotalText.Text = rows.Sum(x => x.VisaAmount).ToString("0.00000");
                MasterTotalText.Text = rows.Sum(x => x.MasterAmount).ToString("0.00000");
                DebitTotalText.Text = rows.Sum(x => x.DebitAmount).ToString("0.00000");
                CheckTotalText.Text = rows.Sum(x => x.CheckAmount).ToString("0.00000");
                MobileTotalText.Text = rows.Sum(x => x.MobileAmount).ToString("0.00000");
                CreditTotalText.Text = rows.Sum(x => x.CreditAmount).ToString("0.00000");
            }
            catch (Exception ex) { MessageBox.Show($"{UiText.T("خطأ", "Error")}: {ex.Message}", UiText.T("خطأ", "Error")); }
            finally { _loadingService.Hide(); }
        }

        private async void InvoicePaymentsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (InvoicePaymentsGrid.SelectedItem is not InvoicePaymentMethodRow row || row.InvoiceId <= 0) return;
            try { _loadingService.Show(); await _sourceDocumentNavigationService.OpenSourceDocument("Invoice", row.InvoiceId); }
            catch (Exception ex) { MessageBox.Show($"{UiText.T("خطأ", "Error")}: {ex.Message}", UiText.T("خطأ", "Error")); }
            finally { _loadingService.Hide(); }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) => Close();

        private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRows.Count == 0)
            {
                MessageBox.Show(UiText.T("Ø§Ø¹Ø±Ø¶ Ø§Ù„ØªÙ‚Ø±ÙŠØ± Ø£ÙˆÙ„Ø§Ù‹.", "Show the report first."));
                return;
            }

            try
            {
                var dialog = new SaveFileDialog { Filter = "Excel File (*.xlsx)|*.xlsx", FileName = "InvoicePaymentMethods.xlsx" };
                if (dialog.ShowDialog() != true)
                    return;

                using var workbook = new XLWorkbook();
                var sheet = workbook.Worksheets.Add("Payment methods");
                var headers = new[] { "Invoice", "Date", "Customer", "Cashier", "Invoice total", "Cash", "Visa", "Master", "Debit", "Check", "Mobile", "Credit" };
                for (var column = 0; column < headers.Length; column++) sheet.Cell(1, column + 1).Value = headers[column];
                sheet.Row(1).Style.Font.Bold = true;
                for (var index = 0; index < _currentRows.Count; index++)
                {
                    var row = _currentRows[index];
                    sheet.Cell(index + 2, 1).Value = row.InvoiceNumber;
                    sheet.Cell(index + 2, 2).Value = row.Date;
                    sheet.Cell(index + 2, 3).Value = row.CustomerName;
                    sheet.Cell(index + 2, 4).Value = row.CashierName;
                    sheet.Cell(index + 2, 5).Value = row.InvoiceTotal;
                    sheet.Cell(index + 2, 6).Value = row.CashAmount;
                    sheet.Cell(index + 2, 7).Value = row.VisaAmount;
                    sheet.Cell(index + 2, 8).Value = row.MasterAmount;
                    sheet.Cell(index + 2, 9).Value = row.DebitAmount;
                    sheet.Cell(index + 2, 10).Value = row.CheckAmount;
                    sheet.Cell(index + 2, 11).Value = row.MobileAmount;
                    sheet.Cell(index + 2, 12).Value = row.CreditAmount;
                }
                sheet.Columns().AdjustToContents();
                var totals = workbook.Worksheets.Add("Totals");
                totals.Cell(1, 1).Value = "Payment method"; totals.Cell(1, 2).Value = "Total"; totals.Row(1).Style.Font.Bold = true;
                var totalRows = new[] { ("Cash", _currentRows.Sum(x => x.CashAmount)), ("Visa", _currentRows.Sum(x => x.VisaAmount)), ("Master", _currentRows.Sum(x => x.MasterAmount)), ("Debit", _currentRows.Sum(x => x.DebitAmount)), ("Check", _currentRows.Sum(x => x.CheckAmount)), ("Mobile", _currentRows.Sum(x => x.MobileAmount)), ("Credit", _currentRows.Sum(x => x.CreditAmount)) };
                for (var index = 0; index < totalRows.Length; index++) { totals.Cell(index + 2, 1).Value = totalRows[index].Item1; totals.Cell(index + 2, 2).Value = totalRows[index].Item2; }
                totals.Columns().AdjustToContents();
                workbook.SaveAs(dialog.FileName);
                MessageBox.Show(UiText.T("ØªÙ… ØªØµØ¯ÙŠØ± Ø§Ù„ØªÙ‚Ø±ÙŠØ±.", "Report exported successfully."));
            }
            catch (Exception ex) { MessageBox.Show($"{UiText.T("Ø®Ø·Ø£", "Error")}: {ex.Message}", UiText.T("Ø®Ø·Ø£", "Error")); }
        }

        private sealed class InvoicePaymentMethodRow
        {
            public InvoicePaymentMethodRow(SalesReportRowDto source)
            {
                InvoiceId = source.InvoiceId; InvoiceNumber = source.InvoiceNumber; Date = source.Date; CustomerName = source.CustomerName; CashierName = source.CashierName; InvoiceTotal = source.Total;
                CashAmount = Get(source, PaymentType.Cash); VisaAmount = Get(source, PaymentType.Visa); MasterAmount = Get(source, PaymentType.Master); DebitAmount = Get(source, PaymentType.Debit); CheckAmount = Get(source, PaymentType.Check); MobileAmount = Get(source, PaymentType.MobilePayment); CreditAmount = Get(source, PaymentType.Credit);
            }
            public int InvoiceId { get; } public string InvoiceNumber { get; } public DateTime Date { get; } public string CustomerName { get; } public string CashierName { get; } public decimal InvoiceTotal { get; }
            public decimal CashAmount { get; } public decimal VisaAmount { get; } public decimal MasterAmount { get; } public decimal DebitAmount { get; } public decimal CheckAmount { get; } public decimal MobileAmount { get; } public decimal CreditAmount { get; }
            private static decimal Get(SalesReportRowDto source, PaymentType type) => source.PaymentAmounts.TryGetValue(type, out var amount) ? amount : 0m;
        }
    }
}
