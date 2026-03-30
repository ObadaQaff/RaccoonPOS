using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Linq;
using System.Windows;

namespace RaccoonWarehouse.Reports
{
    public partial class StockBalancesReport : Window
    {
        private readonly IStockReportService _stockReportService;

        public StockBalancesReport(IStockReportService stockReportService)
        {
            InitializeComponent();
            _stockReportService = stockReportService;
            UiText.ApplyWindow(this);
            Loaded += StockBalancesReport_Loaded;
        }

        private async void StockBalancesReport_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadReportAsync();
        }

        private async System.Threading.Tasks.Task LoadReportAsync()
        {
            try
            {
                var rows = await _stockReportService.GetStockVarianceAsync();
                StockBalancesGrid.ItemsSource = rows;
                VarianceCountTextBlock.Text = rows.Count(x => x.VarianceQuantity != 0).ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("خطأ", "Error")}: {ex.Message}");
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
            await LoadReportAsync();
        }
    }
}
