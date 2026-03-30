using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Helpers.Localization;
using System;
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
            UiText.ApplyWindow(this);

            BalanceDatePicker.SelectedDate = DateTime.Now;
        }

        private async void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (BalanceDatePicker.SelectedDate == null)
                {
                    MessageBox.Show(
                        UiText.T("اختر تاريخاً أولاً.", "Please choose a date first."),
                        UiText.T("تنبيه", "Notice"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var date = BalanceDatePicker.SelectedDate.Value.Date;
                var rows = await _stockReportService.GetStockBalanceByDateAsync(date, includeInvoices: true);

                StockBalanceGrid.ItemsSource = rows;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("خطأ", "Error")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
