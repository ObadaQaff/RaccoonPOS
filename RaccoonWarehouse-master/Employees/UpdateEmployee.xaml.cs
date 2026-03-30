using RaccoonWarehouse.Application.Service.Employees;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Employees.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace RaccoonWarehouse.Employees
{
    public partial class UpdateEmployee : Window
    {
        private readonly IEmployeeService _employeeService;
        private readonly IUserService _userService;
        private EmployeeReadDto? _currentEmployee;

        public UpdateEmployee(IEmployeeService employeeService, IUserService userService)
        {
            _employeeService = employeeService;
            _userService = userService;
            InitializeComponent();
            UiText.ApplyWindow(this);
        }

        public async void Initialize(int employeeId)
        {
            StatusComboBox.ItemsSource = Enum.GetValues(typeof(EmployeeStatus));
            GenderComboBox.ItemsSource = new object[] { UiText.T("غير محدد", "Unspecified") }.Concat(Enum.GetValues(typeof(EmployeeGender)).Cast<object>());

            var users = await _userService.GetAllAsync();
            var userList = users.Data?.ToList() ?? new List<UserReadDto>();
            userList.Insert(0, new UserReadDto { Id = 0, Name = UiText.T("بدون مستخدم", "No User") });
            UserComboBox.ItemsSource = userList;

            var employees = await _employeeService.GetListAsync();
            var employeeList = employees.Data?.Where(x => x.Id != employeeId).ToList() ?? new List<EmployeeReadDto>();
            employeeList.Insert(0, new EmployeeReadDto { Id = 0, FullName = UiText.T("بدون مدير", "No Manager") });
            ManagerComboBox.ItemsSource = employeeList;

            var result = await _employeeService.GetByIdAsync(employeeId);
            if (!result.Success || result.Data == null)
            {
                MessageBox.Show(result.Message ?? UiText.T("تعذر تحميل الموظف.", "Failed to load the employee."));
                Close();
                return;
            }

            _currentEmployee = result.Data;
            CodeTextBox.Text = _currentEmployee.Code;
            FullNameTextBox.Text = _currentEmployee.FullName;
            PhoneTextBox.Text = _currentEmployee.PhoneNumber;
            AltPhoneTextBox.Text = _currentEmployee.AlternatePhoneNumber;
            EmailTextBox.Text = _currentEmployee.Email;
            NationalIdTextBox.Text = _currentEmployee.NationalId;
            StatusComboBox.SelectedItem = _currentEmployee.Status;
            GenderComboBox.SelectedItem = _currentEmployee.Gender.HasValue ? _currentEmployee.Gender.Value : UiText.T("غير محدد", "Unspecified");
            JobTitleTextBox.Text = _currentEmployee.JobTitle;
            BranchTextBox.Text = _currentEmployee.BranchId?.ToString() ?? string.Empty;
            DepartmentTextBox.Text = _currentEmployee.DepartmentId?.ToString() ?? string.Empty;
            ManagerComboBox.SelectedValue = _currentEmployee.ManagerId ?? 0;
            UserComboBox.SelectedValue = _currentEmployee.UserId ?? 0;
            BasicSalaryTextBox.Text = _currentEmployee.BasicSalary?.ToString() ?? string.Empty;
            HireDatePicker.SelectedDate = _currentEmployee.HireDate;
            TerminationDatePicker.SelectedDate = _currentEmployee.TerminationDate;
            BirthDatePicker.SelectedDate = _currentEmployee.DateOfBirth;
            AddressTextBox.Text = _currentEmployee.Address;
            NotesTextBox.Text = _currentEmployee.Notes;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEmployee == null)
                return;

            var dto = new EmployeeUpdateDto
            {
                Id = _currentEmployee.Id,
                Code = CodeTextBox.Text.Trim(),
                FullName = FullNameTextBox.Text.Trim(),
                PhoneNumber = PhoneTextBox.Text.Trim(),
                AlternatePhoneNumber = AltPhoneTextBox.Text.Trim(),
                Email = EmailTextBox.Text.Trim(),
                NationalId = NationalIdTextBox.Text.Trim(),
                Status = StatusComboBox.SelectedItem is EmployeeStatus status ? status : EmployeeStatus.Active,
                Gender = GenderComboBox.SelectedItem is EmployeeGender gender ? gender : null,
                JobTitle = JobTitleTextBox.Text.Trim(),
                BranchId = int.TryParse(BranchTextBox.Text, out var branchId) ? branchId : null,
                DepartmentId = int.TryParse(DepartmentTextBox.Text, out var departmentId) ? departmentId : null,
                ManagerId = ManagerComboBox.SelectedValue is int managerId && managerId > 0 ? managerId : null,
                UserId = UserComboBox.SelectedValue is int userId && userId > 0 ? userId : null,
                BasicSalary = decimal.TryParse(BasicSalaryTextBox.Text, out var salary) ? salary : null,
                HireDate = HireDatePicker.SelectedDate,
                TerminationDate = TerminationDatePicker.SelectedDate,
                DateOfBirth = BirthDatePicker.SelectedDate,
                Address = AddressTextBox.Text.Trim(),
                Notes = NotesTextBox.Text.Trim(),
                CreatedDate = _currentEmployee.CreatedDate,
                UpdatedDate = DateTime.Now
            };

            var result = await _employeeService.UpdateAsync(dto);
            if (!result.Success)
            {
                MessageBox.Show(result.Message ?? UiText.T("فشل تعديل الموظف.", "Failed to update the employee."));
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
