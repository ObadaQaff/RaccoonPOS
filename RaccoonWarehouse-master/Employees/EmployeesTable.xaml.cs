using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Employees;
using RaccoonWarehouse.Application.Service.Payroll;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Application.Service.Audit;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.POS;
using RaccoonWarehouse.Settings;
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
        private sealed record RoleFilterOption(UserRole? Role, string DisplayName);

        private readonly IUserService _userService;
        private readonly IEmployeeFeatureService _featureService;
        private readonly IUserSession _userSession;
        private readonly IPermissionService _permissionService;
        private readonly ILoadingService _loadingService;
        private readonly IPermissionService _auditPermissionService;
        private readonly IEmployeeService _employeeService;
        private readonly EmployeePayrollService _employeePayrollService;
        private readonly PayrollCalculator _payrollCalculator;
        private readonly List<UserReadDto> _items = new();
        private ICollectionView? _view;
        private bool _loaded;

        public EmployeesTable(
            IUserService userService,
            IEmployeeFeatureService featureService,
            IUserSession userSession,
            IPermissionService permissionService,
            ILoadingService loadingService,
            IEmployeeService employeeService,
            EmployeePayrollService employeePayrollService,
            PayrollCalculator payrollCalculator)
        {
            _userService = userService;
            _featureService = featureService;
            _userSession = userSession;
            _permissionService = permissionService;
            _auditPermissionService = permissionService;
            _loadingService = loadingService;
            _employeeService = employeeService;
            _employeePayrollService = employeePayrollService;
            _payrollCalculator = payrollCalculator;
            RaccoonWarehouse.Security.PermissionWindowGuard.Demand(GetType());
            InitializeComponent();
            UiText.ApplyWindow(this);
            InitializeRoleFilter();
            Loaded += EmployeesTable_Loaded;
        }

        private async void PayrollProfiles_Click(object sender,RoutedEventArgs e)
        {
            try
            {
                if(!await _permissionService.HasCurrentUserPermissionAsync("Employees.View"))throw new UnauthorizedAccessException("ليس لديك صلاحية عرض ملفات الموظفين.");
                if(!await ConfigurePayrollWorkspaceAsync())throw new UnauthorizedAccessException("ليس لديك صلاحية عرض ملفات الموظفين.");
                EmployeeMainTabs.SelectedItem = PayrollWorkspaceTab;
            }
            catch(Exception ex){RaccoonWarehouse.Employees.Payroll.PayrollUi.Error(ex);}
        }

        private async Task<bool> ConfigurePayrollWorkspaceAsync()
        {
            var allowed = await _permissionService.HasCurrentUserPermissionAsync("Employees.View");
            LegacyEmployeeToolbarCard.Visibility = Visibility.Collapsed;
            PayrollProfilesBtn.Visibility = Visibility.Collapsed;
            EmployeeAccountsTab.Visibility = Visibility.Collapsed;
            PayrollWorkspaceTab.Visibility = allowed ? Visibility.Visible : Visibility.Collapsed;
            if (!allowed)
                return false;

            if (PayrollWorkspaceHost.Content == null)
            {
                PayrollWorkspaceHost.Content = new RaccoonWarehouse.Employees.Payroll.EmployeePayrollWorkspaceControl(
                    _employeeService,
                    _userService,
                    _employeePayrollService,
                    _permissionService,
                    _payrollCalculator);
            }

            EmployeeMainTabs.SelectedItem = PayrollWorkspaceTab;

            return true;
        }

        private void InitializeRoleFilter()
        {
            if (RoleFilterBox.Items.Count > 0)
                return;

            RoleFilterLabel.Text = UiText.T("الدور", "Role");
            RoleFilterBox.DisplayMemberPath = nameof(RoleFilterOption.DisplayName);
            RoleFilterBox.SelectedValuePath = nameof(RoleFilterOption.Role);
            RoleFilterBox.ItemsSource = new List<RoleFilterOption>
            {
                new(null, UiText.T("الكل", "All")),
                new(UserRole.Admin, UiText.T("مدير النظام", "Admin")),
                new(UserRole.Casher, UiText.T("كاشير", "Cashier")),
                new(UserRole.Manager, UiText.T("مدير", "Manager")),
                new(UserRole.HR, UiText.T("الموارد البشرية", "HR"))
            };
            RoleFilterBox.SelectedIndex = 0;
        }

        private async void EmployeesTable_Loaded(object sender, RoutedEventArgs e)
        {
            if (_loaded)
                return;

            _loaded = true;
            await LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            _loadingService.Show();
            try
            {
                var enabled = await _featureService.IsEnabledAsync();
                if (!enabled)
                {
                    MessageBox.Show(UiText.T("نظام الموظفين غير مفعل حالياً.", "The employees system is currently disabled."));
                    Close();
                    return;
                }

                if (!await ConfigurePayrollWorkspaceAsync())
                {
                    MessageBox.Show(UiText.T("ليس لديك صلاحية عرض الموظفين.", "You do not have permission to view employees."));
                    Close();
                    return;
                }

                EmployeeMainTabs.SelectedItem = PayrollWorkspaceTab;
                return;

                var role = _userSession.CurrentUser?.Role;
                if (!role.HasValue || !await _permissionService.HasPermissionAsync(role.Value, "Users.View"))
                {
                    MessageBox.Show(UiText.T("ليس لديك صلاحية عرض الموظفين.", "You do not have permission to view employees."));
                    Close();
                    return;
                }

                FeatureStateText.Text = UiText.T(
                    "النظام مفعل حالياً ويعرض الموظفين والكاشيرات والمدراء وموظفي الموارد البشرية.",
                    "The system is enabled and shows employees only.");
                AdminHintText.Text = UiText.T(
                    "يمكن البحث بالاسم أو الهاتف أو الدور المختار.",
                    "You can search by name, phone, or the selected role.");

                CreateUserBtn.IsEnabled = true;
                ActivityBtn.Visibility = await HasPermissionAsync("AuditLogs.View") ? Visibility.Visible : Visibility.Collapsed;
                ViewShiftsBtn.Visibility = await HasPermissionAsync("Reports.ShiftSummary") ? Visibility.Visible : Visibility.Collapsed;
                FeatureSettingsBtn.Visibility = await HasPermissionAsync("Settings.ManageSettings") ? Visibility.Visible : Visibility.Collapsed;

                var result = await _userService.GetAllAsync();
                _items.Clear();
                if (result.Data != null)
                {
                    _items.AddRange(result.Data.Where(IsStaffUser));
                }

                TotalUsersText.Text = _items.Count.ToString();
                _view = CollectionViewSource.GetDefaultView(_items);
                _view.Filter = ApplyFilters;
                UsersTable1.ItemsSource = _view;
                UpdateCounters();
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private bool ApplyFilters(object item)
        {
            if (item is not UserReadDto user)
                return false;

            if (RoleFilterBox.SelectedItem is RoleFilterOption selectedRole
                && selectedRole.Role.HasValue
                && user.Role != selectedRole.Role.Value)
            {
                return false;
            }

            var search = SearchBox.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var matched = user.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (user.PhoneNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                    || user.Role.ToString().Contains(search, StringComparison.OrdinalIgnoreCase);

                if (!matched)
                    return false;
            }

            return true;
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            _view?.Refresh();
            UpdateCounters();
        }

        private void RoleFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterChanged(sender, EventArgs.Empty);
        }

        private async void CreateUserBtn_Click(object sender, RoutedEventArgs e)
        {
            var role = _userSession.CurrentUser?.Role;
            if (!role.HasValue || !await _permissionService.HasPermissionAsync(role.Value, "Users.Create"))
            {
                MessageBox.Show(UiText.T("ليس لديك صلاحية إنشاء مستخدم جديد.", "You do not have permission to create a new user."));
                return;
            }

            WindowManager.ShowDialog<CreateUser>(WindowSizeType.MediumRectangle, window => window.InitializeForEmployeeCreate());
            await LoadUsersAsync();
        }

        private async void ViewShiftsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!await HasPermissionAsync("Reports.ShiftSummary"))
                return;

            var selected = GetSelectedUser();
            if (selected == null)
                return;

            if (selected.Role != UserRole.Casher && selected.Role != UserRole.Admin)
            {
                MessageBox.Show(
                    UiText.T("The selected user is not an admin or cashier.", "The selected user is not an admin or cashier."),
                    UiText.T("Notice", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            WindowManager.Show<DailySalesReport>(WindowSizeType.LargeRectangle, window => window.InitializeForCashier(selected.Id));
        }

        private async void ActivityBtn_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedUser();
            if (selected == null || !await HasPermissionAsync("AuditLogs.View"))
                return;

            WindowManager.Show<AuditLogsWindow>(WindowSizeType.LargeRectangle,
                window => window.InitializeForUser(selected.Id, selected.Name));
        }

        private async Task<bool> HasPermissionAsync(string permissionKey)
        {
            var role = _userSession.CurrentUser?.Role;
            return role.HasValue && await _auditPermissionService.HasPermissionAsync(role.Value, permissionKey);
        }

        private UserReadDto? GetSelectedUser()
        {
            if (UsersTable1.SelectedItem is UserReadDto dto)
                return dto;

            MessageBox.Show(UiText.T("يرجى اختيار موظف أولاً.", "Please select an employee first."));
            return null;
        }

        private async void Update_User(object sender, RoutedEventArgs e)
        {
            var role = _userSession.CurrentUser?.Role;
            if (!role.HasValue || !await _permissionService.HasPermissionAsync(role.Value, "Users.Edit"))
            {
                MessageBox.Show(UiText.T("ليس لديك صلاحية تعديل المستخدمين.", "You do not have permission to edit users."));
                return;
            }

            var selected = GetSelectedUser();
            if (selected == null)
                return;

            _loadingService.Show();
            WindowManager.ShowDialog<UpdateUser>(WindowSizeType.SmallSquare, window => window.Initialize(selected.Id));
            _loadingService.Hide();
            await LoadUsersAsync();
        }

        private async void Delete_User(object sender, RoutedEventArgs e)
        {
            var role = _userSession.CurrentUser?.Role;
            if (!role.HasValue || !await _permissionService.HasPermissionAsync(role.Value, "Users.Delete"))
            {
                MessageBox.Show(UiText.T("ليس لديك صلاحية حذف المستخدمين.", "You do not have permission to delete users."));
                return;
            }

            var selected = GetSelectedUser();
            if (selected == null)
                return;

            var messageResult = MessageBox.Show(
                UiText.IsEnglish
                    ? $"Are you sure you want to delete employee {selected.Name}?"
                    : $"هل أنت متأكد من حذف الموظف {selected.Name}؟",
                UiText.T("تأكيد الحذف", "Confirm Deletion"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (messageResult != MessageBoxResult.Yes)
                return;

            await _userService.DeleteAsync(selected.Id);
            await LoadUsersAsync();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _view?.Refresh();
            UpdateCounters();
        }

        private void UpdateCounters()
        {
            TotalUsersText.Text = _items.Count.ToString();
            VisibleUsersText.Text = _view?.Cast<object>().Count().ToString() ?? "0";
        }

        private async void FeatureSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!await HasPermissionAsync("Settings.ManageSettings"))
                return;

            WindowManager.ShowDialog<EmployeeFeatureSettingsWindow>(WindowSizeType.SmallSquare);
        }

        private void UsersTable1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (UsersTable1.SelectedItem is UserReadDto)
                Update_User(sender, e);
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static bool IsStaffUser(UserReadDto user)
        {
            return user.Role is UserRole.Admin or UserRole.Casher or UserRole.Manager or UserRole.HR;
        }
    }
}
