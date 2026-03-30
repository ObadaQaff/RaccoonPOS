using AutoMapper;
using RaccoonWarehouse;
using RaccoonWarehouse.Application.Service.Categories;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.SubCategories.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse.SubCategories
{
    public partial class UpdateSubCategory : Window
    {
        private readonly ISubCategoryService _subCategoryService;
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;
        private readonly ILoadingService _loadingService;
        private SubCategoryWriteDto _writeDto;

        public UpdateSubCategory(
            ISubCategoryService subCategoryService,
            ICategoryService categoryService,
            IMapper mapper,
            ILoadingService loadingService)
        {
            _writeDto = new SubCategoryWriteDto();
            _subCategoryService = subCategoryService;
            _mapper = mapper;
            _categoryService = categoryService;
            _loadingService = loadingService;
            InitializeComponent();
            UiText.ApplyWindow(this);
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public async void Load_SubCategory_For_Update(int id)
        {
            await LoadSubCategoryForUpdateAsync(id);
        }

        private async Task LoadSubCategoryForUpdateAsync(int id)
        {
            try
            {
                _loadingService.Show();

                var result = await _subCategoryService.GetWriteDtoByIdAsync(id);
                var categoriesResult = await _categoryService.GetAllAsync();
                ParentCategoryCombo.ItemsSource = categoriesResult.Data;
                ParentCategoryCombo.DisplayMemberPath = "Name";
                ParentCategoryCombo.SelectedValuePath = "Id";

                if (result.Success && result.Data != null)
                {
                    var subCategory = result.Data;
                    _writeDto = subCategory;
                    SubCategoryName.Text = subCategory.Name;
                    SubCategoryDes.Text = subCategory.Description;
                    SubCategoryImageUrl.Text = subCategory.ImageUrl;
                    ParentCategoryCombo.SelectedValue = subCategory.ParentCategoryId;
                    UiText.ApplyTranslations(this);
                }
                else
                {
                    MessageBox.Show(result.Message ?? UiText.T("تعذر تحميل بيانات الفئة الفرعية.", "Could not load the subcategory data."));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحميل الفئة الفرعية", "An error occurred while loading the subcategory")}: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private async void UpdateSubCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SubCategoryName.Text) || ParentCategoryCombo.SelectedValue == null)
            {
                MessageBox.Show(
                    UiText.T("يرجى تعبئة جميع الحقول المطلوبة.", "Please fill in all required fields."),
                    UiText.T("خطأ في التحقق", "Validation Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                _loadingService.Show();

                _writeDto.Name = SubCategoryName.Text.Trim();
                _writeDto.ParentCategoryId = Convert.ToInt32(ParentCategoryCombo.SelectedValue);
                _writeDto.Description = string.IsNullOrWhiteSpace(SubCategoryDes.Text) ? null : SubCategoryDes.Text.Trim();
                _writeDto.ImageUrl = string.IsNullOrWhiteSpace(SubCategoryImageUrl.Text) ? null : SubCategoryImageUrl.Text.Trim();

                var result = await _subCategoryService.UpdateAsync(_writeDto);
                if (result.Success)
                {
                    MessageBox.Show(UiText.T("تم تحديث الفئة الفرعية بنجاح.", "The subcategory was updated successfully."));
                }
                else
                {
                    MessageBox.Show(result.Message ?? UiText.T("فشل تحديث الفئة الفرعية.", "Failed to update the subcategory."));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحديث الفئة الفرعية", "An error occurred while updating the subcategory")}: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }
    }
}
