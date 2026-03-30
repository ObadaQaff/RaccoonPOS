using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Categories;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace RaccoonWarehouse.SubCategories
{
    /// <summary>
    /// Interaction logic for CreateSubCategory.xaml
    /// </summary>
    public partial class CreateSubCategory : Window
    {
        private readonly ICategoryService _categoryService;
        private readonly ISubCategoryService _subCategoryService;
        private readonly ILoadingService _loadingService;

        public CreateSubCategory(
            ICategoryService categoryService,
            ISubCategoryService subCategoryService,
            ILoadingService loadingService)
        {
            _categoryService = categoryService;
            _subCategoryService = subCategoryService;
            _loadingService = loadingService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                _loadingService.Show();
                var categories = await _categoryService.GetAllAsync();
                ParentCategoryCombo.ItemsSource = categories.Data;
                ParentCategoryCombo.DisplayMemberPath = "Name";
                ParentCategoryCombo.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحميل الفئات", "An error occurred while loading categories")}: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private bool ValidateRequiredFields()
        {
            // Required only by non-nullable DTO fields: Name + ParentCategoryId.
            if (string.IsNullOrWhiteSpace(SubCategoryName.Text) || ParentCategoryCombo.SelectedItem == null)
            {
                MessageBox.Show(UiText.T("يرجى تعبئة جميع الحقول المطلوبة.", "Please fill in all required fields."), UiText.T("خطأ في التحقق", "Validation Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private async Task CreateSubCategoryAsync(bool closeAfterSuccess)
        {
            if (!ValidateRequiredFields())
            {
                return;
            }

            try
            {
                _loadingService.Show();

                var newSubCategory = new Domain.SubCategories.DTOs.SubCategoryWriteDto
                {
                    Name = SubCategoryName.Text.Trim(),
                    ParentCategoryId = (int)ParentCategoryCombo.SelectedValue,
                    // Nullable fields are allowed to be null/empty in UI.
                    Description = string.IsNullOrWhiteSpace(SubCategoryDes.Text) ? null : SubCategoryDes.Text.Trim(),
                    ImageUrl = string.IsNullOrWhiteSpace(SubCategoryImageUrl.Text) ? null : SubCategoryImageUrl.Text.Trim(),
                };

                var result = await _subCategoryService.CreateAsync(newSubCategory);
                if (result.Success)
                {
                    MessageBox.Show(UiText.T("تم إنشاء الفئة الفرعية بنجاح.", "The subcategory was created successfully."), UiText.T("نجاح", "Success"), MessageBoxButton.OK, MessageBoxImage.Information);
                    if (closeAfterSuccess)
                    {
                        Close();
                    }
                }
                else
                {
                    var errors = string.Join("\n", result.Errors ?? new System.Collections.Generic.List<string>());
                    var message = string.IsNullOrWhiteSpace(errors) ? (result.Message ?? UiText.T("فشل إنشاء الفئة الفرعية.", "Failed to create the subcategory.")) : errors;
                    MessageBox.Show($"{UiText.T("فشل إنشاء الفئة الفرعية", "Failed to create the subcategory")}:\n{message}", UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء إنشاء الفئة الفرعية", "An error occurred while creating the subcategory")}: {ex.Message}", UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            await CreateSubCategoryAsync(closeAfterSuccess: true);
        }

        private async void CreateSubCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            await CreateSubCategoryAsync(closeAfterSuccess: false);
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
