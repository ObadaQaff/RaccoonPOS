using RaccoonWarehouse.Application.Service.Warehouses;
using RaccoonWarehouse.Helpers.Localization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Warehouses
{
    public partial class WarehousesTable : Window
    {
        private readonly IWarehouseService _warehouseService;

        public WarehousesTable(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            LoadData();
        }

        private async void LoadData()
        {
            var result = await _warehouseService.GetAllAsync();
            if (result.Success)
            {
                WarehousesGrid.ItemsSource = result.Data;
                UiText.ApplyTranslations(this);
            }
            else
            {
                MessageBox.Show(
                    $"{UiText.T("فشل تحميل المستودعات", "Failed to load warehouses")}: {result.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = (sender as TextBox)?.Text.Trim() ?? string.Empty;

            var result = await _warehouseService.GetPagedListAsync(
                pageNumber: 1,
                pageSize: 20,
                filter: string.IsNullOrEmpty(searchText) ? null : u => u.Name.Contains(searchText),
                orderBy: q => q.OrderBy(u => u.Name));

            WarehousesGrid.ItemsSource = result.Items;
            UiText.ApplyTranslations(this);
        }
    }
}
