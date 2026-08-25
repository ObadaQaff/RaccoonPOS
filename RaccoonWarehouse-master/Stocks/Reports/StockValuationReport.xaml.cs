using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Products;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse.Stocks.Reports
{
    public partial class StockValuationReport : Window
    {
        private readonly IStockReportService _stockReportService;

        public StockValuationReport(IStockReportService stockReportService)
        {
            InitializeComponent();
            _stockReportService = stockReportService;
            UiText.ApplyWindow(this);
            Loaded += StockValuationReport_Loaded;
        }

        private async void StockValuationReport_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadReportAsync();
        }

        private async Task LoadReportAsync()
        {
            try
            {
                var rows = await _stockReportService.GetStockValuationAsync();
                ValuationGrid.ItemsSource = rows;
                TotalValueTextBlock.Text = rows.Sum(x => x.TotalValue).ToString("N5");
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

        private void ValuationGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ValuationGrid.SelectedItem is RaccoonWarehouse.Domain.Reports.Stocks.Dtos.StockValuationRowDto row && row.ProductId > 0)
                WindowManager.ShowDialog<UpdateProduct>(WindowSizeType.MediumRectangle, window => window.Initialize(row.ProductId));
        }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            await LoadReportAsync();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
