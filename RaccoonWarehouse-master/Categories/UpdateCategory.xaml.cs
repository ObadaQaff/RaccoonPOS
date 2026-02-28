using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Categories;
using RaccoonWarehouse.Application.Service.Users;
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
        private int _categoryId;


        public UpdateCategory(ICategoryService categoryService, IMapper mapper)
        {
            _mapper = mapper;
            _categoryService = categoryService;
            InitializeComponent();
            _category = new CategoryWriteDto();

        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }
        public async void Initialize(int Id)
        {
            _categoryId = Id;
            Category_Load(_categoryId);
        }
        private async void Category_Load(int Id)
        {
            var result = await _categoryService.GetWriteDtoByIdAsync(Id);
            _category = result.Data;
            if (result.Success)
            {
                CategoryDes.Text = result.Data.Description;
                CategoryName.Text = result.Data.Name;

            }
        }
        private async void Update_CategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CategoryName.Text) ||
              string.IsNullOrWhiteSpace(CategoryDes.Text) )
            {
                MessageBox.Show("Please fill in all required fields.");
                return;
            }
            else
            {
                _category.Name = CategoryName.Text;
                _category.Description = CategoryDes.Text;
            
                var result = await _categoryService.UpdateAsync(_category);
                if (result.Success) {

                    MessageBox.Show("Update Was successfully!");
                }


            }
        }
        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
     
            this.Close();


        }
    }
}
