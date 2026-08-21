using AutoMapper;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Domain.Units.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Units
{
    public partial class CreateUnit : Window
    {
        private readonly IUnitService _unitService;
        private readonly IMapper _mapper;
        public int? CreatedUnitId { get; private set; }

        public CreateUnit(IUnitService unitService, IMapper mapper)
        {
            _unitService = unitService;
            _mapper = mapper;
            InitializeComponent();
            UiText.ApplyWindow(this);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Name.Text))
            {
                MessageBox.Show(UiText.T("يرجى إدخال اسم الوحدة.", "Please enter the unit name."));
                return;
            }

            var unitWriteDto = new UnitWriteDto
            {
                Name = Name.Text.Trim(),
            };

            var result = await _unitService.CreateAsync(unitWriteDto);
            if (result.Success)
            {
                MessageBox.Show(UiText.T("تمت إضافة الوحدة بنجاح.", "The unit was added successfully."));
                Name.Text = string.Empty;
            }
            else
            {
                MessageBox.Show(result.Message ?? UiText.T("فشل إنشاء الوحدة.", "Failed to create the unit."));
            }
        }
    }
}
