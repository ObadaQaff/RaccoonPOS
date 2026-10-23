using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Application.Service.Insurance;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Insurance;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Navigation;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace RaccoonWarehouse.Insurance;

public partial class InsuranceCompaniesWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly IInsuranceManagementService _service;
    private readonly ILoadingService _loading;
    private readonly IPermissionService _permissions;
    private readonly ObservableCollection<CompanyRow> _companies = new();
    private readonly ICollectionView _view;
    private bool _busy;
    private bool _canCreate;
    private bool _canEdit;
    private bool _canDelete;
    private CompanyRow? _contextRow;

    public InsuranceCompaniesWindow(IInsuranceManagementService service, ILoadingService loading, IPermissionService permissions, IServiceProvider services)
    {
        _services = services;
        _service = service;
        _loading = loading;
        _permissions = permissions;
        InitializeComponent();
        _view = CollectionViewSource.GetDefaultView(_companies);
        _view.Filter = MatchesSearch;
        CompaniesGrid.ItemsSource = _view;
        UiText.ApplyWindow(this);
        Closing += (_, e) => { if (_busy) e.Cancel = true; };
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await LoadAsync();
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();
    private void Back_Click(object sender, RoutedEventArgs e) => Close();

    private async Task LoadAsync()
    {
        if (_busy) return;
        _busy = true;
        AddButton.IsEnabled = RefreshButton.IsEnabled = false;
        try
        {
            _loading.Show();
            _canCreate = await _permissions.HasCurrentUserPermissionAsync("InsuranceCompanies.Create");
            _canEdit = await _permissions.HasCurrentUserPermissionAsync("InsuranceCompanies.Edit");
            _canDelete = await _permissions.HasCurrentUserPermissionAsync("InsuranceCompanies.Delete");
            var result = await _service.GetCompaniesAsync();
            if (!result.Success)
            {
                MessageBox.Show(this, result.Message, UiText.T("خطأ", "Error"));
                return;
            }
            _companies.Clear();
            foreach (var company in result.Data ?? new List<InsuranceCompanyDto>())
                _companies.Add(new CompanyRow(company));
            RefreshSearch();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiText.T("تعذر تحميل شركات التأمين.", "Could not load insurance companies.")
                + Environment.NewLine + ex.Message, UiText.T("خطأ", "Error"));
        }
        finally
        {
            _loading.Hide();
            _busy = false;
            AddButton.IsEnabled = _canCreate;
            RefreshButton.IsEnabled = true;
        }
    }

    private async void Add_Click(object sender, RoutedEventArgs e) => await OpenEditorAsync(null);

    private async Task OpenEditorAsync(int? id)
    {
        if (_busy || (id.HasValue ? !_canEdit : !_canCreate)) return;
        var saved = false;
        WindowManager.ShowDialog<CreateInsuranceCompany>(WindowSizeType.MediumRectangle, window =>
        {
            window.Owner = this;
            if (id.HasValue) window.InitializeForEdit(id.Value);
            window.Closed += (_, _) => saved = window.Saved;
        });
        if (saved) await LoadAsync();
    }

    private void CompaniesGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_busy) { e.Handled = true; return; }
        if (e.OriginalSource is DependencyObject source
            && ItemsControl.ContainerFromElement(CompaniesGrid, source) is DataGridRow row
            && row.Item is CompanyRow company)
        {
            CompaniesGrid.SelectedItem = company;
            row.Focus();
        }
        else
        {
            CompaniesGrid.SelectedItem = null;
            _contextRow = null;
            e.Handled = true;
        }
    }

    private void CompaniesGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        _contextRow = CompaniesGrid.SelectedItem as CompanyRow;
        if (_busy || _contextRow == null) { e.Handled = true; return; }
        CompaniesContextMenu.FlowDirection = UiText.CurrentFlowDirection;
        CompanyStatementMenuItem.Header = UiText.T("كشف الحساب", "Account statement");
        EditCompanyMenuItem.Header = UiText.T("تعديل", "Edit");
        DeleteCompanyMenuItem.Header = UiText.T("حذف", "Delete");
        EditCompanyMenuItem.IsEnabled = _canEdit;
        DeleteCompanyMenuItem.IsEnabled = _canDelete;
    }

    private async void Statement_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || _contextRow is not CompanyRow row) return;
        if (!await _permissions.HasCurrentUserPermissionAsync("Accounting.GeneralLedger.View"))
        { MessageBox.Show(this, UiText.T("لا تملك صلاحية عرض كشف الحساب.", "You do not have permission to view the account statement.")); return; }
        try { new InsuranceCompanyStatementWindow(_services.GetRequiredService<ApplicationDbContext>(),
            _services.GetRequiredService<IAccountingService>(), row.Company) { Owner = this }.ShowDialog(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, UiText.T("خطأ", "Error")); }
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (_contextRow is CompanyRow row) await OpenEditorAsync(row.Company.Id);
    }

    private static string CompanyOperationError(string? message) => message switch
    {
        "Insurance company was not found." => UiText.T("لم يتم العثور على شركة التأمين.", "Insurance company was not found."),
        "Cannot delete an insurance company with linked records." => UiText.T(
            "لا يمكن حذف شركة التأمين لوجود سجلات مرتبطة بها. يمكنك تعديل حالتها إلى غير نشط.",
            "This insurance company has linked records and cannot be deleted. You can change its status to Inactive."),
        "You do not have permission to delete insurance companies." => UiText.T(
            "لا تملك صلاحية حذف شركات التأمين.", "You do not have permission to delete insurance companies."),
        _ => message ?? UiText.T("تعذر حذف شركة التأمين.", "Could not delete the insurance company.")
    };

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || !_canDelete || _contextRow is not CompanyRow row) return;
        var company = row.Company;
        var confirmed = MessageBox.Show(this,
            UiText.T($"هل تريد حذف شركة التأمين «{company.Name}»؟", $"Delete insurance company '{company.Name}'?"),
            UiText.T("تأكيد الحذف", "Confirm Delete"), MessageBoxButton.YesNo,
            MessageBoxImage.Warning, MessageBoxResult.No);
        if (confirmed != MessageBoxResult.Yes) return;
        _busy = true;
        AddButton.IsEnabled = RefreshButton.IsEnabled = CompaniesGrid.IsEnabled = false;
        var deleted = false;
        string? error = null;
        try
        {
            _loading.Show();
            var result = await _service.DeleteCompanyAsync(company.Id);
            deleted = result.Success;
            if (!deleted) error = CompanyOperationError(result.Message);
        }
        catch (Exception ex) { error = ex.Message; }
        finally
        {
            _loading.Hide();
            _busy = false;
            AddButton.IsEnabled = _canCreate;
            RefreshButton.IsEnabled = CompaniesGrid.IsEnabled = true;
        }
        if (!deleted)
        {
            MessageBox.Show(this, error, UiText.T("خطأ", "Error"));
            return;
        }
        _contextRow = null;
        await LoadAsync();
        MessageBox.Show(this, UiText.T("تم حذف شركة التأمين بنجاح.", "Insurance company deleted successfully."), UiText.T("تم", "Success"));
    }

    private bool MatchesSearch(object item)
    {
        if (item is not CompanyRow row) return false;
        var text = SearchTextBox.Text.Trim();
        return text.Length == 0
            || row.Company.Code.Contains(text, StringComparison.CurrentCultureIgnoreCase)
            || row.Company.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase)
            || (row.Company.NameEnglish?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false)
            || (row.Company.Phone?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false);
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshSearch();

    private void RefreshSearch()
    {
        // TextChanged can fire while InitializeComponent is still constructing the view.
        if (_view == null) return;
        _view.Refresh();
        var count = _view.Cast<CompanyRow>().Count();
        CountText.Text = count.ToString();
        EmptyStatePanel.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public sealed class CompanyRow(InsuranceCompanyDto company)
    {
        public InsuranceCompanyDto Company { get; } = company;
        public string OrganizationTypeName => Company.OrganizationType switch
        {
            InsuranceOrganizationType.Insurer => UiText.T("شركة تأمين", "Insurer"),
            InsuranceOrganizationType.Administrator => UiText.T("مدير تأمين", "Administrator"),
            InsuranceOrganizationType.ContractualPayer => UiText.T("جهة دفع متعاقدة", "Contractual payer"),
            InsuranceOrganizationType.InsurerAndPayer => UiText.T("شركة تأمين وجهة دفع", "Insurer and payer"),
            _ => UiText.T("أخرى", "Other")
        };
        public string StatusName => Company.Status switch
        {
            InsuranceRecordStatus.Active => UiText.T("نشط", "Active"),
            InsuranceRecordStatus.Inactive => UiText.T("غير نشط", "Inactive"),
            InsuranceRecordStatus.Expired => UiText.T("منتهي", "Expired"),
            _ => UiText.T("مسودة", "Draft")
        };
    }
}
