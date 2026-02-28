using RaccoonWarehouse.Application.Service.Warehouses;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Warehouses.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RaccoonWarehouse.Warehouses
{
    /// <summary>
    /// Interaction logic for CreateWarehouse.xaml
    /// </summary>
    public partial class CreateWarehouse : Window
    {
        IWarehouseService _warehouseService;
        
        public CreateWarehouse(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
            InitializeComponent();
            WarehouseStatus.ItemsSource = Enum.GetValues(typeof(Domain.Enums.WarehouseStatus));
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
                    MessageBox.Show("تمت إضافة المستودع بنجاح ✅", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                }
                else
                {
                    MessageBox.Show("حدث خطأ: ", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);

                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void ClearForm()
        {
            WarehouseName.Text = string.Empty;
            WarehouseLocation.Text = string.Empty;
            WarehousePhone.Text = string.Empty;
            WarehouseDescription.Text = string.Empty;
            WarehouseStatus.SelectedIndex = -1; // Reset ComboBox selection
        }
    }
}
