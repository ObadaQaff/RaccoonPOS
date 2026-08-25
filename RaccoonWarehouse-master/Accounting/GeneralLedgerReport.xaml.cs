using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Accounting.Accounts.DTOs;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace RaccoonWarehouse.Accounting
{
    public partial class GeneralLedgerReport : Window
    {
        private readonly IAccountingService _accountingService;
        private readonly IAccountingFeatureService _featureService;
        private readonly ILoadingService _loadingService;
        private readonly SourceDocumentNavigationService _sourceDocumentNavigationService;
        private int? _initialAccountId;

        public ObservableCollection<AccountReadDto> Accounts { get; } = new();

        public GeneralLedgerReport(
            IAccountingService accountingService,
            IAccountingFeatureService featureService,
            ILoadingService loadingService,
            SourceDocumentNavigationService sourceDocumentNavigationService)
        {
            _accountingService = accountingService;
            _featureService = featureService;
            _loadingService = loadingService;
            _sourceDocumentNavigationService = sourceDocumentNavigationService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            Loaded += GeneralLedgerReport_Loaded;
        }

        public void OpenForAccount(int accountId)
        {
            _initialAccountId = accountId;
        }

        private async void GeneralLedgerReport_Loaded(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ToDatePicker.SelectedDate = DateTime.Today;
            await LoadAccountsAsync();
            await TryGenerateForInitialAccountAsync();
        }

        private async Task LoadAccountsAsync()
        {
            _loadingService.Show();
            try
            {
            var result = await _accountingService.GetAccountsAsync(activeOnly: true);
            if (!result.Success)
            {
                MessageBox.Show(result.Message ?? UiText.T("ÙØ´Ù„ ØªØ­Ù…ÙŠÙ„ Ø§Ù„Ø­Ø³Ø§Ø¨Ø§Øª.", "Failed to load accounts."));
                return;
            }

            Accounts.Clear();
            foreach (var account in result.Data.Where(x => x.IsPosting))
                Accounts.Add(account);
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private async void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            await LoadLedgerAsync();
        }

        private async Task TryGenerateForInitialAccountAsync()
        {
            if (!_initialAccountId.HasValue)
                return;

            AccountComboBox.SelectedValue = _initialAccountId.Value;
            _initialAccountId = null;
            await LoadLedgerAsync();
        }

        private async Task LoadLedgerAsync()
        {
            try
            {
                if (!await _featureService.IsEnabledAsync())
                {
                    MessageBox.Show(UiText.T("Ù†Ø¸Ø§Ù… Ø§Ù„Ù…Ø­Ø§Ø³Ø¨Ø© Ù…ØªÙˆÙ‚Ù Ø­Ø§Ù„ÙŠØ§Ù‹.", "The accounting system is currently disabled."));
                    Close();
                    return;
                }

                if (FromDatePicker.SelectedDate == null || ToDatePicker.SelectedDate == null)
                {
                    MessageBox.Show(UiText.T("ÙŠØ±Ø¬Ù‰ Ø§Ø®ØªÙŠØ§Ø± Ø§Ù„ÙØªØ±Ø©.", "Please choose the period."));
                    return;
                }

                _loadingService.Show();
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                var accountId = AccountComboBox.SelectedValue is int selectedAccountId ? selectedAccountId : (int?)null;
                var result = await _accountingService.GetGeneralLedgerAsync(new GeneralLedgerFilterDto
                {
                    From = FromDatePicker.SelectedDate.Value.Date,
                    To = ToDatePicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1),
                    AccountId = accountId
                });

                if (!result.Success)
                {
                    MessageBox.Show(result.Message ?? UiText.T("ÙØ´Ù„ ØªØ­Ù…ÙŠÙ„ Ø¯ÙØªØ± Ø§Ù„Ø£Ø³ØªØ§Ø°.", "Failed to load the general ledger."));
                    return;
                }

                var ledger = result.Data.FirstOrDefault();
                HeaderText.Text = ledger == null
                    ? UiText.T("Ù„Ø§ ØªÙˆØ¬Ø¯ Ø­Ø±ÙƒØ© Ø¶Ù…Ù† Ø§Ù„ÙØªØ±Ø© Ø§Ù„Ù…Ø­Ø¯Ø¯Ø©.", "There are no entries in the selected period.")
                    : $"{ledger.AccountCode} - {ledger.AccountName}";
                OpeningText.Text = ledger?.OpeningBalance.ToString("N2") ?? "0.00";
                ClosingText.Text = ledger?.ClosingBalance.ToString("N2") ?? "0.00";
                LedgerGrid.ItemsSource = ledger?.Rows;
                LedgerGrid.UpdateLayout();
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("Ø­Ø¯Ø« Ø®Ø·Ø£ Ø£Ø«Ù†Ø§Ø¡ ØªØ­Ù…ÙŠÙ„ Ø¯ÙØªØ± Ø§Ù„Ø£Ø³ØªØ§Ø°", "An error occurred while loading the general ledger")}: {ex.Message}");
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

        private async void LedgerGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (LedgerGrid.SelectedItem is not Domain.Reports.Accounting.Dtos.GeneralLedgerRowDto row)
                return;

            if (string.IsNullOrWhiteSpace(row.ReferenceType) || !row.ReferenceId.HasValue)
                return;

            try
            {
                await _sourceDocumentNavigationService.OpenSourceDocument(row.ReferenceType, row.ReferenceId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
