using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Categories;
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
        
        public CreateCategory(ICategoryService categoryService, IMapper mapper)
        {
            _categoryService = categoryService;
            _mapper = mapper;
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
            CategoryWriteDto categoryWriteDto = new CategoryWriteDto
            {
               Name = Name.Text,
               Description = Description.Text,
            };
            var result =await _categoryService.CreateAsync(categoryWriteDto);
            if (result.Success) 
            {
                MessageBox.Show("Category added was successfully !");
                Name.Text = "";
                Description.Text = "";
            
            }

        }
    }
}
