using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Permissions;
using RaccoonWarehouse.Domain.Permissions.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RaccoonWarehouse.Settings;

public partial class ReportPermissionsManager : Window
{
    private readonly IPermissionService _permissions;
    private readonly IUserSession _session;
    private readonly ObservableCollection<PermissionRow> _rows = new();
    private readonly ObservableCollection<PermissionCell> _advanced = new();
    private readonly Dictionary<string, bool> _changes = new(StringComparer.OrdinalIgnoreCase);
    private UserRole? _loadedRole;
    private bool _loading;
    private bool _canManage;

    public ReportPermissionsManager(IPermissionService permissionService, IUserSession userSession)
    {
        RaccoonWarehouse.Security.PermissionWindowGuard.Demand(GetType());
        InitializeComponent();
        UiText.ApplyWindow(this);
        _permissions = permissionService;
        _session = userSession;
        PermissionsGrid.ItemsSource = _rows;
        AdvancedGrid.ItemsSource = _advanced;
        CollectionViewSource.GetDefaultView(_rows).GroupDescriptions.Add(new PropertyGroupDescription(nameof(PermissionRow.Module)));
        Loaded += async (_, _) =>
        {
            try
            {
                if (_session.CurrentRole != UserRole.Admin || !await _permissions.HasCurrentUserPermissionAsync("Permissions.View"))
                { Close(); return; }
                _loading = true;
                RoleComboBox.ItemsSource = Enum.GetValues<UserRole>();
                RoleComboBox.SelectedItem = UserRole.Casher;
                _loading = false;
                await LoadRoleAsync();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        };
    }

    private async Task LoadRoleAsync()
    {
        if (_loading || RoleComboBox.SelectedItem is not UserRole role) return;
        _loading = true;
        RoleComboBox.IsEnabled = false;
        SaveButton.IsEnabled = false;
        try
        {
            SetEditingAllowed(await _permissions.HasCurrentUserPermissionAsync("Permissions.ManageRoles"));
            var matrix = await _permissions.GetPermissionMatrixAsync(role);
            var raw = matrix.SelectMany(r => r.Actions.Select(a => (Key: r.Resource + "." + a.Key, Allowed: a.Value)))
                .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.Last().Allowed, StringComparer.OrdinalIgnoreCase);
            _rows.Clear(); _advanced.Clear(); _changes.Clear();
            foreach (var group in PermissionCatalog.All.GroupBy(p => PermissionPresentationPolicy.Resource(p.Key)).OrderBy(g => g.First().Module).ThenBy(g => g.First().DisplayName))
            {
                var row = new PermissionRow { Module = ModuleLabel(group.First().Module), Name = ResourceLabel(group.Key, group.First().DisplayName) };
                PermissionCell Cell(PermissionKind kind, string label)
                {
                    var keys = group.Where(p => PermissionPresentationPolicy.Classify(p.Key) == kind).Select(p => p.Key).ToArray();
                    var cell = new PermissionCell(row.Module, row.Name, label, keys, keys.Select(k => raw.GetValueOrDefault(k)).ToArray(), _canManage);
                    cell.Changed = (current, value) =>
                    {
                        foreach (var key in current.Keys) _changes[key] = value;
                        if (kind is PermissionKind.CreateEdit or PermissionKind.Delete && value) row.View.Set(true);
                        if (kind == PermissionKind.View && !value) { row.CreateEdit.Set(false); row.Delete.Set(false); }
                    };
                    return cell;
                }
                row.View = Cell(PermissionKind.View, UiText.T("عرض", "View"));
                row.CreateEdit = Cell(PermissionKind.CreateEdit, UiText.T("إضافة وتعديل", "Create / Edit"));
                row.Delete = Cell(PermissionKind.Delete, UiText.T("حذف", "Delete"));
                if (row.View.Supported || row.CreateEdit.Supported || row.Delete.Supported) _rows.Add(row);
                foreach (var advanced in group.Where(p => PermissionPresentationPolicy.Classify(p.Key) == PermissionKind.Sensitive)
                    .GroupBy(item => PermissionPresentationPolicy.CanonicalKey(item.Key)))
                {
                    var keys = advanced.Select(item => item.Key).ToArray();
                    var cell = new PermissionCell(row.Module, row.Name, AdvancedLabel(PermissionPresentationPolicy.Action(advanced.First().Key)), keys, keys.Select(key => raw.GetValueOrDefault(key)).ToArray(), _canManage);
                    cell.Changed = (current, value) => { foreach (var key in current.Keys) _changes[key] = value; if (value) row.View.Set(true); };
                    _advanced.Add(cell);
                }
            }
            _loadedRole = role;
            ModuleComboBox.ItemsSource = new[] { UiText.T("الكل", "All") }.Concat(_rows.Select(r => r.Module).Concat(_advanced.Select(a => a.Module)).Distinct().OrderBy(x => x));
            ModuleComboBox.SelectedIndex = 0;
            FilterRows();
        }
        catch (Exception ex) { _loadedRole = null; ShowError(ex.Message); }
        finally { _loading = false; RoleComboBox.IsEnabled = true; SaveButton.IsEnabled = _canManage && _loadedRole.HasValue; }
    }

