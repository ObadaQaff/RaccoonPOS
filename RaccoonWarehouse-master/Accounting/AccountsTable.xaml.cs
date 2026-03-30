using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse.Accounting
{
    public partial class AccountsTable : Window
    {
        private readonly IAccountingService _accountingService;
        private readonly IAccountingFeatureService _featureService;
        private readonly ILoadingService _loadingService;

        public AccountsTable(IAccountingService accountingService, IAccountingFeatureService featureService, ILoadingService loadingService)
        {
            _accountingService = accountingService;
            _featureService = featureService;
            _loadingService = loadingService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            _ = LoadAccountsAsync();
        }

        private async Task LoadAccountsAsync()
        {
            try
            {
                if (!await _featureService.IsEnabledAsync())
                {
                    MessageBox.Show(UiText.T("نظام المحاسبة متوقف حالياً.", "The accounting system is currently disabled."));
                    Close();
                    return;
                }

                _loadingService.Show();
                var result = await _accountingService.GetAccountsAsync(activeOnly: false);
                if (!result.Success)
                {
                    MessageBox.Show(result.Message ?? UiText.T("فشل تحميل الحسابات.", "Failed to load accounts."));
                    return;
                }

                AccountsGrid.ItemsSource = result.Data;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ غير متوقع أثناء تحميل الحسابات", "An unexpected error occurred while loading accounts")}: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            await LoadAccountsAsync();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
