using RaccoonWarehouse.Application.Service.Delegates;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Delegates.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace RaccoonWarehouse.Delegates
{
    public partial class CreateDelegate : Window
    {
        private readonly IDelegateService _delegateService;
        private readonly IUserService _userService;

        public CreateDelegate(IDelegateService delegateService, IUserService userService)
        {
            _delegateService = delegateService;
            _userService = userService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            Loaded += CreateDelegate_Loaded;
        }

        private async void CreateDelegate_Loaded(object sender, RoutedEventArgs e)
        {
            TypeComboBox.ItemsSource = Enum.GetValues(typeof(DelegateType));
            TypeComboBox.SelectedItem = DelegateType.General;
            StatusComboBox.ItemsSource = Enum.GetValues(typeof(DelegateStatus));
            StatusComboBox.SelectedItem = DelegateStatus.Active;

            var users = await _userService.GetAllAsync();
            var userList = users.Data?.ToList() ?? new List<UserReadDto>();
            userList.Insert(0, new UserReadDto { Id = 0, Name = UiText.T("بدون مستخدم", "No User") });
            UserComboBox.ItemsSource = userList;
            UserComboBox.SelectedIndex = 0;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var dto = new DelegateCreateDto
            {
                Code = CodeTextBox.Text.Trim(),
                FullName = FullNameTextBox.Text.Trim(),
                PhoneNumber = PhoneTextBox.Text.Trim(),
                AlternatePhoneNumber = AltPhoneTextBox.Text.Trim(),
                DelegateType = TypeComboBox.SelectedItem is DelegateType type ? type : DelegateType.General,
                Status = StatusComboBox.SelectedItem is DelegateStatus status ? status : DelegateStatus.Active,
                UserId = UserComboBox.SelectedValue is int userId && userId > 0 ? userId : null,
                RegionId = int.TryParse(RegionTextBox.Text, out var regionId) ? regionId : null,
                AreaName = string.IsNullOrWhiteSpace(RegionTextBox.Text) ? null : RegionTextBox.Text.Trim(),
                HireDate = HireDatePicker.SelectedDate,
                Notes = NotesTextBox.Text.Trim()
            };

            var result = await _delegateService.CreateAsync(dto);
            if (!result.Success)
            {
                MessageBox.Show(result.Message ?? UiText.T("فشل إنشاء المندوب.", "Failed to create the delegate."));
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
