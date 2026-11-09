using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RaccoonWarehouse.Domain.Employees.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Employees.Payroll;
using RaccoonWarehouse.POS;

namespace RaccoonWarehouse.Employees;

public sealed class EmployeeProfileSummaryWindow : Window
{
    private readonly TextBlock _identity = new();
    private readonly TextBlock _employment = new();
    private readonly TextBlock _payroll = new();
    private readonly TextBlock _status = new();
    private readonly Button _editUserButton;
    private readonly Button _employeeProfileButton;
    private readonly Button _payrollButton;
    private readonly Button _policyButton;
    private readonly Button _cashierSalesButton;
    private UserReadDto? _user;
    private EmployeeReadDto? _employee;

    public EmployeeProfileSummaryWindow()
    {
        UiText.ApplyWindow(this);
        Title = UiText.T("ملخص ملف الموظف", "Employee profile summary");
        Width = 1050;
        Height = 720;
        MinWidth = 760;
        MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FlowDirection = FlowDirection.RightToLeft;
        if (System.Windows.Application.Current.TryFindResource("AppPageBackgroundBrush") is System.Windows.Media.Brush background)
            Background = background;

        var root = new DockPanel { Margin = new Thickness(24) };
        var title = new TextBlock { FontSize = 28, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 18) };
        title.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(Window.Title)) { Source = this });
        DockPanel.SetDock(title, Dock.Top);
        root.Children.Add(title);

        var actions = new WrapPanel { Margin = new Thickness(0, 0, 0, 18) };
        _editUserButton = PayrollUi.AsyncButton(UiText.T("تعديل حساب المستخدم", "Edit user account"), OpenUserAsync);
        _employeeProfileButton = PayrollUi.AsyncButton(UiText.T("ملف الموظف", "Employee profile"), OpenEmployeeProfileAsync);
        _payrollButton = PayrollUi.AsyncButton(UiText.T("إدارة الرواتب", "Payroll management"), OpenPayrollAsync);
        _policyButton = PayrollUi.AsyncButton(UiText.T("سياسات الرواتب", "Payroll policies"), OpenPoliciesAsync);
        _cashierSalesButton = PayrollUi.AsyncButton(UiText.T("مبيعات الكاشير", "Cashier sales"), OpenCashierSalesAsync);
        actions.Children.Add(_editUserButton);
        actions.Children.Add(_employeeProfileButton);
        actions.Children.Add(_payrollButton);
        actions.Children.Add(_policyButton);
        actions.Children.Add(_cashierSalesButton);
        DockPanel.SetDock(actions, Dock.Top);
        root.Children.Add(actions);

        var cards = new UniformGrid { Columns = 2, Rows = 2 };
        cards.Children.Add(Card(UiText.T("بيانات المستخدم", "User information"), _identity));
        cards.Children.Add(Card(UiText.T("بيانات الموظف", "Employee information"), _employment));
        cards.Children.Add(Card(UiText.T("ملخص الرواتب", "Payroll summary"), _payroll));
        cards.Children.Add(Card(UiText.T("الحالة", "Status"), _status));
        root.Children.Add(cards);

        Content = root;
        Loaded += (_, _) => Render();
    }

    public void Initialize(UserReadDto user, EmployeeReadDto? employee)
    {
        _user = user;
        _employee = employee;
        Render();
    }

    private static Border Card(string title, TextBlock content)
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
        content.TextWrapping = TextWrapping.Wrap;
        content.FontSize = 15;
        panel.Children.Add(content);
        return new Border
        {
            Child = panel,
            Padding = new Thickness(18),
            Margin = new Thickness(8),
            CornerRadius = new CornerRadius(12),
            Background = System.Windows.Application.Current.TryFindResource("AppCardBackgroundBrush") as System.Windows.Media.Brush
        };
    }

    private void Render()
    {
        if (_user == null)
            return;

        Title = string.Format(UiText.T("ملخص الموظف: {0}", "Employee summary: {0}"), _user.Name);
        _identity.Text = string.Join(Environment.NewLine,
            $"{UiText.T("المعرف", "User ID")}: {_user.Id}",
            $"{UiText.T("الاسم", "Name")}: {_user.Name}",
            $"{UiText.T("الهاتف", "Phone")}: {_user.PhoneNumber ?? "-"}",
            $"{UiText.T("الدور", "Role")}: {_user.Role}");

        _employment.Text = _employee == null
            ? UiText.T("لا يوجد ملف موظف مرتبط بهذا المستخدم.", "No employee profile is linked to this user.")
            : string.Join(Environment.NewLine,
                $"{UiText.T("الرقم الوظيفي", "Employee code")}: {_employee.Code}",
                $"{UiText.T("المسمى الوظيفي", "Job title")}: {_employee.JobTitle ?? "-"}",
                $"{UiText.T("تاريخ التعيين", "Hire date")}: {_employee.HireDate:yyyy-MM-dd}",
                $"{UiText.T("الفرع", "Branch")}: {_employee.BranchId?.ToString() ?? "-"}");

        _payroll.Text = _employee?.BasicSalary.HasValue == true
            ? $"{UiText.T("الراتب الأساسي", "Basic salary")}: {_employee.BasicSalary:0.000}"
            : UiText.T("بيانات الراتب متاحة في ملف الموظف.", "Payroll details are available in the employee profile.");
        _status.Text = string.Join(Environment.NewLine,
            $"{UiText.T("حالة الملف", "Profile")}: {_employee?.Status.ToString() ?? UiText.T("غير منشأ", "Missing")}",
            $"{UiText.T("نوع المستخدم", "Account role")}: {_user.Role}");
    }


    

    private async Task OpenUserAsync()
    {
        
        if (_user == null)
            return;
        WindowManager.ShowDialog<UpdateUser>(WindowSizeType.SmallSquare, window => window.Initialize(_user.Id));
    }

    private async Task OpenEmployeeProfileAsync()
    {
        
        if (_employee == null)
            return;
        WindowManager.ShowDialog<EmployeeDetails>(WindowSizeType.LargeRectangle, window => window.Initialize(_employee.Id));
    }

    private async Task OpenPayrollAsync()
    {
        
        WindowManager.ShowDialog<PayrollMonthWindow>(WindowSizeType.LargeRectangle);
    }

    private async Task OpenPoliciesAsync()
    {
        
        WindowManager.ShowDialog<PayrollPolicyWindow>(WindowSizeType.LargeRectangle);
    }

    private async Task OpenCashierSalesAsync()
    {
        
        if (_user == null || _user.Role is not (UserRole.Admin or UserRole.Casher))
            return;
        WindowManager.Show<DailySalesReport>(WindowSizeType.LargeRectangle, window => window.InitializeForCashier(_user.Id));
    }

}
