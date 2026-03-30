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
    public partial class CreateEmployee : Window
    {
        private readonly IEmployeeService _employeeService;
        private readonly IUserService _userService;

        public CreateEmployee(IEmployeeService employeeService, IUserService userService)
        {
            _employeeService = employeeService;
            _userService = userService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            Loaded += CreateEmployee_Loaded;
        }

        private async void CreateEmployee_Loaded(object sender, RoutedEventArgs e)
        {
            StatusComboBox.ItemsSource = Enum.GetValues(typeof(EmployeeStatus));
            StatusComboBox.SelectedItem = EmployeeStatus.Active;
            GenderComboBox.ItemsSource = new object[] { UiText.T("غير محدد", "Unspecified") }.Concat(Enum.GetValues(typeof(EmployeeGender)).Cast<object>());
            GenderComboBox.SelectedIndex = 0;

            var users = await _userService.GetAllAsync();
            var userList = users.Data?.ToList() ?? new List<UserReadDto>();
            userList.Insert(0, new UserReadDto { Id = 0, Name = UiText.T("بدون مستخدم", "No User") });
            UserComboBox.ItemsSource = userList;
            UserComboBox.SelectedIndex = 0;

            var employees = await _employeeService.GetListAsync();
            var employeeList = employees.Data?.ToList() ?? new List<EmployeeReadDto>();
            employeeList.Insert(0, new EmployeeReadDto { Id = 0, FullName = UiText.T("بدون مدير", "No Manager") });
            ManagerComboBox.ItemsSource = employeeList;
            ManagerComboBox.SelectedIndex = 0;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var dto = new EmployeeCreateDto
            {
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
                Notes = NotesTextBox.Text.Trim()
            };

            var result = await _employeeService.CreateAsync(dto);
            if (!result.Success)
            {
                MessageBox.Show(result.Message ?? UiText.T("فشل إنشاء الموظف.", "Failed to create the employee."));
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
