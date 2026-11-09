using RaccoonWarehouse.Application.Service.Employees;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Core.Permissions;
using RaccoonWarehouse.Domain.Employees.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace RaccoonWarehouse.Employees;

public partial class EmployeeProfileWindow : Window
{
    private readonly IEmployeeService _employees;
    private readonly IShiftService _shifts;
    private readonly IPermissionManagementService _permissionManagement;
    private readonly ILoadingService _loading;
    private int? _id;
    private readonly ObservableCollection<EmployeeShiftDto> _shiftItems = new();
    private readonly ObservableCollection<EmployeeBranchDto> _branchItems = new();
    public EmployeeProfileWindow(IEmployeeService employees, IShiftService shifts, IPermissionManagementService permissionManagement, ILoadingService loading) { _employees = employees; _shifts = shifts; _permissionManagement = permissionManagement; _loading = loading; InitializeComponent(); ShiftsGrid.ItemsSource = _shiftItems; BranchesGrid.ItemsSource = _branchItems; Status.ItemsSource = Enum.GetValues<EmployeeStatus>(); ContractType.ItemsSource = Enum.GetValues<EmploymentContractType>(); ShiftDay.ItemsSource = Enum.GetValues<DayOfWeek>(); if (ContractStartDate.Parent is FrameworkElement contractStartPanel) contractStartPanel.Visibility = Visibility.Collapsed; }
    public async Task InitializeAsync(int? id)
    {
        _id = id;
        var branches = await _employees.GetBranchesAsync();
        PrimaryBranch.ItemsSource = branches;
        AdditionalBranch.ItemsSource = branches;
        UserAccount.ItemsSource = await _employees.GetUsersAsync();
        SetShiftsTabVisible(id is int);
        if (id is not int employeeId)
        {
            Status.SelectedItem = EmployeeStatus.Active;
            ContractType.SelectedItem = EmploymentContractType.Indefinite;
            EmploymentDate.SelectedDate = DateTime.Today;
            if (branches.Count > 0)
                PrimaryBranch.SelectedIndex = 0;
            return;
        }
        var details = await _employees.GetAsync(employeeId) ?? throw new InvalidOperationException("Employee was not found."); var x = details.Employee; _branchItems.Clear(); foreach (var branch in details.Branches) _branchItems.Add(branch);
        ShiftTemplate.ItemsSource = await _shifts.GetTemplatesAsync();
        FullName.Text = x.FullName; PhoneNumber.Text = x.PhoneNumber; NationalId.Text = x.NationalId; Email.Text = x.Email; Address.Text = x.Address; JobTitle.Text = x.JobTitle; MonthlySalary.Text = x.MonthlyBasicSalary.ToString(CultureInfo.InvariantCulture); EmploymentDate.SelectedDate = x.EmploymentDate ?? x.ContractStartDate; Status.SelectedItem = x.Status; ContractEndDate.SelectedDate = x.ContractEndDate; SocialSecurityEnrolled.IsChecked = x.SocialSecurityEnrolled; SocialSecurityNumber.Text = x.SocialSecurityNumber; PrimaryBranch.SelectedValue = x.PrimaryBranchId; UserAccount.SelectedValue = x.UserId; StatusText.Text = x.Status.ToString(); _shiftItems.Clear(); foreach (var shift in details.Shifts) _shiftItems.Add(shift); await LoadPermissionSummaryAsync(x.UserId);
    }
    private async Task LoadPermissionSummaryAsync(int? userId)
    {
        if (userId is not int id) { PermissionSummary.Text = "لا يوجد حساب مستخدم مرتبط بهذا الموظف."; return; }
        try { PermissionSummary.Text = await BuildPermissionSummaryAsync(id); }
        catch (UnauthorizedAccessException) { PermissionSummary.Text = "عرض الصلاحيات يتطلب صلاحية إدارة مجموعات الصلاحيات."; }
    }
    private async Task<string> BuildPermissionSummaryAsync(int userId)
    {
        var summary = await _permissionManagement.GetUserAccessAsync(userId);
        if (summary == null) return "لم يتم العثور على الحساب.";
        var groups = summary.GroupNames.Count == 0 ? "لا توجد مجموعات" : string.Join("، ", summary.GroupNames);
        var permissions = summary.EffectivePermissions.Count == 0 ? "لا توجد صلاحيات فعالة" : string.Join("، ", summary.EffectivePermissions.Select(x => $"{x.AreaKey}: {x.Level}"));
        return $"المجموعات: {groups}\nالصلاحيات الفعالة: {permissions}";
    }
    private void ManagePermissions_Click(object sender, RoutedEventArgs e) => WindowManager.Show<RaccoonWarehouse.Permissions.PermissionGroupsWindow>(WindowSizeType.LargeRectangle);
    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(MonthlySalary.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var salary)) salary = 0;
        if (PrimaryBranch.SelectedValue is not int primaryBranchId || primaryBranchId <= 0)
        {
            MessageBox.Show(
                UiText.T("يرجى اختيار الفرع الرئيسي للموظف.", "Please select the employee's primary branch."),
                UiText.T("بيانات ناقصة", "Missing data"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var employmentStartDate = EmploymentDate.SelectedDate ?? DateTime.Today;
        var result = await _employees.SaveAsync(new EmployeeEditDto { Id = _id ?? 0, FullName = FullName.Text, PhoneNumber = PhoneNumber.Text, NationalId = NationalId.Text, Email = Email.Text, Address = Address.Text, JobTitle = JobTitle.Text, MonthlyBasicSalary = salary, EmploymentDate = employmentStartDate, Status = Status.SelectedItem is EmployeeStatus s ? s : EmployeeStatus.Active, ContractType = ContractType.SelectedItem is EmploymentContractType c ? c : EmploymentContractType.Indefinite, ContractStartDate = employmentStartDate, ContractEndDate = ContractEndDate.SelectedDate, SocialSecurityEnrolled = SocialSecurityEnrolled.IsChecked == true, SocialSecurityNumber = SocialSecurityNumber.Text, PrimaryBranchId = primaryBranchId, UserId = UserAccount.SelectedValue is int userId ? userId : null });
        if (!result.Success) { MessageBox.Show(string.Join(Environment.NewLine, result.Errors)); return; } MessageBox.Show(UiText.T("تم حفظ الموظف.", "Employee saved.")); DialogResult = true; Close();
    }
    private async void Deactivate_Click(object sender, RoutedEventArgs e) { if (_id is int id && MessageBox.Show("هل تريد تعطيل هذا الموظف؟", "تأكيد", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { var result = await _employees.DeactivateAsync(id); if (!result.Success) MessageBox.Show(result.Message); else { MessageBox.Show("تم تعطيل الموظف."); Close(); } } }
    private async void AssignShift_Click(object sender, RoutedEventArgs e)
    {
        if (_id is not int id || ShiftTemplate.SelectedValue is not int templateId || ShiftDay.SelectedItem is not DayOfWeek day)
        {
            MessageBox.Show("يرجى حفظ الموظف واختيار الوردية واليوم أولاً.", "بيانات ناقصة", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _loading.Show();
        try
        {
            var result = await _shifts.SaveAssignmentAsync(new EmployeeShiftAssignmentEditDto { EmployeeId = id, ShiftTemplateId = templateId, DayOfWeek = day });
            if (!result.Success)
                MessageBox.Show(string.Join(Environment.NewLine, result.Errors));
            else
                await InitializeAsync(id);
        }
        finally
        {
            _loading.Hide();
        }
    }
    private void SetShiftsTabVisible(bool visible)
    {
        var shiftsTab = ProfileTabs.Items.OfType<System.Windows.Controls.TabItem>().FirstOrDefault(tab => string.Equals(tab.Header?.ToString(), "الورديات", StringComparison.Ordinal));
        if (shiftsTab != null)
            shiftsTab.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }
    private async void AssignBranch_Click(object sender, RoutedEventArgs e)
    {
        if (AdditionalBranch.SelectedValue is not int branchId)
        {
            var createWindow = new CreateBranchWindow(_employees) { Owner = this };
            if (createWindow.ShowDialog() == true)
            {
                var branches = await _employees.GetBranchesAsync();
                PrimaryBranch.ItemsSource = branches;
                AdditionalBranch.ItemsSource = branches;
                if (createWindow.CreatedBranchId is int createdBranchId)
                {
                    PrimaryBranch.SelectedValue = createdBranchId;
                    AdditionalBranch.SelectedValue = createdBranchId;
                }
            }
            return;
        }
        if (_id is not int id)
        {
            MessageBox.Show("يرجى حفظ الموظف أولاً ثم إضافة الفروع الإضافية.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = await _employees.SaveBranchAssignmentAsync(new EmployeeBranchAssignmentEditDto { EmployeeId = id, BranchId = branchId });
        if (!result.Success) MessageBox.Show(string.Join(Environment.NewLine, result.Errors)); else await InitializeAsync(id);
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
