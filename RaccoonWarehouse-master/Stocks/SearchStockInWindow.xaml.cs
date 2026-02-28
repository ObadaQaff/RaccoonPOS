using RaccoonWarehouse.Application.Service.StockDocuments;
using RaccoonWarehouse.Domain.StockDocuments.DTOs;
using System.Windows;

namespace RaccoonWarehouse.Stocks
{
    public partial class SearchStockInWindow : Window
    {
        private readonly IStockDocumentService _stockDocumentService;

        public StockDocumentReadDto? Result { get; private set; }
        private bool _stockIn;

        public SearchStockInWindow(IStockDocumentService stockDocumentService, bool StockIn)
        {
            InitializeComponent();
            _stockDocumentService = stockDocumentService;
            _stockIn = StockIn;
        }

        private async void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            var docs = await _stockDocumentService.SearchDocumentsAsync(
                DocNumberTxt.Text,
                SupplierTxt.Text,
                DateFrom.SelectedDate,
                DateTo.SelectedDate,
                _stockIn
            );

            ResultsGrid.ItemsSource = docs;
        }

        private void SelectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ResultsGrid.SelectedItem is StockDocumentReadDto doc)
            {
                Result = doc;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("يرجى اختيار سند من القائمة.", "تنبيه");
            }
        }
    }
}

