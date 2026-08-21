using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Reports.Accounting.Dtos;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Vouchers;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RaccoonWarehouse.Accounting
{
    public partial class PartyBalanceReport : Window
    {
        private readonly PartyBalanceReportService _reportService;
        private readonly ILoadingService _loadingService;
        private UserRole _role = UserRole.Customer;
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
            _role = role;
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
            Title = isCustomer ? UiText.T("ذمم العملاء", "Customer Debts") : UiText.T("ذمم الموردين", "Supplier Payables");
            TitleText.Text = Title;
            SubtitleText.Text = isCustomer
                ? UiText.T("الأرصدة المستحقة على العملاء حتى التاريخ المحدد.", "Customer balances owed to us through the selected date.")
                : UiText.T("الأرصدة المستحقة للموردين حتى التاريخ المحدد.", "Supplier balances owed by us through the selected date.");
            TotalLabelText.Text = UiText.T("إجمالي المستحق", "Total outstanding");
            AsOfLabelText.Text = UiText.T("حتى تاريخ", "As of date");
            SearchLabelText.Text = UiText.T("بحث بالاسم أو الهاتف", "Search by name or phone");
            OutstandingOnlyCheckBox.Content = UiText.T("المستحق فقط", "Outstanding only");
            GenerateButton.Content = UiText.T("عرض التقرير", "Generate report");
            PrintButton.Content = UiText.T("طباعة", "Print");
            CloseButton.Content = UiText.T("إغلاق", "Close");
            ReceivePaymentButton.Content = isCustomer
                ? UiText.T("تحصيل دفعة", "Receive payment")
                : UiText.T("دفع للمورد", "Pay supplier");
            ReceivePaymentButton.Visibility = Visibility.Visible;
            HintText.Text = UiText.T("انقر مرتين على أي صف لعرض كشف الحساب التفصيلي.", "Double-click a row to open its detailed statement.");
            NameColumn.Header = isCustomer ? UiText.T("اسم العميل", "Customer name") : UiText.T("اسم المورد", "Supplier name");
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
                var result = await _reportService.GetAsync(new PartyBalanceFilterDto
                {
                    Role = _role,
                    AsOfDate = AsOfDatePicker.SelectedDate.Value,
                    Search = SearchTextBox.Text,
                    OutstandingOnly = OutstandingOnlyCheckBox.IsChecked == true
                });
                if (!result.Success || result.Data == null)
                {
                    MessageBox.Show(result.Message ?? UiText.T("تعذر تحميل التقرير.", "Failed to load the report."));
                    return;
                }
                ReportGrid.ItemsSource = result.Data.Rows;
                TotalOutstandingText.Text = result.Data.TotalOutstanding.ToString("N2");
                CountText.Text = string.Format(UiText.T("عدد الحسابات المستحقة: {0}", "Outstanding accounts: {0}"), result.Data.OutstandingCount);
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

        private void ReportGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ReportGrid.SelectedItem is PartyBalanceRowDto row)
                WindowManager.ShowDialog<UserStatementWindow>(WindowSizeType.LargeRectangle, window => window.Initialize(row.UserId, _role));
        }

        private void ReportGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ReceivePaymentButton.IsEnabled = ReportGrid.SelectedItem is PartyBalanceRowDto row && row.Balance > 0;
        }

        private async void ReceivePaymentButton_Click(object sender, RoutedEventArgs e)
        {
            if (ReportGrid.SelectedItem is not PartyBalanceRowDto row || row.Balance <= 0)
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
                    window => window.InitializeCustomerPayment(row.UserId, row.Balance));
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
