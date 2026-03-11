using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse;
using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Brands.DTOs;
using RaccoonWarehouse.Navigation;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse.Brands
{
    /// <summary>
    /// Interaction logic for BrandsTable.xaml
    /// </summary>
    public partial class BrandsTable : Window
    {
        private readonly IBrandService _brandService;
        private readonly IMapper _mapper;
        private readonly ILoadingService _loadingService;

        public BrandsTable(IBrandService brandService, IMapper mapper, ILoadingService loadingService)
        {
            _brandService = brandService;
            _mapper = mapper;
            _loadingService = loadingService;
            InitializeComponent();
            _ = Load_BrandsAsync();
        }

        private async Task Load_BrandsAsync()
        {
            try
            {
                _loadingService.Show();
                var result = await _brandService.GetAllAsync();
                if (result.Success)
                {
                    BrandsTable1.ItemsSource = result.Data;
                }
                else
                {
                    MessageBox.Show(result.Message ?? "Failed to load brands.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error while loading brands: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void Update_Brand(object sender, RoutedEventArgs e)
        {
            if (BrandsTable1.SelectedItem is not BrandReadDto selectedBrand)
            {
                MessageBox.Show("Please select a brand before update or delete.");
                return;
            }

            WindowManager.ShowDialog<UpdateBrand>(WindowSizeType.MediumRectangle, w =>
            {
                w.Initialize(selectedBrand.Id);
            });
        }

        private async void Delete_Brand(object sender, RoutedEventArgs e)
        {
            var selectedBrand = BrandsTable1.SelectedItem as BrandReadDto;
            if (selectedBrand == null)
            {
                MessageBox.Show("No brand selected.");
                return;
            }

            var messageResult = MessageBox.Show(
                $"Are you sure you want to delete brand: '{selectedBrand.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (messageResult != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                _loadingService.Show();
                var result = await _brandService.DeleteAsync(selectedBrand.Id);
                if (result.Success)
                {
                    MessageBox.Show("Deleted successfully !!");
                    await Load_BrandsAsync();
                }
                else
                {
                    MessageBox.Show(result.Message ?? "Delete failed.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error while deleting brand: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void CreateCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            var createCategory = ((App)System.Windows.Application.Current)
                       .ServiceProvider.GetRequiredService<CreateBrand>();
            createCategory.ShowDialog();

        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
