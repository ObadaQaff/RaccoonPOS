using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Domain.SubCategories.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Stocks;
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
    /// Interaction logic for SubCategoryTable.xaml
    /// </summary>
    public partial class SubCategoryTable : Window
    {
        private readonly ISubCategoryService _subCategoryService;
        private readonly IMapper _mapper;
        public SubCategoryTable(ISubCategoryService subCategoryService, IMapper mapper)
        {
            _subCategoryService = subCategoryService;
            _mapper = mapper;
            InitializeComponent();
            Load_SubCategories();
        }
        private async void Load_SubCategories()
        {

            var result = await _subCategoryService.GetAllWithIncludeAsync(s=>s.ParentCategory);
            if (result.Success)
            {
                SubCategoriesTable1.ItemsSource = result.Data;

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
        private void Delete_SubCategory(object sender, RoutedEventArgs e)
        {
            var selectedSubCategory = SubCategoriesTable1.SelectedItem as SubCategoryReadDto;
            if (selectedSubCategory != null)
            {
                var messageResult = MessageBox.Show(
                $"Are you sure you want to delete  \'{selectedSubCategory.Name}\' Category ?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

                if (messageResult == MessageBoxResult.Yes)
                {
                    _subCategoryService.DeleteAsync(selectedSubCategory.Id);
                    MessageBox.Show("Delete was successfully !!");
                    Load_SubCategories();
                }
            }

        }
        private void Update_SubCategory(object sender, RoutedEventArgs e)
        {

            if (SubCategoriesTable1.SelectedItem is not SubCategoryReadDto selectedCategory)
            {
                MessageBox.Show("No subcategory selected.");
                return;
            }

            WindowManager.ShowDialog<UpdateSubCategory>(WindowSizeType.MediumRectangle,w =>
            {
                w.Load_SubCategory_For_Update(selectedCategory.Id);
            });
        }
    }
}
