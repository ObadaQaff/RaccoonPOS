using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Reports.Accounting.Dtos;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Vouchers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace RaccoonWarehouse.Accounting
{
    public sealed class PartyBalanceColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var balance = value is decimal amount ? amount : 0m;
            return balance > 0m
                ? new SolidColorBrush(Color.FromRgb(220, 38, 38))
                : balance < 0m
                    ? new SolidColorBrush(Color.FromRgb(22, 163, 74))
                    : new SolidColorBrush(Color.FromRgb(107, 114, 128));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    public partial class PartyBalanceReport : Window
    {
        private readonly PartyBalanceReportService _reportService;
        private readonly ILoadingService _loadingService;
        private UserRole _role = UserRole.Customer;
        private bool _combinedMode;
        private bool _initialized;

        public PartyBalanceReport(PartyBalanceReportService reportService, ILoadingService loadingService)
        {
            _reportService = reportService;
            _loadingService = loadingService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            Loaded += PartyBalanceReport_Loaded;
        }

        public void Initialize(UserRole role)
        {
            if (role is not UserRole.Customer and not UserRole.Supplier)
                throw new ArgumentOutOfRangeException(nameof(role));
            _combinedMode = false;
            _role = role;
            _initialized = true;
            ApplyLocalizedText();
        }

        public void InitializeCombined()
        {
            _combinedMode = true;
            _role = UserRole.Customer;
            _initialized = true;
            ApplyLocalizedText();
        }

        private async void PartyBalanceReport_Loaded(object sender, RoutedEventArgs e)
        {
            AsOfDatePicker.SelectedDate = DateTime.Today;
            ApplyLocalizedText();
            if (_initialized)
                await GenerateAsync();
        }

        private void ApplyLocalizedText()
        {
            var isCustomer = _role == UserRole.Customer;
            Title = _combinedMode
                ? UiText.T("أرصدة العملاء والموردين", "Accounts Balances")
                : isCustomer ? UiText.T("ذمم العملاء", "Customer Debts") : UiText.T("ذمم الموردين", "Supplier Payables");
            TitleText.Text = Title;
            SubtitleText.Text = _combinedMode
                ? UiText.T("الأرصدة المستحقة على العملاء والمستحقة للموردين حتى التاريخ المحدد.", "Accounts balances based on posted movements through the selected date.")
                : isCustomer
                ? UiText.T("الأرصدة المستحقة على العملاء حتى التاريخ المحدد.", "Customer balances owed to us through the selected date.")
                : UiText.T("الأرصدة المستحقة للموردين حتى التاريخ المحدد.", "Supplier balances owed by us through the selected date.");
            TotalLabelText.Text = UiText.T("إجمالي المستحق", "Total outstanding");
            AsOfLabelText.Text = UiText.T("حتى تاريخ", "As of date");
            SearchLabelText.Text = UiText.T("بحث بالاسم أو الهاتف", "Name or phone");
            RoleFilterLabelText.Text = UiText.T("\u0646\u0648\u0639 \u0627\u0644\u0645\u0633\u062a\u062e\u062f\u0645", "User role");
            BalanceFilterLabelText.Text = UiText.T("مقارنة الرصيد", "Balance amount");
            ((ComboBoxItem)RoleFilterComboBox.Items[0]).Content = UiText.T("\u0627\u0644\u0643\u0644", "All");
            ((ComboBoxItem)RoleFilterComboBox.Items[1]).Content = UiText.T("\u0639\u0645\u064a\u0644", "Customer");
            ((ComboBoxItem)RoleFilterComboBox.Items[2]).Content = UiText.T("\u0645\u0648\u0631\u062f", "Supplier");
            ((ComboBoxItem)BalanceFilterComboBox.Items[0]).Content = UiText.T("\u0627\u0644\u0643\u0644", "All");
            ((ComboBoxItem)BalanceFilterComboBox.Items[1]).Content = UiText.T("أكبر من صفر", "More than zero");
            ((ComboBoxItem)BalanceFilterComboBox.Items[2]).Content = UiText.T("يساوي صفر", "Equal to zero");
            ((ComboBoxItem)BalanceFilterComboBox.Items[3]).Content = UiText.T("أقل من صفر", "Less than zero");
            OutstandingOnlyCheckBox.Content = UiText.T("المستحق فقط", "Outstanding only");
            GenerateButton.Content = UiText.T("عرض التقرير", "Account Balance");
            ExportExcelButton.Content = UiText.T("تصدير Excel", "Export Excel");
            PrintButton.Content = UiText.T("طباعة", "Print");
            CloseButton.Content = UiText.T("إغلاق", "Close");
            ReceivePaymentButton.Content = isCustomer
                ? UiText.T("تحصيل دفعة", "Receive payment")
                : UiText.T("دفع للمورد", "Pay supplier");
            ReceivePaymentButton.Visibility = _combinedMode ? Visibility.Collapsed : Visibility.Visible;
            RoleFilterPanel.Visibility = _combinedMode ? Visibility.Visible : Visibility.Collapsed;
            HintText.Text = UiText.T("انقر مرتين على أي صف لعرض كشف الحساب التفصيلي.", "Double-click a row to open its detailed statement.");
            PartyTypeColumn.Visibility = _combinedMode ? Visibility.Visible : Visibility.Collapsed;
            PartyTypeColumn.Header = UiText.T("النوع", "Party type");
            NameColumn.Header = _combinedMode ? UiText.T("الاسم", "Name") : isCustomer ? UiText.T("اسم العميل", "Customer name") : UiText.T("اسم المورد", "Supplier name");
            PhoneColumn.Header = UiText.T("الهاتف", "Phone");
            DebitColumn.Header = UiText.T("إجمالي المدين", "Total debit");
            CreditColumn.Header = UiText.T("إجمالي الدائن", "Total credit");
            BalanceColumn.Header = UiText.T("الرصيد المستحق", "Outstanding balance");
            LastMovementColumn.Header = UiText.T("آخر حركة", "Last movement");
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e) => await GenerateAsync();

        private async System.Threading.Tasks.Task GenerateAsync()
        {
            try
            {
                if (AsOfDatePicker.SelectedDate == null)
                {
                    MessageBox.Show(UiText.T("يرجى اختيار التاريخ.", "Please select a date."));
                    return;
                }
                _loadingService.Show();
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                var roleFilter = RoleFilterComboBox.SelectedValue?.ToString() ?? "all";
                var filter = new PartyBalanceFilterDto
                {
                    Role = roleFilter == "supplier" ? UserRole.Supplier : _role,
                    AsOfDate = AsOfDatePicker.SelectedDate.Value,
                    Search = SearchTextBox.Text,
                    OutstandingOnly = OutstandingOnlyCheckBox.IsChecked == true,
                    BalanceFilter = BalanceFilterComboBox.SelectedValue?.ToString() ?? "all"
                };
                var result = _combinedMode && roleFilter == "all"
                    ? await _reportService.GetCombinedAsync(filter)
                    : await _reportService.GetAsync(filter);
                if (!result.Success || result.Data == null)
                {
                    MessageBox.Show(result.Message ?? UiText.T("تعذر تحميل التقرير.", "Failed to load the report."));
                    return;
                }
                var rows = result.Data.Rows;
                var totalOutstanding = result.Data.TotalOutstanding;
                var outstandingCount = result.Data.OutstandingCount;
                ReportGrid.ItemsSource = rows;
                TotalOutstandingText.Text = totalOutstanding.ToString("N5");
                CountText.Text = string.Format(UiText.T("عدد الحسابات المستحقة: {0}", "Outstanding accounts: {0}"), outstandingCount);
                ReportGrid.UpdateLayout();
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحميل التقرير", "An error occurred while loading the report")}: {ex.Message}",
                    UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new PrintDialog();
                if (dialog.ShowDialog() == true)
                    dialog.PrintVisual(ReportGrid, Title);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, UiText.T("خطأ في الطباعة", "Print error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var rows = (ReportGrid.ItemsSource as IEnumerable<PartyBalanceRowDto>)?.ToList()
                    ?? new List<PartyBalanceRowDto>();
                if (rows.Count == 0)
                {
                    MessageBox.Show(
                        UiText.T("\u0644\u0627 \u062a\u0648\u062c\u062f \u0628\u064a\u0627\u0646\u0627\u062a \u0644\u062a\u0635\u062f\u064a\u0631\u0647\u0627.", "There is no data to export."),
                        UiText.T("\u062a\u0635\u062f\u064a\u0631 Excel", "Export Excel"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    FileName = $"{(_combinedMode ? "AccountsBalances" : _role == UserRole.Customer ? "CustomerBalances" : "SupplierBalances")}_{DateTime.Today:yyyyMMdd}.xlsx",
                    AddExtension = true,
                    OverwritePrompt = true
                };

                if (dialog.ShowDialog() != true)
                    return;

                var isCustomer = _role == UserRole.Customer;
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add(_combinedMode ? "Party Balances" : isCustomer ? "Customer Balances" : "Supplier Balances");
                worksheet.Cell(1, 1).Value = _combinedMode ? "Accounts balances" : isCustomer ? "Customer balances" : "Supplier balances";
                worksheet.Cell(2, 1).Value = "As of date";
                worksheet.Cell(2, 2).Value = AsOfDatePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? string.Empty;
                worksheet.Cell(3, 1).Value = "Total outstanding";
                worksheet.Cell(3, 2).Value = _combinedMode
                    ? rows.Where(x => x.Balance != 0m).Sum(x => Math.Abs(x.Balance))
                    : rows.Where(x => x.Role == UserRole.Supplier ? x.Balance > 0m : x.Balance < 0m).Sum(x => x.Role == UserRole.Supplier ? x.Balance : -x.Balance);

                const int headerRow = 5;
                var headers = _combinedMode
                    ? new[] { "Party type", "Name", "Phone", "Total debit", "Total credit", "Outstanding balance", "Last movement" }
                    : new[] { "Name", "Phone", "Total debit", "Total credit", "Outstanding balance", "Last movement" };
                for (var column = 0; column < headers.Length; column++)
                    worksheet.Cell(headerRow, column + 1).Value = headers[column];

                for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                {
                    var row = rows[rowIndex];
                    var excelRow = headerRow + rowIndex + 1;
                    var offset = _combinedMode ? 1 : 0;
                    if (_combinedMode && (RoleFilterComboBox.SelectedValue?.ToString() ?? "all") == "all")
                        worksheet.Cell(excelRow, 1).Value = row.RoleLabel;
                    worksheet.Cell(excelRow, 1 + offset).Value = row.Name ?? string.Empty;
                    worksheet.Cell(excelRow, 2 + offset).Value = row.PhoneNumber ?? string.Empty;
                    worksheet.Cell(excelRow, 3 + offset).Value = row.TotalDebit;
                    worksheet.Cell(excelRow, 4 + offset).Value = row.TotalCredit;
                    worksheet.Cell(excelRow, 5 + offset).Value = row.Balance;
                    if (row.LastMovementDate.HasValue)
                        worksheet.Cell(excelRow, 6 + offset).Value = row.LastMovementDate.Value;
                }

                var header = worksheet.Range(headerRow, 1, headerRow, headers.Length);
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F766E");
                header.Style.Font.FontColor = XLColor.White;
                var numericStart = _combinedMode ? 4 : 3;
                worksheet.Columns(numericStart, numericStart + 2).Style.NumberFormat.Format = "#,##0.00000";
                worksheet.Column(_combinedMode ? 7 : 6).Style.DateFormat.Format = "yyyy-mm-dd";
                worksheet.Columns().AdjustToContents();
                worksheet.SheetView.FreezeRows(headerRow);
                workbook.SaveAs(dialog.FileName);

                MessageBox.Show(
                    UiText.T("\u062a\u0645 \u062a\u0635\u062f\u064a\u0631 \u0627\u0644\u062a\u0642\u0631\u064a\u0631 \u0628\u0646\u062c\u0627\u062d.", "The report was exported successfully."),
                    UiText.T("\u062a\u0635\u062f\u064a\u0631 Excel", "Export Excel"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("\u062d\u062f\u062b \u062e\u0637\u0623 \u0623\u062b\u0646\u0627\u0621 \u0627\u0644\u062a\u0635\u062f\u064a\u0631", "An error occurred while exporting")}: {ex.Message}",
                    UiText.T("\u062e\u0637\u0623", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ReportGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ReportGrid.SelectedItem is PartyBalanceRowDto row)
                WindowManager.ShowDialog<UserStatementWindow>(WindowSizeType.LargeRectangle, window =>
                {
                    if (_combinedMode)
                        window.InitializeCombined(row.UserId);
                    else
                        window.Initialize(row.UserId, _role);
                });
        }

        private void ReportGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ReceivePaymentButton.IsEnabled = ReportGrid.SelectedItem is PartyBalanceRowDto row && (_role == UserRole.Customer ? row.Balance < 0m : row.Balance > 0m);
        }

        private async void ReceivePaymentButton_Click(object sender, RoutedEventArgs e)
        {
            if (ReportGrid.SelectedItem is not PartyBalanceRowDto row || (_role == UserRole.Customer ? row.Balance >= 0m : row.Balance <= 0m))
            {
                MessageBox.Show(_role == UserRole.Customer
                        ? UiText.T("يرجى اختيار عميل لديه رصيد مستحق.", "Please select a customer with an outstanding balance.")
                        : UiText.T("يرجى اختيار مورد لديه رصيد مستحق.", "Please select a supplier with an outstanding balance."),
                    UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_role == UserRole.Customer)
            {
                WindowManager.ShowDialog<CreateVoucher>(WindowSizeType.MediumRectangle,
                    window => window.InitializeCustomerPayment(row.UserId, -row.Balance));
            }
            else
            {
                WindowManager.ShowDialog<PaymentVoucher>(WindowSizeType.MediumRectangle,
                    window => window.InitializeSupplierPayment(row.UserId, row.Balance));
            }
            await GenerateAsync();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
