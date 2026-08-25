using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Windows;
using System.Windows.Threading;

namespace RaccoonWarehouse.Accounting
{
    public partial class BalanceSheetReport : Window
    {
        private readonly IAccountingService _accountingService;
        private readonly IAccountingFeatureService _featureService;
        private readonly ILoadingService _loadingService;

        public BalanceSheetReport(IAccountingService accountingService, IAccountingFeatureService featureService, ILoadingService loadingService)
        {
            _accountingService = accountingService;
            _featureService = featureService;
            _loadingService = loadingService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            Loaded += BalanceSheetReport_Loaded;
        }

        private void BalanceSheetReport_Loaded(object sender, RoutedEventArgs e)
        {
            AsOfDatePicker.SelectedDate = DateTime.Today;
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

                if (AsOfDatePicker.SelectedDate == null)
                {
                    MessageBox.Show(UiText.T("يرجى اختيار التاريخ.", "Please choose a date."));
                    return;
                }

                _loadingService.Show();
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                var result = await _accountingService.GetBalanceSheetAsync(new BalanceSheetFilterDto
                {
                    AsOfDate = AsOfDatePicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1),
                    IncludeZeroBalances = IncludeZeroCheckBox.IsChecked == true
                });

                if (!result.Success)
                {
                    MessageBox.Show(result.Message ?? UiText.T("فشل تحميل الميزانية العمومية.", "Failed to load the balance sheet."));
                    return;
                }

                AssetsGrid.ItemsSource = result.Data.Assets.Rows;
                LiabilitiesGrid.ItemsSource = result.Data.Liabilities.Rows;
                EquityGrid.ItemsSource = result.Data.Equity.Rows;
                AssetsTotalText.Text = result.Data.Assets.Total.ToString("N2");
                LiabilitiesTotalText.Text = result.Data.Liabilities.Total.ToString("N2");
                EquityTotalText.Text = result.Data.Equity.Total.ToString("N2");
                LiabilitiesAndEquityText.Text = result.Data.TotalLiabilitiesAndEquity.ToString("N2");
                AssetsGrid.UpdateLayout();
                LiabilitiesGrid.UpdateLayout();
                EquityGrid.UpdateLayout();
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحميل الميزانية العمومية", "An error occurred while loading the balance sheet")}: {ex.Message}");
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
