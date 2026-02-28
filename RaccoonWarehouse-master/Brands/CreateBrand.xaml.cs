using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Categories;
using RaccoonWarehouse.Domain.Brands.DTOs;
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

namespace RaccoonWarehouse.Brands
{
    /// <summary>
    /// Interaction logic for CreateBrand.xaml
    /// </summary>
    public partial class CreateBrand : Window
    {
        private readonly IBrandService _brandService;
        private readonly IMapper _mapper;
        public CreateBrand(IBrandService brandService ,IMapper mapper)
        {
            _brandService = brandService;
            _mapper = mapper;
            InitializeComponent();
        }
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var createCategory = ((App)System.Windows.Application.Current)
                                 .ServiceProvider.GetRequiredService<BrandsTable>();
            createCategory.Show();
            this.Close();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            BrandWriteDto categoryWriteDto = new BrandWriteDto
            {
                Name = Name.Text,
                ImageUrl  = ImageUrl.Text,
            };
            var result = await _brandService.CreateAsync(categoryWriteDto);
            if (result.Success)
            {
                MessageBox.Show(" تم اضافة العلامة التجارية بنجاح!");
                Name.Text = "";
                ImageUrl.Text = "";

            }

        }
    }
}
