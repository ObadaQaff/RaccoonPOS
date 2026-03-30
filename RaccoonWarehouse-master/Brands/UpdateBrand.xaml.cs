using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse;
using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Brands.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse.Brands
{
    public partial class UpdateBrand : Window
    {
        private readonly IBrandService _brandService;
        private readonly IMapper _mapper;
        private readonly ILoadingService _loadingService;
        private int _brandId;
        private BrandWriteDto _brand = new();

        public UpdateBrand(IBrandService brandService, IMapper mapper, ILoadingService loadingService)
        {
            _brandService = brandService;
            _mapper = mapper;
            _loadingService = loadingService;
            InitializeComponent();
            UiText.ApplyWindow(this);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public async void Initialize(int id)
        {
            _brandId = id;
            await LoadBrandAsync(_brandId);
        }

        private async Task LoadBrandAsync(int id)
        {
            try
            {
                _loadingService.Show();
                var result = await _brandService.GetWriteDtoByIdAsync(id);
                if (result.Success && result.Data != null)
                {
                    _brand = result.Data;
                    Name.Text = result.Data.Name;
                    ImageUrl.Text = result.Data.ImageUrl;
                }
                else
                {
                    MessageBox.Show(result.Message ?? UiText.T("تعذر تحميل بيانات العلامة التجارية.", "Could not load the brand data."));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحميل العلامة التجارية", "An error occurred while loading the brand")}: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private async void Update_CategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Name.Text))
            {
                MessageBox.Show(UiText.T("اسم العلامة التجارية مطلوب.", "The brand name is required."));
                return;
            }

            try
            {
                _loadingService.Show();

                _brand.Name = Name.Text.Trim();
                _brand.ImageUrl = string.IsNullOrWhiteSpace(ImageUrl.Text) ? null : ImageUrl.Text.Trim();

                var result = await _brandService.UpdateAsync(_brand);
                if (result.Success)
                {
                    MessageBox.Show(UiText.T("تم تحديث العلامة التجارية بنجاح.", "The brand was updated successfully."));
                }
                else
                {
                    MessageBox.Show(result.Message ?? UiText.T("فشل تحديث العلامة التجارية.", "Failed to update the brand."));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحديث العلامة التجارية", "An error occurred while updating the brand")}: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            var brandsTable = ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<BrandsTable>();
            brandsTable.Show();
            Close();
        }
    }
}
