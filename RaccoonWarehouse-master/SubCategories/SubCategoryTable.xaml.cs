using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.SubCategories.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Stocks;
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
    /// Interaction logic for SubCategoryTable.xaml
    /// </summary>
    public partial class SubCategoryTable : Window
    {
        private readonly ISubCategoryService _subCategoryService;
        private readonly IMapper _mapper;
        private readonly ILoadingService _loadingService;

        public SubCategoryTable(ISubCategoryService subCategoryService, IMapper mapper, ILoadingService loadingService)
        {
            _subCategoryService = subCategoryService;
            _mapper = mapper;
            _loadingService = loadingService;
            InitializeComponent();
            _ = Load_SubCategoriesAsync();
        }

        private async Task Load_SubCategoriesAsync()
        {
            try
            {
                _loadingService.Show();

                var result = await _subCategoryService.GetAllWithIncludeAsync(s => s.ParentCategory);
                if (result.Success)
                {
                    SubCategoriesTable1.ItemsSource = result.Data;
                }
                else
                {
                    MessageBox.Show(result.Message ?? "Failed to load sub-categories.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error while loading sub-categories: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void CreateCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            var createCategory = ((RaccoonWarehouse.App)System.Windows.Application.Current)
                       .ServiceProvider.GetRequiredService<CreateSubCategory>();
            createCategory.Show();
            this.Hide();

        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {

            this.Close();
        }

        private void CreateSubCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            var createCategory = ((RaccoonWarehouse.App)System.Windows.Application.Current)
                     .ServiceProvider.GetRequiredService<CreateSubCategory>();
            createCategory.Show();
            this.Hide();
        }

        private async void Delete_SubCategory(object sender, RoutedEventArgs e)
        {
            var selectedSubCategory = SubCategoriesTable1.SelectedItem as SubCategoryReadDto;
            if (selectedSubCategory == null)
            {
                MessageBox.Show("No subcategory selected.");
                return;
            }

            var messageResult = MessageBox.Show(
                $"Are you sure you want to delete  '{selectedSubCategory.Name}' Category ?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (messageResult != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                _loadingService.Show();
                var result = await _subCategoryService.DeleteAsync(selectedSubCategory.Id);

                if (result.Success)
                {
                    MessageBox.Show("Delete was successful !!");
                    await Load_SubCategoriesAsync();
                }
                else
                {
                    MessageBox.Show(result.Message ?? "Delete failed.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error while deleting sub-category: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void Update_SubCategory(object sender, RoutedEventArgs e)
        {
            if (SubCategoriesTable1.SelectedItem is not SubCategoryReadDto selectedCategory)
            {
                MessageBox.Show("No subcategory selected.");
                return;
            }

            WindowManager.ShowDialog<UpdateSubCategory>(WindowSizeType.MediumRectangle, w =>
            {
                w.Load_SubCategory_For_Update(selectedCategory.Id);
            });
        }
    }
}
