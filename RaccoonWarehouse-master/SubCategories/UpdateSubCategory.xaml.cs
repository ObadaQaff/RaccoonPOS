using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Categories;
using RaccoonWarehouse.Application.Service.SubCategories;
using RaccoonWarehouse.Categories;
using RaccoonWarehouse.Domain.SubCategories.DTOs;
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
    /// Interaction logic for UpdateSubCategory.xaml
    /// </summary>
    public partial class UpdateSubCategory : Window
    {
        private readonly ISubCategoryService _subCategoryService;
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;
        private SubCategoryWriteDto _writeDto;
        public UpdateSubCategory(ISubCategoryService subCategoryService,ICategoryService categoryService,IMapper mapper)
        {
            _writeDto = new SubCategoryWriteDto();
            _subCategoryService = subCategoryService;
            _mapper = mapper;
            _categoryService = categoryService;
            InitializeComponent();
            
        }

        private async void BackBtn_Click(object sender, RoutedEventArgs e)
        {

            // var createCategory = ((RaccoonWarehouse.App)System.Windows.Application.Current)
            // .ServiceProvider.GetRequiredService<SubCategoryTable>();
            //createCategory.Show();
            this.Close();
       
        }
       
        public async void Load_SubCategory_For_Update(int id)
        {
            var result = await _subCategoryService.GetWriteDtoByIdAsync(id);
            var  categoriesResult = await _categoryService.GetAllAsync();
            ParentCategoryCombo.ItemsSource = categoriesResult.Data;
            
            if (result.Success)
            {
               
                var subCategory = result.Data;
                _writeDto = subCategory;
                SubCategoryName.Text = subCategory.Name;
                SubCategoryDes.Text = subCategory.Description;
                SubCategoryImageUrl.Text =subCategory.ImageUrl;
                ParentCategoryCombo.SelectedValue = subCategory.ParentCategoryId;

            }
            else
            {
                MessageBox.Show("Failed to load sub-category data.");
            }
        }
        private async void UpdateSubCategoryBtn_Click(object sender, RoutedEventArgs e)
        {

            _writeDto.Name = SubCategoryName.Text;
            _writeDto.Description = SubCategoryDes.Text;
            _writeDto.ImageUrl = SubCategoryImageUrl.Text;
            _writeDto.ParentCategoryId = Convert.ToInt32(ParentCategoryCombo.SelectedValue);

           
            var result = await _subCategoryService.UpdateAsync(_writeDto);
            if (result.Success)
            {
                MessageBox.Show("Sub-category updated successfully!");
            }
            else
            {
                MessageBox.Show("Failed to update sub-category.");
                return;
            }
        }
    }
}
