using RaccoonWarehouse.Application.Service.Cashers;
using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Units;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Application.Service.Warehouses;
using RaccoonWarehouse.Application.Service.Vouchers;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Checks;
using RaccoonWarehouse.Domain.Checks.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;



namespace RaccoonWarehouse.Vouchers
{
    public partial class CreateVoucher : Window
    {
        private List<CheckWriteDto> _checks = new();
        private readonly  IVoucherService _voucherService;
        private readonly IUserService _userService;
        private int? _currentVoucherId = null;
        private List<CheckReadDto> _originalChecks = new();
        private readonly IFinancialTransactionService _financialService;
        private readonly IUserSession _userSession;
        private readonly ICashierSessionService _cashierSessionService;
        private readonly IWarehouseService _warehouseService;
        private readonly ILoadingService _loadingService;
        private int? _initialCustomerId;
        private decimal? _maximumCollectionAmount;
        private bool _customerCollectionMode;
        private List<UserReadDto> _allAccountUsers = new();
        private bool _isFilteringAccountUsers;
        private bool _isNavigatingAccountChoices;


        public CreateVoucher(IVoucherService voucherService, IUserService userService,
                                     IFinancialTransactionService financialService,
                                     IUserSession userSession,
                                     IWarehouseService warehouseService,
                                     ILoadingService loadingService)
        {
            _voucherService = voucherService;
            _userService = userService;
            _financialService = financialService;
            _userSession = userSession;
            _warehouseService = warehouseService;
            _loadingService = loadingService;
            InitializeComponent();
            AccountComboBox.Loaded += AccountComboBox_Loaded;
            AccountComboBox.PreviewKeyDown += AccountComboBox_PreviewKeyDown;
            AccountComboBox.PreviewTextInput += AccountComboBox_PreviewTextInput;
            AccountComboBox.KeyUp += AccountComboBox_KeyUp;
            UiText.ApplyWindow(this);

            Loaded += async (s, e) => await CreateVoucher_Loaded();
            ReceiptNumber.Text = GenerateDocumentNumber();

        }

