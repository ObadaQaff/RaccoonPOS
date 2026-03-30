using RaccoonWarehouse.Application.Service.Warehouses;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Warehouses.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Windows;

namespace RaccoonWarehouse.Warehouses
{
    public partial class CreateWarehouse : Window
    {
        private readonly IWarehouseService _warehouseService;

        public CreateWarehouse(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            WarehouseStatus.ItemsSource = Enum.GetValues(typeof(WarehouseStatus));
            UiText.ApplyTranslations(this);
        }

        private async void CreateWarehouseBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dto = new WarehouseWriteDto
                {
                    Name = WarehouseName.Text,
                    Location = WarehouseLocation.Text,
                    PhoneNumber = int.TryParse(WarehousePhone.Text, out var phone) ? phone : 0,
                    Description = WarehouseDescription.Text,
                    Status = (WarehouseStatus)WarehouseStatus.SelectedValue,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };

                var result = await _warehouseService.CreateAsync(dto);
                if (result.Success)
                {
                    MessageBox.Show(
                        UiText.T("تمت إضافة المستودع بنجاح.", "The warehouse was added successfully."),
                        UiText.T("نجاح", "Success"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        result.Message ?? UiText.T("حدث خطأ أثناء إضافة المستودع.", "An error occurred while adding the warehouse."),
                        UiText.T("خطأ", "Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("حدث خطأ", "An error occurred")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ClearForm()
        {
            WarehouseName.Text = string.Empty;
            WarehouseLocation.Text = string.Empty;
            WarehousePhone.Text = string.Empty;
            WarehouseDescription.Text = string.Empty;
            WarehouseStatus.SelectedIndex = -1;
        }
    }
}
