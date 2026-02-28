using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Categories;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Interaction logic for CreateSubCategory.xaml
    /// </summary>
    public partial class CreateSubCategory : Window
    {
        private readonly ICategoryService _categoryService;
        private readonly ISubCategoryService _subCategoryService;
        public CreateSubCategory(ICategoryService categoryService,ISubCategoryService subCategoryService)
        {
            _categoryService = categoryService;
            _subCategoryService = subCategoryService;
            InitializeComponent();
            Load_data();
        }
        private async void Load_data()
        {
            var categories = await _categoryService.GetAllAsync();
            ParentCategoryCombo.ItemsSource = categories.Data;
            ParentCategoryCombo.DisplayMemberPath = "Name";
            ParentCategoryCombo.SelectedValuePath = "Id";
        }
        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SubCategoryName.Text) || ParentCategoryCombo.SelectedItem == null)
            {
                MessageBox.Show("Please fill in all required fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var newSubCategory = new Domain.SubCategories.DTOs.SubCategoryWriteDto
            {
                Name = SubCategoryName.Text,
                ParentCategoryId = (int)ParentCategoryCombo.SelectedValue,
                Description = SubCategoryDes.Text,
                ImageUrl = SubCategoryImageUrl.Text,
            };
            var result = await _subCategoryService.CreateAsync(newSubCategory);
            if (result.Success)
            {
                MessageBox.Show("Sub-category created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            else
            {
                string errors = string.Join("\n", result.Errors);
                MessageBox.Show($"Failed to create sub-category:\n{errors}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


       
        private async void CreateSubCategoryBtn_Click(object sender, RoutedEventArgs e)
        {

            if (string.IsNullOrWhiteSpace(SubCategoryName.Text) || ParentCategoryCombo.SelectedItem == null)
            {
                MessageBox.Show("Please fill in all required fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var newSubCategory = new Domain.SubCategories.DTOs.SubCategoryWriteDto
            {
                Name = SubCategoryName.Text,
                ParentCategoryId = (int)ParentCategoryCombo.SelectedValue,
                Description = SubCategoryDes.Text,
                ImageUrl = SubCategoryImageUrl.Text,
            };
            var result = await _subCategoryService.CreateAsync(newSubCategory);
            if (result.Success)
            {
                MessageBox.Show("Sub-category created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                string errors = string.Join("\n", result.Errors);
                MessageBox.Show($"Failed to create sub-category:\n{errors}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {

            //var createCategory = ((RaccoonWarehouse.App)System.Windows.Application.Current)
            //.ServiceProvider.GetRequiredService<SubCategoryTable>();
            //createCategory.Show();
            this.Close();
        }
    }
}
