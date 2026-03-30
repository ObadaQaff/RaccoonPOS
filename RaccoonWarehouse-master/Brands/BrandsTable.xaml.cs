using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse;
using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Brands.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse.Brands
{
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
            UiText.ApplyWindow(this);
            _ = LoadBrandsAsync();
        }

        private async Task LoadBrandsAsync()
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
                    MessageBox.Show(result.Message ?? UiText.T("فشل تحميل العلامات التجارية.", "Failed to load brands."));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحميل العلامات التجارية", "An error occurred while loading brands")}: {ex.Message}");
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
                MessageBox.Show(UiText.T("يرجى اختيار علامة تجارية قبل التعديل أو الحذف.", "Please select a brand before editing or deleting."));
                return;
            }

            WindowManager.ShowDialog<UpdateBrand>(WindowSizeType.MediumRectangle, w =>
            {
                w.Initialize(selectedBrand.Id);
            });
        }

        private async void Delete_Brand(object sender, RoutedEventArgs e)
        {
            if (BrandsTable1.SelectedItem is not BrandReadDto selectedBrand)
            {
                MessageBox.Show(UiText.T("لم يتم اختيار علامة تجارية.", "No brand selected."));
                return;
            }

            var message = UiText.IsEnglish
                ? $"Are you sure you want to delete the brand '{selectedBrand.Name}'?"
                : $"هل أنت متأكد من أنك تريد حذف العلامة التجارية '{selectedBrand.Name}'؟";

            var messageResult = MessageBox.Show(
                message,
                UiText.T("تأكيد الحذف", "Confirm Delete"),
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
                    MessageBox.Show(UiText.T("تم الحذف بنجاح.", "Delete was successful."));
                    await LoadBrandsAsync();
                }
                else
                {
                    MessageBox.Show(result.Message ?? UiText.T("فشل الحذف.", "Delete failed."));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء حذف العلامة التجارية", "An error occurred while deleting the brand")}: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void CreateCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            var createBrand = ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<CreateBrand>();
            createBrand.ShowDialog();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
