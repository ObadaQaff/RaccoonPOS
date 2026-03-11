using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Categories;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Categories.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RaccoonWarehouse.Categories
{
    /// <summary>
    /// Interaction logic for UpdateCategory.xaml
    /// </summary>
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

        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }
        public async void Initialize(int Id)
        {
            _categoryId = Id;
            await Category_LoadAsync(_categoryId);
        }

        private async Task Category_LoadAsync(int Id)
        {
            try
            {
                _loadingService.Show();
                var result = await _categoryService.GetWriteDtoByIdAsync(Id);
                if (result.Success && result.Data != null)
                {
                    _category = result.Data;
                    CategoryDes.Text = result.Data.Description;
                    CategoryName.Text = result.Data.Name;
                }
                else
                {
                    MessageBox.Show(result.Message ?? "Failed to load category data.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error while loading category: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private async void Update_CategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CategoryName.Text) ||
              string.IsNullOrWhiteSpace(CategoryDes.Text))
            {
                MessageBox.Show("Please fill in all required fields.");
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
                    MessageBox.Show("Update was successful!");
                }
                else
                {
                    MessageBox.Show(result.Message ?? "Failed to update category.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error while updating category: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }
        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
