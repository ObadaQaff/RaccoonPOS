using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Checks;
using RaccoonWarehouse.Domain.Checks.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.JournalEntries.DTOs;
using RaccoonWarehouse.Domain.Settings;
using RaccoonWarehouse.Domain.Vouchers;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using RaccoonWarehouse.Invoices;
using RaccoonWarehouse.Common.Loading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace RaccoonWarehouse
{
    public partial class ChecksDashboard : Window
    {
        private sealed class FilterOption
        {
            public string Key { get; init; } = string.Empty;
            public string Display { get; init; } = string.Empty;
        }

        private sealed class PartyFilterOption
        {
            public int? UserId { get; init; }
            public string Display { get; init; } = string.Empty;
        }

        private sealed class CheckDashboardRow
            : INotifyPropertyChanged
        {
            private readonly CheckReadDto _check;

            public CheckDashboardRow(CheckReadDto check, IReadOnlyDictionary<int, string> userNames)
            {
                _check = check;

                if (check.Invoice?.CustomerId is int invoiceCustomerId)
                    SetParty(invoiceCustomerId, "customer", userNames);
                else if (check.Invoice?.SupplierId is int invoiceSupplierId)
                    SetParty(invoiceSupplierId, "supplier", userNames);
                else if (check.Voucher?.CustomerId is int voucherCustomerId)
                    SetParty(voucherCustomerId, "customer", userNames);
                else if (check.Voucher?.SupplierId is int voucherSupplierId)
                    SetParty(voucherSupplierId, "supplier", userNames);
            }

            public int? PartyUserId { get; private set; }
            public string PartyName { get; private set; } = "-";
            public string PartyTypeKey { get; private set; } = "none";
            public string PartyTypeText => PartyTypeKey switch
            {
                "customer" => UiText.T("عميل", "Customer"),
                "supplier" => UiText.T("مورد", "Supplier"),
                _ => UiText.T("غير مرتبط", "Unlinked")
            };

            public int Id => _check.Id;
            public string CheckNumber => _check.CheckNumber ?? string.Empty;
            public string BankName => _check.BankName ?? string.Empty;
            public DateTime DueDate => _check.DueDate;
            public decimal Amount => _check.Amount;
            public string Notes => _check.Notes ?? string.Empty;
            public DateTime CreatedDate => _check.CreatedDate;

            public string SourceTypeKey => _check.InvoiceId.HasValue ? "invoice" : _check.VoucherId.HasValue ? "voucher" : "none";
            public string SourceTypeText => SourceTypeKey switch
            {
                "invoice" => UiText.T("فاتورة", "Invoice"),
                "voucher" => UiText.T("سند", "Voucher"),
                _ => UiText.T("بدون مصدر", "No source")
            };

            public string SourceNumber => _check.Invoice?.InvoiceNumber
                ?? _check.Voucher?.VoucherNumber
                ?? "-";

            public string SourceReferenceType => _check.InvoiceId.HasValue ? "Invoice"
                : _check.VoucherId.HasValue ? "Voucher"
                : string.Empty;

            public int? SourceReferenceId => _check.InvoiceId ?? _check.VoucherId;
            public bool CanOpenSource => SourceReferenceId.HasValue && !string.IsNullOrWhiteSpace(SourceReferenceType);

            public event PropertyChangedEventHandler? PropertyChanged;

            public CheckStatus Status
            {
                get => Enum.IsDefined(typeof(CheckStatus), _check.Status) ? _check.Status : CheckStatus.Pending;
                set
                {
                    if (_check.Status == value)
                        return;

                    _check.Status = value;
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(StatusKey));
                    OnPropertyChanged(nameof(StatusText));
                }
            }

            public string StatusKey => Status.ToString().ToLowerInvariant();

            public string StatusText => StatusKey switch
            {
                "pending" => UiText.T("معلق", "Pending"),
                "deposited" => UiText.T("مودع", "Deposited"),
                "cleared" => UiText.T("مصفاة", "Cleared"),
                "bounced" => UiText.T("راجع", "Bounced"),
                "cancelled" => UiText.T("ملغى", "Cancelled"),
                _ => UiText.T("معلق", "Pending")
            };

            public bool MatchesSearch(string search)
            {
                if (string.IsNullOrWhiteSpace(search))
                    return true;

                return (CheckNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (BankName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (Notes?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (SourceNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                    || PartyName.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || PartyTypeText.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || StatusKey.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || StatusText.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || Amount.ToString("N2").Contains(search, StringComparison.OrdinalIgnoreCase)
                    || Id.ToString().Contains(search, StringComparison.OrdinalIgnoreCase);
            }

            private void SetParty(int userId, string typeKey, IReadOnlyDictionary<int, string> userNames)
            {
                PartyUserId = userId;
                PartyTypeKey = typeKey;
                PartyName = userNames.TryGetValue(userId, out var name) && !string.IsNullOrWhiteSpace(name)
                    ? name
                    : $"#{userId}";
            }

            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private readonly ICheckService _checkService;
        private readonly IAccountingService _accountingService;
        private readonly IUOW _uow;
        private readonly ApplicationDbContext _db;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SourceDocumentNavigationService _sourceDocumentNavigationService;
        private readonly ILoadingService _loadingService;
        private readonly List<CheckDashboardRow> _checks = new();
        private ICollectionView? _checksView;
        private bool _isLoaded;
        private bool _isRefreshingPartyFilter;
        private string _partyNameSearch = string.Empty;

        public ChecksDashboard(
            ICheckService checkService,
            IAccountingService accountingService,
            IUOW uow,
            ApplicationDbContext db,
            IServiceScopeFactory scopeFactory,
            SourceDocumentNavigationService sourceDocumentNavigationService,
            ILoadingService loadingService)
        {
            _checkService = checkService;
            _accountingService = accountingService;
            _uow = uow;
            _db = db;
            _scopeFactory = scopeFactory;
            _sourceDocumentNavigationService = sourceDocumentNavigationService;
            _loadingService = loadingService;

            InitializeComponent();
            PartyFilter.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(PartyFilter_TextChanged));
            UiText.ApplyWindow(this);
            InitializeFilters();
            FeatureStateText.Text = UiText.T(
                "استعرض الشيكات وانتقل إلى الفاتورة أو السند المرتبط بها مباشرة.",
                "Browse checks and jump to the linked invoice or voucher directly.");

            Loaded += async (_, _) =>
            {
                if (_isLoaded)
                    return;

                _isLoaded = true;
                await LoadChecksAsync();
            };
        }

        private void InitializeFilters()
        {
            StatusFilter.ItemsSource = new[]
            {
                new FilterOption { Key = "all", Display = UiText.T("الكل", "All") },
                new FilterOption { Key = "pending", Display = UiText.T("معلق", "Pending") },
                new FilterOption { Key = "deposited", Display = UiText.T("مودع", "Deposited") },
                new FilterOption { Key = "cleared", Display = UiText.T("مصفاة", "Cleared") },
                new FilterOption { Key = "bounced", Display = UiText.T("راجع", "Bounced") },
                new FilterOption { Key = "cancelled", Display = UiText.T("ملغى", "Cancelled") }
            };
            StatusFilter.DisplayMemberPath = nameof(FilterOption.Display);
            StatusFilter.SelectedIndex = 0;

            SourceFilter.ItemsSource = new[]
            {
                new FilterOption { Key = "all", Display = UiText.T("الكل", "All") },
                new FilterOption { Key = "invoice", Display = UiText.T("فواتير", "Invoices") },
                new FilterOption { Key = "voucher", Display = UiText.T("سندات", "Vouchers") },
                new FilterOption { Key = "none", Display = UiText.T("بدون مصدر", "No source") }
            };
            SourceFilter.DisplayMemberPath = nameof(FilterOption.Display);
            SourceFilter.SelectedIndex = 0;

            PartyTypeFilter.ItemsSource = new[]
            {
                new FilterOption { Key = "all", Display = UiText.T("كل الجهات", "All parties") },
                new FilterOption { Key = "customer", Display = UiText.T("العملاء", "Customers") },
                new FilterOption { Key = "supplier", Display = UiText.T("الموردون", "Suppliers") },
                new FilterOption { Key = "none", Display = UiText.T("غير مرتبط", "Unlinked") }
            };
            PartyTypeFilter.DisplayMemberPath = nameof(FilterOption.Display);
            PartyTypeFilter.SelectedIndex = 0;
        }

        private async Task LoadChecksAsync()
        {
            var result = await _checkService.GetAllWithIncludeAsync(x => x.Invoice, x => x.Voucher);
            if (!result.Success || result.Data == null)
            {
                MessageBox.Show(
                    result.Message ?? UiText.T("تعذر تحميل الشيكات.", "Could not load checks."),
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var userNames = await _db.Set<User>()
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            _checks.Clear();
            _checks.AddRange(result.Data
                .OrderByDescending(x => x.DueDate)
                .ThenByDescending(x => x.Id)
                .Select(x => new CheckDashboardRow(x, userNames)));

            RefreshPartyFilterOptions();

            _checksView = CollectionViewSource.GetDefaultView(_checks);
            _checksView.Filter = FilterChecks;
            ChecksGrid.ItemsSource = _checksView;

            ApplyTranslations();
            UpdateCounters();
        }

        private void ApplyTranslations()
        {
            UiText.ApplyTranslations(this);
            PartyTypeColumn.Header = UiText.T("نوع الجهة", "Party type");
            PartyNameColumn.Header = UiText.T("المستخدم", "User");
        }

        private bool FilterChecks(object item)
        {
            if (item is not CheckDashboardRow row)
                return false;

            var search = SearchBox.Text?.Trim() ?? string.Empty;
            if (!row.MatchesSearch(search))
                return false;

            if (StatusFilter.SelectedItem is FilterOption statusFilter && statusFilter.Key != "all")
            {
                if (!string.Equals(row.StatusKey, statusFilter.Key, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (SourceFilter.SelectedItem is FilterOption sourceFilter && sourceFilter.Key != "all")
            {
                if (!string.Equals(row.SourceTypeKey, sourceFilter.Key, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (PartyTypeFilter.SelectedItem is FilterOption partyTypeFilter && partyTypeFilter.Key != "all")
            {
                if (!string.Equals(row.PartyTypeKey, partyTypeFilter.Key, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (PartyFilter.SelectedItem is PartyFilterOption partyFilter && partyFilter.UserId.HasValue &&
                string.IsNullOrWhiteSpace(_partyNameSearch))
            {
                if (row.PartyUserId != partyFilter.UserId.Value)
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(_partyNameSearch) &&
                !row.PartyName.Contains(_partyNameSearch, StringComparison.OrdinalIgnoreCase))
                return false;

            if (DueFromPicker.SelectedDate.HasValue && row.DueDate.Date < DueFromPicker.SelectedDate.Value.Date)
                return false;

            if (DueToPicker.SelectedDate.HasValue && row.DueDate.Date > DueToPicker.SelectedDate.Value.Date)
                return false;

            return true;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterChanged(sender, e);
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            _checksView?.Refresh();
            UpdateCounters();
        }

        private void PartyTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshPartyFilterOptions();
            FilterChanged(sender, e);
        }

        private void PartyFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isRefreshingPartyFilter || !PartyFilter.IsEditable)
                return;

            var text = PartyFilter.Text?.Trim() ?? string.Empty;
            if (PartyFilter.SelectedItem is PartyFilterOption selected && !selected.UserId.HasValue &&
                string.Equals(text, selected.Display, StringComparison.Ordinal))
                text = string.Empty;

            _partyNameSearch = text;
            _checksView?.Refresh();
            UpdateCounters();

            if (PartyFilter.IsKeyboardFocusWithin && !string.IsNullOrWhiteSpace(text))
                PartyFilter.IsDropDownOpen = true;
        }

        private void RefreshPartyFilterOptions()
        {
            if (PartyFilter == null)
                return;

            _isRefreshingPartyFilter = true;

            try
            {
                var selectedType = (PartyTypeFilter?.SelectedItem as FilterOption)?.Key ?? "all";
                var options = new List<PartyFilterOption>
                {
                    new() { UserId = null, Display = UiText.T("كل المستخدمين", "All users") }
                };
                options.AddRange(_checks
                    .Where(x => x.PartyUserId.HasValue && (selectedType == "all" || x.PartyTypeKey == selectedType))
                    .GroupBy(x => new { x.PartyUserId, x.PartyName })
                    .OrderBy(x => x.Key.PartyName)
                    .Select(x => new PartyFilterOption { UserId = x.Key.PartyUserId, Display = x.Key.PartyName }));

                PartyFilter.ItemsSource = options;
                PartyFilter.DisplayMemberPath = nameof(PartyFilterOption.Display);
                PartyFilter.SelectedIndex = 0;
                _partyNameSearch = string.Empty;
            }
            finally
            {
                _isRefreshingPartyFilter = false;
            }
        }

        private void UpdateCounters()
        {
            TotalChecksText.Text = _checks.Count.ToString();
            VisibleChecksText.Text = _checksView?.Cast<object>().Count().ToString() ?? "0";
            DueChecksText.Text = _checksView?.Cast<CheckDashboardRow>()
                .Count(x => x.Status is CheckStatus.Pending or CheckStatus.Deposited)
                .ToString() ?? "0";
        }

        private static string GetStatusDisplayName(CheckStatus status) => status switch
        {
            CheckStatus.Pending => UiText.T("معلق", "Pending"),
            CheckStatus.Deposited => UiText.T("مودع", "Deposited"),
            CheckStatus.Cleared => UiText.T("مصفاة", "Cleared"),
            CheckStatus.Bounced => UiText.T("راجع", "Bounced"),
            CheckStatus.Cancelled => UiText.T("ملغى", "Cancelled"),
            _ => UiText.T("معلق", "Pending")
        };

        private static async Task<int> ResolveSystemAccountIdAsync(
            IUOW uow,
            ApplicationDbContext db,
            string settingKey,
            string fallbackCode)
        {
            var code = await db.AppSettings
                .AsNoTracking()
                .Where(x => x.Key == settingKey)
                .Select(x => x.Value)
                .FirstOrDefaultAsync();

            code = string.IsNullOrWhiteSpace(code) ? fallbackCode : code.Trim();

            var account = await uow.Accounts.GetByCodeAsync(code, activeOnly: false)
                ?? await uow.Accounts.GetByCodeAsync(fallbackCode, activeOnly: false);

            if (account == null)
            {
                throw new InvalidOperationException($"System account '{settingKey}' was not found.");
            }

            return account.Id;
        }

        private static async Task<int> ResolveCheckCounterAccountIdAsync(
            IUOW uow,
            ApplicationDbContext db,
            CheckReadDto check)
        {
            if (check.Invoice?.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn ||
                check.Voucher?.VoucherType == VoucherType.Payment)
            {
                return await ResolveSystemAccountIdAsync(uow, db,
                    AccountingService.AccountsPayableAccountCodeKey,
                    "2110000000");
            }

            if (check.InvoiceId.HasValue || check.Voucher?.VoucherType == VoucherType.Receipt)
            {
                return await ResolveSystemAccountIdAsync(uow, db,
                    AccountingService.AccountsReceivableAccountCodeKey,
                    "1140000000");
            }

            return await ResolveSystemAccountIdAsync(uow, db,
                AccountingService.OtherReceivablesAccountCodeKey,
                "1170000000");
        }

        private static async Task<bool> PostCheckJournalAsync(
            IAccountingService accountingService,
            CheckReadDto check,
            int debitAccountId,
            int creditAccountId,
            string description,
            CheckStatus targetStatus,
            int? customerId = null,
            int? supplierId = null,
            bool tagCounterpartyOnCredit = false)
        {
            var result = await accountingService.PostJournalEntryAsync(new JournalEntryWriteDto
            {
                EntryDate = DateTime.Now,
                Description = description,
                ReferenceType = $"Check.{targetStatus}",
                ReferenceId = check.Id,
                Lines = new List<JournalEntryLineWriteDto>
                {
                    new()
                    {
                        AccountId = debitAccountId,
                        Debit = check.Amount,
                        CustomerId = tagCounterpartyOnCredit ? null : customerId,
                        SupplierId = tagCounterpartyOnCredit ? null : supplierId,
                        Description = description
                    },
                    new()
                    {
                        AccountId = creditAccountId,
                        Credit = check.Amount,
                        CustomerId = tagCounterpartyOnCredit ? customerId : null,
                        SupplierId = tagCounterpartyOnCredit ? supplierId : null,
                        Description = description
                    }
                }
            });

            if (!result.Success)
            {
                MessageBox.Show(
                    result.Message ?? UiText.T("تعذر ترحيل حركة الشيك.", "Could not post the check entry."),
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private async Task ChangeCheckStatusAsync(CheckStatus targetStatus)
        {
            var loadingShown = false;
            try
            {
                if (ChecksGrid.SelectedItem is not CheckDashboardRow selectedRow)
                {
                    MessageBox.Show(
                        UiText.T("يجب تحديد شيك أولاً.", "You must select a check first."),
                        UiText.T("تنبيه", "Notice"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var checkService = scope.ServiceProvider.GetRequiredService<ICheckService>();
                var accountingService = scope.ServiceProvider.GetRequiredService<IAccountingService>();
                var uow = scope.ServiceProvider.GetRequiredService<IUOW>();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var checkResult = await checkService.GetByIdWithIncludeAsync(
                    x => x.Id == selectedRow.Id,
                    x => x.Invoice,
                    x => x.Voucher);

                if (!checkResult.Success || checkResult.Data == null)
                {
                    MessageBox.Show(
                        checkResult.Message ?? UiText.T("تعذر تحميل الشيك.", "Could not load the check."),
                        UiText.T("خطأ", "Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                var check = checkResult.Data;
                var currentStatus = Enum.IsDefined(typeof(CheckStatus), check.Status) ? check.Status : CheckStatus.Pending;
                var isIssuedCheck = check.Invoice?.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn ||
                    check.Voucher?.VoucherType == VoucherType.Payment;
                if (currentStatus == targetStatus)
                {
                    MessageBox.Show(
                        UiText.T("الشيك موجود بالفعل في هذه الحالة.", "This check is already in the requested status."),
                        UiText.T("تنبيه", "Notice"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                if (isIssuedCheck && targetStatus == CheckStatus.Deposited)
                {
                    MessageBox.Show(
                        UiText.T("الشيك الصادر للمورد لا يتم إيداعه؛ استخدم تصفية عند خصمه من البنك.", "An issued supplier check is not deposited; use Cleared when it is charged to the bank."),
                        UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (!isIssuedCheck && targetStatus == CheckStatus.Deposited && currentStatus != CheckStatus.Pending)
                {
                    MessageBox.Show(
                        UiText.T("يمكن إيداع الشيك المعلق فقط.", "Only pending checks can be deposited."),
                        UiText.T("تنبيه", "Notice"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                if ((!isIssuedCheck && targetStatus == CheckStatus.Cleared && currentStatus != CheckStatus.Deposited) ||
                    (isIssuedCheck && targetStatus == CheckStatus.Cleared && currentStatus != CheckStatus.Pending))
                {
                    MessageBox.Show(
                        isIssuedCheck
                            ? UiText.T("يمكن تصفية الشيك الصادر المعلق فقط.", "Only a pending issued check can be cleared.")
                            : UiText.T("يجب إيداع الشيك قبل تصفيته.", "Deposit the check before clearing it."),
                        UiText.T("تنبيه", "Notice"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                if (targetStatus == CheckStatus.Cancelled && currentStatus != CheckStatus.Pending)
                {
                    MessageBox.Show(
                        UiText.T("يمكن إلغاء الشيك المعلق فقط.", "Only pending checks can be cancelled."),
                        UiText.T("تنبيه", "Notice"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                if (isIssuedCheck && targetStatus == CheckStatus.Bounced && currentStatus != CheckStatus.Pending)
                {
                    MessageBox.Show(
                        UiText.T("يمكن إرجاع الشيك الصادر المعلق فقط.", "Only a pending issued check can be bounced."),
                        UiText.T("تنبيه", "Notice"), MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (!isIssuedCheck && targetStatus == CheckStatus.Bounced &&
                    currentStatus != CheckStatus.Pending &&
                    currentStatus != CheckStatus.Deposited &&
                    currentStatus != CheckStatus.Cleared)
                {
                    MessageBox.Show(
                        UiText.T("لا يمكن تغيير حالة الشيك إلى راجع من الحالة الحالية.", "This check cannot be bounced from the current status."),
                        UiText.T("تنبيه", "Notice"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var confirm = MessageBox.Show(
                    UiText.T(
                        $"هل تريد تغيير حالة الشيك إلى {GetStatusDisplayName(targetStatus)}؟",
                        $"Change the check status to {GetStatusDisplayName(targetStatus)}?"),
                    UiText.T("تأكيد", "Confirm"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                _loadingService.Show();
                loadingShown = true;

                string description = targetStatus switch
                {
                    CheckStatus.Deposited => $"Check #{check.CheckNumber} deposited",
                    CheckStatus.Cleared => $"Check #{check.CheckNumber} cleared",
                    CheckStatus.Bounced => $"Check #{check.CheckNumber} bounced",
                    CheckStatus.Cancelled => $"Check #{check.CheckNumber} cancelled",
                    _ => $"Check #{check.CheckNumber} status change"
                };

                await using var transaction = db.Database.IsRelational()
                    ? await db.Database.BeginTransactionAsync()
                    : null;

                if (isIssuedCheck && targetStatus == CheckStatus.Cleared)
                {
                    var issuedChecksId = await ResolveSystemAccountIdAsync(uow, db, AccountingService.IssuedChecksPayableAccountCodeKey, "2140000000");
                    var bankId = await ResolveSystemAccountIdAsync(uow, db, AccountingService.BankAccountCodeKey, "1130000000");

                    if (!await PostCheckJournalAsync(accountingService, check, issuedChecksId, bankId, description, targetStatus))
                        return;
                }
                else if (targetStatus == CheckStatus.Deposited)
                {
                    var bankId = await ResolveSystemAccountIdAsync(uow, db, AccountingService.BankAccountCodeKey, "1130000000");
                    var checksInHandId = await ResolveSystemAccountIdAsync(uow, db, AccountingService.ChecksInHandAccountCodeKey, "1180000000");

                    if (!await PostCheckJournalAsync(accountingService, check, bankId, checksInHandId, description, targetStatus))
                        return;
                }
                else if (isIssuedCheck && (targetStatus == CheckStatus.Bounced || targetStatus == CheckStatus.Cancelled))
                {
                    var issuedChecksId = await ResolveSystemAccountIdAsync(uow, db, AccountingService.IssuedChecksPayableAccountCodeKey, "2140000000");
                    var payableId = await ResolveSystemAccountIdAsync(uow, db, AccountingService.AccountsPayableAccountCodeKey, "2110000000");
                    var supplierId = check.Invoice?.SupplierId ?? check.Voucher?.SupplierId;

                    if (!await PostCheckJournalAsync(
                            accountingService, check, issuedChecksId, payableId, description, targetStatus,
                            supplierId: supplierId, tagCounterpartyOnCredit: true))
                        return;
                }
                else if (targetStatus == CheckStatus.Bounced || targetStatus == CheckStatus.Cancelled)
                {
                    var counterAccountId = await ResolveCheckCounterAccountIdAsync(uow, db, check);
                    var bankId = await ResolveSystemAccountIdAsync(uow, db, AccountingService.BankAccountCodeKey, "1130000000");
                    var checksInHandId = await ResolveSystemAccountIdAsync(uow, db, AccountingService.ChecksInHandAccountCodeKey, "1180000000");

                    var creditAccountId = currentStatus == CheckStatus.Deposited || currentStatus == CheckStatus.Cleared
                        ? bankId
                        : checksInHandId;

                    var customerId = check.Invoice?.CustomerId ?? check.Voucher?.CustomerId;
                    var supplierId = customerId.HasValue
                        ? null
                        : check.Invoice?.SupplierId ?? check.Voucher?.SupplierId;

                    if (!await PostCheckJournalAsync(
                            accountingService, check, counterAccountId, creditAccountId, description, targetStatus,
                            customerId, supplierId))
                        return;
                }

                var updateResult = await checkService.UpdateStatusAsync(check.Id, targetStatus);
                if (!updateResult.Success)
                {
                    MessageBox.Show(
                        updateResult.Message ?? UiText.T("تعذر تحديث حالة الشيك.", "Could not update the check status."),
                        UiText.T("خطأ", "Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                if (transaction != null)
                    await transaction.CommitAsync();

                selectedRow.Status = targetStatus;
                await LoadChecksAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                if (loadingShown)
                    _loadingService.Hide();
            }
        }

        private async Task EditSelectedCheckAsync()
        {
            try
            {
                if (ChecksGrid.SelectedItem is not CheckDashboardRow selectedRow)
                {
                    MessageBox.Show(
                        UiText.T("يجب تحديد شيك أولاً.", "You must select a check first."),
                        UiText.T("تنبيه", "Notice"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var checkService = scope.ServiceProvider.GetRequiredService<ICheckService>();

                var writeDtoResult = await checkService.GetWriteDtoByIdAsync(selectedRow.Id);
                if (!writeDtoResult.Success || writeDtoResult.Data == null)
                {
                    MessageBox.Show(
                        writeDtoResult.Message ?? UiText.T("تعذر تحميل الشيك.", "Could not load the check."),
                        UiText.T("خطأ", "Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                var dto = writeDtoResult.Data;
                var editWindow = new CheckEditWindow(dto)
                {
                    Owner = this
                };

                if (editWindow.ShowDialog() != true)
                    return;

                var updateResult = await checkService.UpdateAsync(editWindow.EditedCheck);
                if (!updateResult.Success)
                {
                    MessageBox.Show(
                        updateResult.Message ?? UiText.T("تعذر تحديث بيانات الشيك.", "Could not update the check details."),
                        UiText.T("خطأ", "Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                await LoadChecksAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task OpenSelectedSourceAsync()
        {
            if (ChecksGrid.SelectedItem is not CheckDashboardRow row)
            {
                MessageBox.Show(
                    UiText.T("يجب تحديد شيك أولاً.", "You must select a check first."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (!row.CanOpenSource)
            {
                MessageBox.Show(
                    UiText.T("لا يوجد مصدر مرتبط بهذا الشيك.", "This check does not have a linked source document."),
                    UiText.T("تنبيه", "Notice"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                await _sourceDocumentNavigationService.OpenSourceDocument(row.SourceReferenceType, row.SourceReferenceId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    UiText.T("خطأ", "Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void OpenSourceBtn_Click(object sender, RoutedEventArgs e)
        {
            await OpenSelectedSourceAsync();
        }

        private async void OpenSourceMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is CheckDashboardRow row)
            {
                ChecksGrid.SelectedItem = row;
                await OpenSelectedSourceAsync();
            }
        }

        private async void EditCheckMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is CheckDashboardRow row)
            {
                ChecksGrid.SelectedItem = row;
                await EditSelectedCheckAsync();
            }
        }

        private async void MarkDepositedMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is CheckDashboardRow row)
            {
                ChecksGrid.SelectedItem = row;
                await ChangeCheckStatusAsync(CheckStatus.Deposited);
            }
        }

        private async void MarkClearedMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is CheckDashboardRow row)
            {
                ChecksGrid.SelectedItem = row;
                await ChangeCheckStatusAsync(CheckStatus.Cleared);
            }
        }

        private async void MarkBouncedMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is CheckDashboardRow row)
            {
                ChecksGrid.SelectedItem = row;
                await ChangeCheckStatusAsync(CheckStatus.Bounced);
            }
        }

        private async void MarkCancelledMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is CheckDashboardRow row)
            {
                ChecksGrid.SelectedItem = row;
                await ChangeCheckStatusAsync(CheckStatus.Cancelled);
            }
        }

        private async void ChecksGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            await OpenSelectedSourceAsync();
        }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            await LoadChecksAsync();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
