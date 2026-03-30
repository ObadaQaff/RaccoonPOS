using AutoMapper;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Common;
using RaccoonWarehouse.Domain.Units.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Windows;

namespace RaccoonWarehouse.Units
{
    public partial class UpdateUnit : Window
    {
        private readonly IMapper _mapper;
        private readonly IUnitService _unitService;
        private int _id;
        private UnitWriteDto _unit = new();

        public UpdateUnit(IUnitService unitService, IMapper mapper)
        {
            _unitService = unitService;
            _mapper = mapper;
            InitializeComponent();
            UiText.ApplyWindow(this);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public void Initialize(int id)
        {
            _id = id;
            Unit_Load(_id);
        }

        private async void Unit_Load(int id)
        {
            try
            {
                var result = await _unitService.GetWriteDtoByIdAsync(id);
                if (!result.Success || result.Data == null)
                {
                    MessageBox.Show(result.Message ?? UiText.T("تعذر تحميل بيانات الوحدة.", "Could not load the unit data."));
                    return;
                }

                _unit = result.Data;
                Name.Text = result.Data.Name;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحميل الوحدة", "An error occurred while loading the unit")}: {ex.Message}");
            }
        }

        private async void Update_CategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Name.Text))
            {
                MessageBox.Show(UiText.T("يجب إدخال اسم الوحدة.", "The unit name is required."));
                return;
            }

            try
            {
                _unit.Name = Name.Text.Trim();

                var result = await _unitService.UpdateAsync(_unit);
                if (result.Success)
                {
                    MessageBox.Show(UiText.T("تم التحديث بنجاح.", "The update completed successfully."));
                    CatalogRefreshNotifier.NotifyCatalogChanged();
                }
                else
                {
                    MessageBox.Show(result.Message ?? UiText.T("فشل تحديث الوحدة.", "Failed to update the unit."));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحديث الوحدة", "An error occurred while updating the unit")}: {ex.Message}");
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
