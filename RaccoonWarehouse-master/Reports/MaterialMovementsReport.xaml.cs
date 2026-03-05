using RaccoonWarehouse.Application.Service.Stocks;
using System;
using System.Windows;

namespace RaccoonWarehouse.Reports
{
    public partial class MaterialMovementsReport : Window
    {
        private readonly IStockReportService _stockReportService;

        public MaterialMovementsReport(IStockReportService stockReportService)
        {
            InitializeComponent();
            _stockReportService = stockReportService;
            FromDatePicker.SelectedDate = DateTime.Today;
            ToDatePicker.SelectedDate = DateTime.Today;
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var from = FromDatePicker.SelectedDate?.Date;
                var to = ToDatePicker.SelectedDate?.Date.AddDays(1).AddTicks(-1);
                var rows = await _stockReportService.GetStockAdjustmentsAsync(from, to);
                MaterialMovementsGrid.ItemsSource = rows;
                AdjustmentCountTextBlock.Text = rows.Count.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}");
            }
        }
    }
}
