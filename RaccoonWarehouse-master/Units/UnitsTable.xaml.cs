using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Domain.Units.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System.Windows;

namespace RaccoonWarehouse.Units
{
    public partial class UnitsTable : Window
    {
        private readonly IMapper _mapper;
        private readonly IUnitService _unitService;

        public UnitsTable(IUnitService unitService, IMapper mapper)
        {
            _mapper = mapper;
            _unitService = unitService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            Load_Units();
        }

        private async void Load_Units()
        {
            var result = await _unitService.GetAllAsync();
            if (result.Success)
            {
                UnitsTable1.ItemsSource = result.Data;
                UiText.ApplyTranslations(this);
            }
        }

        private void Update_Unit(object sender, RoutedEventArgs e)
        {
            if (UnitsTable1.SelectedItem is not UnitReadDto selectedUnit)
            {
                MessageBox.Show(UiText.T("يجب عليك تحديد وحدة للتمكن من التعديل.", "You must select a unit before editing."));
                return;
            }

            WindowManager.ShowDialog<UpdateUnit>(
                WindowSizeType.SmallSquare,
                async w => w.Initialize(selectedUnit.Id));
        }

        private async void Delete_Unit(object sender, RoutedEventArgs e)
        {
            if (UnitsTable1.SelectedItem is not UnitReadDto selectedUnit)
                return;

            var messageResult = MessageBox.Show(
                UiText.IsEnglish
                    ? $"Are you sure you want to delete the unit '{selectedUnit.Name}'?"
                    : $"هل أنت متأكد من أنك تريد حذف الوحدة '{selectedUnit.Name}'؟",
                UiText.T("تأكيد الحذف", "Confirm Delete"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (messageResult == MessageBoxResult.Yes)
            {
                await _unitService.DeleteAsync(selectedUnit.Id);
                MessageBox.Show(UiText.T("تم الحذف بنجاح.", "Delete was successful."));
                Load_Units();
            }
        }

        private void CreateCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            var createUnit = ((App)System.Windows.Application.Current)
                .ServiceProvider.GetRequiredService<CreateUnit>();
            createUnit.ShowDialog();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
