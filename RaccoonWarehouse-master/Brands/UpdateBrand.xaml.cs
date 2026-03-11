using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Brands.DTOs;
using RaccoonWarehouse;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse.Brands
{
    /// <summary>
    /// Interaction logic for UpdateBrand.xaml
    /// </summary>
    public partial class UpdateBrand : Window
    {
        private readonly IBrandService _brandService;
        private readonly IMapper _mapper;
        private readonly ILoadingService _loadingService;
        private int _brandId;
        private BrandWriteDto _brand = new BrandWriteDto();

        public UpdateBrand(IBrandService brandService, IMapper mapper, ILoadingService loadingService)
        {
            _brandService = brandService;
            _mapper = mapper;
            _loadingService = loadingService;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public async void Initialize(int Id)
        {
            _brandId = Id;
            await LoadBrandAsync(_brandId);
        }

        private async Task LoadBrandAsync(int Id)
        {
            try
            {
                _loadingService.Show();
                var result = await _brandService.GetWriteDtoByIdAsync(Id);
                if (result.Success && result.Data != null)
                {
                    _brand = result.Data;
                    Name.Text = result.Data.Name;
                    ImageUrl.Text = result.Data.ImageUrl;
                }
                else
                {
                    MessageBox.Show(result.Message ?? "Failed to load brand data.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error while loading brand: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private async void Update_CategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            // Required by non-nullable DTO field.
            if (string.IsNullOrWhiteSpace(Name.Text))
            {
                MessageBox.Show("Brand name is required.");
                return;
            }

            try
            {
                _loadingService.Show();

                _brand.Name = Name.Text.Trim();
                // Nullable in DTO, so UI allows null/empty.
                _brand.ImageUrl = string.IsNullOrWhiteSpace(ImageUrl.Text) ? null : ImageUrl.Text.Trim();

                var result = await _brandService.UpdateAsync(_brand);
                if (result.Success)
                {
                    MessageBox.Show("Update completed successfully!");
                }
                else
                {
                    MessageBox.Show(result.Message ?? "Failed to update brand.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error while updating brand: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            var createCategory = ((App)System.Windows.Application.Current)
                       .ServiceProvider.GetRequiredService<BrandsTable>();
            createCategory.Show();
            this.Close();
        }
    }
}
