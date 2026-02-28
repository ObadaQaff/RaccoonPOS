using RaccoonWarehouse.Application.Service.Warehouses;
using RaccoonWarehouse.Products;
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
    /// Interaction logic for WarehousesTable.xaml
    /// </summary>
    public partial class WarehousesTable : Window
    {
        private readonly IWarehouseService _warehouseService;
        public WarehousesTable(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
            InitializeComponent();
            Load_Data();
        }

        private async void Load_Data()
        {
            var result = await _warehouseService.GetAllAsync();
            if (result.Success)
            {
                WarehousesGrid.ItemsSource = result.Data;
            }
            else
            {
                MessageBox.Show("Failed to load warehouses: " + result.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            string searchText = textBox?.Text.Trim() ?? string.Empty;

            var result = await _warehouseService.GetPagedListAsync(
                pageNumber: 1,
                pageSize: 20,
                filter: string.IsNullOrEmpty(searchText)
                    ? null
                    : u => u.Name.Contains(searchText),
                orderBy: q => q.OrderBy(u => u.Name)
            );

            WarehousesGrid.ItemsSource = result.Items;
        }

    }
}
