using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse;
using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Application.Service.Categories;
using RaccoonWarehouse.Categories;
using RaccoonWarehouse.Domain.Brands.DTOs;
using RaccoonWarehouse.Domain.Categories.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Units;
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

namespace RaccoonWarehouse.Brands
{
    /// <summary>
    /// Interaction logic for BrandsTable.xaml
    /// </summary>
    public partial class BrandsTable : Window
    {
        private readonly IBrandService _brandService;
        private readonly IMapper _mapper;
        public BrandsTable(IBrandService brandService ,IMapper mapper)
        {
            _brandService = brandService;
            _mapper = mapper;
            InitializeComponent();
            Load_Brands();
        }


        private async void Load_Brands()
        {

            var result = await _brandService.GetAllAsync();
            if (result.Success)
            {
                BrandsTable1.ItemsSource = result.Data;

            }

        }
        private void Update_Brand(object sender, RoutedEventArgs e)
        {

            if (BrandsTable1.SelectedItem is not BrandReadDto selectedBrand)
            {
                MessageBox.Show("يحب تحديد علامة تجارية قبل القيام بالتحديث او الحذف ");
                return;
            }

            WindowManager.ShowDialog<UpdateBrand>(WindowSizeType.MediumRectangle,w =>
            {
                w.Initialize(selectedBrand.Id);
            });

        }
        private async void Delete_Brand(object sender, RoutedEventArgs e)
        {

            var selectedCategory = BrandsTable1.SelectedItem as BrandReadDto;
            if (selectedCategory != null)
            {
                var messageResult = MessageBox.Show(
                $"هل انت متاكد من انك تريد حذف العلامة التجارية : \'{selectedCategory.Name}\' ?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

                if (messageResult == MessageBoxResult.Yes)
                {
                    await _brandService.DeleteAsync(selectedCategory.Id);
                    MessageBox.Show("تم الحذف بنجاح !!");
                    Load_Brands();

                }
            }
        }

        private void CreateCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            var createCategory = ((App)System.Windows.Application.Current)
                       .ServiceProvider.GetRequiredService<CreateBrand>();
            createCategory.ShowDialog();

        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {

            this.Close();
        }
    }
}