    private static string ModuleLabel(string key) => key switch
    {
        "MasterData" => UiText.T("البيانات الأساسية", "Master data"),
        "Sales" => UiText.T("المبيعات", "Sales"),
        "Purchases" => UiText.T("المشتريات", "Purchases"),
        "Inventory" => UiText.T("المخزون", "Inventory"),
        "Accounting" => UiText.T("المحاسبة", "Accounting"),
        "Finance" => UiText.T("المالية", "Finance"),
        "Administration" => UiText.T("الإدارة", "Administration"),
        "Employees" => UiText.T("الموظفون", "Employees"),
        "Insurance" => UiText.T("التأمين", "Insurance"),
        "Dashboard" => UiText.T("لوحة التحكم", "Dashboard"),
        "Reports" => UiText.T("التقارير", "Reports"),
        "Operations" => UiText.T("العمليات", "Operations"),
        _ => key
    };

    private static string ResourceLabel(string key, string fallback) => key switch
    {
        "Products" => UiText.T("الأصناف", "Products"),
        "Categories" => UiText.T("الفئات", "Categories"),
        "SubCategories" => UiText.T("الفئات الفرعية", "Subcategories"),
        "Brands" => UiText.T("العلامات التجارية", "Brands"),
        "Units" => UiText.T("الوحدات", "Units"),
        "Warehouses" => UiText.T("المستودعات", "Warehouses"),
        "Users" => UiText.T("حسابات المستخدمين", "User accounts"),
        "Customers" => UiText.T("العملاء", "Customers"),
        "Suppliers" => UiText.T("الموردون", "Suppliers"),
        "Employees" => UiText.T("الموظفون", "Employees"),
        "Delegates" => UiText.T("المندوبون", "Delegates"),
        "InsuranceCompanies" => UiText.T("شركات التأمين", "Insurance companies"),
        "InsuranceNetworks" => UiText.T("خطط وشبكات التأمين", "Insurance plans and networks"),
        "InsuranceContracts" => UiText.T("عقود التأمين والتغطيات", "Insurance contracts and coverage"),
        "InsurancePolicies" => UiText.T("وثائق التأمين", "Insurance policies"),
        "InsuranceClaims" => UiText.T("مطالبات التأمين", "Insurance claims"),
        "InsurancePayments" => UiText.T("دفعات وتسويات التأمين", "Insurance payments and settlements"),
        "InsuranceReports" => UiText.T("تقارير التأمين", "Insurance reports"),
        "PurchaseOrder" => UiText.T("أوامر الشراء", "Purchase orders"),
        "PurchaseRequest" => UiText.T("طلبات الشراء", "Purchase requests"),
        "Settings" => UiText.T("الإعدادات", "Settings"),
        "Permissions" => UiText.T("الأدوار والصلاحيات", "Roles and permissions"),
        "Payroll" => UiText.T("الرواتب", "Payroll"),
        "CashDrawer" => UiText.T("الصندوق والورديات", "Cash drawer and shifts"),
        "Vouchers" => UiText.T("سندات القبض والدفع", "Vouchers"),
        "Accounting.Accounts" => UiText.T("شجرة الحسابات", "Accounts"),
        "Accounting.JournalEntry" => UiText.T("القيود اليومية", "Journal entry"),
        "Accounting.JournalEntries" => UiText.T("سجل القيود", "Journal entries"),
        "Accounting.Operations" => UiText.T("العمليات المحاسبية", "Accounting operations"),
        "Accounting.Reconciliation" => UiText.T("التسويات البنكية", "Bank reconciliation"),
        "Accounting.Periods" => UiText.T("الفترات المحاسبية", "Accounting periods"),
        "Accounting.CostCenters" => UiText.T("مراكز التكلفة", "Cost centers"),
        "Accounting.Currency" => UiText.T("العملات", "Currencies"),
        "Accounting.OpeningBalance" => UiText.T("الأرصدة الافتتاحية", "Opening balances"),
        "Accounting.RecurringJournal" => UiText.T("القيود الدورية", "Recurring journals"),
        "Accounting.Tax" => UiText.T("الضرائب", "Taxes"),
        "FinancialTransactions" => UiText.T("الحركات المالية", "Financial transactions"),
        "StockTransactions" => UiText.T("حركات المخزون", "Stock transactions"),
        "GoodsReceipt" => UiText.T("استلام المشتريات", "Goods receipts"),
        "PurchaseReturn" => UiText.T("مردودات المشتريات", "Purchase returns"),
        "SupplierInvoice" => UiText.T("مطابقة فواتير الموردين", "Supplier invoice matching"),
        "SupplierPayment" => UiText.T("تخصيص دفعات الموردين", "Supplier payment allocation"),
        "PartyStatement" => UiText.T("كشوف حساب الأطراف", "Party statements"),
        "Orders" => UiText.T("الطلبات والاستيراد", "Orders and import"),
        "ChatAssistant" => UiText.T("المساعد", "Assistant"),
        _ => fallback
    };

