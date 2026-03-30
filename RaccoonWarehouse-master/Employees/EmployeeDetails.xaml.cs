using RaccoonWarehouse.Application.Service.Employees;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;

namespace RaccoonWarehouse.Employees
{
    public partial class EmployeeDetails : Window
    {
        private readonly IEmployeeService _employeeService;

        public EmployeeDetails(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
            InitializeComponent();
            UiText.ApplyWindow(this);
        }

        public async void Initialize(int employeeId)
        {
            var result = await _employeeService.GetByIdAsync(employeeId);
            if (!result.Success || result.Data == null)
            {
                MessageBox.Show(result.Message ?? UiText.T("تعذر تحميل التفاصيل.", "Failed to load the details."));
                Close();
                return;
            }

            var dto = result.Data;
            NameText.Text = dto.FullName;
            MetaText.Text = UiText.IsEnglish
                ? $"Hire Date: {(dto.HireDate?.ToString("yyyy-MM-dd") ?? "—")} | Birth Date: {(dto.DateOfBirth?.ToString("yyyy-MM-dd") ?? "—")}"
                : $"تاريخ التعيين: {(dto.HireDate?.ToString("yyyy-MM-dd") ?? "—")} | تاريخ الميلاد: {(dto.DateOfBirth?.ToString("yyyy-MM-dd") ?? "—")}";
            CodeText.Text = dto.Code;
            StatusText.Text = dto.Status.ToString();
            PhoneText.Text = dto.PhoneNumber ?? "—";
            EmailText.Text = dto.Email ?? "—";
            JobTitleText.Text = dto.JobTitle ?? "—";
            ManagerText.Text = dto.ManagerName ?? UiText.T("بدون مدير", "No Manager");
            UserText.Text = dto.UserName ?? UiText.T("بدون مستخدم", "No User");
            BranchText.Text = dto.BranchId?.ToString() ?? "—";
            DepartmentText.Text = dto.DepartmentId?.ToString() ?? "—";
            SalaryText.Text = dto.BasicSalary?.ToString("0.##") ?? "—";
            AddressText.Text = string.IsNullOrWhiteSpace(dto.Address) ? "—" : dto.Address;
            NotesText.Text = string.IsNullOrWhiteSpace(dto.Notes) ? "—" : dto.Notes;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
