using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Brands;
using RaccoonWarehouse.Domain.Brands.DTOs;
using RaccoonWarehouse.Domain.Units.DTOs;
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

namespace RaccoonWarehouse.Units
{
    /// <summary>
    /// Interaction logic for CreateUnit.xaml
    /// </summary>
    public partial class CreateUnit : Window
    {
        private readonly IUnitService _unitService;
        private readonly IMapper _mapper;
        public CreateUnit(IUnitService unitService ,IMapper mapper)
        {
            _unitService = unitService;
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
            UnitWriteDto unitWriteDto = new UnitWriteDto
            {
                Name = Name.Text,
            };
            var result = await _unitService.CreateAsync(unitWriteDto);
            if (result.Success)
            {
                MessageBox.Show(" تم اضافة  الوحدة بنجاح!");
                Name.Text = "";

            }

        }
    }
}
