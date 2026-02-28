using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse;
using RaccoonWarehouse.Application.Service.Brands;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Brands;
using RaccoonWarehouse.Domain.Brands.DTOs;
using RaccoonWarehouse.Domain.Units.DTOs;
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

namespace RaccoonWarehouse.Units
{
    /// <summary>
    /// Interaction logic for UnitsTable.xaml
    /// </summary>
    public partial class UnitsTable : Window
    {

        private readonly IMapper _mapper;
        private readonly IUnitService _unitService;

        public UnitsTable(IUnitService unitService, IMapper mapper)
        {
            _mapper = mapper;
            _unitService = unitService;
            InitializeComponent();
            Load_Units();

        }

        private async void Load_Units()
        {

            var result = await _unitService.GetAllAsync();
            if (result.Success)
            {
                UnitsTable1.ItemsSource = result.Data;

            }

        }
        private void Update_Unit(object sender, RoutedEventArgs e)
        {

            if (UnitsTable1.SelectedItem is not UnitReadDto selectedBrand)
            {
                MessageBox.Show("يجب عليك تحديد وحدة للتمكن من التعديل ");
                return;
            }

           
            WindowManager.ShowDialog<UpdateUnit>(
                WindowSizeType.SmallSquare,
                async w =>  w.Initialize(selectedBrand.Id)
            );


        }
        private async void Delete_Unit(object sender, RoutedEventArgs e)
        {

            var selectedCategory = UnitsTable1.SelectedItem as UnitReadDto;
            if (selectedCategory != null)
            {
                var messageResult = MessageBox.Show(
                $" ؟'{selectedCategory.Name}' :هل انت متاكد من انك تريد حذف الوحدة",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

                if (messageResult == MessageBoxResult.Yes)
                {
                    await _unitService.DeleteAsync(selectedCategory.Id);
                    MessageBox.Show("تم الحذف بنجاح !!");
                    Load_Units();

                }
            }
        }

        private void CreateCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            var createCategory = ((App)System.Windows.Application.Current)
                       .ServiceProvider.GetRequiredService<CreateUnit>();
            createCategory.ShowDialog();
           

        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {

            this.Close();
        }

    }
}
