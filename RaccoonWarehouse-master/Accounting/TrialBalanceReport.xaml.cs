using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Windows;

namespace RaccoonWarehouse.Accounting
{
    public partial class TrialBalanceReport : Window
    {
        private readonly IAccountingService _accountingService;
        private readonly IAccountingFeatureService _featureService;
        private readonly ILoadingService _loadingService;

        public TrialBalanceReport(IAccountingService accountingService, IAccountingFeatureService featureService, ILoadingService loadingService)
        {
            _accountingService = accountingService;
            _featureService = featureService;
            _loadingService = loadingService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            Loaded += TrialBalanceReport_Loaded;
        }

        private void TrialBalanceReport_Loaded(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ToDatePicker.SelectedDate = DateTime.Today;
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
                var result = await _accountingService.GetTrialBalanceAsync(new TrialBalanceFilterDto
                {
                    From = FromDatePicker.SelectedDate.Value.Date,
                    To = ToDatePicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1),
                    IncludeZeroBalances = IncludeZeroCheckBox.IsChecked == true
                });

                if (!result.Success)
                {
                    MessageBox.Show(result.Message ?? UiText.T("فشل تحميل ميزان المراجعة.", "Failed to load the trial balance."));
                    return;
                }

                TrialBalanceGrid.ItemsSource = result.Data.rows;
                TotalDebitText.Text = result.Data.summary.TotalClosingDebit.ToString("N2");
                TotalCreditText.Text = result.Data.summary.TotalClosingCredit.ToString("N2");
                BalancedText.Text = result.Data.summary.IsBalanced ? UiText.T("متوازن", "Balanced") : UiText.T("غير متوازن", "Unbalanced");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحميل ميزان المراجعة", "An error occurred while loading the trial balance")}: {ex.Message}");
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
