using RaccoonWarehouse.Application.Service.Employees;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Domain.Employees.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace RaccoonWarehouse.Employees
{
    public partial class EmployeesTable : Window
    {
        private readonly IEmployeeService _employeeService;
        private readonly IEmployeeFeatureService _featureService;
        private readonly List<EmployeeReadDto> _items = new();
        private ICollectionView? _view;

        public EmployeesTable(IEmployeeService employeeService, IEmployeeFeatureService featureService)
        {
            _employeeService = employeeService;
            _featureService = featureService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            Loaded += EmployeesTable_Loaded;
        }

        private async void EmployeesTable_Loaded(object sender, RoutedEventArgs e)
        {
            StatusFilter.ItemsSource = new object[] { UiText.T("الكل", "All") }.Concat(Enum.GetValues(typeof(EmployeeStatus)).Cast<object>());
            StatusFilter.SelectedIndex = 0;
            await LoadEmployeesAsync();
        }

        private async Task LoadEmployeesAsync()
        {
            var enabled = await _featureService.IsEnabledAsync();
            if (!enabled)
            {
                MessageBox.Show(UiText.T("نظام الموظفين غير مفعل حالياً.", "The employees system is currently disabled."));
                Close();
                return;
            }

            FeatureStateText.Text = UiText.T("النظام مفعل حالياً ويمكن إدارة بيانات الموظفين.", "The system is currently enabled and employee records can be managed.");
            HintText.Text = UiText.T("يمكن البحث بالاسم أو الكود أو الهاتف أو البريد.", "You can search by name, code, phone, or email.");
            CreateEmployeeBtn.IsEnabled = true;

            var result = await _employeeService.GetListAsync();
            _items.Clear();
            if (result.Data != null)
                _items.AddRange(result.Data);

            TotalEmployeesText.Text = _items.Count.ToString();
            _view = CollectionViewSource.GetDefaultView(_items);
            _view.Filter = ApplyFilters;
            EmployeesGrid.ItemsSource = _view;
        }

        private bool ApplyFilters(object item)
        {
            if (item is not EmployeeReadDto dto)
                return false;

            var search = SearchBox.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var matched = dto.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || dto.Code.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (dto.PhoneNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (dto.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);

                if (!matched)
                    return false;
            }

            if (StatusFilter.SelectedItem is EmployeeStatus status && dto.Status != status)
                return false;

            if (int.TryParse(BranchFilterTextBox.Text, out var branchId) && dto.BranchId != branchId)
                return false;

            if (int.TryParse(DepartmentFilterTextBox.Text, out var departmentId) && dto.DepartmentId != departmentId)
                return false;

            return true;
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            _view?.Refresh();
        }

        private async void CreateEmployeeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!await _featureService.IsEnabledAsync())
            {
                MessageBox.Show(UiText.T("نظام الموظفين غير مفعل.", "The employees system is disabled."));
                return;
            }

            WindowManager.ShowDialog<CreateEmployee>(WindowSizeType.MediumRectangle);
            await LoadEmployeesAsync();
        }

        private EmployeeReadDto? GetSelectedEmployee()
        {
            if (EmployeesGrid.SelectedItem is EmployeeReadDto dto)
                return dto;

            MessageBox.Show(UiText.T("يرجى اختيار موظف أولاً.", "Please select an employee first."));
            return null;
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedEmployee();
            if (selected == null)
                return;

            WindowManager.ShowDialog<UpdateEmployee>(WindowSizeType.MediumRectangle, window => window.Initialize(selected.Id));
            await LoadEmployeesAsync();
        }

        private async void Details_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedEmployee();
            if (selected == null)
                return;

            WindowManager.ShowDialog<EmployeeDetails>(WindowSizeType.MediumRectangle, window => window.Initialize(selected.Id));
            await LoadEmployeesAsync();
        }

        private async void Activate_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedEmployee();
            if (selected == null)
                return;

            await _employeeService.SetStatusAsync(selected.Id, EmployeeStatus.Active);
            await LoadEmployeesAsync();
        }

        private async void Deactivate_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedEmployee();
            if (selected == null)
                return;

            await _employeeService.SetStatusAsync(selected.Id, EmployeeStatus.Inactive);
            await LoadEmployeesAsync();
        }

        private async void Suspend_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedEmployee();
            if (selected == null)
                return;

            await _employeeService.SetStatusAsync(selected.Id, EmployeeStatus.Suspended);
            await LoadEmployeesAsync();
        }

        private async void FeatureSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowManager.ShowDialog<EmployeeFeatureSettingsWindow>(WindowSizeType.SmallSquare);
            await LoadEmployeesAsync();
        }

        private void EmployeesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Details_Click(sender, e);
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
