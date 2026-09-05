using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RaccoonWarehouse.Invoices
{
    public partial class ChangeInvoicePaymentWindow : Window
    {
        public sealed class PaymentLine
        {
            public PaymentType PaymentType { get; init; }
            public decimal Amount { get; init; }
            public string RemoveText => UiText.T("حذف", "Remove");
            public string DisplayName => UiText.T(PaymentType switch
            {
                PaymentType.Cash => "نقدي",
                PaymentType.Credit => "آجل",
                PaymentType.Check => "شيك",
                PaymentType.MobilePayment => "موبايل",
                PaymentType.Debit => "تحويل",
                PaymentType.Master => "ماستر",
                PaymentType.Visa => "فيزا",
                _ => PaymentType.ToString()
            }, PaymentType.ToString());
        }

        private sealed record PaymentChoice(PaymentType Value, string Arabic, string English)
        {
            public string DisplayName => UiText.T(Arabic, English);
        }

        private readonly List<PaymentChoice> _choices = new()
        {
            new(PaymentType.Cash, "نقدي", "Cash"),
            new(PaymentType.Visa, "Visa", "Visa"),
            new(PaymentType.Master, "Master", "Master"),
            new(PaymentType.Debit, "مدى/تحويل بنكي", "Debit / Bank transfer"),
            new(PaymentType.Check, "شيك", "Check"),
            new(PaymentType.MobilePayment, "دفع إلكتروني", "Mobile payment"),
            new(PaymentType.Credit, "آجل", "Credit")
        };

        private List<UserReadDto> _allCustomers = new();
        private bool _isFilteringCustomers;
        private bool _isNavigatingCustomerChoices;
        private readonly decimal _invoiceTotal;
        private readonly List<PaymentLine> _payments = new();

        public IReadOnlyList<PaymentLine> SelectedPayments => _payments;

        public PaymentType? SelectedPaymentType =>
            (PaymentMethodComboBox.SelectedItem as PaymentChoice)?.Value;

        public int? SelectedCustomerId => (CustomerComboBox.SelectedItem as UserReadDto)?.Id;

        public ChangeInvoicePaymentWindow(
            PaymentType? currentPaymentType,
            IEnumerable<UserReadDto>? customers = null,
            int? currentCustomerId = null,
            decimal invoiceTotal = 0m,
            IEnumerable<InvoicePaymentReadDto>? payments = null)
        {
            InitializeComponent();
            UiText.ApplyWindow(this);
            TitleText.Text = UiText.T("تغيير طريقة الدفع", "Change payment method");
            SaveButton.Content = UiText.T("حفظ", "Save");
            CancelButton.Content = UiText.T("إلغاء", "Cancel");
            CustomerLabel.Text = UiText.T("العميل", "Customer");
            PaymentMethodComboBox.ItemsSource = _choices;
            PaymentMethodComboBox.DisplayMemberPath = nameof(PaymentChoice.DisplayName);
            PaymentMethodComboBox.SelectedItem = _choices.FirstOrDefault(x => x.Value == currentPaymentType)
                ?? _choices.First();
            _allCustomers = (customers ?? Enumerable.Empty<UserReadDto>())
                .Where(x => x.Id > 0 && !string.IsNullOrWhiteSpace(x.Name))
                .GroupBy(x => x.Id)
                .Select(x => x.First())
                .ToList();
            CustomerComboBox.ItemsSource = _allCustomers;
            CustomerComboBox.SelectedItem = _allCustomers.FirstOrDefault(x => x.Id == currentCustomerId);
            _invoiceTotal = Math.Abs(invoiceTotal);
            if (payments != null)
                _payments.AddRange(payments.Where(payment => payment.Amount > 0m).Select(payment => new PaymentLine
                {
                    PaymentType = payment.PaymentType,
                    Amount = payment.Amount
                }));
            if (_payments.Count == 0 && currentPaymentType.HasValue && _invoiceTotal > 0m)
                _payments.Add(new PaymentLine { PaymentType = currentPaymentType.Value, Amount = _invoiceTotal });
            PaymentsGrid.ItemsSource = _payments;
            ((DataGridTextColumn)PaymentsGrid.Columns[0]).Header = UiText.T("طريقة", "Payment");
            ((DataGridTextColumn)PaymentsGrid.Columns[1]).Header = UiText.T("المبلغ", "Amount");
            PaymentsGrid.Columns[2].Header = UiText.T("حذف", "Remove");
            AddPaymentButton.Content = UiText.T("إضافة", "Add");
            PaymentAmountTextBox.Text = "0.000";
            PaymentMethodComboBox.SelectionChanged += PaymentMethodComboBox_SelectionChanged;
            UpdateCustomerVisibility();
        }

        private void PaymentMethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateCustomerVisibility();

        private void CustomerComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            CustomerComboBox.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(CustomerComboBox_TextChanged));
        }

        private void UpdateCustomerVisibility()
        {
            CustomerPanel.Visibility = SelectedPaymentType == PaymentType.Credit
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void CustomerComboBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            CustomerComboBox.SelectedItem = null;
            Dispatcher.BeginInvoke(new System.Action(() => FilterCustomers(CustomerComboBox.Text)), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void CustomerComboBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key is System.Windows.Input.Key.Back or System.Windows.Input.Key.Delete)
            {
                CustomerComboBox.SelectedItem = null;
                FilterCustomers(CustomerComboBox.Text);
            }
        }

        private void CustomerComboBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (sender is not ComboBox combo)
                return;

            switch (e.Key)
            {
                case System.Windows.Input.Key.Enter:
                    e.Handled = true;
                    var selectedCustomer = combo.SelectedItem as UserReadDto
                        ?? combo.Items.OfType<UserReadDto>().FirstOrDefault()
                        ?? _allCustomers.FirstOrDefault(customer =>
                            string.Equals(customer.Name, combo.Text?.Trim(), StringComparison.CurrentCultureIgnoreCase));
                    if (selectedCustomer != null)
                    {
                        combo.SelectedItem = selectedCustomer;
                        combo.Text = selectedCustomer.Name;
                    }
                    combo.IsDropDownOpen = false;
                    break;

                case System.Windows.Input.Key.Escape:
                    e.Handled = true;
                    combo.IsDropDownOpen = false;
                    break;

                case System.Windows.Input.Key.Down:
                case System.Windows.Input.Key.Up:
                    if (!combo.IsDropDownOpen)
                        combo.IsDropDownOpen = true;

                    var typedCustomerText = combo.Text ?? string.Empty;
                    var nextIndex = combo.SelectedIndex;
                    nextIndex = e.Key == System.Windows.Input.Key.Down
                        ? Math.Min(nextIndex + 1, combo.Items.Count - 1)
                        : Math.Max(nextIndex - 1, 0);

                    if (combo.Items.Count > 0)
                    {
                        _isNavigatingCustomerChoices = true;
                        try { combo.SelectedIndex = nextIndex; }
                        finally { _isNavigatingCustomerChoices = false; }

                        combo.Text = typedCustomerText;
                        if (combo.Template.FindName("PART_EditableTextBox", combo) is TextBox editableTextBox)
                            editableTextBox.CaretIndex = editableTextBox.Text.Length;
                    }

                    e.Handled = true;
                    break;
            }
        }

        private void CustomerComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFilteringCustomers || _isNavigatingCustomerChoices ||
                CustomerComboBox.Template.FindName("PART_EditableTextBox", CustomerComboBox) is not TextBox textBox)
                return;

            var typedText = textBox.Text ?? string.Empty;
            FilterCustomers(typedText);
        }

        private void FilterCustomers(string? text)
        {
            var searchText = text?.Trim() ?? string.Empty;
            var filtered = string.IsNullOrWhiteSpace(searchText)
                ? _allCustomers.ToList()
                : _allCustomers
                    .Where(customer => customer.Name.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                    .ToList();

            _isFilteringCustomers = true;
            try
            {
                CustomerComboBox.ItemsSource = filtered;
                CustomerComboBox.IsDropDownOpen = filtered.Count > 0;
                if (CustomerComboBox.Template.FindName("PART_EditableTextBox", CustomerComboBox) is TextBox textBox)
                {
                    textBox.Text = text ?? string.Empty;
                    textBox.CaretIndex = textBox.Text.Length;
                }
            }
            finally
            {
                _isFilteringCustomers = false;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_payments.Count == 0)
                return;

            if (_invoiceTotal > 0m && Math.Round(_payments.Sum(payment => payment.Amount), 3) != Math.Round(_invoiceTotal, 3))
            {
                MessageBox.Show(
                    UiText.T("يجب أن يساوي مجموع الدفعات إجمالي الفاتورة.", "Payment amounts must equal the invoice total."),
                    UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_payments.Any(payment => payment.PaymentType == PaymentType.Credit) && SelectedCustomerId == null)
            {
                MessageBox.Show(
                    UiText.T("يرجى اختيار العميل عند تحويل الفاتورة إلى آجل.", "Please select a customer when changing the invoice to credit."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void AddPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            if (PaymentMethodComboBox.SelectedItem is not PaymentChoice choice ||
                !decimal.TryParse(PaymentAmountTextBox.Text, out var amount) || amount <= 0m)
                return;

            if (_payments.Any(payment => payment.PaymentType == choice.Value))
                return;

            _payments.Add(new PaymentLine { PaymentType = choice.Value, Amount = amount });
            PaymentsGrid.Items.Refresh();
        }

        private void RemovePaymentButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is PaymentLine payment)
            {
                _payments.Remove(payment);
                PaymentsGrid.Items.Refresh();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
