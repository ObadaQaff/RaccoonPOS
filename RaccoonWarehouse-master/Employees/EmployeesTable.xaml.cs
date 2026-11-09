using RaccoonWarehouse.Application.Service.Employees;
using RaccoonWarehouse.Domain.Employees.DTOs;
using RaccoonWarehouse.Navigation;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RaccoonWarehouse.Employees;

public partial class EmployeesTable : Window
{
    private readonly IEmployeeService _service;
    private readonly ObservableCollection<EmployeeListItemDto> _items = new();
    public EmployeesTable(IEmployeeService service) { _service = service; InitializeComponent(); EmployeesGrid.ItemsSource = _items; }
    private async void Window_Loaded(object sender, RoutedEventArgs e) => await LoadAsync();
    private async Task LoadAsync()
    {
        try { var data = await _service.SearchAsync(SearchBox.Text, IncludeInactive.IsChecked == true); _items.Clear(); foreach (var item in data) _items.Add(item); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "تعذر تحميل الموظفين"); }
    }
    private async void Search_Click(object sender, RoutedEventArgs e) => await LoadAsync();
    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e) { if (IsLoaded) await LoadAsync(); }
    private async void FilterChanged(object sender, RoutedEventArgs e) { if (IsLoaded) await LoadAsync(); }
    private async void Add_Click(object sender, RoutedEventArgs e) { await WindowManager.ShowDialogAsync<EmployeeProfileWindow>(WindowSizeType.LargeRectangle, w => w.InitializeAsync(null)); await LoadAsync(); }
    private async void Grid_DoubleClick(object sender, MouseButtonEventArgs e) { if (EmployeesGrid.SelectedItem is EmployeeListItemDto item) { await WindowManager.ShowDialogAsync<EmployeeProfileWindow>(WindowSizeType.LargeRectangle, w => w.InitializeAsync(item.Id)); await LoadAsync(); } }
    private void Shifts_Click(object sender, RoutedEventArgs e) => WindowManager.Show<ShiftManagementWindow>(WindowSizeType.LargeRectangle);
}
