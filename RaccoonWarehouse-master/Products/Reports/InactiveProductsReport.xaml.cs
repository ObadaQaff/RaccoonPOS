using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Domain.Stock.Filters;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Windows;

namespace RaccoonWarehouse.Products.Reports
{
    public partial class InactiveProductsReport : Window
    {
        private readonly IStockReportService _reportService;

        public InactiveProductsReport(IStockReportService reportService)
        {
            _reportService = reportService;
            InitializeComponent();
            UiText.ApplyWindow(this);
        }

        private async void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(DaysTextBox.Text, out var days))
                {
                    MessageBox.Show(UiText.T("يرجى إدخال عدد أيام صحيح.", "Please enter a valid number of days."));
                    return;
                }

                var filter = new InactiveProductsFilterDto
                {
                    DaysWithoutMovement = days,
                    AsOfDate = AsOfDatePicker.SelectedDate ?? DateTime.Today,
                    IncludeZeroStockOnly = ZeroStockCheck.IsChecked == true
                };

                var data = await _reportService.GetInactiveProductsAsync(filter);
                InactiveGrid.ItemsSource = data;
                UiText.ApplyTranslations(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ", "Error")}: {ex.Message}");
            }
        }
    }
}