        public void InitializeCustomerPayment(int customerId, decimal outstandingBalance)
        {
            if (customerId <= 0)
                throw new ArgumentOutOfRangeException(nameof(customerId));
            if (outstandingBalance <= 0)
                throw new ArgumentOutOfRangeException(nameof(outstandingBalance));

            _customerCollectionMode = true;
            _initialCustomerId = customerId;
            _maximumCollectionAmount = outstandingBalance;
            Amount.Text = outstandingBalance.ToString("0.00000");
            ReceiptDescription.Text = UiText.T("تحصيل ذمم عميل", "Customer credit collection");
            Title = UiText.T("تحصيل دفعة من عميل", "Receive Customer Payment");

            var creditItem = PaymentTypeCombo.Items.OfType<ComboBoxItem>()
                .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), ((int)PaymentType.Credit).ToString(), StringComparison.Ordinal));
            if (creditItem != null)
                PaymentTypeCombo.Items.Remove(creditItem);
            PaymentTypeCombo.SelectedIndex = 0;
        }
        private string GenerateDocumentNumber()
        {
            return (DateTime.Now.Ticks % 90000 + 10000).ToString();
        }

        private async Task CreateVoucher_Loaded()
        {
            try
            {
                _loadingService.Show();
                ReceiptDate.SelectedDate = DateTime.Now;
                var users = await _userService.GetAllAsync();
                _allAccountUsers = users.Data?.ToList() ?? new List<UserReadDto>();
                AccountComboBox.ItemsSource = _allAccountUsers.ToList();
                AccountComboBox.DisplayMemberPath = "Name";
                AccountComboBox.SelectedValuePath = "Id";
                if (_initialCustomerId.HasValue)
                {
                    AccountComboBox.SelectedValue = _initialCustomerId.Value;
                    AccountComboBox.IsEnabled = false;
                }

                var warehouses = await _warehouseService.GetAllAsync();
                WarehouseComboBox.ItemsSource = warehouses.Data;
                WarehouseComboBox.DisplayMemberPath = "Name";
                WarehouseComboBox.SelectedValuePath = "Id";
                UiText.ApplyTranslations(this);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Amount.Focus();
                    Amount.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء تحميل البيانات", "An error occurred while loading data")}:\n{ex.Message}", UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _loadingService.Hide();
            }

        }
        private bool TryGetActiveCashierSession(out CashierSessionReadDto? session)
        {
            session = _userSession.CurrentCashierSession;
            if (session != null)
                return true;

            MessageBox.Show(UiText.T("لا توجد جلسة كاشير مفتوحة. الرجاء فتح جلسة أولاً.", "There is no open cashier session. Please open a session first."), UiText.T("خطأ", "Error"));
            return false;
        }

        /*private async void SaveReceiptBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                if (!decimal.TryParse(Amount.Text, out decimal amount))
                {
                    MessageBox.Show(UiText.T("يرجى إدخال مبلغ صالح.", "Please enter a valid amount."), UiText.T("تحذير", "Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedUser = AccountComboBox.SelectedItem as UserWriteDto;

                // 🔥 VERY IMPORTANT: Push DataGrid edits into the object
                ChecksGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                ChecksGrid.CommitEdit(DataGridEditingUnit.Row, true);

                // 🔥 Now read checks from DataGrid safely
                if (ChecksGrid.Items.Count > 0)
                {
                    _checks = ChecksGrid.Items
                                        .Cast<object>()
                                        .Where(x => x is CheckWriteDto)
                                        .Cast<CheckWriteDto>()
                                        .ToList();
                }
                bool isUpdate = _currentVoucherId != null;
                var dto = new VoucherWriteDto
                {
                    VoucherNumber = ReceiptNumber.Text,
                    VoucherType = VoucherType.Receipt,
                    Amount = amount,
                    CasherId = 0,
                    Notes = ReceiptDescription.Text,
                    CustomerId = AccountComboBox.SelectedValue != null ? (int)AccountComboBox.SelectedValue : null,
                    CreatedDate = ReceiptDate.SelectedDate ?? DateTime.Now,
                    UpdatedDate = DateTime.Now,
                    // PAYMENT TYPE
                    PaymentType = (PaymentType)int.Parse((PaymentTypeCombo.SelectedItem as ComboBoxItem).Tag.ToString())

                };
                if (dto.PaymentType == PaymentType.Check)
                {
                    dto.Checks = _checks.ToList();
                    if (_checks.Count == 0)
                    {
                        MessageBox.Show(UiText.T("يرجى إضافة شيك واحد على الأقل.", "Please add at least one check."), UiText.T("تنبيه", "Notice"));
                        return;
                    }
                }
                else
                {
                    dto.Checks = null;

                }


                if (!isUpdate)
                {
                    var result = await _voucherService.CreateAsync(dto);

                    if (result.Success)
                    {
                        MessageBox.Show(UiText.T("تم حفظ السند بنجاح.", "The voucher was saved successfully."), UiText.T("نجاح", "Success"), MessageBoxButton.OK, MessageBoxImage.Information);
                        PrintBtn.Visibility = Visibility.Visible;  // 🔥 Show Print Button
                        NewVoucherBtn.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        MessageBox.Show($"{UiText.T("فشل في الحفظ", "Save failed")}: {result.Message}", UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    dto.Id = _currentVoucherId.Value;
                    var result = await _voucherService.UpdateAsync(dto);
                    if (result.Success)
                    {
                        MessageBox.Show(UiText.T("تم تحديث السند بنجاح.", "The voucher was updated successfully."), UiText.T("نجاح", "Success"), MessageBoxButton.OK, MessageBoxImage.Information);
                        PrintBtn.Visibility = Visibility.Visible;  // 🔥 Show Print Button
                        NewVoucherBtn.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        MessageBox.Show($"{UiText.T("فشل في التحديث", "Update failed")}: {result.Message}", UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء حفظ السند", "An error occurred while saving the voucher")}:\n{ex.Message}", UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }*/
        private async void SaveReceiptBtn_Click(object sender, RoutedEventArgs e)
        {
            var loadingShown = false;

            void HideLoadingIfShown()
            {
                if (!loadingShown)
                    return;

                _loadingService.Hide();
                loadingShown = false;
            }

            try
            {
                if (!decimal.TryParse(Amount.Text, out decimal amount) || amount <= 0)
                {
                    MessageBox.Show(UiText.T("يرجى إدخال مبلغ صالح.", "Please enter a valid amount."), UiText.T("تحذير", "Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_customerCollectionMode && AccountComboBox.SelectedValue == null)
                {
                    MessageBox.Show(UiText.T("يرجى اختيار العميل.", "Please select the customer."), UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_maximumCollectionAmount.HasValue && amount > _maximumCollectionAmount.Value)
                {
                    MessageBox.Show(
                        string.Format(UiText.T("لا يمكن أن تتجاوز الدفعة الرصيد المستحق {0:N5}.", "The payment cannot exceed the outstanding balance of {0:N5}."), _maximumCollectionAmount.Value),
                        UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (PaymentTypeCombo.SelectedItem is not ComboBoxItem payItem || payItem.Tag == null)
                {
                    MessageBox.Show(UiText.T("يرجى اختيار طريقة الدفع.", "Please choose a payment method."), UiText.T("تنبيه", "Notice"));
                    return;
                }

                var paymentType = (PaymentType)int.Parse(payItem.Tag.ToString()!);
                if (paymentType == PaymentType.Credit)
                {
                    MessageBox.Show(UiText.T("طريقة الذمم غير صالحة لسند القبض.", "Credit is not a valid payment method for a receipt voucher."), UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Commit edits for checks
                ChecksGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                ChecksGrid.CommitEdit(DataGridEditingUnit.Row, true);

                if (paymentType == PaymentType.Check)
                {
                    _checks = ChecksGrid.Items
                        .OfType<CheckWriteDto>()
                        .ToList();
                }
                else
                {
                    _checks.Clear();
                }

                // Validate checks before showing loading or saving the voucher.
                // An invalid check must never leave a persisted voucher or a stuck overlay.
                if (paymentType == PaymentType.Check && !ValidatePaymentByCheck(amount))
                    return;

                bool isUpdate = _currentVoucherId != null;
                if (!TryGetActiveCashierSession(out var session))
                    return;
                _loadingService.Show();
                loadingShown = true;

                var dto = new VoucherWriteDto
                {
                    VoucherNumber = ReceiptNumber.Text,
                    VoucherType = VoucherType.Receipt, // أو حسب شاشتك (Receipt/Payment)
                    Amount = amount,
                    CasherId = session.CashierId,
                    WarehouseId = WarehouseComboBox.SelectedValue != null ? (int)WarehouseComboBox.SelectedValue : null,
                    CustomerId = AccountComboBox.SelectedValue != null ? (int)AccountComboBox.SelectedValue : null,
                    Notes = ReceiptDescription.Text,
                    CreatedDate = ReceiptDate.SelectedDate ?? DateTime.Now,
                    UpdatedDate = DateTime.Now,
                    PaymentType = paymentType,
                    Checks = paymentType == PaymentType.Check ? _checks.ToList() : null
                };

                // =========================
                // 1) Save Voucher (Create/Update)
                // =========================
                int savedVoucherId;

                if (!isUpdate)
                {
                    var createResult = await _voucherService.CreateAsync(dto);
                    if (!createResult.Success)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show($"{UiText.T("فشل في الحفظ", "Save failed")}: {createResult.Message}", UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    savedVoucherId = createResult.Data.Id; // تأكد CreateAsync بيرجع Id في Data
                    _currentVoucherId = savedVoucherId;
                }
                else
                {
                    dto.Id = _currentVoucherId!.Value;

                    var updateResult = await _voucherService.UpdateAsync(dto);
                    if (!updateResult.Success)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show($"{UiText.T("فشل في التحديث", "Update failed")}: {updateResult.Message}", UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    savedVoucherId = dto.Id;
                }
                
                // =========================
                // 2) Financial handling
                // =========================
                var sourceType = GetSourceType(dto.VoucherType);
                var direction = GetDirection(dto.VoucherType);
                var method = MapPaymentMethod(dto.PaymentType);

                // Update case: void old financial then post new
                if (isUpdate)
                {
                    var voidRes = await _financialService.VoidBySourceAsync(
                        sourceType,
                        savedVoucherId,
                        reason: $"Voucher updated #{dto.VoucherNumber}"
                    );

                    // حتى لو ما لقى حركات قديمة، ما تعتبرها فشل
                    if (!voidRes.Success)
                    {
                        HideLoadingIfShown();
                        MessageBox.Show(voidRes.Message ?? UiText.T("فشل في إلغاء الحركات القديمة.", "Failed to void previous transactions."), UiText.T("خطأ", "Error"));
                        return;
                    }
                }

                var postDto = new FinancialPostDto
                {
                    Direction = direction,
                    Method = method,
                    Amount = dto.Amount,
                    TransactionDate = dto.CreatedDate,

                    SourceType = sourceType,
                    SourceId = savedVoucherId,

                    CashierSessionId = session.Id,
                    CashierId = session.CashierId,

                    Notes = $"{dto.VoucherType} Voucher #{dto.VoucherNumber}"
                };

                // مهم: إذا الطريقة Cash لازم SessionId مش null (حسب validations عندك)
                var postRes = await _financialService.PostAsync(postDto);
                if (!postRes.Success)
                {
                    HideLoadingIfShown();
                    MessageBox.Show(postRes.Message ?? UiText.T("تم حفظ السند لكن فشل تسجيل الحركة المالية", "The voucher was saved, but posting the financial transaction failed."), UiText.T("تحذير", "Warning"));
                    return;
                }

                // =========================
                // 3) UI
                // =========================
                HideLoadingIfShown();
                MessageBox.Show(
                    isUpdate
                        ? UiText.T("تم تحديث السند وتسجيل الحركة المالية ✅", "The voucher was updated and the financial transaction was posted successfully.")
                        : UiText.T("تم حفظ السند وتسجيل الحركة المالية ✅", "The voucher was saved and the financial transaction was posted successfully."),
                    UiText.T("نجاح", "Success"));
                PrintBtn.Visibility = Visibility.Visible;
                NewVoucherBtn.Visibility = Visibility.Visible;
                if (_customerCollectionMode)
                {
                    try
                    {
                        PrintVoucherPdf(dto);
                    }
                    catch (Exception pdfEx)
                    {
                        MessageBox.Show(
                            $"{UiText.T("تم حفظ الدفعة، ولكن تعذر تصدير ملف PDF", "The payment was saved, but the PDF could not be exported")}:\n{pdfEx.Message}",
                            UiText.T("تحذير", "Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                    DialogResult = true;
                    Close();
                }

                NewVoucherBtn_Click(this, new RoutedEventArgs());
            }
            catch (Exception ex)
            {
                HideLoadingIfShown();
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء حفظ السند", "An error occurred while saving the voucher")}:\n{ex.Message}", UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                HideLoadingIfShown();
            }
        }

        private void NewVoucherBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();


            // Hide buttons after clearing
            PrintBtn.Visibility = Visibility.Collapsed;
            NewVoucherBtn.Visibility = Visibility.Collapsed;
        }

        private void ClearFields()
        {
            // Reset voucher fields
            ReceiptNumber.Text = GenerateDocumentNumber();
            Amount.Text = string.Empty;
            AccountComboBox.SelectedIndex = -1;
            WarehouseComboBox.SelectedIndex = -1;
            ReceiptDescription.Text = string.Empty;
            ReceiptDate.SelectedDate = DateTime.Now;

            // Reset payment method
            PaymentTypeCombo.SelectedIndex = -1;

            // Clear check input fields
            CheckNumberBox.Text = string.Empty;
            BankNameBox.Text = string.Empty;
            CheckAmountBox.Text = string.Empty;
            CheckNotesBox.Text = string.Empty;
            CheckDueDatePicker.SelectedDate = null;

            // Clear check list
            _checks.Clear();

            // Clear DataGrid
            ChecksGrid.ItemsSource = null;

            // Hide check UI
            CheckFieldsPanel.Visibility = Visibility.Collapsed;
            ChecksGrid.Visibility = Visibility.Collapsed;
            AddCheckButton.Visibility = Visibility.Collapsed;
            UiText.ApplyTranslations(this);
        }


        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CreateVoucher_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.F1)
                return;

            e.Handled = true;
            SaveReceiptBtn_Click(SaveReceiptBtn, new RoutedEventArgs());
        }

        private void Amount_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;
            AccountComboBox.Focus();
            AccountComboBox.IsDropDownOpen = true;
        }

        private void FormField_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;
            if (sender == WarehouseComboBox)
                PaymentTypeCombo.Focus();
            else if (sender == PaymentTypeCombo)
                (CheckFieldsPanel.Visibility == Visibility.Visible ? CheckNumberBox : ReceiptDescription).Focus();
        }
        private void PaymentTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PaymentTypeCombo.SelectedItem is ComboBoxItem selected)
            {
                if (selected.Tag == null || !int.TryParse(selected.Tag.ToString(), out int paymentType))
                    return;

                // Show check fields only if user selected "Check = 4"
                ChecksGrid.Visibility = (paymentType == (int)PaymentType.Check)
                                              ? Visibility.Visible
                                              : Visibility.Collapsed;
                AddCheckButton.Visibility = (paymentType == (int)PaymentType.Check)
                                              ? Visibility.Visible
                                              : Visibility.Collapsed;
                CheckFieldsPanel.Visibility = (paymentType == (int)PaymentType.Check)
                                              ? Visibility.Visible
                                              : Visibility.Collapsed;

            }
        }

        private async void AddCustomerBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int? createdCustomerId = null;
                var initialName = AccountComboBox.Text?.Trim();
                WindowManager.ShowDialog<CreateUser>(WindowSizeType.SmallSquare, window =>
                {
                    window.InitializeForCustomerQuickCreate(initialName);
                    window.Closed += (_, __) => createdCustomerId = window.CreatedUserId;
                });

                var users = await _userService.GetAllAsync();
                _allAccountUsers = users.Data?.ToList() ?? new List<UserReadDto>();
                AccountComboBox.ItemsSource = _allAccountUsers.ToList();
                if (createdCustomerId.HasValue)
                    AccountComboBox.SelectedValue = createdCustomerId.Value;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("تعذر إضافة العميل", "Could not add the customer")}: {ex.Message}",
                    UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AccountComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            AccountComboBox.DisplayMemberPath = "Name";
            AccountComboBox.SelectedValuePath = "Id";
        }
        private void AccountComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is not (Key.Down or Key.Up or Key.Enter))
                return;

            if (AccountComboBox.Template.FindName("PART_EditableTextBox", AccountComboBox) is not TextBox textBox)
                return;

            if (e.Key == Key.Enter)
            {
                var selected = AccountComboBox.SelectedItem as UserReadDto
                    ?? AccountComboBox.Items.OfType<UserReadDto>().FirstOrDefault();

                if (selected == null)
                {
                    AccountComboBox.IsDropDownOpen = true;
                    return;
                }

                _isNavigatingAccountChoices = true;
                try
                {
                    textBox.Text = selected.Name;
                    AccountComboBox.SelectedItem = selected;
                    AccountComboBox.SelectedValue = selected.Id;
                }
                finally { _isNavigatingAccountChoices = false; }
                textBox.CaretIndex = textBox.Text.Length;
                AccountComboBox.IsDropDownOpen = false;
                e.Handled = true;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    AccountComboBox.IsDropDownOpen = false;
                    WarehouseComboBox.Focus();
                    Keyboard.Focus(WarehouseComboBox);
                }), System.Windows.Threading.DispatcherPriority.ContextIdle);
                return;
            }

            var typedText = textBox.Text ?? string.Empty;
            if (!AccountComboBox.IsDropDownOpen)
                AccountComboBox.IsDropDownOpen = true;

            var nextIndex = AccountComboBox.SelectedIndex;
            nextIndex = e.Key == Key.Down
                ? Math.Min(nextIndex + 1, AccountComboBox.Items.Count - 1)
                : Math.Max(nextIndex - 1, 0);

            if (AccountComboBox.Items.Count > 0)
            {
                _isNavigatingAccountChoices = true;
                try { AccountComboBox.SelectedIndex = nextIndex; }
                finally { _isNavigatingAccountChoices = false; }
                textBox.Text = typedText;
                textBox.CaretIndex = textBox.Text.Length;
            }

            e.Handled = true;
        }
        private void AccountComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            AccountComboBox.SelectedItem = null;
            Dispatcher.BeginInvoke(new Action(() => FilterAccountList(AccountComboBox.Text)), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void AccountComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                AccountComboBox.SelectedItem = null;
                FilterAccountList(AccountComboBox.Text);
            }
        }

        private void FilterAccountList(string text)
        {
            var filtered = _allAccountUsers
                .Where(user => !string.IsNullOrEmpty(user.Name) && user.Name.Contains(text ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                .GroupBy(user => user.Id)
                .Select(group => group.First())
                .ToList();

            _isFilteringAccountUsers = true;
            try
            {
                AccountComboBox.ItemsSource = filtered;
                AccountComboBox.SelectedItem = null;
                AccountComboBox.SelectedIndex = -1;
                AccountComboBox.Text = text ?? string.Empty;
                AccountComboBox.IsDropDownOpen = filtered.Count > 0;
            }
            finally { _isFilteringAccountUsers = false; }
        }
        private void AccountComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFilteringAccountUsers || _isNavigatingAccountChoices)
                return;

            var textBox = e.OriginalSource as TextBox ?? Keyboard.FocusedElement as TextBox;
            if (textBox == null || !textBox.IsKeyboardFocusWithin)
                return;

            var searchText = textBox.Text ?? string.Empty;
            var filterText = searchText.Trim();
            _isFilteringAccountUsers = true;
            try
            {
                AccountComboBox.SelectedItem = null;
                AccountComboBox.SelectedValue = null;
                AccountComboBox.ItemsSource = string.IsNullOrWhiteSpace(filterText)
                    ? _allAccountUsers.ToList()
                    : _allAccountUsers.Where(user => (user.Name ?? string.Empty).Contains(filterText, StringComparison.CurrentCultureIgnoreCase)).ToList();
                AccountComboBox.SelectedIndex = -1;
                AccountComboBox.IsDropDownOpen = true;
                textBox.Text = searchText;
                textBox.CaretIndex = textBox.Text.Length;
            }
            finally
            {
                _isFilteringAccountUsers = false;
            }
        }
        #region Check Handle 
        private void AddCheck_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CheckNumberBox.Text))
                {
                    MessageBox.Show(UiText.T("يرجى إدخال رقم الشيك.", "Please enter the check number."), UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(CheckAmountBox.Text, out var checkAmount) || checkAmount <= 0)
                {
                    MessageBox.Show(UiText.T("يرجى إدخال مبلغ شيك صالح.", "Please enter a valid check amount."), UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var checkNumber = CheckNumberBox.Text.Trim();
                if (_checks.Any(c => string.Equals(c.CheckNumber, checkNumber, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show(UiText.T("رقم الشيك مكرر.", "The check number is duplicated."), UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var check = new CheckWriteDto
                {
                    CheckNumber = checkNumber,
                    BankName = string.IsNullOrWhiteSpace(BankNameBox.Text) ? "-" : BankNameBox.Text.Trim(),
                    DueDate = CheckDueDatePicker.SelectedDate ?? DateTime.Now,
                    Amount = checkAmount,
                    Status = CheckStatus.Pending,
                    Notes = string.IsNullOrWhiteSpace(CheckNotesBox.Text) ? null : CheckNotesBox.Text.Trim(),
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };

                _checks.Add(check);

                ChecksGrid.ItemsSource = null;
                ChecksGrid.ItemsSource = _checks;

                ChecksGrid.Visibility = Visibility.Visible;
                UiText.ApplyTranslations(ChecksGrid);

                // Clear input fields
                CheckNumberBox.Text = "";
                BankNameBox.Text = "";
                CheckAmountBox.Text = "";
                CheckNotesBox.Text = "";
                CheckDueDatePicker.SelectedDate = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء إضافة الشيك", "An error occurred while adding the check")}:\n{ex.Message}", UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DeleteCheck_Click(object sender, RoutedEventArgs e)
        {
            var check = (sender as Button).DataContext as CheckWriteDto;
            if (check == null)
                return;

            _checks.Remove(check);

            ChecksGrid.ItemsSource = null;
            ChecksGrid.ItemsSource = _checks;

            if (_checks.Count == 0)
                ChecksGrid.Visibility = Visibility.Collapsed;
        }

        #endregion
        #region Print handle 
        private void PrintVoucher(VoucherWriteDto dto)
        {
            // Create FlowDocument
            var doc = new FlowDocument
            {
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 14,
                PagePadding = new Thickness(40),
                ColumnWidth = double.PositiveInfinity // single column
            };

            // ==== HEADER ====
            var header = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 20,
                FontWeight = FontWeights.Bold
            };
            header.Inlines.Add("Raccoon Warehouse");
            doc.Blocks.Add(header);

            var subHeader = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 18,
                FontWeight = FontWeights.Bold
            };
            subHeader.Inlines.Add(UiText.T("سند قبض", "Receipt Voucher"));
            doc.Blocks.Add(subHeader);

            doc.Blocks.Add(new Paragraph(new Run("────────────────────────────────────────")));

            // ==== BASIC INFO TABLE ====
            var infoTable = new Table();
            infoTable.Columns.Add(new TableColumn { Width = new GridLength(150) });
            infoTable.Columns.Add(new TableColumn());

            var infoRowGroup = new TableRowGroup();
            infoTable.RowGroups.Add(infoRowGroup);

            void AddInfoRow(string label, string value)
            {
                var row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(label)) { FontWeight = FontWeights.Bold }));
                row.Cells.Add(new TableCell(new Paragraph(new Run(value ?? ""))));
                infoRowGroup.Rows.Add(row);
            }

            AddInfoRow(UiText.T("رقم السند:", "Voucher No:"), ReceiptNumber.Text);
            AddInfoRow(UiText.T("التاريخ:", "Date:"), (dto.CreatedDate).ToString("yyyy/MM/dd"));
            AddInfoRow(UiText.T("العميل / الجهة:", "Customer / Party:"), (AccountComboBox.Text ?? ""));
            AddInfoRow(UiText.T("المبلغ:", "Amount:"), dto.Amount.ToString("N5"));
            AddInfoRow(UiText.T("طريقة الدفع:", "Payment Method:"), dto.PaymentType.ToString());

            doc.Blocks.Add(infoTable);

            doc.Blocks.Add(new Paragraph(new Run(" ")));// spacer
            doc.Blocks.Add(new Paragraph(new Run(UiText.T("تفاصيل الشيكات:", "Check Details:")))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 16
            });

            // ==== CHECKS TABLE (IF ANY) ====
            if (dto.Checks != null && dto.Checks.Count > 0)
            {
                var checkTable = new Table();
                checkTable.CellSpacing = 0;
                checkTable.Columns.Add(new TableColumn { Width = new GridLength(120) }); // check number
                checkTable.Columns.Add(new TableColumn { Width = new GridLength(120) }); // bank
                checkTable.Columns.Add(new TableColumn { Width = new GridLength(80) });  // amount
                checkTable.Columns.Add(new TableColumn { Width = new GridLength(100) }); // due date
                checkTable.Columns.Add(new TableColumn());                               // notes

                var checkHeaderGroup = new TableRowGroup();
                checkTable.RowGroups.Add(checkHeaderGroup);

                // Header row
                var headerRow = new TableRow();
                string[] headers =
                {
                    UiText.T("رقم الشيك", "Check Number"),
                    UiText.T("البنك", "Bank"),
                    UiText.T("المبلغ", "Amount"),
                    UiText.T("تاريخ الاستحقاق", "Due Date"),
                    UiText.T("ملاحظات", "Notes")
                };
                foreach (var h in headers)
                {
                    var cell = new TableCell(new Paragraph(new Run(h)))
                    {
                        FontWeight = FontWeights.Bold,
                        Padding = new Thickness(3),
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(0, 0, 0, 1)
                    };
                    headerRow.Cells.Add(cell);
                }
                checkHeaderGroup.Rows.Add(headerRow);

                // Data rows
                foreach (var c in dto.Checks)
                {
                    var row = new TableRow();

                    TableCell MakeCell(string text)
                    {
                        return new TableCell(new Paragraph(new Run(text ?? "")))
                        {
                            Padding = new Thickness(3),
                            BorderBrush = Brushes.LightGray,
                            BorderThickness = new Thickness(0, 0, 0, 0.5)
                        };
                    }

                    row.Cells.Add(MakeCell(c.CheckNumber));
                    row.Cells.Add(MakeCell(c.BankName));
                    row.Cells.Add(MakeCell(c.Amount.ToString("N5")));
                    row.Cells.Add(MakeCell(c.DueDate.ToString("yyyy/MM/dd")));
                    row.Cells.Add(MakeCell(c.Notes));

                    checkHeaderGroup.Rows.Add(row);
                }

                doc.Blocks.Add(checkTable);
            }
            else
            {
                doc.Blocks.Add(new Paragraph(new Run(UiText.T("لا يوجد شيكات.", "There are no checks."))));
            }

            // ==== NOTES ====
            doc.Blocks.Add(new Paragraph(new Run(" ")));// spacer
            doc.Blocks.Add(new Paragraph(new Run(UiText.T("ملاحظات:", "Notes:")))
            {
                FontWeight = FontWeights.Bold
            });
            doc.Blocks.Add(new Paragraph(new Run(dto.Notes ?? "")));

            doc.Blocks.Add(new Paragraph(new Run("────────────────────────────────────────")));
            doc.Blocks.Add(new Paragraph(new Run(UiText.T("شكراً لتعاملكم", "Thank you for your business")))
            {
                TextAlignment = TextAlignment.Center,
                FontStyle = FontStyles.Italic
            });
            UiText.ApplyDocument(doc);

            // ==== PRINT DIALOG ====
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                IDocumentPaginatorSource dps = doc;
                printDialog.PrintDocument(dps.DocumentPaginator, UiText.T("طباعة سند قبض", "Print Receipt Voucher"));
            }
        }

        private void PrintVoucherA4(VoucherWriteDto dto)
        {
            FlowDocument doc = new FlowDocument
            {
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 16,
                PagePadding = new Thickness(50),
                ColumnWidth = double.PositiveInfinity
            };

            // HEADER
            Paragraph header = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 26,
                FontWeight = FontWeights.Bold
            };
            header.Inlines.Add("Raccoon Warehouse");
            doc.Blocks.Add(header);

            Paragraph title = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 24,
                FontWeight = FontWeights.Bold
            };
            title.Inlines.Add(UiText.T("سند قبض", "Receipt Voucher"));
            doc.Blocks.Add(title);

            doc.Blocks.Add(new Paragraph(new Run("-----------------------------------------------------------")));

            // BASIC INFORMATION TABLE
            Table infoTable = new Table();
            infoTable.CellSpacing = 10;
            infoTable.Columns.Add(new TableColumn());
            infoTable.Columns.Add(new TableColumn());

            var group = new TableRowGroup();
            infoTable.RowGroups.Add(group);

            void AddInfo(string label, string value)
            {
                var row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(label)) { FontWeight = FontWeights.Bold }));
                row.Cells.Add(new TableCell(new Paragraph(new Run(value))));
                group.Rows.Add(row);
            }

            AddInfo(UiText.T("رقم السند:", "Voucher No:"), dto.Id.ToString());
            AddInfo(UiText.T("التاريخ:", "Date:"), dto.CreatedDate.ToString("yyyy/MM/dd"));
            AddInfo(UiText.T("العميل:", "Customer:"), AccountComboBox.Text);
            AddInfo(UiText.T("طريقة الدفع:", "Payment Method:"), dto.PaymentType.ToString());
            AddInfo(UiText.T("المبلغ:", "Amount:"), dto.Amount.ToString("N5"));

            doc.Blocks.Add(infoTable);

            doc.Blocks.Add(new Paragraph(new Run(" ")));


            // CHECKS SECTION
            Paragraph checkTitle = new Paragraph(new Run(UiText.T("تفاصيل الشيكات", "Check Details")))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 20
            };
            doc.Blocks.Add(checkTitle);

            if (dto.Checks != null && dto.Checks.Count > 0)
            {
                Table tbl = new Table();
                tbl.CellSpacing = 0;

                string[] headers =
                {
                    UiText.T("رقم الشيك", "Check Number"),
                    UiText.T("البنك", "Bank"),
                    UiText.T("المبلغ", "Amount"),
                    UiText.T("تاريخ الاستحقاق", "Due Date"),
                    UiText.T("ملاحظات", "Notes")
                };

                foreach (var _ in headers)
                    tbl.Columns.Add(new TableColumn());

                TableRowGroup tgroup = new TableRowGroup();
                tbl.RowGroups.Add(tgroup);

                // Header row
                TableRow headerRow = new TableRow();
                foreach (var h in headers)
                {
                    headerRow.Cells.Add(new TableCell(new Paragraph(new Run(h)))
                    {
                        FontWeight = FontWeights.Bold,
                        Padding = new Thickness(5),
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(0, 0, 0, 1)
                    });
                }
                tgroup.Rows.Add(headerRow);

                // Data rows
                foreach (var ch in dto.Checks)
                {
                    TableRow r = new TableRow();
                    r.Cells.Add(new TableCell(new Paragraph(new Run(ch.CheckNumber))) { Padding = new Thickness(5) });
                    r.Cells.Add(new TableCell(new Paragraph(new Run(ch.BankName))) { Padding = new Thickness(5) });
                    r.Cells.Add(new TableCell(new Paragraph(new Run(ch.Amount.ToString("N5")))) { Padding = new Thickness(5) });
                    r.Cells.Add(new TableCell(new Paragraph(new Run(ch.DueDate.ToString("yyyy/MM/dd")))) { Padding = new Thickness(5) });
                    r.Cells.Add(new TableCell(new Paragraph(new Run(ch.Notes ?? ""))) { Padding = new Thickness(5) });

                    tgroup.Rows.Add(r);
                }

                doc.Blocks.Add(tbl);
            }
            else
            {
                doc.Blocks.Add(new Paragraph(new Run(UiText.T("لا يوجد شيكات.", "There are no checks."))));
            }

            doc.Blocks.Add(new Paragraph(new Run(" ")));
            doc.Blocks.Add(new Paragraph(new Run(UiText.T("ملاحظات:", "Notes:"))) { FontWeight = FontWeights.Bold });
            doc.Blocks.Add(new Paragraph(new Run(dto.Notes ?? "")));

            doc.Blocks.Add(new Paragraph(new Run("-----------------------------------------------------------")));

            var footer = new Paragraph
            {
                TextAlignment = TextAlignment.Left,
                FontSize = 18,
                FontWeight = FontWeights.Bold
            };
            footer.Inlines.Add(UiText.T("توقيع الموظف: ________________________", "Employee Signature: ________________________"));
            doc.Blocks.Add(footer);
            UiText.ApplyDocument(doc);

            // PRINT
            PrintDialog dialog = new PrintDialog();
            if (dialog.ShowDialog() == true)
            {
                IDocumentPaginatorSource dps = doc;
                dialog.PrintDocument(dps.DocumentPaginator, "Print Voucher A4");
            }
        }



        private FlowDocument BuildVoucherA4Document(VoucherWriteDto dto)
        {
            FlowDocument doc = new FlowDocument
            {
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 16,
                PagePadding = new Thickness(50),
                ColumnWidth = double.PositiveInfinity
            };

            // ---- HEADER ----
            Paragraph header = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 26,
                FontWeight = FontWeights.Bold
            };
            header.Inlines.Add("Raccoon Warehouse");
            doc.Blocks.Add(header);

            Paragraph title = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 24,
                FontWeight = FontWeights.Bold
            };
            title.Inlines.Add(UiText.T("سند قبض", "Receipt Voucher"));
            doc.Blocks.Add(title);

            doc.Blocks.Add(new Paragraph(new Run("-----------------------------------------------------------")));

            // BASIC INFO
            Table infoTable = new Table();
            infoTable.CellSpacing = 10;
            infoTable.Columns.Add(new TableColumn());
            infoTable.Columns.Add(new TableColumn());

            var group = new TableRowGroup();
            infoTable.RowGroups.Add(group);

            void AddInfo(string label, string value)
            {
                var row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(label)) { FontWeight = FontWeights.Bold }));
                row.Cells.Add(new TableCell(new Paragraph(new Run(value))));
                group.Rows.Add(row);
            }

            AddInfo(UiText.T("رقم السند:", "Voucher No:"), dto.Id.ToString());
            AddInfo(UiText.T("التاريخ:", "Date:"), dto.CreatedDate.ToString("yyyy/MM/dd"));
            AddInfo(UiText.T("العميل:", "Customer:"), AccountComboBox.Text);
            AddInfo(UiText.T("طريقة الدفع:", "Payment Method:"), dto.PaymentType.ToString());
            AddInfo(UiText.T("المبلغ:", "Amount:"), dto.Amount.ToString("N5"));

            doc.Blocks.Add(infoTable);
            doc.Blocks.Add(new Paragraph(new Run(" ")));

            // CHECKS TABLE
            Paragraph checkTitle = new Paragraph(new Run(UiText.T("تفاصيل الشيكات", "Check Details")))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 20
            };
            doc.Blocks.Add(checkTitle);

            if (dto.Checks != null && dto.Checks.Count > 0)
            {
                Table tbl = new Table();
                tbl.CellSpacing = 0;

                string[] headers =
                {
                    UiText.T("رقم الشيك", "Check Number"),
                    UiText.T("البنك", "Bank"),
                    UiText.T("المبلغ", "Amount"),
                    UiText.T("تاريخ الاستحقاق", "Due Date"),
                    UiText.T("ملاحظات", "Notes")
                };
                foreach (var _ in headers)
                    tbl.Columns.Add(new TableColumn());

                TableRowGroup tgroup = new TableRowGroup();
                tbl.RowGroups.Add(tgroup);

                // Header row
                TableRow hr = new TableRow();
                foreach (var h in headers)
                {
                    hr.Cells.Add(new TableCell(new Paragraph(new Run(h)))
                    {
                        FontWeight = FontWeights.Bold,
                        Padding = new Thickness(5),
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(0, 0, 0, 2)
                    });
                }
                tgroup.Rows.Add(hr);

                // Data rows
                foreach (var ch in dto.Checks)
                {
                    TableRow r = new TableRow();
                    r.Cells.Add(new TableCell(new Paragraph(new Run(ch.CheckNumber))) { Padding = new Thickness(5) });
                    r.Cells.Add(new TableCell(new Paragraph(new Run(ch.BankName))) { Padding = new Thickness(5) });
                    r.Cells.Add(new TableCell(new Paragraph(new Run(ch.Amount.ToString("N5")))) { Padding = new Thickness(5) });
                    r.Cells.Add(new TableCell(new Paragraph(new Run(ch.DueDate.ToString("yyyy/MM/dd")))) { Padding = new Thickness(5) });
                    r.Cells.Add(new TableCell(new Paragraph(new Run(ch.Notes ?? ""))) { Padding = new Thickness(5) });

                    tgroup.Rows.Add(r);
                }

                doc.Blocks.Add(tbl);
            }

            // NOTES
            doc.Blocks.Add(new Paragraph(new Run(" ")));
            doc.Blocks.Add(new Paragraph(new Run(UiText.T("ملاحظات:", "Notes:"))) { FontWeight = FontWeights.Bold });
            doc.Blocks.Add(new Paragraph(new Run(dto.Notes ?? "")));

            doc.Blocks.Add(new Paragraph(new Run("-----------------------------------------------------------")));

            var footer = new Paragraph
            {
                TextAlignment = TextAlignment.Left,
                FontSize = 18,
                FontWeight = FontWeights.Bold
            };
            footer.Inlines.Add(UiText.T("توقيع الموظف: ________________________", "Employee Signature: ________________________"));
            doc.Blocks.Add(footer);
            UiText.ApplyDocument(doc);

            return doc;
        }
        private FixedDocument ConvertFlowDocumentToFixed(FlowDocument flowDoc)
        {
            DocumentPaginator paginator = ((IDocumentPaginatorSource)flowDoc).DocumentPaginator;

            paginator.PageSize = new Size(793, 1122); // A4 size

            FixedDocument fixedDoc = new FixedDocument();

            for (int i = 0; i < paginator.PageCount; i++)
            {
                DocumentPage page = paginator.GetPage(i);

                FixedPage fixedPage = new FixedPage();
                fixedPage.Width = paginator.PageSize.Width;
                fixedPage.Height = paginator.PageSize.Height;

                // WRAP VISUAL INSIDE RECTANGLE → UIElement
                Rectangle rect = new Rectangle
                {
                    Width = paginator.PageSize.Width,
                    Height = paginator.PageSize.Height,
                    Fill = new VisualBrush(page.Visual)
                };

                // add rectangle to page
                fixedPage.Children.Add(rect);

                PageContent pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(fixedPage);

                fixedDoc.Pages.Add(pageContent);
            }

            return fixedDoc;
        }
      
        private void ForceRenderDocument(FlowDocument doc)
        {
            // Create hidden RichTextBox to render the document
            RichTextBox rtb = new RichTextBox();
            rtb.Document = doc;
            rtb.Width = 800;
            rtb.Height = 1122;

            // Force layout pass
            rtb.Measure(new Size(800, 1122));
            rtb.Arrange(new Rect(new Size(800, 1122)));
            rtb.UpdateLayout();
        }


        private void PrintBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Commit DataGrid edits
                ChecksGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                ChecksGrid.CommitEdit(DataGridEditingUnit.Row, true);

                _checks = ChecksGrid.Items
                                    .OfType<CheckWriteDto>()
                                    .ToList();

                if (!decimal.TryParse(Amount.Text, out decimal amount))
                {
                    MessageBox.Show(UiText.T("يرجى إدخال مبلغ صحيح.", "Please enter a valid amount."), UiText.T("خطأ", "Error"));
                    return;
                }

                if (PaymentTypeCombo.SelectedItem is not ComboBoxItem paymentItem || paymentItem.Tag == null)
                {
                    MessageBox.Show(UiText.T("يرجى اختيار طريقة الدفع.", "Please choose a payment method."), UiText.T("تنبيه", "Notice"));
                    return;
                }

                var paymentType = (PaymentType)int.Parse(paymentItem.Tag.ToString()!);
                var dto = new VoucherWriteDto
                {
                    Id = _currentVoucherId ?? 0,
                    VoucherNumber = ReceiptNumber.Text,
                    Amount = amount,
                    Notes = string.IsNullOrWhiteSpace(ReceiptDescription.Text) ? null : ReceiptDescription.Text.Trim(),
                    CreatedDate = ReceiptDate.SelectedDate ?? DateTime.Now,
                    UpdatedDate = DateTime.Now,
                    VoucherType = VoucherType.Receipt,
                    PaymentType = paymentType,
                    Checks = paymentType == PaymentType.Check ? _checks : null
                };

                if (paymentType == PaymentType.Check && !ValidatePaymentByCheck(amount))
                    return;

                PrintVoucherPdf(dto);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء الطباعة", "An error occurred while printing")}:\n{ex.Message}", UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintVoucherPdf(VoucherWriteDto dto)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF File (*.pdf)|*.pdf",
                FileName = $"Voucher_{dto.VoucherNumber}.pdf"
            };

            if (dlg.ShowDialog() == true)
            {
                PdfGenerator.GenerateVoucherPdf(dto, dlg.FileName);

                MessageBox.Show(UiText.T("تم حفظ ملف PDF بنجاح.", "The PDF file was saved successfully."), UiText.T("نجاح", "Success"),
                    MessageBoxButton.OK, MessageBoxImage.Information);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dlg.FileName,
                    UseShellExecute = true
                });
            }
        }

        #endregion
        #region search voucher 
        private async void SearchVoucherBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var search = new SearchVoucherWindow(_voucherService, _allAccountUsers, true);
                if (search.ShowDialog() == true && search.Result != null)
                {
                    await LoadVoucherWithLoadingAsync(search.Result);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("حدث خطأ أثناء البحث عن السند", "An error occurred while searching for the voucher")}:\n{ex.Message}", UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadVoucherWithLoadingAsync(VoucherReadDto dto)
        {
            _loadingService.Show();
            try
            {
                await Task.Delay(1);
                LoadVoucher(dto);
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void LoadVoucher(VoucherReadDto dto)
        {
            if (dto == null)
                return;

            _currentVoucherId = dto.Id;
            _originalChecks = dto.Checks?.ToList() ?? new();

            ReceiptNumber.Text = dto.VoucherNumber;
            ReceiptDate.SelectedDate = dto.CreatedDate;
            Amount.Text = dto.Amount.ToString();
            ReceiptDescription.Text = dto.Notes;

            AccountComboBox.SelectedValue = dto.CustomerId;
            WarehouseComboBox.SelectedValue = dto.WarehouseId;

            PaymentTypeCombo.SelectedIndex = (int)dto.PaymentType - 1;

            _checks = dto.Checks?.Select(c => new CheckWriteDto
            {
                Id = c.Id,
                BankName = c.BankName,
                CheckNumber = c.CheckNumber,
                Amount = c.Amount,
                Status = c.Status,
                Notes = c.Notes,
                DueDate = c.DueDate
            }).ToList() ?? new();

            ChecksGrid.ItemsSource = _checks;
            ChecksGrid.Visibility = dto.PaymentType == PaymentType.Check ? Visibility.Visible : Visibility.Collapsed;
            AddCheckButton.Visibility = dto.PaymentType == PaymentType.Check ? Visibility.Visible : Visibility.Collapsed;
            CheckFieldsPanel.Visibility = dto.PaymentType == PaymentType.Check ? Visibility.Visible : Visibility.Collapsed;
            UiText.ApplyTranslations(this);

            PrintBtn.Visibility = Visibility.Visible;
            NewVoucherBtn.Visibility = Visibility.Visible;
        }
        #endregion
        #region payment method handle 
        private PaymentMethod MapPaymentMethod(PaymentType paymentType)
        {
            return paymentType switch
            {
                PaymentType.Cash => PaymentMethod.Cash,
                PaymentType.Visa => PaymentMethod.Visa,
                PaymentType.Master => PaymentMethod.Master,
                PaymentType.Debit => PaymentMethod.BankTransfer,
                PaymentType.Check => PaymentMethod.Check,
                PaymentType.MobilePayment => PaymentMethod.MobilePayment,
                PaymentType.Credit => PaymentMethod.Credit,
                _ => PaymentMethod.Cash
            };
        }

        private FinancialSourceType GetSourceType(VoucherType voucherType)
        {
            return voucherType switch
            {
                VoucherType.Receipt => FinancialSourceType.ReceiptVoucher,
                VoucherType.Payment => FinancialSourceType.PaymentVoucher,
                _ => FinancialSourceType.Manual
            };
        }

        private TransactionDirection GetDirection(VoucherType voucherType)
        {
            // سند قبض = In ، سند صرف = Out
            return voucherType switch
            {
                VoucherType.Receipt => TransactionDirection.In,
                VoucherType.Payment => TransactionDirection.Out,
                _ => TransactionDirection.In
            };
        }

        private bool ValidatePaymentByCheck(decimal voucherAmount)
        {
            if (_checks.Count == 0)
            {
                MessageBox.Show(UiText.T("يرجى إضافة شيك واحد على الأقل.", "Please add at least one check."), UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (_checks.Any(c => string.IsNullOrWhiteSpace(c.CheckNumber) || c.Amount <= 0))
            {
                MessageBox.Show(UiText.T("بيانات الشيك غير مكتملة أو تحتوي مبالغ غير صالحة.", "The check data is incomplete or contains invalid amounts."), UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var duplicateCheck = _checks
                .Where(c => !string.IsNullOrWhiteSpace(c.CheckNumber))
                .GroupBy(c => c.CheckNumber.Trim(), StringComparer.OrdinalIgnoreCase)
                .Any(g => g.Count() > 1);

            if (duplicateCheck)
            {
                MessageBox.Show(UiText.T("لا يمكن تكرار رقم الشيك داخل نفس السند.", "The check number cannot be duplicated within the same voucher."), UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var totalChecks = _checks.Sum(c => c.Amount);
            if (totalChecks != voucherAmount)
            {
                MessageBox.Show(UiText.T("مجموع مبالغ الشيكات يجب أن يساوي مبلغ السند.", "The total of the checks must equal the voucher amount."), UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        #endregion


    }
}
