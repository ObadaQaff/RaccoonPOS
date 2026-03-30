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
    public partial class UpdateDelegate : Window
    {
        private readonly IDelegateService _delegateService;
        private readonly IUserService _userService;
        private DelegateReadDto? _currentDelegate;

        public UpdateDelegate(IDelegateService delegateService, IUserService userService)
        {
            _delegateService = delegateService;
            _userService = userService;
            InitializeComponent();
            UiText.ApplyWindow(this);
        }

        public async void Initialize(int delegateId)
        {
            TypeComboBox.ItemsSource = Enum.GetValues(typeof(DelegateType));
            StatusComboBox.ItemsSource = Enum.GetValues(typeof(DelegateStatus));

            var users = await _userService.GetAllAsync();
            var userList = users.Data?.ToList() ?? new List<UserReadDto>();
            userList.Insert(0, new UserReadDto { Id = 0, Name = UiText.T("بدون مستخدم", "No User") });
            UserComboBox.ItemsSource = userList;

            var result = await _delegateService.GetByIdAsync(delegateId);
            if (!result.Success || result.Data == null)
            {
                MessageBox.Show(result.Message ?? UiText.T("تعذر تحميل المندوب.", "Failed to load the delegate."));
                Close();
                return;
            }

            _currentDelegate = result.Data;
            CodeTextBox.Text = _currentDelegate.Code;
            FullNameTextBox.Text = _currentDelegate.FullName;
            PhoneTextBox.Text = _currentDelegate.PhoneNumber;
            AltPhoneTextBox.Text = _currentDelegate.AlternatePhoneNumber;
            TypeComboBox.SelectedItem = _currentDelegate.DelegateType;
            StatusComboBox.SelectedItem = _currentDelegate.Status;
            UserComboBox.SelectedValue = _currentDelegate.UserId ?? 0;
            RegionTextBox.Text = _currentDelegate.AreaName ?? _currentDelegate.RegionId?.ToString() ?? string.Empty;
            HireDatePicker.SelectedDate = _currentDelegate.HireDate;
            NotesTextBox.Text = _currentDelegate.Notes;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDelegate == null)
            {
                return;
            }

            var dto = new DelegateUpdateDto
            {
                Id = _currentDelegate.Id,
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
                Notes = NotesTextBox.Text.Trim(),
                CreatedDate = _currentDelegate.CreatedDate,
                UpdatedDate = DateTime.Now
            };

            var result = await _delegateService.UpdateAsync(dto);
            if (!result.Success)
            {
                MessageBox.Show(result.Message ?? UiText.T("فشل تعديل المندوب.", "Failed to update the delegate."));
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
