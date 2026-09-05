using RaccoonWarehouse.Application.Service.Cashers;
using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RaccoonWarehouse.Auth
{
    /// <summary>
    /// Interaction logic for CloseCashierSessionWindow.xaml
    /// </summary>
    public partial class CloseCashierSessionWindow : Window
    {
        private readonly ICashierSessionService _cashierSessionService;
        private readonly IFinancialTransactionService _financialService;
        private readonly IUserSession _userSession;
        private readonly ILoadingService _loadingService;

        private decimal _opening;
        private decimal _expected;
        private decimal _expectedClosingCash;
        private bool _isInitialized;
        private bool _isClosing;

        public CloseCashierSessionWindow(
            ICashierSessionService cashierSessionService,
            IFinancialTransactionService financialService,
            IUserSession userSession,
            ILoadingService loadingService)
        {
            InitializeComponent();
            UiText.ApplyWindow(this);
            UiText.ApplyTranslations(this);

            _cashierSessionService = cashierSessionService;
            _financialService = financialService;
            _userSession = userSession;
            _loadingService = loadingService;
            Loaded += CloseCashierSessionWindow_Loaded;
        }

        private async void CloseCashierSessionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _loadingService.Show();

            try
            {
                await InitAsync();
                _isInitialized = _userSession.CurrentCashierSession != null;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                _loadingService.Hide();
                CloseSessionButton.IsEnabled = true;
            }
        }

        public async Task InitAsync()
        {
            var currentUser = _userSession.CurrentUser;
            var currentSession = _userSession.CurrentCashierSession;

            if (currentUser == null || currentSession == null)
            {
                ErrorText.Text = UiText.T("لا توجد جلسة مفتوحة.", "There is no open session.");
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            CashierNameText.Text = currentUser.Name;
            SessionIdText.Text = currentSession.Id.ToString();

            _opening = currentSession.StatrBalance;
            OpeningText.Text = _opening.ToString("N2");

            _expected = await _financialService
                .GetExpectedCashForSessionAsync(currentSession.Id);
            _expectedClosingCash = _opening + _expected;
            ExpectedText.Text = _expectedClosingCash.ToString("N2");

            CountedTextBox.Text = Math.Max(_expectedClosingCash, 0m).ToString("N2"); // default
            UpdateDiff();
        }

        private async void CloseSession_Click(object sender, RoutedEventArgs e)
        {
            if (_isClosing)
                return;

            if (!_isInitialized)
            {
                ShowError(UiText.T("تعذر تحميل الجلسة الحالية.", "The current session could not be loaded."));
                return;
            }

            ErrorText.Visibility = Visibility.Collapsed;

            var currentSession = _userSession.CurrentCashierSession;
            if (currentSession == null)
            {
                ShowError(UiText.T("لا توجد جلسة مفتوحة.", "There is no open session."));
                return;
            }

            if (!TryParseDecimal(CountedTextBox.Text, out var counted))
            {
                ShowError(UiText.T("يرجى إدخال مبلغ صحيح.", "Please enter a valid amount."));
                return;
            }

            if (counted < 0)
            {
                ShowError(UiText.T("لا يمكن أن يكون المبلغ سالب.", "The amount cannot be negative."));
                return;
            }

            var sessionId = currentSession.Id;
            var diff = counted - _expectedClosingCash; // + over, - short

            try
            {
                _isClosing = true;
                CloseSessionButton.IsEnabled = false;
                _loadingService.Show();

                // 1) Close session (store ending balance)
                await _cashierSessionService.CloseSessionAsync(sessionId, counted);

                // 2) Record Over/Short as FinancialTransaction (اختياري لكن احترافي)
                if (diff != 0)
                {
                    var direction = diff > 0 ? TransactionDirection.In : TransactionDirection.Out;

                    var post = new FinancialPostDto
                    {
                        Direction = direction,
                        Method = PaymentMethod.Cash,
                        Amount = Math.Abs(diff),
                        TransactionDate = DateTime.Now,

                        SourceType = FinancialSourceType.SessionClosing,
                        SourceId = null,

                        CashierSessionId = sessionId,
                        CashierId = currentSession.CashierId,

                        Notes = $"Cash Over/Short on close. Diff={diff:N2}. {NotesTextBox.Text}"
                    };

                    var fin = await _financialService.PostAsync(post);
                    if (!fin.Success)
                        MessageBox.Show(
                            fin.Message ?? UiText.T("تم إغلاق الجلسة لكن فشل تسجيل فرق الإغلاق.", "The session was closed, but recording the closing difference failed."),
                            UiText.T("تحذير", "Warning"));
                }

                // 3) Clear only cashier-session runtime state
                _userSession.ClearCashierSession();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                _isClosing = false;
                CloseSessionButton.IsEnabled = true;
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CountedTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateDiff();
        }

        private void UpdateDiff()
        {
            if (!TryParseDecimal(CountedTextBox.Text, out var counted))
            {
                DiffText.Text = "-";
                return;
            }

            var diff = counted - _expectedClosingCash;
            DiffText.Text = diff.ToString("N2");
        }

        private void ShowError(string msg)
        {
            ErrorText.Text = msg;
            ErrorText.Visibility = Visibility.Visible;
        }

        private bool TryParseDecimal(string text, out decimal value)
        {
            return decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value)
                || decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }
    }
}
