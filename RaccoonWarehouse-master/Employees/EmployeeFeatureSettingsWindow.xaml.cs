using RaccoonWarehouse.Application.Service.Modules;
using RaccoonWarehouse.Core.Modules;
using System.Windows;

namespace RaccoonWarehouse.Employees;

public partial class EmployeeFeatureSettingsWindow : Window
{
    private readonly IModuleStateStore _modules;
    public EmployeeFeatureSettingsWindow(IModuleStateStore modules) { _modules = modules; InitializeComponent(); Loaded += (_, _) => Enabled.IsChecked = _modules.IsEnabled(ModuleStateStore.EmployeesModuleKey); }
    private async void Save_Click(object sender, RoutedEventArgs e) { await _modules.SetEnabledAsync(ModuleStateStore.EmployeesModuleKey, Enabled.IsChecked == true); MessageBox.Show("تم حفظ إعدادات الموظفين. أعد فتح لوحة التحكم لتحديث القوائم."); Close(); }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
