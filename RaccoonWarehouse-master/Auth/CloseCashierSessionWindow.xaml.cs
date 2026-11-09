using RaccoonWarehouse.Application.Service.Cashers;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
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
        private readonly ICashierSessionSettlementService _settlementService;
        private readonly IUserSession _userSession;
        private readonly ILoadingService _loadingService;
        private readonly RaccoonWarehouse.Core.Audit.IAuditLogService _auditLogService;

        private decimal _opening;
        private bool _isInitialized;
        private bool _isClosing;
        private bool _sessionFinalized;
        private CashierSessionSettlementReadDto? _calculation;

        public CloseCashierSessionWindow(
            ICashierSessionService cashierSessionService,
            ICashierSessionSettlementService settlementService,
            IUserSession userSession,
            ILoadingService loadingService,
            RaccoonWarehouse.Core.Audit.IAuditLogService auditLogService)
        {
            
            InitializeComponent();
            UiText.ApplyWindow(this);
            UiText.ApplyTranslations(this);

            _cashierSessionService = cashierSessionService;
            _settlementService = settlementService;
            _userSession = userSession;
            _loadingService = loadingService;
            _auditLogService = auditLogService;
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

            var savedSettlement = await _settlementService.LoadSavedAsync(currentSession.Id);
            if (savedSettlement != null)
            {
                currentSession.Status = SessionStatus.Closing;
                ApplyCalculation(savedSettlement, isSaved: true);
                return;
            }

            ExpectedText.Text = "—";
            CountedTextBox.Text = string.Empty;
            CountedVisaTextBox.Text = string.Empty;
            CountedTextBox.IsReadOnly = false;
            CountedVisaTextBox.IsReadOnly = false;
            ResultPanel.Visibility = Visibility.Collapsed;
            NotesPanel.Visibility = Visibility.Collapsed;
            ActionsPanel.Visibility = Visibility.Collapsed;
            ActionsItemsControl.ItemsSource = null;
            CalculateButton.Visibility = Visibility.Visible;
            CalculateButton.IsEnabled = true;
            CloseSessionButton.Visibility = Visibility.Collapsed;
            _calculation = null;
        }

        private async void Calculate_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized || _isClosing)
                return;

            ErrorText.Visibility = Visibility.Collapsed;
            if (!TryParseDecimal(CountedTextBox.Text, out var countedCash) || countedCash < 0
                || !TryParseDecimal(CountedVisaTextBox.Text, out var countedVisa) || countedVisa < 0)
            {
                ShowError(UiText.T("يرجى إدخال مبلغ نقدي وVisa صحيحين.", "Please enter valid non-negative Cash and Visa amounts."));
                return;
            }

            try
            {
                CalculateButton.IsEnabled = false;
                _loadingService.Show();
                _calculation = await _settlementService.CalculateAsync(
                    _userSession.CurrentCashierSession!.Id,
                    countedCash,
                    countedVisa);
                _userSession.CurrentCashierSession!.Status = SessionStatus.Closing;

                ApplyCalculation(_calculation, isSaved: true);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                CalculateButton.IsEnabled = true;
            }
            finally
            {
                _loadingService.Hide();
            }
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

            if (_calculation == null)
            {
                ShowError(UiText.T("يرجى حساب الفرق أولاً.", "Please calculate the differences first."));
                return;
            }

            var sessionId = currentSession.Id;

            try
            {
                _isClosing = true;
                CloseSessionButton.IsEnabled = false;
                _loadingService.Show();

                await _cashierSessionService.CloseSessionAsync(
                    sessionId,
                    new CashierSessionSettlementDto
                    {
                        CountedCash = _calculation.CountedCash,
                        CountedVisa = _calculation.CountedVisa,
                        ClosingNotes = NotesTextBox.Text.Trim(),
                        ClosedBy = currentSession.CashierId
                    },
                    _calculation);

                // 3) Clear only cashier-session runtime state
                _sessionFinalized = true;
                _userSession.ClearCashierSession();
                await _auditLogService.WriteAsync(new RaccoonWarehouse.Core.Audit.AuditEvent(
                    "CashierSession.Close",
                    EntityType: "CashierSession",
                    EntityId: sessionId));

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

        private void CloseCashierSessionWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_sessionFinalized)
                return;

            e.Cancel = true;
            ShowError(UiText.T(
                "يجب حفظ وإنهاء جلسة الكاشير قبل إغلاق هذه النافذة.",
                "You must save and close the cashier session before closing this window."));
        }

        private void ApplyCalculation(CashierSessionSettlementReadDto calculation, bool isSaved)
        {
            _calculation = calculation;
            ExpectedText.Text = calculation.ExpectedCash.ToString("N2");
            CountedTextBox.Text = calculation.CountedCash.ToString("N2");
            CountedVisaTextBox.Text = calculation.CountedVisa.ToString("N2");
            CountedTextBox.IsReadOnly = isSaved;
            CountedVisaTextBox.IsReadOnly = isSaved;
            ExpectedCashResultText.Text = calculation.ExpectedCash.ToString("N2");
            ExpectedVisaResultText.Text = calculation.ExpectedVisa.ToString("N2");
            DiffText.Text = calculation.CashDifference.ToString("N2");
            VisaDiffText.Text = calculation.VisaDifference.ToString("N2");
            DiffText.Foreground = DifferenceBrush(calculation.CashDifference);
            VisaDiffText.Foreground = DifferenceBrush(calculation.VisaDifference);
            ActionsItemsControl.ItemsSource = calculation.Actions;
            var matched = calculation.IsCashMatched && calculation.IsVisaMatched;
            SettlementStatusText.Text = matched
                ? UiText.T("مطابق: النقد وVisa متساويان. تم حفظ النتيجة.", "Matched: Cash and Visa are identical. Result saved.")
                : UiText.T("يوجد فرق وتم حفظ النتيجة. لا يمكن إعادة إدخال المبالغ.", "Difference detected and result saved. Amounts cannot be entered again.");
            SettlementStatusText.Foreground = DifferenceBrush(matched ? 0m : 1m);

            ResultPanel.Visibility = Visibility.Visible;
            NotesPanel.Visibility = Visibility.Visible;
            ActionsPanel.Visibility = Visibility.Visible;
            CalculateButton.Visibility = Visibility.Collapsed;
            CloseSessionButton.Visibility = Visibility.Visible;
            CloseSessionButton.IsEnabled = true;
        }

        private static SolidColorBrush DifferenceBrush(decimal difference) =>
            difference == 0m ? new SolidColorBrush(Color.FromRgb(46, 125, 50)) : new SolidColorBrush(Color.FromRgb(198, 40, 40));

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
