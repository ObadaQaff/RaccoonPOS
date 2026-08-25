using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Cashers;
using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Application.Service.Vouchers;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.POS.VM;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Helpers.Pdf;
using RaccoonWarehouse.Helpers.Pdf.Reports;
using RaccoonWarehouse.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RaccoonWarehouse.POS
{
    public partial class DailySalesReport : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUserSession? _userSession;
        private readonly DailySalesReportViewModel _vm;
        private int? _initialCashierSessionId;
        private int? _initialCashierId;
        private int? _selectedSessionIdOverride;
        private bool _isLoading;
        private bool _isInitializing;
        private List<SessionTransactionRowDto> _allTransactionRows = new();

        private static bool IsTransactionOperator(UserReadDto user)
            => user.Role == UserRole.Casher || user.Role == UserRole.Admin;

        [ActivatorUtilitiesConstructor]
        public DailySalesReport(IServiceProvider serviceProvider, IUserSession userSession)
            : this(serviceProvider, userSession, null, null)
        {
        }

        public DailySalesReport(IServiceProvider serviceProvider, int cashierSessionId, int cashierId)
            : this(serviceProvider, null, cashierSessionId, cashierId)
        {
        }

        private DailySalesReport(IServiceProvider serviceProvider, IUserSession? userSession, int? cashierSessionId, int? cashierId)
        {
            InitializeComponent();
            UiText.ApplyWindow(this);
            UiText.ApplyTranslations(this);

            _serviceProvider = serviceProvider;
            _userSession = userSession;
            _initialCashierSessionId = cashierSessionId;
            _initialCashierId = cashierId;
            _vm = new DailySalesReportViewModel();
            DataContext = _vm;
            ContentRendered += DailySalesReport_ContentRendered;
        }

        public void InitializeForCashier(int cashierId, int? cashierSessionId = null)
        {
            _initialCashierId = cashierId;
            _initialCashierSessionId = cashierSessionId;
        }

        private async void DailySalesReport_ContentRendered(object? sender, EventArgs e)
        {
            ContentRendered -= DailySalesReport_ContentRendered;
            await InitializeDashboardAsync();
            await LoadReportAsync();
        }

        private async Task InitializeDashboardAsync()
        {
            _isInitializing = true;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var cashierSessionService = scope.ServiceProvider.GetRequiredService<ICashierSessionService>();

                var usersResult = await userService.GetAllAsync();
                var cashiers = usersResult.Success && usersResult.Data != null
                    ? usersResult.Data
                        .Where(IsTransactionOperator)
                        .OrderBy(x => x.Name)
                        .ToList()
                    : new List<UserReadDto>();

                CashierComboBox.ItemsSource = cashiers;
                CashierComboBox.SelectedItem = null;

                if (cashiers.Count == 0)
                {
                    SessionComboBox.ItemsSource = new[] { SessionOption.CreateAll() };
                    SessionComboBox.SelectedIndex = 0;
                    return;
                }

                var selectedCashierId = _initialCashierId
                    ?? _userSession?.CurrentCashierSession?.CashierId
                    ?? cashiers.First().Id;

                var selectedCashier = cashiers.FirstOrDefault(x => x.Id == selectedCashierId) ?? cashiers.First();
                CashierComboBox.SelectedItem = selectedCashier;

                var sessionsResult = await cashierSessionService.GetAllAsync();
                var cashierSessions = sessionsResult.Success && sessionsResult.Data != null
                    ? sessionsResult.Data
                        .Where(x => x.CashierId == selectedCashier.Id)
                        .OrderByDescending(x => x.OpenedAt)
                        .ToList()
                    : new List<CashierSessionReadDto>();

                BindSessionOptions(cashierSessions, _initialCashierSessionId ?? _userSession?.CurrentCashierSession?.Id);
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private void BindSessionOptions(List<CashierSessionReadDto> sessions, int? preferredSessionId = null)
        {
            var options = new List<SessionOption> { SessionOption.CreateAll() };
            options.AddRange(sessions.Select(SessionOption.Create));

            SessionComboBox.ItemsSource = options;

            SessionOption? selectedOption = null;

            if (preferredSessionId.HasValue)
                selectedOption = options.FirstOrDefault(x => x.SessionId == preferredSessionId.Value);

            if (selectedOption == null && _userSession?.CurrentCashierSession != null)
                selectedOption = options.FirstOrDefault(x => x.SessionId == _userSession.CurrentCashierSession.Id);

            SessionComboBox.SelectedItem = selectedOption ?? options.First();
        }

        private async void CashierComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || !IsLoaded)
                return;

            if (CashierComboBox.SelectedItem is not UserReadDto cashier)
                return;

            await ReloadSessionsForCashierAsync(cashier.Id, null);
            await LoadReportAsync();
        }

        private async void SessionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || !IsLoaded)
                return;

            if (SessionComboBox.SelectedItem is SessionOption option)
            {
                _selectedSessionIdOverride = option.SessionId;
            }

            await LoadReportAsync();
        }

        private async void RefreshScope_Click(object sender, RoutedEventArgs e)
        {
            await LoadReportAsync();
        }

        private async Task ReloadSessionsForCashierAsync(int cashierId, int? preferredSessionId)
        {
            _isInitializing = true;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var cashierSessionService = scope.ServiceProvider.GetRequiredService<ICashierSessionService>();
                var sessionsResult = await cashierSessionService.GetAllAsync();
                var cashierSessions = sessionsResult.Success && sessionsResult.Data != null
                    ? sessionsResult.Data
                        .Where(x => x.CashierId == cashierId)
                        .OrderByDescending(x => x.OpenedAt)
                        .ToList()
                    : new List<CashierSessionReadDto>();

                BindSessionOptions(cashierSessions, preferredSessionId);
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private async Task LoadReportAsync()
        {
            if (_isLoading)
                return;

            if (CashierComboBox.SelectedItem is not UserReadDto selectedCashier)
                return;

            _isLoading = true;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var cashierSessionService = scope.ServiceProvider.GetRequiredService<ICashierSessionService>();
                var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
                var voucherService = scope.ServiceProvider.GetRequiredService<IVoucherService>();
                var financialService = scope.ServiceProvider.GetRequiredService<IFinancialTransactionService>();

                var sessionsResult = await cashierSessionService.GetAllAsync();
                var cashierSessions = sessionsResult.Success && sessionsResult.Data != null
                    ? sessionsResult.Data
                        .Where(x => x.CashierId == selectedCashier.Id)
                        .OrderByDescending(x => x.OpenedAt)
                        .ToList()
                    : new List<CashierSessionReadDto>();

                BindSessionItems(cashierSessions);
                var selectedOption = SessionComboBox.SelectedItem as SessionOption;
                var selectedSession = selectedOption?.IsAll == true
                    ? null
                    : selectedOption?.Session;

                if (_selectedSessionIdOverride.HasValue)
                {
                    selectedSession = cashierSessions.FirstOrDefault(x => x.Id == _selectedSessionIdOverride.Value);
                }

                if (selectedSession == null && _initialCashierSessionId.HasValue)
                {
                    selectedSession = cashierSessions.FirstOrDefault(x => x.Id == _initialCashierSessionId.Value);
                }

                var scopedSessions = selectedSession != null
                    ? new List<CashierSessionReadDto> { selectedSession }
                    : cashierSessions;

                _vm.CashierName = selectedCashier.Name;
                _vm.ScopeLabel = selectedSession != null
                    ? $"{UiText.T("جلسة", "Session")} #{selectedSession.Id}"
                    : UiText.T("كل الورديات", "All shifts");
                _vm.SessionId = selectedSession?.Id ?? 0;
                _vm.SessionStatus = selectedSession != null
                    ? selectedSession.Status.ToString()
                    : UiText.T("الكل", "All");
                _vm.OpeningBalance = scopedSessions.Sum(x => x.StatrBalance);
                _vm.ClosingBalance = scopedSessions.Sum(x => x.EndingBalance);
                _vm.ReportDate = selectedSession?.OpenedAt.Date
                    ?? scopedSessions.OrderBy(x => x.OpenedAt).FirstOrDefault()?.OpenedAt.Date
                    ?? DateTime.Today;

                var from = scopedSessions.Count > 0
                    ? scopedSessions.Min(x => x.OpenedAt)
                    : DateTime.Today;
                var to = scopedSessions.Count > 0
                    ? scopedSessions.Max(x => x.ClosedAt ?? DateTime.Now)
                    : DateTime.Now;

                var sessionIds = scopedSessions.Select(x => x.Id).ToHashSet();
                var isSpecificSession = selectedSession != null;

                var rows = new List<SessionTransactionRowDto>();

                var invoiceResult = await invoiceService.SearchSalesInvoicesAsync(
                    invoiceNumber: null,
                    customerName: null,
                    dateFrom: from,
                    dateTo: to,
                    isSal: null,
                    isPOS: null,
                    status: InvoiceStatus.Completed);

                if (invoiceResult.Success && invoiceResult.Data != null)
                {
                    var invoices = invoiceResult.Data
                        .Where(i => i.CreatedDate >= from && i.CreatedDate <= to)
                        .Where(i => MatchesCashierScope(i.CashierSessionId, i.CasherId, selectedCashier.Id, sessionIds, isSpecificSession))
                        .OrderBy(i => i.ClosedAt ?? i.CreatedDate)
                        .ToList();

                    foreach (var invoice in invoices)
                        rows.Add(BuildInvoiceRow(invoice, selectedCashier.Name));

                    _vm.TotalInvoices = invoices.Count;
                    _vm.TotalSales = invoices.Sum(i => i.TotalAmount);
                    _vm.TotalDiscount = invoices.Sum(i => i.DiscountAmount ?? 0m);
                }
                else
                {
                    _vm.TotalInvoices = 0;
                    _vm.TotalSales = 0;
                    _vm.TotalDiscount = 0;
                }

                var sessionVouchers = (await voucherService.SearchVouchersAsync(
                        voucherNumber: null,
                        customerName: null,
                        dateFrom: from,
                        dateTo: to,
                        paymentType: null,
                        type: null))
                    .Where(v => v.CreatedDate >= from && v.CreatedDate <= to)
                    .Where(v => MatchesCashierScope(v.CashierSessionId, v.CasherId, selectedCashier.Id, sessionIds, isSpecificSession))
                    .OrderBy(v => v.CreatedDate)
                    .ToList();

                foreach (var voucher in sessionVouchers)
                    rows.Add(BuildVoucherRow(voucher, selectedCashier.Name));

                _vm.TotalVouchers = sessionVouchers.Count;

                var financialResult = await financialService.GetAllAsync();
                var financialTransactions = financialResult.Success && financialResult.Data != null
                    ? financialResult.Data
                        .Where(t => t.TransactionDate >= from && t.TransactionDate <= to)
                        .Where(t => MatchesCashierScope(t.CashierSessionId, t.CashierId, selectedCashier.Id, sessionIds, isSpecificSession))
                        .Where(t => !IsInvoicePostingFinancialSource(t.SourceType))
                        .OrderBy(t => t.TransactionDate)
                        .ToList()
                    : new List<RaccoonWarehouse.Domain.FinancialTransactions.DTOs.FinancialTransactionReadDto>();

                foreach (var tx in financialTransactions)
                    rows.Add(BuildFinancialRow(tx, selectedCashier.Name));

                _vm.TotalFinancialTransactions = financialTransactions.Count;
                _vm.TotalDocuments = rows.Count;
                _vm.TotalIn = rows.Where(IsIncoming).Sum(x => x.Amount);
                _vm.TotalOut = rows.Where(x => !IsIncoming(x)).Sum(x => x.Amount);

                _allTransactionRows = rows
                    .OrderByDescending(x => x.Date)
                    .ThenByDescending(x => x.SourceKind)
                    .ThenByDescending(x => x.DocumentNumber)
                    .ToList();
                ApplyInvoiceSearch();

                if (selectedSession != null)
                {
                    SessionsGrid.SelectedItem = cashierSessions.FirstOrDefault(x => x.Id == selectedSession.Id);
                }

                if (_vm.TotalDocuments == 0)
                {
                    MessageBox.Show(
                        UiText.T("لا توجد حركات ضمن النطاق المحدد.", "There are no transactions for the selected scope."),
                        UiText.T("تنبيه", "Notice"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("تعذر تحميل لوحة الكاشيرات", "Failed to load the cashiers dashboard")}: {ex.Message}",
                    UiText.T("خطأ", "Error"));
            }
            finally
            {
                _isLoading = false;
            }
        }

        private static bool MatchesCashierScope(
            int? rowSessionId,
            int? rowCashierId,
            int cashierId,
            HashSet<int> sessionIds,
            bool isSpecificSession)
        {
            if (isSpecificSession)
                return rowSessionId.HasValue
                    ? rowSessionId.Value == sessionIds.First()
                    : rowCashierId == cashierId;

            return rowSessionId.HasValue
                ? sessionIds.Contains(rowSessionId.Value)
                : rowCashierId == cashierId;
        }

        private void BindSessionItems(List<CashierSessionReadDto> sessions)
        {
            _vm.Sessions.Clear();
            foreach (var session in sessions)
                _vm.Sessions.Add(session);
        }

        private async void SessionsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _isLoading || !IsLoaded)
                return;

            if (SessionsGrid.SelectedItem is not CashierSessionReadDto session)
                return;

            _selectedSessionIdOverride = session.Id;
            _isInitializing = true;
            try
            {
                if (SessionComboBox.ItemsSource is IEnumerable<SessionOption> options)
                {
                    SessionComboBox.SelectedItem = options.FirstOrDefault(x => x.SessionId == session.Id) ?? options.FirstOrDefault(x => x.IsAll);
                }
            }
            finally
            {
                _isInitializing = false;
            }

            await LoadReportAsync();
        }

        private static SessionTransactionRowDto BuildInvoiceRow(InvoiceReadDto invoice, string cashierName)
        {
            var isReturn = invoice.InvoiceType == InvoiceType.Return;
            var referenceText = !string.IsNullOrWhiteSpace(invoice.OriginalInvoiceId)
                ? invoice.OriginalInvoiceId
                : invoice.Voucher?.VoucherNumber ?? invoice.InvoiceNumber;

            return new SessionTransactionRowDto
            {
                Date = invoice.ClosedAt ?? invoice.CreatedDate,
                DocumentTypeText = invoice.InvoiceType == InvoiceType.Sale ? "فاتورة بيع" : invoice.InvoiceType.ToString(),
                DocumentNumber = invoice.InvoiceNumber,
                FalconInvoiceNumber = invoice.FalconInvoiceNumber,
                SearchText = string.Join(" ", new[]
                {
                    invoice.InvoiceNumber,
                    invoice.FalconInvoiceNumber,
                    invoice.Customer?.Name
                }.Where(value => !string.IsNullOrWhiteSpace(value))),
                CustomerName = invoice.Customer?.Name,
                ReferenceText = referenceText,
                DirectionText = isReturn ? "صادر" : "وارد",
                MethodText = invoice.PaymentType?.ToString() ?? "—",
                Amount = invoice.TotalAmount,
                CashierName = invoice.User?.Name ?? cashierName,
                Notes = null,
                StatusText = invoice.Status?.ToString() ?? "—",
                ReferenceType = "Invoice",
                ReferenceId = invoice.Id,
                SourceKind = "Invoice"
            };
        }

        private void ApplyInvoiceSearch()
        {
            var search = InvoiceSearchTextBox?.Text?.Trim();
            IEnumerable<SessionTransactionRowDto> filteredRows = _allTransactionRows;

            if (!string.IsNullOrWhiteSpace(search))
            {
                filteredRows = _allTransactionRows.Where(row =>
                    row.IsInvoice &&
                    (row.SearchText?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            _vm.Transactions.Clear();
            foreach (var row in filteredRows)
                _vm.Transactions.Add(row);
        }

        private void InvoiceSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
                ApplyInvoiceSearch();
        }

        private void ClearInvoiceSearch_Click(object sender, RoutedEventArgs e)
        {
            InvoiceSearchTextBox.Clear();
        }

        private void CopyInvoiceNumber_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not SessionTransactionRowDto row || !row.IsInvoice)
                return;

            if (!string.IsNullOrWhiteSpace(row.DocumentNumber))
                Clipboard.SetText(row.DocumentNumber);
        }

        private void CopyFalconInvoiceNumber_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not SessionTransactionRowDto row ||
                !row.IsInvoice ||
                string.IsNullOrWhiteSpace(row.FalconInvoiceNumber))
            {
                return;
            }

            Clipboard.SetText(row.FalconInvoiceNumber);
        }

        private async void PrintInvoice_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not SessionTransactionRowDto row ||
                !row.IsInvoice ||
                !row.ReferenceId.HasValue)
            {
                return;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
                var invoice = await invoiceService.GetFullInvoiceByIdAsync(row.ReferenceId.Value);
                if (invoice == null)
                {
                    MessageBox.Show(
                        UiText.T("تعذر تحميل الفاتورة للطباعة.", "The invoice could not be loaded for printing."),
                        UiText.T("تنبيه", "Notice"));
                    return;
                }

                ReportPrintService.PrintSmallInvoice(invoice, this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("تعذر طباعة الفاتورة", "Could not print the invoice")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static SessionTransactionRowDto BuildVoucherRow(VoucherReadDto voucher, string cashierName)
        {
            var isIncoming = voucher.VoucherType is VoucherType.Sales
                or VoucherType.Receipt
                or VoucherType.ReturnPurchase
                or VoucherType.Adjustment;

            return new SessionTransactionRowDto
            {
                Date = voucher.CreatedDate,
                DocumentTypeText = voucher.VoucherType.ToString(),
                DocumentNumber = voucher.VoucherNumber ?? voucher.Id.ToString(),
                ReferenceText = voucher.ReferenceNumber
                                 ?? voucher.Checks?.FirstOrDefault()?.CheckNumber
                                 ?? voucher.VoucherNumber
                                 ?? voucher.Id.ToString(),
                DirectionText = isIncoming ? "وارد" : "صادر",
                MethodText = voucher.PaymentType.ToString(),
                Amount = voucher.Amount,
                CashierName = voucher.CashierSession?.CashierName ?? cashierName,
                Notes = voucher.Notes,
                StatusText = voucher.PostingStatus.ToString(),
                ReferenceType = "Voucher",
                ReferenceId = voucher.Id,
                SourceKind = "Voucher"
            };
        }

        private static SessionTransactionRowDto BuildFinancialRow(RaccoonWarehouse.Domain.FinancialTransactions.DTOs.FinancialTransactionReadDto tx, string cashierName)
        {
            var referenceType = ResolveFinancialReferenceType(tx.SourceType);
            var referenceText = tx.SourceId.HasValue
                ? $"{tx.SourceType} #{tx.SourceId.Value}"
                : tx.TransactionNumber;

            return new SessionTransactionRowDto
            {
                Date = tx.TransactionDate,
                DocumentTypeText = "حركة مالية",
                DocumentNumber = tx.TransactionNumber,
                ReferenceText = referenceText,
                DirectionText = tx.Direction == TransactionDirection.In ? "وارد" : "صادر",
                MethodText = tx.Method.ToString(),
                Amount = tx.Amount,
                CashierName = tx.Cashier?.Name ?? tx.CashierSession?.CashierName ?? cashierName,
                Notes = tx.Notes,
                StatusText = tx.Status.ToString(),
                ReferenceType = referenceType,
                ReferenceId = tx.SourceId,
                SourceKind = "FinancialTransaction"
            };
        }

        private static bool IsIncoming(SessionTransactionRowDto row)
            => string.Equals(row.DirectionText, "وارد", StringComparison.OrdinalIgnoreCase);

        private static bool IsInvoicePostingFinancialSource(FinancialSourceType sourceType)
            => sourceType is FinancialSourceType.PosSaleInvoice
                or FinancialSourceType.SaleInvoice
                or FinancialSourceType.SaleReturn
                or FinancialSourceType.PurchaseInvoice
                or FinancialSourceType.PurchaseReturn;

        private static string? ResolveFinancialReferenceType(FinancialSourceType sourceType)
        {
            return sourceType switch
            {
                FinancialSourceType.PosSaleInvoice
                    or FinancialSourceType.SaleInvoice
                    or FinancialSourceType.SaleReturn
                    or FinancialSourceType.PurchaseInvoice
                    or FinancialSourceType.PurchaseReturn => "Invoice",
                FinancialSourceType.ReceiptVoucher
                    or FinancialSourceType.PaymentVoucher => "Voucher",
                _ => null
            };
        }

        private async void TransactionsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TransactionsGrid.SelectedItem is not SessionTransactionRowDto row)
                return;

            if (!row.IsOpenable)
            {
                MessageBox.Show(
                    UiText.T("لا يوجد مستند مرتبط بهذه الحركة.", "There is no linked document for this transaction."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                var navigator = new SourceDocumentNavigationService(_serviceProvider);
                await navigator.OpenSourceDocument(row.ReferenceType, row.ReferenceId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("تعذر فتح المستند المرتبط", "Could not open the linked document")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private sealed class SessionOption
        {
            public int? SessionId { get; private set; }
            public string DisplayText { get; private set; } = string.Empty;
            public CashierSessionReadDto? Session { get; private set; }

            public bool IsAll => SessionId == null;

            public static SessionOption Create(CashierSessionReadDto session)
            {
                return new SessionOption
                {
                    SessionId = session.Id,
                    Session = session,
                    DisplayText = $"{session.Id} - {session.OpenedAt:dd-MM-yyyy HH:mm} - {session.Status}"
                };
            }

            public static SessionOption CreateAll()
            {
                return new SessionOption
                {
                    SessionId = null,
                    Session = null,
                    DisplayText = UiText.T("كل الورديات", "All shifts")
                };
            }
        }
    }
}
