using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;

namespace RaccoonWarehouse.Accounting
{
    public partial class AccountingFeatureSettingsWindow : Window
    {
        private readonly IAccountingService _accountingService;
        private readonly IAccountingFeatureService _featureService;

        public AccountingFeatureSettingsWindow(IAccountingService accountingService, IAccountingFeatureService featureService)
        {
            _accountingService = accountingService;
            _featureService = featureService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            Loaded += AccountingFeatureSettingsWindow_Loaded;
        }

        private async void AccountingFeatureSettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            EnabledCheckBox.IsChecked = await _featureService.IsEnabledAsync();
            PostingLockDatePicker.SelectedDate = await _accountingService.GetPostingLockDateAsync();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var result = await _featureService.SetEnabledAsync(EnabledCheckBox.IsChecked == true);
            if (!result.Success)
            {
                MessageBox.Show(result.Message ?? UiText.T("تعذر حفظ إعدادات نظام المحاسبة.", "Failed to save accounting settings."));
                return;
            }

            var lockDateResult = await _accountingService.SetPostingLockDateAsync(PostingLockDatePicker.SelectedDate);
            if (!lockDateResult.Success)
            {
                MessageBox.Show(lockDateResult.Message ?? UiText.T("تعذر حفظ تاريخ قفل الترحيل.", "Failed to save the posting lock date."));
                return;
            }

            Close();
        }

        private void ClearPostingLockDate_Click(object sender, RoutedEventArgs e)
        {
            PostingLockDatePicker.SelectedDate = null;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
