using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using RaccoonWarehouse.Application.Service.Employees;
using RaccoonWarehouse.Application.Service.Payroll;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Employees.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.POS;

namespace RaccoonWarehouse.Employees.Payroll;

public sealed class EmployeePayrollWorkspaceControl : UserControl
{
    private sealed class EmployeeUserRow
    {
        public UserReadDto User { get; init; } = null!;
        public EmployeeReadDto? Profile { get; init; }
        public int UserId => User.Id;
        public string Name => User.Name;
        public string PhoneNumber => User.PhoneNumber ?? string.Empty;
        public string Role => RoleText(User.Role);
        public string EmployeeCode => Profile?.Code ?? string.Empty;
        public string JobTitle => Profile?.JobTitle ?? string.Empty;
        public string ProfileStatus => Profile == null
            ? UiText.T("ملف غير منشأ", "Profile missing")
            : UiText.T("ملف مرتبط", "Profile linked");
    }

    private static readonly UserRole[] StaffRoles =
    {
        UserRole.Admin,
        UserRole.Casher,
        UserRole.Manager,
        UserRole.HR
    };

    private readonly IEmployeeService _employees;
    private readonly IUserService _users;
    private readonly EmployeePayrollService _payroll;
    private readonly TextBox _search = new() { MinWidth = 300, Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 10) };
    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, CanUserAddRows = false, IsReadOnly = true, SelectionMode = DataGridSelectionMode.Single };
    private readonly Button _profileButton;
    private readonly Button _editButton;
    private readonly Button _payrollButton;
    private readonly Button _policyButton;
    private readonly Button _userButton;
    private readonly Button _settingsButton;
    private readonly Button _shiftsButton;
    private readonly TextBlock _emptyState = new() { TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(24), FontSize = 16, Visibility = Visibility.Collapsed };
    private List<EmployeeUserRow> _rows = new();
    private bool _initialized;

    public EmployeePayrollWorkspaceControl(
        IEmployeeService employees,
        IUserService users,
        EmployeePayrollService payroll,
        PayrollCalculator calculator)
    {
        (_employees, _users, _payroll) = (employees, users, payroll);

        FlowDirection = FlowDirection.RightToLeft;
        var root = new DockPanel { Margin = new Thickness(14) };

        var tools = new WrapPanel();
        tools.Children.Add(PayrollUi.AsyncButton(UiText.T("تحديث", "Refresh"), LoadAsync));
        var createButton = PayrollUi.AsyncButton(UiText.T("إنشاء موظف", "Create employee"), CreateAsync);
        tools.Children.Add(createButton);
        _profileButton = PayrollUi.AsyncButton(UiText.T("ملخص الموظف", "Employee summary"), OpenProfileAsync);
        _editButton = PayrollUi.AsyncButton(UiText.T("تعديل ملف الموظف", "Edit employee profile"), EditProfileAsync);
        _payrollButton = PayrollUi.AsyncButton(UiText.T("إدارة الرواتب", "Payroll management"), OpenPayrollAsync);
        _policyButton = PayrollUi.AsyncButton(UiText.T("سياسات الرواتب", "Payroll policies"), OpenPoliciesAsync);
        _userButton = PayrollUi.AsyncButton(UiText.T("حساب المستخدم", "User account"), OpenUserAsync);
        _settingsButton = PayrollUi.AsyncButton(UiText.T("إعدادات الموظفين", "Employee settings"), OpenSettingsAsync);
        _shiftsButton = PayrollUi.AsyncButton(UiText.T("مبيعات الكاشير", "Cashier sales"), OpenCashierSalesAsync);
        
        tools.Children.Add(_profileButton);
        tools.Children.Add(_editButton);
        tools.Children.Add(_payrollButton);
        tools.Children.Add(_policyButton);
        tools.Children.Add(_userButton);
        tools.Children.Add(_settingsButton);
        tools.Children.Add(_shiftsButton);
        DockPanel.SetDock(tools, Dock.Top);
        root.Children.Add(tools);

        var searchPanel = PayrollUi.Fields();
        PayrollUi.Field(searchPanel, UiText.T("البحث بالاسم أو الهاتف أو الدور أو الرقم الوظيفي", "Search by name, phone, role, or employee code"), _search);
        DockPanel.SetDock(searchPanel, Dock.Top);
        root.Children.Add(searchPanel);
        _search.TextChanged += (_, _) => Filter();

        AddColumn(UiText.T("المعرف", "User ID"), "UserId");
        AddColumn(UiText.T("اسم الموظف", "Employee name"), "Name");
        AddColumn(UiText.T("الدور", "Role"), "Role");
        AddColumn(UiText.T("الهاتف", "Phone"), "PhoneNumber");
        AddColumn(UiText.T("الرقم الوظيفي", "Employee code"), "EmployeeCode");
        AddColumn(UiText.T("المسمى الوظيفي", "Job title"), "JobTitle");
        AddColumn(UiText.T("حالة الملف", "Profile status"), "ProfileStatus");
        _grid.SelectionChanged += (_, _) => UpdateActions();
        _grid.MouseDoubleClick += async (_, _) => await OpenProfileAsync();

        var listRoot = new Grid();
        listRoot.Children.Add(_grid);
        listRoot.Children.Add(_emptyState);
        root.Children.Add(listRoot);
        Content = root;

        Loaded += async (_, _) =>
        {
            try
            {
                await InitializeAsync();
            }
            catch (Exception ex)
            {
                PayrollUi.Error(ex);
            }
        };
    }

    private static bool IsStaff(UserRole role) => StaffRoles.Contains(role);

    private static string RoleText(UserRole role) => role switch
    {
        UserRole.Admin => UiText.T("مدير النظام", "Admin"),
        UserRole.Casher => UiText.T("كاشير", "Cashier"),
        UserRole.Manager => UiText.T("مدير", "Manager"),
        UserRole.HR => UiText.T("الموارد البشرية", "HR"),
        _ => role.ToString()
    };

    private async Task InitializeAsync()
    {
        if (_initialized)
            return;

        _initialized = true;
        foreach (var button in new[] { _profileButton, _editButton, _payrollButton, _policyButton, _userButton, _settingsButton, _shiftsButton })
            button.Visibility = Visibility.Visible;
        await LoadAsync();
    }

    private void AddColumn(string header, string path)
    {
        _grid.Columns.Add(new DataGridTextColumn
        {
            Header = header,
            Binding = new Binding(path),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star)
        });
    }

    private async Task LoadAsync()
    {
        
        var usersResult = await _users.GetAllAsync();
        if (!usersResult.Success)
            throw new ArgumentException(usersResult.Message);
        var profilesResult = await _employees.GetListAsync();
        if (!profilesResult.Success)
            throw new ArgumentException(profilesResult.Message);

        var profiles = (profilesResult.Data ?? new List<EmployeeReadDto>())
            .Where(item => item.UserId.HasValue)
            .GroupBy(item => item.UserId!.Value)
            .ToDictionary(group => group.Key, group => group.First());

        _rows = (usersResult.Data ?? new List<UserReadDto>())
            .Where(user => IsStaff(user.Role))
            .Select(user => new EmployeeUserRow { User = user, Profile = profiles.GetValueOrDefault(user.Id) })
            .OrderBy(item => item.Name)
            .ToList();
        Filter();
    }

    private void Filter()
    {
        var search = _search.Text.Trim();
        var visible = _rows.Where(row => search.Length == 0
            || row.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
            || row.PhoneNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
            || row.Role.Contains(search, StringComparison.OrdinalIgnoreCase)
            || row.EmployeeCode.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        _grid.ItemsSource = visible;
        _emptyState.Text = search.Length == 0
            ? UiText.T("لا يوجد موظفون بأدوار الموظفين حالياً.", "No users with staff roles were found.")
            : UiText.T("لا توجد نتائج مطابقة.", "No matching employees found.");
        _emptyState.Visibility = visible.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdateActions();
    }

    private EmployeeUserRow? SelectedRow() => _grid.SelectedItem as EmployeeUserRow;

    private void UpdateActions()
    {
        var row = SelectedRow();
        var selected = row != null;
        foreach (var button in new[] { _profileButton, _editButton, _payrollButton, _policyButton, _userButton, _settingsButton })
            button.IsEnabled = selected;
        _shiftsButton.IsEnabled = row?.User.Role is UserRole.Admin or UserRole.Casher;
    }

    private async Task CreateAsync()
    {
        
        CreateUser? created = null;
        WindowManager.ShowDialog<CreateUser>(WindowSizeType.MediumRectangle, window =>
        {
            created = window;
            window.InitializeForEmployeeCreate();
        });
        await LoadAsync();
        if (created?.CreatedUserId is int userId)
        {
            var row = _rows.FirstOrDefault(item => item.UserId == userId);
            if (row != null)
            {
                _grid.SelectedItem = row;
                await OpenProfileAsync();
            }
        }
    }

    private async Task OpenProfileAsync()
    {
        
        var row = SelectedRow() ?? throw new ArgumentException(UiText.T("اختر موظفاً أولاً.", "Select an employee first."));
        WindowManager.ShowDialog<EmployeeProfileSummaryWindow>(WindowSizeType.LargeRectangle,
            window => window.Initialize(row.User, row.Profile));
    }

    private async Task EditProfileAsync()
    {
        
        var row = SelectedRow() ?? throw new ArgumentException(UiText.T("اختر موظفاً أولاً.", "Select an employee first."));
        if (row.Profile == null)
        {
            MessageBox.Show(UiText.T("افتح ملخص الموظف لإنشاء ملفه الوظيفي المرتبط.", "Open the employee summary to create the linked employee profile."));
            await OpenProfileAsync();
            return;
        }
        WindowManager.ShowDialog<UpdateEmployee>(WindowSizeType.LargeRectangle, window => window.Initialize(row.Profile.Id));
        await LoadAsync();
    }

    private async Task OpenPayrollAsync()
    {
        
        WindowManager.ShowDialog<PayrollMonthWindow>(WindowSizeType.LargeRectangle);
    }

    private async Task OpenPoliciesAsync()
    {
        
        WindowManager.ShowDialog<PayrollPolicyWindow>(WindowSizeType.LargeRectangle);
    }

    private async Task OpenUserAsync()
    {
        
        var row = SelectedRow() ?? throw new ArgumentException(UiText.T("اختر موظفاً أولاً.", "Select an employee first."));
        WindowManager.ShowDialog<UpdateUser>(WindowSizeType.SmallSquare, window => window.Initialize(row.UserId));
        await LoadAsync();
    }

    private async Task OpenSettingsAsync()
    {
        
        WindowManager.ShowDialog<EmployeeFeatureSettingsWindow>(WindowSizeType.SmallSquare);
    }

    private async Task OpenCashierSalesAsync()
    {
        
        var row = SelectedRow() ?? throw new ArgumentException(UiText.T("اختر موظفاً أولاً.", "Select an employee first."));
        if (row.User.Role is not (UserRole.Admin or UserRole.Casher))
            throw new ArgumentException(UiText.T("هذا التقرير متاح للكاشير والمدير فقط.", "This report is available only for cashiers and admins."));
        WindowManager.Show<DailySalesReport>(WindowSizeType.LargeRectangle, window => window.InitializeForCashier(row.UserId));
    }
}