    private static string AdvancedLabel(string action) => action switch
    {
        "Employees" => UiText.T("إعدادات الموظفين", "Employee feature settings"),
        "Delegates" => UiText.T("إعدادات المندوبين", "Delegate feature settings"),
        "Accounting" => UiText.T("إعدادات المحاسبة", "Accounting feature settings"),
        "Language" => UiText.T("إعدادات اللغة", "Language settings"),
        "ViewCost" => UiText.T("عرض التكلفة", "View cost"), "ViewProfit" => UiText.T("عرض الأرباح", "View profit"),
        "ApplyDiscount" => UiText.T("تطبيق الخصم", "Apply discount"), "ChangePrice" => UiText.T("تغيير السعر", "Change price"),
        "Configure" => UiText.T("إعداد السياسة", "Configure policy"), "Calculate" => UiText.T("احتساب الرواتب", "Calculate payroll"),
        "Adjust" => UiText.T("تسوية موثقة", "Documented adjustment"), "Correct" => UiText.T("تصحيح معتمد", "Approved correction"),
        "SensitiveView" => UiText.T("عرض البيانات الحساسة", "View sensitive data"), "SensitiveEdit" => UiText.T("تعديل البيانات الحساسة", "Edit sensitive data"),
        "Close" => UiText.T("إغلاق", "Close"), "Submit" => UiText.T("إرسال للاعتماد", "Submit for approval"),
        "Reject" => UiText.T("رفض", "Reject"), "Reconcile" => UiText.T("تسوية الدفعات", "Reconcile payments"),
        "Reverse" => UiText.T("عكس الحركة", "Reverse transaction"), "Import" => UiText.T("استيراد البيانات", "Import data"),
        "Approve" => UiText.T("اعتماد", "Approve"), "Post" => UiText.T("ترحيل مالي", "Post"),
        "Confirm" => UiText.T("تأكيد العملية", "Confirm"), "Cancel" => UiText.T("إلغاء", "Cancel"),
        "Return" => UiText.T("إرجاع", "Return"), "MarkPaid" or "Payment" => UiText.T("تنفيذ الدفع", "Execute payment"),
        "Receipt" => UiText.T("تنفيذ القبض", "Execute receipt"), "CloseShift" => UiText.T("إغلاق الوردية", "Close shift"),
        "ManageRoles" => UiText.T("إدارة الأدوار والصلاحيات", "Manage roles and permissions"),
        "ManageSettings" => UiText.T("تعديل الإعدادات الحساسة", "Modify sensitive settings"),
        "ManageUsers" => UiText.T("إدارة حسابات المستخدمين", "Manage user accounts"),
        "Print" => UiText.T("طباعة", "Print"), "Export" => UiText.T("تصدير", "Export"),
        _ => UiText.T("عملية متقدمة", "Advanced operation") + " — " + action
    };

