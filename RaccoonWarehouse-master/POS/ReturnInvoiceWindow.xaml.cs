using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.AuthService;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.POS
{
    public partial class ReturnInvoiceWindow : Window
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IUserSession _userSession;
        private readonly IAuthService _authService;
        private readonly ObservableCollection<ReturnInvoiceLookupRow> _rows = new();
        private bool _isBusy;

        public string OriginalInvoiceId { get; private set; } = string.Empty;

        public ReturnInvoiceWindow(IInvoiceService invoiceService, IUserSession userSession, IAuthService authService)
        {
            InitializeComponent();
            UiText.ApplyWindow(this);
            _invoiceService = invoiceService;
            _userSession = userSession;
            _authService = authService;
            ResultsGrid.ItemsSource = _rows;
            Loaded += async (_, _) =>
            {
                if (!await ConfirmReturnPasswordAsync())
                {
                    Close();
                    return;
                }

                await SearchAsync();
            };
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

        private async Task SearchAsync()
        {
            if (_isBusy)
                return;

            try
            {
                _isBusy = true;
                var result = await _invoiceService.SearchReturnInvoiceHeadersAsync(
                    DateFrom.SelectedDate,
                    DateTo.SelectedDate,
                    InvoiceNumberTextBox.Text?.Trim());

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
