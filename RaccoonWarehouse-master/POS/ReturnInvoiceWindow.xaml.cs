using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.AuthService;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Helpers.Localization;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace RaccoonWarehouse.POS
{
    public partial class ReturnInvoiceWindow : Window
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IUserSession _userSession;
        private readonly IAuthService _authService;
        private readonly IProductService _productService;
        private readonly ObservableCollection<ReturnInvoiceLookupRow> _rows = new();
        private readonly DispatcherTimer _productSuggestionTimer = new() { Interval = TimeSpan.FromMilliseconds(180) };
        private readonly SemaphoreSlim _serviceCallLock = new(1, 1);
        private List<string> _productNameCatalog = new();
        private bool _isBusy;
        private bool _isApplyingProductSuggestion;

        public string OriginalInvoiceId { get; private set; } = string.Empty;

        public ReturnInvoiceWindow(
            IInvoiceService invoiceService,
            IUserSession userSession,
            IAuthService authService,
            IProductService productService)
        {

            InitializeComponent();
            UiText.ApplyWindow(this);
            _invoiceService = invoiceService;
            _userSession = userSession;
            _authService = authService;
            _productService = productService;
            ResultsGrid.ItemsSource = _rows;
            _productSuggestionTimer.Tick += ProductSuggestionTimer_Tick;
            Loaded += async (_, _) =>
            {
                await LoadProductNameCatalogAsync();
                await SearchAsync();
            };
        }

        private async Task LoadProductNameCatalogAsync()
        {
            try
            {
                await _serviceCallLock.WaitAsync();
                try
                {
                    var result = await _productService.GetProductNameCatalogAsync();
                    _productNameCatalog = result.Success
                        ? result.Data ?? new List<string>()
                        : new List<string>();
                }
                finally
                {
                    _serviceCallLock.Release();
                }
            }
            catch
            {
                // The invoice search remains available if the optional suggestion catalog fails.
                _productNameCatalog = new List<string>();
            }
        }

        private async Task<bool> ConfirmReturnPasswordAsync()
        {
            var currentUser = _userSession.CurrentUser;
            if (currentUser == null)
                return false;

            var prompt = new Window
            {
                Title = UiText.T("تأكيد كلمة مرور الإرجاع", "Return password confirmation"),
                Width = 390,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                FlowDirection = FlowDirection.RightToLeft
            };
            var username = new TextBox { Text = currentUser.Name, Margin = new Thickness(0, 8, 0, 8), Height = 32 };
            var password = new PasswordBox { Margin = new Thickness(0, 0, 0, 12), Height = 32 };
            var message = new TextBlock
            {
                Text = UiText.T("أدخل اسم مستخدم وكلمة مرور المدير أو المسؤول قبل تحميل فواتير الإرجاع.", "Enter an Admin or Manager username and password before loading return invoices."),
                TextWrapping = TextWrapping.Wrap
            };
            var confirm = new Button { Content = UiText.T("متابعة", "Continue"), Width = 100, Margin = new Thickness(0, 0, 8, 0) };
            var cancel = new Button { Content = UiText.T("إلغاء", "Cancel"), Width = 100 };
            var buttons = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            buttons.Children.Add(confirm);
            buttons.Children.Add(cancel);
            var panel = new StackPanel { Margin = new Thickness(18) };
            panel.Children.Add(message);
            panel.Children.Add(username);
            panel.Children.Add(password);
            panel.Children.Add(buttons);
            prompt.Content = panel;
            confirm.Click += async (_, _) =>
            {
                var auth = await _authService.AuthenticateAsync(username.Text.Trim(), password.Password);
                prompt.DialogResult = auth.Success && auth.User != null &&
                    auth.User.Role is RaccoonWarehouse.Domain.Enums.UserRole.Admin or RaccoonWarehouse.Domain.Enums.UserRole.Manager;
            };
            cancel.Click += (_, _) => prompt.DialogResult = false;
            prompt.Loaded += (_, _) => password.Focus();

            var accepted = prompt.ShowDialog() == true;
            if (!accepted)
                MessageBox.Show(
                    UiText.T("كلمة المرور غير صحيحة أو تم إلغاء العملية.", "The password is incorrect or the operation was cancelled."),
                    UiText.T("تم رفض الإرجاع", "Return denied"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            return accepted;
        }

        private async void Search_Click(object sender, RoutedEventArgs e) => await SearchAsync();

        private void ProductNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isApplyingProductSuggestion)
                return;

            _productSuggestionTimer.Stop();
            ProductSuggestionsListBox.ItemsSource = null;
            ProductSuggestionsPopup.IsOpen = false;

            if (ProductNameTextBox.Text.Trim().Length >= 2)
                _productSuggestionTimer.Start();
        }

        private async void ProductSuggestionTimer_Tick(object? sender, EventArgs e)
        {
            _productSuggestionTimer.Stop();
            var searchText = ProductNameTextBox.Text.Trim();

            if (searchText.Length < 2)
                return;

            try
            {
                var suggestions = _productNameCatalog
                    .Where(name => MatchesProductName(name, searchText))
                    .Take(15)
                    .ToList();

                if (!string.Equals(searchText, ProductNameTextBox.Text.Trim(), StringComparison.Ordinal))
                    return;

                ProductSuggestionsListBox.ItemsSource = suggestions;
                ProductSuggestionsPopup.IsOpen = suggestions.Count > 0;
            }
            catch
            {
                ProductSuggestionsListBox.ItemsSource = null;
                ProductSuggestionsPopup.IsOpen = false;
            }
        }

        private static bool MatchesProductName(string productName, string searchText)
        {
            var terms = searchText
                .Split(new[] { ' ', '\t', '\r', '\n', '%' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var searchStart = 0;

            foreach (var term in terms)
            {
                var matchIndex = productName.IndexOf(term, searchStart, StringComparison.OrdinalIgnoreCase);
                if (matchIndex < 0)
                    return false;

                searchStart = matchIndex + term.Length;
            }

            return terms.Length > 0;
        }

        private void ProductNameTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && ProductSuggestionsPopup.IsOpen && ProductSuggestionsListBox.Items.Count > 0)
            {
                ProductSuggestionsListBox.Focus();
                ProductSuggestionsListBox.SelectedIndex = 0;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                ProductSuggestionsPopup.IsOpen = false;
            }
        }

        private void ProductSuggestionsListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ProductSuggestionsListBox.SelectedItem is string suggestion)
            {
                ApplyProductSuggestion(suggestion);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                ProductSuggestionsPopup.IsOpen = false;
                ProductNameTextBox.Focus();
                e.Handled = true;
            }
        }

        private void ProductSuggestionsListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ProductSuggestionsListBox.SelectedItem is string suggestion)
            {
                ApplyProductSuggestion(suggestion);
                e.Handled = true;
            }
        }

        private void ApplyProductSuggestion(string suggestion)
        {
            _isApplyingProductSuggestion = true;
            ProductNameTextBox.Text = suggestion;
            ProductNameTextBox.CaretIndex = ProductNameTextBox.Text.Length;
            _isApplyingProductSuggestion = false;
            ProductSuggestionsPopup.IsOpen = false;
            ProductSuggestionsListBox.SelectedItem = null;
            ProductNameTextBox.Focus();
        }

        private async Task SearchAsync()
        {
            if (_isBusy)
                return;

            try
            {
                _isBusy = true;
                if (!int.TryParse(DaysBackTextBox.Text?.Trim(), out var daysBack) || daysBack < 0)
                {
                    MessageBox.Show(
                        UiText.T("أدخل عدد أيام صحيحاً: 0 لليوم، 1 للأمس، وهكذا.", "Enter a valid number of days: 0 for today, 1 for yesterday, and so on."),
                        UiText.T("قيمة غير صحيحة", "Invalid value"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    DaysBackTextBox.Focus();
                    DaysBackTextBox.SelectAll();
                    return;
                }

                var selectedDate = DateTime.Today.AddDays(-daysBack);
                await _serviceCallLock.WaitAsync();
                Result<List<ReturnInvoiceLookupDto>> result;
                try
                {
                    result = await _invoiceService.SearchReturnInvoiceHeadersAsync(
                        selectedDate,
                        selectedDate,
                        InvoiceNumberTextBox.Text?.Trim(),
                        null,
                        ProductNameTextBox.Text?.Trim(),
                        BarcodeLookup.Normalize(BarcodeTextBox.Text));
                }
                finally
                {
                    _serviceCallLock.Release();
                }

                if (!result.Success)
                {
                    MessageBox.Show(
                        result.Message ?? UiText.T("تعذر تحميل الفواتير.", "Failed to load invoices."),
                        UiText.T("خطأ", "Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                _rows.Clear();
                foreach (var header in result.Data ?? new List<ReturnInvoiceLookupDto>())
                    _rows.Add(new ReturnInvoiceLookupRow(header));

                UiText.ApplyTranslations(ResultsGrid);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    UiText.T($"تعذر تحميل الفواتير: {ex.Message}", $"Failed to load invoices: {ex.Message}"),
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy || ResultsGrid.SelectedItem is not ReturnInvoiceLookupRow selected)
                return;

            try
            {
                _isBusy = true;
                if (selected.IsFullyReturned)
                {
                    MessageBox.Show(
                        UiText.T("هذه الفاتورة تم إرجاعها بالكامل ولا توجد كميات متبقية للإرجاع.", "This invoice has already been fully returned and has no remaining quantity to return."),
                        UiText.T("الفاتورة مرتجعة بالكامل", "Invoice fully returned"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // The POS return flow loads the full invoice once after this dialog closes.
                OriginalInvoiceId = selected.InvoiceNumber;
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    UiText.T($"تعذر تحميل تفاصيل الفاتورة: {ex.Message}", $"Failed to load invoice details: {ex.Message}"),
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        protected override void OnClosed(EventArgs e)
        {
            _productSuggestionTimer.Stop();
            _serviceCallLock.Dispose();
            base.OnClosed(e);
        }

        public sealed class ReturnInvoiceLookupRow
        {
            private readonly ReturnInvoiceLookupDto _source;

            public int Id => _source.Id;
            public string InvoiceNumber => _source.InvoiceNumber;
            public DateTime CreatedDate => _source.CreatedDate;
            public string CustomerName => string.IsNullOrWhiteSpace(_source.CustomerName) ? "—" : _source.CustomerName;
            public decimal TotalAmount => _source.TotalAmount;
            public string ReturnStatus => _source.ReturnStatus;
            public bool IsFullyReturned => _source.IsFullyReturned;
            public string PaymentMethodDisplay => _source.PaymentTypes.Count == 0
                ? UiText.T("غير محدد", "Not specified")
                : string.Join(" / ", _source.PaymentTypes.Select(GetPaymentName));

            public ReturnInvoiceLookupRow(ReturnInvoiceLookupDto source) => _source = source;

            private static string GetPaymentName(PaymentType paymentType) => paymentType switch
            {
                PaymentType.Cash => UiText.T("نقدي", "Cash"),
                PaymentType.Visa => "Visa",
                PaymentType.Master => "Master",
                PaymentType.Debit => UiText.T("تحويل بنكي", "Bank transfer"),
                PaymentType.Check => UiText.T("شيك", "Check"),
                PaymentType.MobilePayment => UiText.T("دفع إلكتروني", "Mobile payment"),
                PaymentType.Credit => UiText.T("آجل", "Credit"),
                _ => paymentType.ToString()
            };
        }
    }
}
