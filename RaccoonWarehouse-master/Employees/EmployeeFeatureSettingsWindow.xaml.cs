using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Application.Service.Modules;
using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;

namespace RaccoonWarehouse.Employees
{
    public partial class EmployeeFeatureSettingsWindow : Window
    {
        private readonly IEmployeeFeatureService _featureService;
        private readonly IModuleStateStore _moduleState;

        public EmployeeFeatureSettingsWindow(IEmployeeFeatureService featureService, IModuleStateStore moduleState)
        {
            _featureService = featureService;
            _moduleState = moduleState;

            InitializeComponent();
            UiText.ApplyWindow(this);
            Loaded += EmployeeFeatureSettingsWindow_Loaded;
        }

        private async void EmployeeFeatureSettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            EnabledCheckBox.IsChecked = await _featureService.IsEnabledAsync() && _moduleState.IsEnabled(ModuleStateStore.EmployeesModuleKey);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var enabled = EnabledCheckBox.IsChecked == true;
            try
            {
                await _moduleState.SetEnabledAsync(ModuleStateStore.EmployeesModuleKey, enabled);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            var result = await _featureService.SetEnabledAsync(enabled);
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
