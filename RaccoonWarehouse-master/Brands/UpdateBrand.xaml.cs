using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Categories;
using RaccoonWarehouse.Domain.Brands.DTOs;
using RaccoonWarehouse.Domain.Categories;
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
    /// Interaction logic for UpdateBrand.xaml
    /// </summary>
    public partial class UpdateBrand : Window
    {
        private readonly IBrandService _brandService;
        private readonly IMapper _mapper;
        private int _brandId;
        BrandWriteDto _brand =new  BrandWriteDto(); 
        public UpdateBrand(IBrandService brandService ,IMapper mapper)
        {
            _brandService = brandService;
            _mapper = mapper;
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }
        public async void Initialize(int Id)
        {
            _brandId = Id;
            Category_Load(_brandId);
        }
        private async void Category_Load(int Id)
        {
            var result = await _brandService.GetWriteDtoByIdAsync(Id);
            _brand = result.Data;
            if (result.Success)
            {
                Name.Text = result.Data.Name;
                ImageUrl.Text = result.Data.ImageUrl;

            }
        }
        private async void Update_CategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Name.Text))  
            {
                MessageBox.Show("يجب ادخال اسم العلامة التجارية");
                return;
            }
            else
            {
                _brand.Name =  Name.Text;
                _brand.ImageUrl = ImageUrl.Text;

                var result = await _brandService.UpdateAsync(_brand);
                if (result.Success)
                {

                    MessageBox.Show("!تم التحديث بنجاح");
                }


            }
        }
        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
          var createCategory = ((App)System.Windows.Application.Current)
                       .ServiceProvider.GetRequiredService<BrandsTable>();
            createCategory.Show();
            this.Close();


        }
    }
}
