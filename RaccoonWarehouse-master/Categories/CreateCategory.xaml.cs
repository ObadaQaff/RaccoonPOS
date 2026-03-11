using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Categories;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Categories.DTOs;
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

namespace RaccoonWarehouse.Categories
{
    /// <summary>
    /// Interaction logic for CreateCategory.xaml
    /// </summary>
    public partial class CreateCategory : Window
    {
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;
        private readonly ILoadingService _loadingService;

        public CreateCategory(ICategoryService categoryService, IMapper mapper, ILoadingService loadingService)
        {
            _categoryService = categoryService;
            _mapper = mapper;
            _loadingService = loadingService;
            InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Name.Text))
                {
                    MessageBox.Show("Please enter a valid category name.");
                    return;
                }

                _loadingService.Show();

                CategoryWriteDto categoryWriteDto = new CategoryWriteDto
                {
                    Name = Name.Text.Trim(),
                    Description = string.IsNullOrWhiteSpace(Description.Text) ? null : Description.Text.Trim(),
                };

                var result = await _categoryService.CreateAsync(categoryWriteDto);
                if (result.Success)
                {
                    MessageBox.Show("Category added was successfully !");
                    Name.Text = "";
                    Description.Text = "";
                }
                else
                {
                    MessageBox.Show(result.Message ?? "Failed to create category.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error while creating category: {ex.Message}");
            }
            finally
            {
                _loadingService.Hide();
            }
        }
    }
}
