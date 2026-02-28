using RaccoonWarehouse.Application.Service.Stocks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse.Stocks.Reports
{
    public partial class StockMovementsReport : Window
    {
        private readonly IStockReportService _stockReportService;

        public StockMovementsReport(IStockReportService stockReportService)
        {
            InitializeComponent();
            _stockReportService = stockReportService;

            FromDatePicker.SelectedDate = DateTime.Today;
            ToDatePicker.SelectedDate = DateTime.Today;
        }

        private async void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime? from = FromDatePicker.SelectedDate?.Date;
                DateTime? to = ToDatePicker.SelectedDate?.Date.AddDays(1).AddTicks(-1);

                var data = await _stockReportService.GetStockMovementsAsync(from, to);
                MovementsGrid.ItemsSource = data ?? new List<Domain.Stock.DTOs.StockMovementDto>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}");
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}