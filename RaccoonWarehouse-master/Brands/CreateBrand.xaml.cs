using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Brands.DTOs;
using RaccoonWarehouse;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Brands
{
    /// <summary>
    /// Interaction logic for CreateBrand.xaml
    /// </summary>
    public partial class CreateBrand : Window
    {
        private readonly IBrandService _brandService;
        private readonly IMapper _mapper;
        private readonly ILoadingService _loadingService;

        public CreateBrand(IBrandService brandService, IMapper mapper, ILoadingService loadingService)
        {
            _brandService = brandService;
            _mapper = mapper;
            _loadingService = loadingService;
            InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var createCategory = ((App)System.Windows.Application.Current)
                                 .ServiceProvider.GetRequiredService<BrandsTable>();
            createCategory.Show();
            this.Close();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // Required by non-nullable DTO field.
            if (string.IsNullOrWhiteSpace(Name.Text))
            {
                MessageBox.Show("Please enter a valid brand name.");
                return;
            }

            try
            {
                _loadingService.Show();

                BrandWriteDto brandWriteDto = new BrandWriteDto
                {
                    Name = Name.Text.Trim(),
                    // Nullable in DTO, so UI allows null/empty.
                    ImageUrl = string.IsNullOrWhiteSpace(ImageUrl.Text) ? null : ImageUrl.Text.Trim(),
                };

                var result = await _brandService.CreateAsync(brandWriteDto);
                if (result.Success)
                {
                    MessageBox.Show("Brand added successfully!");
                    Name.Text = "";
                    ImageUrl.Text = "";
                }
                else
                {
                    MessageBox.Show(result.Message ?? "Failed to create brand.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error while creating brand: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }
    }
}
