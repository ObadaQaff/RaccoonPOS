using RaccoonWarehouse.Application.Service.StockDocuments;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.StockDocuments.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;

namespace RaccoonWarehouse.Stocks
{
    public partial class SearchStockInWindow : Window
    {
        private readonly IStockDocumentService _stockDocumentService;

        public StockDocumentReadDto? Result { get; private set; }
        private bool _stockIn;
        private bool _isBusy;
        private readonly ILoadingService _loadingService = new LoadingService();

        public SearchStockInWindow(IStockDocumentService stockDocumentService, bool StockIn)
        {
            InitializeComponent();
            _stockDocumentService = stockDocumentService;
            _stockIn = StockIn;
            UiText.ApplyWindow(this);
        }

        private async void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy)
                return;

            _isBusy = true;
            _loadingService.Show();
            try
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
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء البحث عن السند", "An error occurred while searching for the document")}:\n{ex.Message}", UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _loadingService.Hide();
                _isBusy = false;
            }
        }

        private async void SelectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy)
                return;

            if (ResultsGrid.SelectedItem is StockDocumentReadDto doc)
            {
                _isBusy = true;
                _loadingService.Show();
                try
                {
                    Result = await _stockDocumentService.GetFullDocumentByIdAsync(doc.Id) ?? doc;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{UiText.T("تعذر تحميل تفاصيل السند كاملة", "Could not load the complete document details")}:\n{ex.Message}", UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                finally
                {
                    _loadingService.Hide();
                    _isBusy = false;
                }

                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(
                    UiText.T("يرجى اختيار سند من القائمة.", "Please select a document from the list."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}
