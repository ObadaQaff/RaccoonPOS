using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Reports.Financial.Dtos;
using RaccoonWarehouse.Domain.Reports.Financial.Filters;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Helpers.Localization;
using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.FinancialTransactions.Reports
{
    public partial class CashFlowReport : Window
    {
        private readonly IFinancialTransactionService _service;
        private readonly ILoadingService _loadingService;
        private List<CashFlowRowDto> _currentRows = new();

        public CashFlowReport(IFinancialTransactionService service, ILoadingService loadingService)
        {
            InitializeComponent();
            _service = service;
            _loadingService = loadingService;
            UiText.ApplyWindow(this);

            Loaded += CashFlowReport_Loaded;
        }

        private void CashFlowReport_Loaded(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = DateTime.Today;
            ToDatePicker.SelectedDate = DateTime.Today;
            IncludeVoidedCheckBox.IsChecked = false;

            // Direction
            DirectionComboBox.Items.Clear();
            DirectionComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("الكل", "All"), Tag = null });
            DirectionComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("داخل (قبض)", "In (Receipt)"), Tag = TransactionDirection.In });
            DirectionComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("خارج (صرف)", "Out (Payment)"), Tag = TransactionDirection.Out });
            DirectionComboBox.SelectedIndex = 0;

            // PaymentMethod
            PaymentMethodComboBox.Items.Clear();
            PaymentMethodComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("الكل", "All"), Tag = null });
            foreach (var v in Enum.GetValues(typeof(PaymentMethod)).Cast<PaymentMethod>())
                PaymentMethodComboBox.Items.Add(new ComboBoxItem { Content = v.ToString(), Tag = v });
            PaymentMethodComboBox.SelectedIndex = 0;

            // SourceType
            SourceTypeComboBox.Items.Clear();
            SourceTypeComboBox.Items.Add(new ComboBoxItem { Content = UiText.T("الكل", "All"), Tag = null });
            foreach (var v in Enum.GetValues(typeof(FinancialSourceType)).Cast<FinancialSourceType>())
                SourceTypeComboBox.Items.Add(new ComboBoxItem { Content = v.ToString(), Tag = v });
            SourceTypeComboBox.SelectedIndex = 0;
        }

        private async void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
            var loadingShown = false;
            try
            {
                if (FromDatePicker.SelectedDate == null || ToDatePicker.SelectedDate == null)
                {
                    MessageBox.Show(UiText.T("يرجى اختيار تاريخ البداية والنهاية.", "Please choose the start and end dates."));
                    return;
                }

                _loadingService.Show();
                loadingShown = true;

                var filter = new CashFlowFilterDto
                {
                    From = FromDatePicker.SelectedDate.Value.Date,
                    To = ToDatePicker.SelectedDate.Value.Date,
                    IncludeVoided = IncludeVoidedCheckBox.IsChecked == true
                };

                if (DirectionComboBox.SelectedItem is ComboBoxItem d && d.Tag is TransactionDirection dir)
                    filter.Direction = dir;

                if (PaymentMethodComboBox.SelectedItem is ComboBoxItem pm && pm.Tag is PaymentMethod method)
                    filter.Method = method;

                if (SourceTypeComboBox.SelectedItem is ComboBoxItem st && st.Tag is FinancialSourceType src)
                    filter.SourceType = src;

                var (summary, rows) = await _service.GetCashFlowAsync(filter);

                _currentRows = rows ?? new List<CashFlowRowDto>();
                CashFlowGrid.ItemsSource = _currentRows;

                TotalInText.Text = summary.TotalIn.ToString("0.00");
                TotalOutText.Text = summary.TotalOut.ToString("0.00");
                NetText.Text = summary.Net.ToString("0.00");
                CountText.Text = summary.CountAll.ToString();
                CashNetText.Text = summary.CashNet.ToString("0.00");
                VisaNetText.Text = summary.VisaNet.ToString("0.00");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ", "Error")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
            finally
            {
                if (loadingShown)
                    _loadingService.Hide();
            }
        }

        private void ExportExcelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRows.Count == 0)
            {
                MessageBox.Show(UiText.T("اعرض التقرير أولاً قبل التصدير.", "Generate the report before exporting."), UiText.T("تنبيه", "Notice"));
                return;
            }

            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Excel workbook (*.xlsx)|*.xlsx",
                    FileName = $"Payments-Receipts-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx",
                    AddExtension = true,
                    DefaultExt = ".xlsx"
                };

                if (dialog.ShowDialog() != true)
                    return;

                using var workbook = new XLWorkbook();
                var sheet = workbook.Worksheets.Add(UiText.T("المدفوعات والمقبوضات", "Payments and Receipts"));
                var headers = new[]
                {
                    UiText.T("التاريخ", "Date"), UiText.T("الاتجاه", "Direction"),
                    UiText.T("طريقة الدفع", "Payment method"), UiText.T("داخل", "In"),
                    UiText.T("خارج", "Out"), UiText.T("الصافي", "Net"),
                    UiText.T("المصدر", "Source"), UiText.T("رقم المصدر", "Source number"),
                    UiText.T("الكاشير", "Cashier"), UiText.T("الحالة", "Status"),
                    UiText.T("ملاحظات", "Notes")
                };

                for (var column = 0; column < headers.Length; column++)
                    sheet.Cell(1, column + 1).Value = headers[column];

                for (var index = 0; index < _currentRows.Count; index++)
                {
                    var row = _currentRows[index];
                    var excelRow = index + 2;
                    sheet.Cell(excelRow, 1).Value = row.Date;
                    sheet.Cell(excelRow, 2).Value = row.Direction.ToString();
                    sheet.Cell(excelRow, 3).Value = row.Method.ToString();
                    sheet.Cell(excelRow, 4).Value = row.AmountIn;
                    sheet.Cell(excelRow, 5).Value = row.AmountOut;
                    sheet.Cell(excelRow, 6).Value = row.Net;
                    sheet.Cell(excelRow, 7).Value = row.SourceType.ToString();
                    sheet.Cell(excelRow, 8).Value = row.SourceId;
                    sheet.Cell(excelRow, 9).Value = row.CashierName;
                    sheet.Cell(excelRow, 10).Value = row.StatusText;
                    sheet.Cell(excelRow, 11).Value = row.Notes;
                }

                sheet.Row(1).Style.Font.Bold = true;
                sheet.Row(1).Style.Fill.BackgroundColor = XLColor.LightBlue;
                sheet.Columns().AdjustToContents();
                workbook.SaveAs(dialog.FileName);

                MessageBox.Show(UiText.T("تم تصدير التقرير بنجاح.", "The report was exported successfully."), UiText.T("نجاح", "Success"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر تصدير التقرير", "Could not export the report")}: {ex.Message}", UiText.T("خطأ", "Error"));
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e) => Close();
    }
}
