using AutoMapper;
using RaccoonWarehouse.Application.Service.Categories;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Categories.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse.Categories
{
    public partial class UpdateCategory : Window
    {
        private CategoryWriteDto _category;
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;
        private readonly ILoadingService _loadingService;
        private int _categoryId;

        public UpdateCategory(ICategoryService categoryService, IMapper mapper, ILoadingService loadingService)
        {
            _mapper = mapper;
            _categoryService = categoryService;
            _loadingService = loadingService;
            InitializeComponent();
            _category = new CategoryWriteDto();
            UiText.ApplyWindow(this);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public async void Initialize(int id)
        {
            _categoryId = id;
            await LoadCategoryAsync(_categoryId);
        }

        private async Task LoadCategoryAsync(int id)
        {
            try
            {
                _loadingService.Show();
                var result = await _categoryService.GetWriteDtoByIdAsync(id);
                if (result.Success && result.Data != null)
                {
                    _category = result.Data;
                    CategoryDes.Text = result.Data.Description;
                    CategoryName.Text = result.Data.Name;
                }
                else
                {
                    MessageBox.Show(result.Message ?? UiText.T("تعذر تحميل بيانات الفئة.", "Could not load the category data."));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحميل الفئة", "An error occurred while loading the category")}: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private async void Update_CategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_category == null || _category.Id <= 0)
            {
                MessageBox.Show(UiText.T("تعذر تحميل بيانات الفئة قبل التحديث.", "Could not load the category data before updating."));
                return;
            }

            if (string.IsNullOrWhiteSpace(CategoryName.Text) || string.IsNullOrWhiteSpace(CategoryDes.Text))
            {
                MessageBox.Show(UiText.T("يرجى تعبئة جميع الحقول المطلوبة.", "Please fill in all required fields."));
                return;
            }

            try
            {
                _loadingService.Show();

                _category.Name = CategoryName.Text.Trim();
                _category.Description = CategoryDes.Text.Trim();

                var result = await _categoryService.UpdateAsync(_category);
                if (result.Success)
                {
                    MessageBox.Show(UiText.T("تم تحديث الفئة بنجاح.", "The category was updated successfully."));
                }
                else
                {
                    MessageBox.Show(result.Message ?? UiText.T("فشل تحديث الفئة.", "Failed to update the category."));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحديث الفئة", "An error occurred while updating the category")}: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
