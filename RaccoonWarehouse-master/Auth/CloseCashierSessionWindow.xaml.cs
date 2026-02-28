using RaccoonWarehouse.Application.Service.Cashers;
using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
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

        private decimal _opening;
        private decimal _expected;

        public CloseCashierSessionWindow(
            ICashierSessionService cashierSessionService,
            IFinancialTransactionService financialService,
            IUserSession userSession)
        {
            InitializeComponent();

            _cashierSessionService = cashierSessionService;
            _financialService = financialService;
            _userSession = userSession;
            InitAsync();
        }

        public async Task InitAsync()
        {
            if (_userSession.CurrentUser == null || _userSession.CurrentCashierSession == null)
            {
                ErrorText.Text = "لا توجد جلسة مفتوحة.";
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            CashierNameText.Text = _userSession.CurrentUser.Name;
            SessionIdText.Text = _userSession.CurrentCashierSession.Id.ToString();

            _opening = _userSession.CurrentCashierSession.StatrBalance;
            OpeningText.Text = _opening.ToString("N2");

            _expected = await _financialService
                .GetExpectedCashForSessionAsync(_userSession.CurrentCashierSession.Id);
            ExpectedText.Text = _expected.ToString("N2");

            CountedTextBox.Text = _expected.ToString("N2"); // default
            UpdateDiff();
        }

        private async void CloseSession_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            if (_userSession.CurrentCashierSession == null)
            {
                ShowError("لا توجد جلسة مفتوحة.");
                return;
            }

            if (!TryParseDecimal(CountedTextBox.Text, out var counted))
            {
                ShowError("يرجى إدخال مبلغ صحيح.");
                return;
            }

            if (counted < 0)
            {
                ShowError("لا يمكن أن يكون المبلغ سالب.");
                return;
            }

            var sessionId = _userSession.CurrentCashierSession.Id;
            var diff = counted - _expected; // + over, - short

            try
            {
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

                        SourceType = FinancialSourceType.Manual,
                        SourceId = null,

                        CashierSessionId = sessionId,
                        CashierId = _userSession.CurrentCashierSession.CashierId,

                        Notes = $"Cash Over/Short on close. Diff={diff:N2}. {NotesTextBox.Text}"
                    };

                    var fin = await _financialService.PostAsync(post);
                    if (!fin.Success)
                        MessageBox.Show(fin.Message ?? "تم إغلاق الجلسة لكن فشل تسجيل فرق الإغلاق.", "تحذير");
                }

                // 3) End runtime session
                _userSession.EndSession();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
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
                DiffText.Text = "—";
                return;
            }

            var diff = counted - _expected;
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
