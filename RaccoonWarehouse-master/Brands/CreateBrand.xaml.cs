using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse;
using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Brands.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Brands
{
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
            UiText.ApplyWindow(this);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var brandsTable = ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<BrandsTable>();
            brandsTable.Show();
            Close();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Name.Text))
            {
                MessageBox.Show(UiText.T("يرجى إدخال اسم العلامة التجارية.", "Please enter the brand name."));
                return;
            }

            try
            {
                _loadingService.Show();

                var brandWriteDto = new BrandWriteDto
                {
                    Name = Name.Text.Trim(),
                    ImageUrl = string.IsNullOrWhiteSpace(ImageUrl.Text) ? null : ImageUrl.Text.Trim(),
                };

                var result = await _brandService.CreateAsync(brandWriteDto);
                if (result.Success)
                {
                    MessageBox.Show(UiText.T("تمت إضافة العلامة التجارية بنجاح.", "The brand was added successfully."));
                    Name.Clear();
                    ImageUrl.Clear();
                }
                else
                {
                    MessageBox.Show(result.Message ?? UiText.T("فشل إنشاء العلامة التجارية.", "Failed to create the brand."));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء إنشاء العلامة التجارية", "An error occurred while creating the brand")}: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }
    }
}
