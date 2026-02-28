using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Domain.Stock.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse.Stocks.Reports
{
    public partial class StockBalanceByDateReport : Window
    {
        private readonly IStockReportService _stockReportService;

        public StockBalanceByDateReport(IStockReportService stockReportService)
        {
            InitializeComponent();
            _stockReportService = stockReportService;

            BalanceDatePicker.SelectedDate = DateTime.Now;
        }

        private async void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (BalanceDatePicker.SelectedDate == null)
                {
                    MessageBox.Show("اختر تاريخاً أولاً.");
                    return;
                }

                var date = BalanceDatePicker.SelectedDate.Value.Date;

                // includeInvoices=true => يضيف فواتير البيع/المرتجع للحركات
                var rows = await _stockReportService.GetStockBalanceByDateAsync(date, includeInvoices: true);

                StockBalanceGrid.ItemsSource = rows;
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