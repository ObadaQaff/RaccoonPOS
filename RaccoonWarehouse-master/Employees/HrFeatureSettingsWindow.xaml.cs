using RaccoonWarehouse.Application.Service.Modules;
using RaccoonWarehouse.Core.Modules;
using System.Windows;
namespace RaccoonWarehouse.Employees;
public partial class HrFeatureSettingsWindow : Window
{
    private readonly IModuleStateStore _modules; public HrFeatureSettingsWindow(IModuleStateStore modules) { _modules = modules; InitializeComponent(); Loaded += (_, _) => Enabled.IsChecked = _modules.IsEnabled(ModuleStateStore.HrModuleKey); }
    private async void Save_Click(object sender, RoutedEventArgs e) { try { await _modules.SetEnabledAsync(ModuleStateStore.HrModuleKey, Enabled.IsChecked == true); MessageBox.Show("تم حفظ إعدادات الموارد البشرية."); Close(); } catch (Exception ex) { MessageBox.Show(ex.Message); } }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