    private void FilterRows()
    {
        var search = SearchTextBox.Text.Trim();
        var module = ModuleComboBox.SelectedIndex > 0 ? ModuleComboBox.SelectedItem as string : null;
        bool Matches(string name, string group) => (module == null || group == module) &&
            (search.Length == 0 || name.Contains(search, StringComparison.OrdinalIgnoreCase) || group.Contains(search, StringComparison.OrdinalIgnoreCase));
        CollectionViewSource.GetDefaultView(_rows).Filter = item => item is PermissionRow r && Matches(r.Name, r.Module);
        CollectionViewSource.GetDefaultView(_advanced).Filter = item => item is PermissionCell c && Matches(c.Screen + " " + c.Label, c.Module);
    }

    private async void RoleChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        if (_changes.Count > 0 && MessageBox.Show(UiText.T("تجاهل التغييرات غير المحفوظة؟", "Discard unsaved changes?"),
            UiText.T("الصلاحيات", "Permissions"), MessageBoxButton.YesNo) != MessageBoxResult.Yes)
        { _loading = true; RoleComboBox.SelectedItem = _loadedRole; _loading = false; return; }
        await LoadRoleAsync();
    }
    private void FilterChanged(object sender, RoutedEventArgs e) { if (IsLoaded) FilterRows(); }
    private void FilterTextChanged(object sender, TextChangedEventArgs e) { if (IsLoaded) FilterRows(); }
    private void SelectAll(object sender, RoutedEventArgs e) => SetVisible(true);
    private void ClearAll(object sender, RoutedEventArgs e) => SetVisible(false);
    public void SetEditingAllowed(bool allowed)
    {
        _canManage = allowed;
        foreach (var row in _rows) { row.View.SetEditable(allowed); row.CreateEdit.SetEditable(allowed); row.Delete.SetEditable(allowed); }
        foreach (var cell in _advanced) cell.SetEditable(allowed);
        SaveButton.IsEnabled = allowed && !_loading;
    }

    private void SetVisible(bool value)
    {
        if (!_canManage) return;
        // Bulk selection deliberately excludes sensitive permissions.
        foreach (PermissionRow row in CollectionViewSource.GetDefaultView(_rows))
        { row.View.Set(value); row.CreateEdit.Set(value); row.Delete.Set(value); }
    }
    private async void Save(object sender, RoutedEventArgs e)
    {
        if (!_canManage || _loading || _loadedRole is not { } role || _changes.Count == 0) return;
        SaveButton.IsEnabled = false;
        try
        {
            var result = await _permissions.SavePermissionsAsync(role, _changes.Select(c => new RolePermissionWriteDto
            { Role = role, PermissionKey = c.Key, IsAllowed = c.Value }).ToArray());
            MessageBox.Show(this, result.Message, UiText.T("الصلاحيات", "Permissions"));
            if (result.Success) await LoadRoleAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { SaveButton.IsEnabled = _canManage; }
    }
    private void ShowError(string message) => MessageBox.Show(this, message, UiText.T("خطأ", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
    private void CloseWindow(object sender, RoutedEventArgs e) => Close();
}

public sealed class PermissionRow
{
    public string Module { get; init; } = "";
    public string Name { get; init; } = "";
    public PermissionCell View { get; set; } = null!;
    public PermissionCell CreateEdit { get; set; } = null!;
    public PermissionCell Delete { get; set; } = null!;
}
public sealed class PermissionCell : INotifyPropertyChanged
{
    public string Module { get; }
    public string Screen { get; }
    public string Label { get; }
    public string[] Keys { get; }
    public bool Supported => Keys.Length > 0;
    public bool Editable { get; private set; }
    private bool? _allowed;
    public bool? IsAllowed { get => _allowed; set { if (value.HasValue) Set(value.Value); } }
    public Action<PermissionCell, bool>? Changed { get; set; }
    public PermissionCell(string module, string screen, string label, string[] keys, bool[] values, bool editable)
    { Module = module; Screen = screen; Label = label; Keys = keys; Editable = editable; _allowed = PermissionPresentationPolicy.Aggregate(values); }
    public void SetEditable(bool value) { Editable = value; PropertyChanged?.Invoke(this, new(nameof(Editable))); }
    public void Set(bool value)
    {
        if (!Supported || !Editable || _allowed == value) return;
        _allowed = value;
        PropertyChanged?.Invoke(this, new(nameof(IsAllowed)));
        Changed?.Invoke(this, value);
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}
