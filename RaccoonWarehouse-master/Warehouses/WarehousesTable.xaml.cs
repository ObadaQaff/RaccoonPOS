using RaccoonWarehouse.Application.Service.Warehouses;
using RaccoonWarehouse.Domain.Warehouses.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using Microsoft.Extensions.DependencyInjection;
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

        private async void EditWarehouseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not WarehouseReadDto warehouse)
                return;

            var selected = await _warehouseService.GetWriteDtoByIdAsync(warehouse.Id);
            if (!selected.Success || selected.Data == null)
            {
                MessageBox.Show(
                    selected.Message ?? UiText.T("تعذر تحميل بيانات المستودع.", "Could not load the warehouse data."),
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var window = ((global::RaccoonWarehouse.App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<CreateWarehouse>();
            window.InitializeForEdit(selected.Data);
            window.ShowDialog();
            LoadData();
        }

        private async void DeleteWarehouseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not WarehouseReadDto warehouse)
                return;

            var confirm = MessageBox.Show(
                UiText.T("هل تريد حذف هذا المستودع؟", "Do you want to delete this warehouse?"),
                UiText.T("تأكيد الحذف", "Delete confirmation"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            var result = await _warehouseService.DeleteAsync(warehouse.Id);
            if (!result.Success)
            {
                MessageBox.Show(
                    result.Message ?? UiText.T("فشل حذف المستودع.", "Failed to delete the warehouse."),
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            LoadData();
        }
    }
}
