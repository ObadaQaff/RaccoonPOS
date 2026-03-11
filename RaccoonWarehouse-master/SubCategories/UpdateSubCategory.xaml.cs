using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Categories;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Categories;
using RaccoonWarehouse.Domain.SubCategories.DTOs;
using RaccoonWarehouse;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RaccoonWarehouse.SubCategories
{
    /// <summary>
    /// Interaction logic for UpdateSubCategory.xaml
    /// </summary>
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
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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
                }
                else
                {
                    MessageBox.Show(result.Message ?? "Failed to load sub-category data.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error while loading sub-category: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private async void UpdateSubCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            // Required only by non-nullable DTO fields: Name + ParentCategoryId.
            if (string.IsNullOrWhiteSpace(SubCategoryName.Text) || ParentCategoryCombo.SelectedValue == null)
            {
                MessageBox.Show("Please fill in all required fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _loadingService.Show();

                _writeDto.Name = SubCategoryName.Text.Trim();
                _writeDto.ParentCategoryId = Convert.ToInt32(ParentCategoryCombo.SelectedValue);
                // Nullable fields are allowed to be null/empty in UI.
                _writeDto.Description = string.IsNullOrWhiteSpace(SubCategoryDes.Text) ? null : SubCategoryDes.Text.Trim();
                _writeDto.ImageUrl = string.IsNullOrWhiteSpace(SubCategoryImageUrl.Text) ? null : SubCategoryImageUrl.Text.Trim();

                var result = await _subCategoryService.UpdateAsync(_writeDto);
                if (result.Success)
                {
                    MessageBox.Show("Sub-category updated successfully!");
                }
                else
                {
                    MessageBox.Show(result.Message ?? "Failed to update sub-category.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error while updating sub-category: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }
    }
}
