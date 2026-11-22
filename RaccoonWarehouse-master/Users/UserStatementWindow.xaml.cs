using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Reports.Accounting.Dtos;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Helpers.Pdf;
using RaccoonWarehouse.Navigation;
using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.IO;
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
        private readonly IUserStatementService _statementService;
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
            IUserStatementService statementService,
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
                var branding = DocumentPrintRuntime.Branding();
                using var workbook = new XLWorkbook();
                var isSupplier = _statementRole == UserRole.Supplier;
                var partyLabel = isSupplier
                    ? UiText.T("المورد", "Supplier")
                    : _statementRole == UserRole.Customer
                        ? UiText.T("العميل", "Customer")
                        : UiText.T("الحساب", "Account");
                var sheet = workbook.Worksheets.Add(UiText.IsEnglish ? $"{partyLabel} Statement" : $"كشف {partyLabel}");
                sheet.RightToLeft = !UiText.IsEnglish;
                const int columnCount = 12;
                var reportTitle = UiText.IsEnglish ? $"{partyLabel} statement" : $"كشف حساب {partyLabel}";
                sheet.Range(1, 1, 1, columnCount).Merge();
                sheet.Cell(1, 1).Value = reportTitle;
                sheet.Range(2, 1, 2, columnCount).Merge();
                sheet.Cell(2, 1).Value = branding.DisplayName;
                sheet.Range(3, 1, 3, columnCount).Merge();
                sheet.Cell(3, 1).Value = string.Join(" | ", new[] { branding.Address, branding.Phone }.Where(x => !string.IsNullOrWhiteSpace(x)));
                sheet.Range(4, 1, 4, columnCount).Merge();
                sheet.Cell(4, 1).Value = string.IsNullOrWhiteSpace(branding.TaxNumber)
                    ? string.Empty
                    : UiText.T("الرقم الضريبي: ", "Tax number: ") + branding.TaxNumber;
                sheet.Cell(5, 1).Value = partyLabel;
                sheet.Cell(5, 2).Value = UserSummaryText.Text;
                sheet.Cell(6, 1).Value = UiText.T("من تاريخ", "From date");
                sheet.Cell(6, 2).Value = FromDatePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? string.Empty;
                sheet.Cell(6, 3).Value = UiText.T("إلى تاريخ", "To date");
                sheet.Cell(6, 4).Value = ToDatePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? string.Empty;
                sheet.Cell(7, 1).Value = UiText.T("الرصيد الختامي", "Closing balance");
                sheet.Cell(7, 2).Value = decimal.TryParse(ClosingText.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var closing) ? closing : 0m;
                sheet.Cell(7, 3).Value = UiText.T("إجمالي المدين", "Total debit");
                sheet.Cell(7, 4).Value = decimal.TryParse(DebitText.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var debit) ? debit : 0m;
                sheet.Cell(7, 5).Value = UiText.T("إجمالي الدائن", "Total credit");
                sheet.Cell(7, 6).Value = decimal.TryParse(CreditText.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var credit) ? credit : 0m;
                var headers = new[]
                {
                    UiText.T("التاريخ", "Date"), UiText.T("رقم القيد", "Entry number"), UiText.T("البيان", "Description"),
                    UiText.T("المرجع", "Reference"), UiText.T("نوع المرجع", "Reference type"), UiText.T("رقم المرجع", "Reference ID"),
                    UiText.T("المبلغ", "Document amount"), UiText.T("طريقة الدفع", "Payment method"), UiText.T("نوع المستند", "Document type"),
                    UiText.T("مدين", "Debit"), UiText.T("دائن", "Credit"), UiText.T("الرصيد الجاري", "Running balance")
                };

                const int headerRow = 10;
                for (var i = 0; i < headers.Length; i++)
                    sheet.Cell(headerRow, i + 1).Value = headers[i];

                for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                {
                    var row = rows[rowIndex];
                    var excelRow = headerRow + rowIndex + 1;
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

                var usedRange = sheet.Range(headerRow, 1, headerRow + rows.Count, headers.Length);
                usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                usedRange.Style.Alignment.WrapText = true;
                sheet.Row(headerRow).Style.Font.Bold = true;
                sheet.Row(headerRow).Style.Fill.BackgroundColor = XLColor.FromHtml("#0F766E");
                sheet.Row(headerRow).Style.Font.FontColor = XLColor.White;
                sheet.Row(headerRow).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                sheet.Column(1).Style.DateFormat.Format = "yyyy-MM-dd";
                sheet.Columns(7, 7).Style.NumberFormat.Format = "#,##0.00";
                sheet.Columns(10, 12).Style.NumberFormat.Format = "#,##0.00";
                sheet.Columns().AdjustToContents();
                sheet.Range(headerRow, 1, headerRow + rows.Count, headers.Length).SetAutoFilter();
                sheet.SheetView.FreezeRows(headerRow);
                sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
                sheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
                sheet.PageSetup.FitToPages(1, 0);
                sheet.PageSetup.Margins.Left = 0.25;
                sheet.PageSetup.Margins.Right = 0.25;
                sheet.PageSetup.Margins.Top = 0.5;
                sheet.PageSetup.Margins.Bottom = 0.5;
                sheet.PageSetup.SetRowsToRepeatAtTop(headerRow, headerRow);
                sheet.PageSetup.PrintAreas.Add(sheet.Range(1, 1, headerRow + rows.Count, headers.Length).RangeAddress.ToString());
                sheet.PageSetup.Header.Center.AddText(reportTitle);
                sheet.PageSetup.Footer.Center.AddText(branding.Footer ?? string.Empty);
                sheet.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
                sheet.Range(1, 1, 1, headers.Length).Style.Font.FontSize = 16;
                sheet.Range(1, 1, 1, headers.Length).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                sheet.Range(2, 1, 7, headers.Length).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                if (!string.IsNullOrWhiteSpace(branding.LogoBase64))
                {
                    try
                    {
                        using var logoStream = new MemoryStream(Convert.FromBase64String(branding.LogoBase64));
                        sheet.AddPicture(logoStream).MoveTo(sheet.Cell(1, 1)).WithSize(90, 45);
                    }
                    catch (FormatException)
                    {
                        // Ignore an invalid legacy logo and still export the report text.
                    }
                }
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
