using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace RaccoonWarehouse.Stocks.Reports
{
    public partial class LowStockReport : Window
    {
        private readonly IStockReportService _stockReportService;
        private ObservableCollection<LowStockDto> _items = new();

        public LowStockReport(IStockReportService stockReportService)
        {
            _stockReportService = stockReportService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            LowStockGrid.ItemsSource = _items;
            Loaded += LowStockReport_Loaded;
        }

        private async void LowStockReport_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var data = await _stockReportService.GetLowStockAsync();

            _items.Clear();
            foreach (var item in data)
                _items.Add(item);

            CountText.Text = _items.Count.ToString();
        }
    }
}
