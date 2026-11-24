using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Helpers.Pdf;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;

namespace RaccoonWarehouse.FinancialTransactions
{
    public partial class ReceiptWindow : Window
    {
        private readonly IFinancialTransactionService _service;
        private readonly IUserService _userService;
        private readonly FinancialPostDto _dto;
        private DispatcherTimer _timer;

        private readonly int _cashierSessionId;
        private readonly int _cashierId;
        private List<UserReadDto> _allParties = new();
        private bool _updatingPartyList;

        public ReceiptWindow(IFinancialTransactionService service, IUserService userService, int cashierSessionId, int cashierId)
        {

            InitializeComponent();

            _service = service;
            _userService = userService;
            _cashierSessionId = cashierSessionId;
            _cashierId = cashierId;

            _dto = new FinancialPostDto
            {
                Direction = TransactionDirection.In,            // ✅ قبض
                SourceType = FinancialSourceType.Manual,        // ✅ يدوي
                TransactionDate = DateTime.Now,
                CashierSessionId = _cashierSessionId,
                CashierId = _cashierId,
                Method = PaymentMethod.Cash,                    // افتراضي
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            DataContext = _dto;
            UiText.ApplyWindow(this);

            LoadPaymentMethods();
            PartyTypeCombo.SelectedIndex = 0;
            Loaded += async (_, _) => await LoadPartiesAsync();
            StartClock();
        }

        private async System.Threading.Tasks.Task LoadPartiesAsync()
        {
            var result = await _userService.GetAllAsync();
            _allParties = result.Data?
                .Where(x => x.Role is UserRole.Customer or UserRole.Supplier)
                .OrderBy(x => x.Name)
                .ToList() ?? new List<UserReadDto>();
            ApplyPartyFilter(string.Empty);
        }

        private void PartyTypeCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (IsLoaded)
                ApplyPartyFilter(string.Empty);
        }

        private void PartyCombo_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key is System.Windows.Input.Key.Up or System.Windows.Input.Key.Down or System.Windows.Input.Key.Enter)
                return;

            ApplyPartyFilter(PartyCombo.Text);
        }

        private void ApplyPartyFilter(string? search)
        {
            if (_updatingPartyList)
                return;

            var roleTag = (PartyTypeCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString();
            var role = roleTag == "Supplier" ? UserRole.Supplier : UserRole.Customer;
            var text = search?.Trim() ?? string.Empty;
            var filtered = _allParties
                .Where(x => x.Role == role)
                .Where(x => string.IsNullOrWhiteSpace(text)
                    || (x.Name ?? string.Empty).Contains(text, StringComparison.CurrentCultureIgnoreCase)
                    || (x.PhoneNumber ?? string.Empty).Contains(text, StringComparison.CurrentCultureIgnoreCase))
                .ToList();

            _updatingPartyList = true;
            try
            {
                PartyCombo.ItemsSource = filtered;
                PartyCombo.Text = text;
                PartyCombo.IsDropDownOpen = !string.IsNullOrWhiteSpace(text) && filtered.Count > 0;
            }
            finally { _updatingPartyList = false; }
        }

        private void LoadPaymentMethods()
        {
            PaymentMethodCombo.ItemsSource = Enum.GetValues(typeof(PaymentMethod));
        }

        private void StartClock()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, _) =>
            {
                _dto.TransactionDate = DateTime.Now;
                // ما في داعي تعمل DataContext = null; خليها بسيطة
            };
            _timer.Start();
        }

        private async void Save_Click(object sender, RoutedEventArgs e) => await SaveAsync(false);

        private async void SaveAndPrint_Click(object sender, RoutedEventArgs e) => await SaveAsync(true);

        private async Task SaveAsync(bool printAfterSave)
        {
            if (!decimal.TryParse(AmountTextBox.Text, out var amount) || amount <= 0)
            {
                MessageBox.Show(UiText.T("يرجى إدخال مبلغ صحيح", "Please enter a valid amount."));
                return;
            }

            if (PaymentMethodCombo.SelectedItem == null)
            {
                MessageBox.Show(UiText.T("يرجى اختيار طريقة القبض", "Please choose a receipt method."));
                return;
            }

            if (MessageBox.Show(
                    UiText.T("هل تريد حفظ سند القبض؟", "Do you want to save the receipt transaction?"),
                    UiText.T("تأكيد الحفظ", "Confirm save"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _dto.Amount = amount;
            _dto.Method = (PaymentMethod)PaymentMethodCombo.SelectedItem;
            _dto.Notes = NotesTextBox.Text?.Trim();
            if (PartyCombo.SelectedItem is UserReadDto party)
            {
                _dto.CustomerId = party.Role == UserRole.Customer ? party.Id : null;
                _dto.SupplierId = party.Role == UserRole.Supplier ? party.Id : null;
            }
            _dto.UpdatedDate = DateTime.Now;

            // ✅ Post (مش CreateAsync)
            var result = await _service.PostAsync(_dto);

            if (result.Success)
            {
                _timer.Stop();
                if (printAfterSave && result.Data != null)
                {
                    var selectedParty = PartyCombo.SelectedItem as UserReadDto;
                    try
                    {
                        DocumentPrintRuntime.Preview(FinancialTransactionPrintDocument.Create(result.Data, true, selectedParty?.Name, selectedParty?.PhoneNumber), this);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message, UiText.T("تعذر إنشاء الطباعة", "Unable to create the print document."), MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                MessageBox.Show(UiText.T("تم تسجيل سند القبض بنجاح ✅", "Receipt voucher saved successfully."));
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(result.Message ?? UiText.T("حدث خطأ أثناء الحفظ", "An error occurred while saving."));
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();
            DialogResult = false;
            Close();
        }
    }
}
