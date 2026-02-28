using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Brands;
using RaccoonWarehouse.Domain.Brands;
using RaccoonWarehouse.Domain.Units.DTOs;
using RaccoonWarehouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace RaccoonWarehouse.Units
{
    /// <summary>
    /// Interaction logic for UpdateUnit.xaml
    /// </summary>
    public partial class UpdateUnit : Window
    {
        private readonly IMapper _mapper;
        private readonly IUnitService _unitService;
        private int _Id;
        private UnitWriteDto _unit = new UnitWriteDto();
        public UpdateUnit(IUnitService unitService,IMapper mapper)
        {
            _unitService = unitService;
            _mapper = mapper;
            InitializeComponent();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }
        public async void Initialize(int Id)
        {
            _Id = Id;
            Unit_Load(_Id);
        }   

        private async void Unit_Load(int Id)
        {
            var result = await _unitService.GetWriteDtoByIdAsync(Id);
            _unit = result.Data;
            if (result.Success)
            {
                Name.Text = result.Data.Name;

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
                _unit.Name = Name.Text;

                var result = await _unitService.UpdateAsync(_unit);
                if (result.Success)
                {

                    MessageBox.Show("!تم التحديث بنجاح");
                }


            }
        }
        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
           Close();



        }
    }
}
