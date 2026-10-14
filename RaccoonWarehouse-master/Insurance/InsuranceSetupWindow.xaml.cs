using RaccoonWarehouse.Application.Service.Insurance;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Insurance;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Invoices;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace RaccoonWarehouse.Insurance;

public partial class InsuranceSetupWindow : Window
{
    private readonly IInsuranceManagementService _service;
    private readonly ILoadingService _loading;
    private readonly IProductService _productService;
    private readonly IStockService _stockService;
    private readonly ObservableCollection<InsuranceContractDto> _contracts = new();
    private readonly ObservableCollection<InsurancePlanDto> _plans = new();
    private readonly ObservableCollection<InsuranceMemberPolicyDto> _members = new();
    private readonly ObservableCollection<InsuranceCoverageRuleDto> _rules = new();

    public InsuranceSetupWindow(IInsuranceManagementService service, ILoadingService loading, IProductService productService, IStockService stockService)
    {
        _service = service;
        _loading = loading;
        _productService = productService;
        _stockService = stockService;
        InitializeComponent();
        ContractsGrid.ItemsSource = _contracts;
        PlansGrid.ItemsSource = _plans;
        MembersGrid.ItemsSource = _members;
        RulesGrid.ItemsSource = _rules;
        UiText.ApplyWindow(this);
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading.Show();
        try
        {
            var contracts = await _service.GetContractsAsync();
            var members = await _service.GetMemberPoliciesAsync();
            if (!contracts.Success || !members.Success)
            {
                MessageBox.Show(contracts.Success ? members.Message : contracts.Message, UiText.T("خطأ", "Error"));
                return;
            }
            _contracts.Clear();
            foreach (var row in contracts.Data ?? new()) _contracts.Add(row);
            _members.Clear();
            foreach (var row in members.Data ?? new()) _members.Add(row);
            if (_contracts.Count > 0)
            {
                ContractsGrid.SelectedIndex = 0;
                await LoadPlansAsync(_contracts[0].Id);
            }
        }
        finally
        {
            _loading.Hide();
        }
    }

    private async void ContractsGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ContractsGrid.SelectedItem is InsuranceContractDto contract)
            await LoadPlansAsync(contract.Id);
    }

    private async Task LoadPlansAsync(int contractId)
    {
        var result = await _service.GetPlansAsync(contractId);
        if (!result.Success) return;
        _plans.Clear();
        foreach (var row in result.Data ?? new()) _plans.Add(row);
    }

    private async void SaveContract_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(ContractCompanyIdTextBox.Text, out var companyId))
        {
            MessageBox.Show(UiText.T("أدخل رقم شركة صحيح.", "Enter a valid company ID."));
            return;
        }
        var result = await _service.SaveContractAsync(new InsuranceContractWriteDto
        {
            ContractNumber = ContractNumberTextBox.Text,
            Name = ContractNameTextBox.Text,
            InsuranceCompanyId = companyId,
            ValidFrom = DateTime.Today,
            ValidTo = DateTime.Today.AddYears(1),
            Status = InsuranceRecordStatus.Draft
        });
        MessageBox.Show(result.Message, result.Success ? UiText.T("تم", "Success") : UiText.T("خطأ", "Error"));
        if (result.Success) await LoadAsync();
    }

    private async void SavePlan_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(PlanContractIdTextBox.Text, out var contractId))
        {
            MessageBox.Show(UiText.T("أدخل رقم عقد صحيح.", "Enter a valid contract ID."));
            return;
        }
        var result = await _service.SavePlanAsync(new InsurancePlanWriteDto
        {
            InsuranceContractId = contractId, Code = PlanCodeTextBox.Text, Name = PlanNameTextBox.Text,
            ValidFrom = DateTime.Today, ValidTo = DateTime.Today.AddYears(1), Status = InsuranceRecordStatus.Draft
        });
        MessageBox.Show(result.Message, result.Success ? UiText.T("تم", "Success") : UiText.T("خطأ", "Error"));
        if (result.Success) await LoadPlansAsync(contractId);
    }

    private async void SaveMember_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(MemberCustomerIdTextBox.Text, out var customerId)
            || !int.TryParse(MemberCompanyIdTextBox.Text, out var companyId)
            || !int.TryParse(MemberContractIdTextBox.Text, out var contractId))
        {
            MessageBox.Show(UiText.T("أدخل أرقام العميل والشركة والعقد بشكل صحيح.", "Enter valid customer, company, and contract IDs."));
            return;
        }
        var result = await _service.SaveMemberPolicyAsync(new InsuranceMemberPolicyWriteDto
        {
            CustomerId = customerId, InsuranceCompanyId = companyId, InsuranceContractId = contractId,
            MemberNumber = MemberNumberTextBox.Text, ValidFrom = DateTime.Today, ValidTo = DateTime.Today.AddYears(1),
            Status = InsuranceRecordStatus.Active
        });
        MessageBox.Show(result.Message, result.Success ? UiText.T("تم", "Success") : UiText.T("خطأ", "Error"));
        if (result.Success) await LoadAsync();
    }

    private async void SaveRule_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(RuleVersionIdTextBox.Text, out var versionId)
            || !int.TryParse(RuleProductIdTextBox.Text, out var productId)
            || !decimal.TryParse(RuleCoverageTextBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var coverage)
            || !decimal.TryParse(string.IsNullOrWhiteSpace(RuleDiscountTextBox.Text) ? "0" : RuleDiscountTextBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var discount)
            || !decimal.TryParse(string.IsNullOrWhiteSpace(RuleFixedDiscountTextBox.Text) ? "0" : RuleFixedDiscountTextBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var fixedDiscount)
            || !decimal.TryParse(string.IsNullOrWhiteSpace(RuleCopayTextBox.Text) ? "0" : RuleCopayTextBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var copay))
        {
            MessageBox.Show(UiText.T("أدخل رقم الإصدار والمنتج ونسبة تغطية صحيحة.", "Enter valid version, product, and coverage values."));
            return;
        }
        int? planId = int.TryParse(RulePlanIdTextBox.Text, out var parsedPlanId) ? parsedPlanId : null;
        var result = await _service.SaveCoverageRuleAsync(new InsuranceCoverageRuleWriteDto
        {
            InsuranceContractVersionId = versionId, InsurancePlanId = planId, ProductId = productId,
            CoveragePercent = coverage, CopayPercent = copay, DiscountPercent = discount,
            FixedDiscountAmount = fixedDiscount, ValidFrom = DateTime.Today, ValidTo = DateTime.Today.AddYears(1)
        });
        MessageBox.Show(result.Message, result.Success ? UiText.T("تم", "Success") : UiText.T("خطأ", "Error"));
        if (result.Success) await LoadRulesAsync(versionId, planId);
    }

    private void SelectRuleProduct_Click(object sender, RoutedEventArgs e)
    {
        var searchWindow = new ProductSearchWindow(_productService, _stockService);
        searchWindow.Owner = this;
        if (searchWindow.ShowDialog() == true && searchWindow.SelectedProduct != null)
        {
            RuleProductIdTextBox.Text = searchWindow.SelectedProduct.Id.ToString(CultureInfo.InvariantCulture);
            RuleProductIdTextBox.ToolTip = searchWindow.SelectedProduct.Name;
        }
    }

    private async Task LoadRulesAsync(int versionId, int? planId)
    {
        var result = await _service.GetCoverageRulesAsync(versionId, planId);
        if (!result.Success) return;
        _rules.Clear();
        foreach (var row in result.Data ?? new()) _rules.Add(row);
    }
}
