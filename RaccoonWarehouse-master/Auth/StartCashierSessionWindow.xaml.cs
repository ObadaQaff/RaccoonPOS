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
    /// Interaction logic for StartCashierSessionWindow.xaml
    /// </summary>
    public partial class StartCashierSessionWindow : Window
    {
        private readonly ICashierSessionService _cashierSessionService;
        private readonly IFinancialTransactionService _financialService;
        private readonly IUserSession _userSession;

        public StartCashierSessionWindow(
            ICashierSessionService cashierSessionService,
            IFinancialTransactionService financialService,
            IUserSession userSession)
        {
            InitializeComponent();

            _cashierSessionService = cashierSessionService;
            _financialService = financialService;
            _userSession = userSession;

            CashierNameText.Text = _userSession.CurrentUser?.Name ?? "—";
        }

        private async void StartSession_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            if (_userSession.CurrentUser == null)
            {
                ShowError("لا يوجد مستخدم مسجل دخول.");
                return;
            }

            if (!TryParseDecimal(StartBalanceTextBox.Text, out var opening))
            {
                ShowError("يرجى إدخال مبلغ صحيح.");
                return;
            }

            if (opening < 0)
            {
                ShowError("لا يمكن أن يكون الرصيد الافتتاحي سالب.");
                return;
            }

            try
            {
                // 1) Create DB Session
                var session = await _cashierSessionService.OpenSessionAsync(_userSession.CurrentUser.Id, opening);

                // 2) Put it in runtime session (UserSession)
                _userSession.StartSession(_userSession.CurrentUser);
                session.CashierName = _userSession.CurrentUser.Name; // Optional, for easier access in UI
                _userSession.AttachCashierSession(session);


                // 3) Post Financial (Manual Cash IN) for opening float
                if (opening > 0)
                {
                    var post = new FinancialPostDto
                    {
                        Direction = TransactionDirection.In,
                        Method = PaymentMethod.Cash,
                        Amount = opening,
                        TransactionDate = DateTime.Now,
                        SourceType = FinancialSourceType.SessionOpening, 
                        SourceId = null,
                        CashierSessionId = session.Id,
                        CashierId = session.CashierId,

                        Notes = $"Opening Balance - Session #{session.Id}"
                    };

                    var fin = await _financialService.PostAsync(post);
                    if (!fin.Success)
                    {
                        // هنا قرارك: تترك الجلسة مفتوحة بس تنبه، أو تعمل rollback (أصعب)
                        MessageBox.Show(fin.Message ?? "تم فتح الجلسة لكن فشل تسجيل حركة الافتتاح.", "تحذير");
                    }
                }

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

        private void ShowError(string msg)
        {
            ErrorText.Text = msg;
            ErrorText.Visibility = Visibility.Visible;
        }

        private bool TryParseDecimal(string text, out decimal value)
        {
            // يقبل 10.5 أو 10,5 حسب نظام الجهاز
            return decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value)
                || decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }
    }
}
