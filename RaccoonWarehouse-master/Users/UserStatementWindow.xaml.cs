using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Reports.Accounting.Dtos;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;

namespace RaccoonWarehouse
{
    public partial class UserStatementWindow : Window
    {
        private readonly UserStatementService _statementService;
        private readonly ILoadingService _loadingService;
        private readonly SourceDocumentNavigationService _sourceDocumentNavigationService;
        private int _userId;
        private UserRole? _statementRole;
        private bool _includeAllPartyRelationships;
        private bool _isInitialized;
        private bool _isWindowLoaded;
        private ICollectionView? _statementRowsView;
        private IReadOnlyList<UserStatementRowDto> _statementRows = Array.Empty<UserStatementRowDto>();

        public UserStatementWindow(
            UserStatementService statementService,
            ILoadingService loadingService,
            SourceDocumentNavigationService sourceDocumentNavigationService)
        {
            _statementService = statementService;
            _loadingService = loadingService;
            _sourceDocumentNavigationService = sourceDocumentNavigationService;
            InitializeComponent();
            UiText.ApplyWindow(this);
            OpenReferenceColumn.Header = UiText.T("فتح", "Open");
            Loaded += UserStatementWindow_Loaded;
        }

        public void Initialize(int userId)
        {
            _userId = userId;
            _includeAllPartyRelationships = true;
            _isInitialized = true;
        }

        public void Initialize(int userId, UserRole role)
        {
            _userId = userId;
            _statementRole = role;
            _includeAllPartyRelationships = true;
            _isInitialized = true;
        }

        public void InitializeCombined(int userId)
        {
            _userId = userId;
            _statementRole = null;
            _includeAllPartyRelationships = true;
            _isInitialized = true;
        }

        private void UserStatementWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _isWindowLoaded = true;
            FromDatePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ToDatePicker.SelectedDate = DateTime.Today;

            if (_isInitialized)
                _ = GenerateAsync();
        }

