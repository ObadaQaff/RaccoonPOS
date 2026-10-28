using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace RaccoonWarehouse.Invoices
{
    public partial class SearchSalesInvoiceWindow : Window
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IProductService _productService;
        private readonly IStockService _stockService;
        private readonly IUserService _userService;
        private List<UserReadDto> _allUsers = new();
        private bool _isFilteringUsers;
        private bool _isNavigatingUserChoices;
        private readonly bool? _isSal = true;
        private readonly bool? _isPOS;
        private readonly ILoadingService _loadingService = new LoadingService();
        private bool _isBusy;
        public InvoiceReadDto? Result { get; private set; }

        public SearchSalesInvoiceWindow(
            IInvoiceService invoiceService,
            IEnumerable<UserReadDto> users,
            bool? isSal,
            bool? isPOS,
            IProductService productService,
            IStockService stockService,
            IUserService userService)
        {

            InitializeComponent();
            UiText.ApplyWindow(this);
            _invoiceService = invoiceService;
            _productService = productService;
            _stockService = stockService;
            _userService = userService;
            _allUsers = users?.GroupBy(x => x.Id).Select(x => x.First()).ToList() ?? new();
            CustomerTxt.ItemsSource = _allUsers;
            _isSal = isSal;
            _isPOS = isPOS;
            if (_isPOS == true)
            {
                DaysBackTxt.Visibility = Visibility.Visible;
                DateFrom.Visibility = Visibility.Collapsed;
                DateTo.Visibility = Visibility.Collapsed;
            }
            else
            {
                DaysBackTxt.Visibility = Visibility.Collapsed;
                DateFrom.Visibility = Visibility.Visible;
                DateTo.Visibility = Visibility.Visible;
            }
            PartyFilterLabel.Text = _isSal == false
                ? UiText.T("اسم المورد", "Supplier name")
                : UiText.T("اسم الزبون", "Customer name");
        }
        private void CustomerTxt_Loaded(object sender, RoutedEventArgs e)
        {
            CustomerTxt.DisplayMemberPath = "Name";
            CustomerTxt.SelectedValuePath = "Id";
        }

        private void CustomerTxt_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            CustomerTxt.SelectedItem = null;
            Dispatcher.BeginInvoke(new Action(() => FilterUserList(CustomerTxt.Text)), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void CustomerTxt_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                CustomerTxt.SelectedItem = null;
                FilterUserList(CustomerTxt.Text);
            }
        }

        private void FilterUserList(string text)
        {
            var filtered = _allUsers
                .Where(user => !string.IsNullOrEmpty(user.Name) && user.Name.Contains(text ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                .GroupBy(user => user.Id)
                .Select(group => group.First())
                .ToList();

            _isFilteringUsers = true;
            try
            {
                CustomerTxt.ItemsSource = filtered;
                CustomerTxt.SelectedItem = null;
                CustomerTxt.SelectedIndex = -1;
                CustomerTxt.Text = text ?? string.Empty;
                CustomerTxt.IsDropDownOpen = filtered.Count > 0;
            }
            finally { _isFilteringUsers = false; }
        }
        private void CustomerTxt_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is not (Key.Down or Key.Up or Key.Enter))
                return;

            if (CustomerTxt.Template.FindName("PART_EditableTextBox", CustomerTxt) is not TextBox textBox)
                return;

            if (e.Key == Key.Enter)
            {
                if (CustomerTxt.SelectedItem is UserReadDto selected)
                {
                    _isNavigatingUserChoices = true;
                    try { textBox.Text = selected.Name; }
                    finally { _isNavigatingUserChoices = false; }
                    textBox.CaretIndex = textBox.Text.Length;
                }

                CustomerTxt.IsDropDownOpen = false;
                e.Handled = true;
                return;
            }

            var typedText = textBox.Text ?? string.Empty;
            if (!CustomerTxt.IsDropDownOpen)
                CustomerTxt.IsDropDownOpen = true;

            var nextIndex = CustomerTxt.SelectedIndex;
            nextIndex = e.Key == Key.Down
                ? Math.Min(nextIndex + 1, CustomerTxt.Items.Count - 1)
                : Math.Max(nextIndex - 1, 0);

            if (CustomerTxt.Items.Count > 0)
            {
                _isNavigatingUserChoices = true;
                try { CustomerTxt.SelectedIndex = nextIndex; }
                finally { _isNavigatingUserChoices = false; }
                textBox.Text = typedText;
                textBox.CaretIndex = textBox.Text.Length;
            }

            e.Handled = true;
        }
        private void CustomerTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFilteringUsers || _isNavigatingUserChoices || CustomerTxt.Template.FindName("PART_EditableTextBox", CustomerTxt) is not TextBox textBox)
                return;

            var typedText = textBox.Text ?? string.Empty;
            var filterText = typedText.Trim();
            _isFilteringUsers = true;
            try
            {
                CustomerTxt.SelectedItem = null;
                CustomerTxt.ItemsSource = string.IsNullOrWhiteSpace(filterText)
                    ? _allUsers.ToList()
                    : _allUsers.Where(x => x.Name.Contains(filterText, StringComparison.CurrentCultureIgnoreCase)).ToList();
                CustomerTxt.IsDropDownOpen = true;
                textBox.Text = typedText;
                textBox.CaretIndex = textBox.Text.Length;
            }
            finally { _isFilteringUsers = false; }
        }
        private async void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            await SearchInvoicesAsync();
        }

        private async Task SearchInvoicesAsync()
        {
            DateTime? fromDate = DateFrom.SelectedDate;
            DateTime? toDate = DateTo.SelectedDate;

            if (_isPOS == true)
            {
                if (!int.TryParse(DaysBackTxt.Text?.Trim(), out var daysBack) || daysBack < 0)
                {
                    MessageBox.Show(
                        "Enter a valid number of days: 0 for today, 1 for yesterday, and so on.",
                        "Invalid date range",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    DaysBackTxt.Focus();
                    DaysBackTxt.SelectAll();
                    return;
                }

                var today = DateTime.Today;
                var selectedDate = today.AddDays(-daysBack);
                fromDate = selectedDate;
                toDate = selectedDate;
            }

            var result = await _invoiceService.SearchSalesInvoicesAsync(
                InvoiceNumberTxt.Text?.Trim(),
                CustomerTxt.Text,
                fromDate,
                toDate,
                _isSal,
                _isPOS,
                productBarcode: ItemBarcodeTxt.Text?.Trim()
            );

            if (result.Success)
            {
                ResultsGrid.ItemsSource = result.Data;
                UiText.ApplyTranslations(ResultsGrid);
            }
            else
                MessageBox.Show(result.Message ?? UiText.T("تعذر تحميل نتائج البحث.", "Failed to load search results."), UiText.T("خطأ", "Error"));
        }

        private async void SearchSalesInvoiceWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.F1)
                return;

            e.Handled = true;
            ProductReadDto? selectedProduct = null;
            ProductSearchWindow? searchWindow = null;
            searchWindow = new ProductSearchWindow(
                _productService,
                _stockService,
                _userService,
                onAddProduct: row =>
                {
                    selectedProduct = row.Product;
                    searchWindow!.DialogResult = true;
                    return Task.FromResult(true);
                },
                preloadedSupplierLookupItems: _allUsers
                    .Where(user => user.Role == RaccoonWarehouse.Domain.Enums.UserRole.Supplier)
                    .ToList())
            {
                Owner = this
            };

            if (searchWindow.ShowDialog() != true)
                return;

            selectedProduct ??= searchWindow.SelectedProduct;
            if (selectedProduct?.ITEMCODE is not long itemCode)
                return;

            ItemBarcodeTxt.Text = itemCode.ToString(System.Globalization.CultureInfo.InvariantCulture);
            await SearchInvoicesAsync();
        }

        private async void SelectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy)
                return;

            if (ResultsGrid.SelectedItem is InvoiceReadDto invoice)
            {
                try
                {
                    _isBusy = true;
                    _loadingService.Show();
                    var fullInvoice = await _invoiceService.GetFullInvoiceByIdAsync(invoice.Id);
                    Result = fullInvoice ?? invoice;
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        UiText.T($"تعذر تحميل الفاتورة الكاملة: {ex.Message}", $"Failed to load full invoice: {ex.Message}"),
                        UiText.T("خطأ", "Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                finally
                {
                    _loadingService.Hide();
                    _isBusy = false;
                }
            }
        }
    }
}
