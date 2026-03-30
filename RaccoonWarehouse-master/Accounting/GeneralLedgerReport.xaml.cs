using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Accounting.Accounts.DTOs;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse.Accounting
{
    public partial class GeneralLedgerReport : Window
    {
        private readonly IAccountingService _accountingService;
        private readonly IAccountingFeatureService _featureService;
        private readonly ILoadingService _loadingService;

        public ObservableCollection<AccountReadDto> Accounts { get; } = new();

        public GeneralLedgerReport(IAccountingService accountingService, IAccountingFeatureService featureService, ILoadingService loadingService)
        {
            _accountingService = accountingService;
            _featureService = featureService;
            _loadingService = loadingService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            Loaded += GeneralLedgerReport_Loaded;
        }

        private async void GeneralLedgerReport_Loaded(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ToDatePicker.SelectedDate = DateTime.Today;
            await LoadAccountsAsync();
        }

        private async Task LoadAccountsAsync()
        {
            var result = await _accountingService.GetAccountsAsync(activeOnly: true);
            if (!result.Success)
            {
                MessageBox.Show(result.Message ?? UiText.T("فشل تحميل الحسابات.", "Failed to load accounts."));
                return;
            }

            Accounts.Clear();
            foreach (var account in result.Data.Where(x => x.IsPosting))
                Accounts.Add(account);
        }

        private async void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!await _featureService.IsEnabledAsync())
                {
                    MessageBox.Show(UiText.T("نظام المحاسبة متوقف حالياً.", "The accounting system is currently disabled."));
                    Close();
                    return;
                }

                if (FromDatePicker.SelectedDate == null || ToDatePicker.SelectedDate == null)
                {
                    MessageBox.Show(UiText.T("يرجى اختيار الفترة.", "Please choose the period."));
                    return;
                }

                _loadingService.Show();
                var accountId = AccountComboBox.SelectedValue is int selectedAccountId ? selectedAccountId : (int?)null;
                var result = await _accountingService.GetGeneralLedgerAsync(new GeneralLedgerFilterDto
                {
                    From = FromDatePicker.SelectedDate.Value.Date,
                    To = ToDatePicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1),
                    AccountId = accountId
                });

                if (!result.Success)
                {
                    MessageBox.Show(result.Message ?? UiText.T("فشل تحميل دفتر الأستاذ.", "Failed to load the general ledger."));
                    return;
                }

                var ledger = result.Data.FirstOrDefault();
                HeaderText.Text = ledger == null
                    ? UiText.T("لا توجد حركة ضمن الفترة المحددة.", "There are no entries in the selected period.")
                    : $"{ledger.AccountCode} - {ledger.AccountName}";
                OpeningText.Text = ledger?.OpeningBalance.ToString("N2") ?? "0.00";
                ClosingText.Text = ledger?.ClosingBalance.ToString("N2") ?? "0.00";
                LedgerGrid.ItemsSource = ledger?.Rows;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحميل دفتر الأستاذ", "An error occurred while loading the general ledger")}: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
