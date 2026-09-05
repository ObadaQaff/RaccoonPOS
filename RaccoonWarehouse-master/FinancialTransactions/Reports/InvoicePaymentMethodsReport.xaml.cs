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

namespace RaccoonWarehouse.FinancialTransactions.Reports
{
    public partial class InvoicePaymentMethodsReport : Window
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ILoadingService _loadingService;
        private readonly SourceDocumentNavigationService _sourceDocumentNavigationService;

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
                InvoicePaymentsGrid.ItemsSource = rows;
                CountText.Text = rows.Count.ToString(); TotalText.Text = rows.Sum(x => x.InvoiceTotal).ToString("0.00000");
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
