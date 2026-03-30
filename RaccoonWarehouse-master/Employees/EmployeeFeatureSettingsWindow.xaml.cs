using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;

namespace RaccoonWarehouse.Employees
{
    public partial class EmployeeFeatureSettingsWindow : Window
    {
        private readonly IEmployeeFeatureService _featureService;

        public EmployeeFeatureSettingsWindow(IEmployeeFeatureService featureService)
        {
            _featureService = featureService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            Loaded += EmployeeFeatureSettingsWindow_Loaded;
        }

        private async void EmployeeFeatureSettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            EnabledCheckBox.IsChecked = await _featureService.IsEnabledAsync();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var result = await _featureService.SetEnabledAsync(EnabledCheckBox.IsChecked == true);
            if (!result.Success)
            {
                MessageBox.Show(result.Message ?? UiText.T("تعذر حفظ الإعداد.", "Failed to save the setting."));
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
