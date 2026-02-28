using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse;
using RaccoonWarehouse.Application.Service.Categories;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Categories.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Navigation;
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
    /// Interaction logic for CategoriesTable.xaml
    /// </summary>
    public partial class CategoriesTable : Window
    {
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;
        private string _currentNameSearch = "";
        private CancellationTokenSource _searchCts;

        public CategoriesTable(ICategoryService categoryService,IMapper mapper)
        {
            _categoryService = categoryService;
            _mapper = mapper;
            InitializeComponent();
            Load_Categories();
        }
        private async void Load_Categories()
        {

            var result  = await _categoryService.GetAllAsync();
            if (result.Success)
            {
                CategoriesTable1.ItemsSource=result.Data;
            
            }

        }
        private void Update_Category(object sender, RoutedEventArgs e)
        {
            if (CategoriesTable1.SelectedItem is not CategoryReadDto selectedCategory)
            {
                MessageBox.Show("No category selected.");
                return;
            }

            WindowManager.ShowDialog<UpdateCategory>(WindowSizeType.MediumRectangle, w =>
            {
                w.Initialize(selectedCategory.Id);
            });

        }
        private async void Delete_Category(object sender, RoutedEventArgs e) {

            var selectedCategory = CategoriesTable1.SelectedItem as CategoryReadDto;
            if (selectedCategory != null)
            {
                var messageResult = MessageBox.Show(
                $"Are you sure you want to delete  \'{selectedCategory.Name}\' Category ?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

                if (messageResult == MessageBoxResult.Yes)
                {
                    _categoryService.DeleteAsync(selectedCategory.Id);
                    MessageBox.Show("Delete was successfully !!");
                    Load_Categories();

                }
            }
        }
       

        private void CreateCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            var createCategory = ((App)System.Windows.Application.Current)
                       .ServiceProvider.GetRequiredService<CreateCategory>();
            createCategory.Show();
            this.Hide();

        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {

            this.Close();
        }
    }
}