        private async void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            await GenerateAsync();
        }

        private async System.Threading.Tasks.Task GenerateAsync()
        {
            try
            {
                if (_userId <= 0)
                {
                    MessageBox.Show(UiText.T("لم يتم تحديد المستخدم.", "No user was selected."));
                    return;
                }

                if (FromDatePicker.SelectedDate == null || ToDatePicker.SelectedDate == null)
                {
                    MessageBox.Show(UiText.T("يرجى اختيار الفترة.", "Please choose the period."));
                    return;
                }

                _loadingService.Show();
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                var result = await _statementService.GetAsync(new UserStatementFilterDto
                {
                    UserId = _userId,
                    Role = _statementRole,
                    IncludeAllPartyRelationships = _includeAllPartyRelationships,
                    From = FromDatePicker.SelectedDate.Value.Date,
                    To = ToDatePicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1)
                });

                if (!result.Success || result.Data == null)
                {
                    MessageBox.Show(result.Message ?? UiText.T("تعذر تحميل كشف الحساب.", "Failed to load the statement."));
                    return;
                }

                var report = result.Data;
                _statementRows = report.Rows ?? new List<UserStatementRowDto>();
                _statementRowsView = CollectionViewSource.GetDefaultView(_statementRows);
                _statementRowsView.Filter = FilterStatementRows;
                StatementGrid.ItemsSource = _statementRowsView;
                HeaderText.Text = _includeAllPartyRelationships
                    ? $"{report.UserName} ({UiText.T("جميع الحركات", "All transactions")})"
                    : $"{report.UserName} ({report.Role})";
                UserSummaryText.Text = report.UserName;
                OpeningText.Text = report.OpeningBalance.ToString("N2");
                DebitText.Text = report.TotalDebit.ToString("N2");
                CreditText.Text = report.TotalCredit.ToString("N2");
                ClosingText.Text = report.ClosingBalance.ToString("N2");
                TransactionTotalText.Text = report.TotalTransactionAmount.ToString("N2");
                PurchaseOrderTotalText.Text = report.TotalPurchaseOrderAmount.ToString("N2");
                UpdateTableTotal();
                StatementGrid.UpdateLayout();
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{UiText.T("حدث خطأ أثناء تحميل كشف الحساب", "An error occurred while loading the statement")}: {ex.Message}",
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _loadingService.Hide();
            }
        }

        private void ReferenceFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isWindowLoaded)
                return;

            _statementRowsView?.Refresh();
            UpdateTableTotal();
        }

        private bool FilterStatementRows(object item)
        {
            if (item is not UserStatementRowDto row)
                return false;

            var filter = ReferenceFilterComboBox.SelectedValue?.ToString();
            if (string.IsNullOrWhiteSpace(filter) || string.Equals(filter, "all", StringComparison.OrdinalIgnoreCase))
                return true;

            var referenceType = row.ReferenceType ?? string.Empty;
            var documentType = row.DocumentType ?? string.Empty;

            return filter switch
            {
                "SaleInvoice" => referenceType.Equals("Invoice", StringComparison.OrdinalIgnoreCase)
                    && documentType.Equals("Sale", StringComparison.OrdinalIgnoreCase),
                "PurchaseInvoice" => referenceType.Equals("Invoice", StringComparison.OrdinalIgnoreCase)
                    && documentType.Equals("Purchase", StringComparison.OrdinalIgnoreCase),
                "Return" => referenceType.Equals("Invoice", StringComparison.OrdinalIgnoreCase)
                    && (documentType.Contains("Return", StringComparison.OrdinalIgnoreCase)
                        || documentType.Equals("Exchange", StringComparison.OrdinalIgnoreCase)),
                "PurchaseOrder" => referenceType.Equals("PurchaseOrder", StringComparison.OrdinalIgnoreCase),
                "Voucher" => referenceType.Equals("Voucher", StringComparison.OrdinalIgnoreCase),
                "JournalEntry" => string.IsNullOrWhiteSpace(referenceType),
                "ReceiptPayment" => referenceType.Contains("Receipt", StringComparison.OrdinalIgnoreCase)
                    || referenceType.Contains("Payment", StringComparison.OrdinalIgnoreCase)
                    || documentType.Contains("Receipt", StringComparison.OrdinalIgnoreCase)
                    || documentType.Contains("Payment", StringComparison.OrdinalIgnoreCase),
                "Other" => !referenceType.Equals("Invoice", StringComparison.OrdinalIgnoreCase)
                    && !referenceType.Equals("PurchaseOrder", StringComparison.OrdinalIgnoreCase)
                    && !referenceType.Equals("Voucher", StringComparison.OrdinalIgnoreCase),
                _ => true
            };
        }

        private void UpdateTableTotal()
        {
            if (TableTotalText == null)
                return;

            var visibleRows = _statementRowsView?.Cast<UserStatementRowDto>() ?? Enumerable.Empty<UserStatementRowDto>();
            var total = visibleRows
                .Where(row => row.DocumentAmount.HasValue && row.ReferenceId.HasValue)
                .GroupBy(row => $"{row.ReferenceType}:{row.ReferenceId}", StringComparer.OrdinalIgnoreCase)
                .Sum(group => group.Max(row => Math.Abs(row.DocumentAmount!.Value)));

            TableTotalText.Text = total.ToString("N2");
        }

        private List<UserStatementRowDto> GetVisibleRows()
        {
            return _statementRowsView?.Cast<UserStatementRowDto>().ToList()
                ?? new List<UserStatementRowDto>();
        }

        private void ExportExcelBtn_Click(object sender, RoutedEventArgs e)
        {
            var rows = GetVisibleRows();
            if (rows.Count == 0)
            {
                MessageBox.Show(UiText.T("لا توجد حركات لتصديرها.", "There are no transactions to export."));
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Excel File (*.xlsx)|*.xlsx",
                FileName = $"UserStatement_{_userId}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                using var workbook = new XLWorkbook();
                var sheet = workbook.Worksheets.Add("User Statement");
                var headers = new[]
                {
                    "Date", "Entry number", "Description", "Reference", "Reference type",
                    "Reference ID", "Document amount", "Payment method", "Document type", "Debit", "Credit", "Running balance"
                };

                for (var i = 0; i < headers.Length; i++)
                    sheet.Cell(1, i + 1).Value = headers[i];

                for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                {
                    var row = rows[rowIndex];
                    var excelRow = rowIndex + 2;
                    sheet.Cell(excelRow, 1).Value = row.EntryDate;
                    sheet.Cell(excelRow, 2).Value = row.EntryNumber;
                    sheet.Cell(excelRow, 3).Value = row.Description;
                    sheet.Cell(excelRow, 4).Value = row.Reference;
                    sheet.Cell(excelRow, 5).Value = row.ReferenceType ?? string.Empty;
                    sheet.Cell(excelRow, 6).Value = row.ReferenceId ?? 0;
                    sheet.Cell(excelRow, 7).Value = row.DocumentAmount ?? 0m;
                    sheet.Cell(excelRow, 8).Value = row.PaymentMethod ?? string.Empty;
                    sheet.Cell(excelRow, 9).Value = row.DocumentType ?? string.Empty;
                    sheet.Cell(excelRow, 10).Value = row.Debit;
                    sheet.Cell(excelRow, 11).Value = row.Credit;
                    sheet.Cell(excelRow, 12).Value = row.RunningBalance;
                }

                var usedRange = sheet.Range(1, 1, rows.Count + 1, headers.Length);
                usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                usedRange.Style.Alignment.WrapText = true;
                sheet.Row(1).Style.Font.Bold = true;
                sheet.Row(1).Style.Fill.BackgroundColor = XLColor.LightGray;
                sheet.Column(1).Style.DateFormat.Format = "yyyy-MM-dd";
                sheet.Columns(7, 7).Style.NumberFormat.Format = "#,##0.00";
                sheet.Columns(10, 12).Style.NumberFormat.Format = "#,##0.00";
                sheet.Columns().AdjustToContents();
                workbook.SaveAs(dialog.FileName);

                MessageBox.Show(UiText.T("تم تصدير كشف الحساب بنجاح.", "The account statement was exported successfully."));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذر تصدير كشف الحساب", "Could not export the account statement")}: {ex.Message}",
                    UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintBtn_Click(object sender, RoutedEventArgs e)
        {
            if (GetVisibleRows().Count == 0)
            {
                MessageBox.Show(UiText.T("لا توجد حركات للطباعة.", "There are no transactions to print."));
                return;
            }

            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() != true)
                    return;

                StatementGrid.UpdateLayout();
                printDialog.PrintVisual(StatementGrid, $"User Statement - {UserSummaryText.Text}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{UiText.T("تعذرت طباعة كشف الحساب", "Could not print the account statement")}: {ex.Message}",
                    UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void StatementGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (StatementGrid.SelectedItem is not UserStatementRowDto row)
                return;

            await OpenRowReferenceAsync(row);
        }

        private async void OpenReference_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not UserStatementRowDto row)
                return;

            await OpenRowReferenceAsync(row);
        }

        private async System.Threading.Tasks.Task OpenRowReferenceAsync(UserStatementRowDto row)
        {
            if (string.IsNullOrWhiteSpace(row.ReferenceType) || !row.ReferenceId.HasValue)
            {
                MessageBox.Show(
                    UiText.T("لا يوجد مرجع مرتبط بهذه الحركة.", "No source document is linked to this row."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                await _sourceDocumentNavigationService.OpenSourceDocument(row.ReferenceType, row.ReferenceId.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
