using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Categories;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Common.Loading;
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
                MessageBox.Show($"Unexpected error while loading categories: {ex.Message}");
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
                MessageBox.Show("Please fill in all required fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    MessageBox.Show("Sub-category created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    if (closeAfterSuccess)
                    {
                        Close();
                    }
                }
                else
                {
                    var errors = string.Join("\n", result.Errors ?? new System.Collections.Generic.List<string>());
                    var message = string.IsNullOrWhiteSpace(errors) ? (result.Message ?? "Failed to create sub-category.") : errors;
                    MessageBox.Show($"Failed to create sub-category:\n{message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error while creating sub-category: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
