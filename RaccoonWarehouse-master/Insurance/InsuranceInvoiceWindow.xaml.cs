using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Insurance;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.Sales;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Insurance;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.StockTransactions.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Invoices;
using RaccoonWarehouse.Application.Service.Users;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace RaccoonWarehouse.Insurance;

public sealed class InsuranceInvoiceLineViewModel
{
    public ProductReadDto Product { get; init; } = new();
    public int ProductUnitId { get; init; }
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; init; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal CoveredAmount { get; set; }
    public decimal PatientAmount { get; set; }
    public string ProductName => Product.Name ?? string.Empty;
    public string RemoveText => "×";
}

public partial class InsuranceInvoiceWindow : Window
{
    private readonly IInsuranceCoverageService _coverageService;
    private readonly IInsuranceManagementService _insuranceService;
    private readonly IProductService _productService;
    private readonly IStockService _stockService;
    private readonly ISaleCheckoutService _checkoutService;
    private readonly IUserSession _userSession;
    private readonly IUserService _userService;
    private readonly ILoadingService _loading;
    private readonly ApplicationDbContext _db;
    private readonly ObservableCollection<InsuranceInvoiceLineViewModel> _lines = new();
    private InsuranceCoverageCalculationResult? _calculation;

    public InsuranceInvoiceWindow(
        IInsuranceCoverageService coverageService,
        IInsuranceManagementService insuranceService,
        IProductService productService,
        IStockService stockService,
        ISaleCheckoutService checkoutService,
        IUserSession userSession,
        IUserService userService,
        ILoadingService loading,
        ApplicationDbContext db)
    {
        _coverageService = coverageService;
        _insuranceService = insuranceService;
        _productService = productService;
        _stockService = stockService;
        _checkoutService = checkoutService;
        _userSession = userSession;
        _userService = userService;
        _loading = loading;
        _db = db;
        RaccoonWarehouse.Security.PermissionWindowGuard.Demand(GetType());
        InitializeComponent();
        LinesGrid.ItemsSource = _lines;
        InvoiceNumberTextBox.Text = (DateTime.Now.Ticks % 100000).ToString("00000", CultureInfo.InvariantCulture);
        UiText.ApplyWindow(this);
    }

    private void AddProduct_Click(object sender, RoutedEventArgs e)
    {
        var search = new ProductSearchWindow(_productService, _stockService, _userService) { Owner = this };
        if (search.ShowDialog() != true || search.SelectedProduct == null)
            return;
        var product = search.SelectedProduct;
        var unit = product.DefaultSaleUnit ?? product.BaseUnit;
        if (unit == null)
        {
            MessageBox.Show(UiText.T("الصنف لا يحتوي على وحدة بيع.", "The product has no sale unit."));
            return;
        }
        if (_lines.Any(x => x.Product.Id == product.Id && x.ProductUnitId == unit.Id))
            return;
        _lines.Add(new InsuranceInvoiceLineViewModel
        {
            Product = product,
            ProductUnitId = unit.Id,
            UnitPrice = unit.SalePrice
        });
        _calculation = null;
    }

    private async void Calculate_Click(object sender, RoutedEventArgs e) => await CalculateAsync(showMessages: true);

    private async Task<bool> CalculateAsync(bool showMessages)
    {
        if (!TryParseIds(out var customerId, out var policyId, out var companyId, out var contractId, out var planId, out var versionId) || _lines.Count == 0)
        {
            if (showMessages) MessageBox.Show(UiText.T("أدخل بيانات العضوية وأضف منتجاً واحداً على الأقل.", "Enter membership data and add at least one product."));
            return false;
        }
        var policy = await _db.InsuranceMemberPolicies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == policyId
            && x.CustomerId == customerId && x.InsuranceCompanyId == companyId && x.InsuranceContractId == contractId
            && x.ValidFrom <= DateTime.Today && x.ValidTo >= DateTime.Today && x.Status == InsuranceRecordStatus.Active);
        if (policy == null)
        {
            if (showMessages) MessageBox.Show(UiText.T("العضوية غير صالحة أو لا تطابق العقد.", "The membership is invalid or does not match the contract."));
            return false;
        }
        var validContract = await _db.InsuranceContracts.AsNoTracking().AnyAsync(x => x.Id == contractId
            && x.InsuranceCompanyId == companyId && x.ValidFrom <= DateTime.Today && x.ValidTo >= DateTime.Today
            && x.Status == InsuranceRecordStatus.Active);
        var validVersion = await _db.InsuranceContractVersions.AsNoTracking().AnyAsync(x => x.Id == versionId
            && x.InsuranceContractId == contractId && x.IsActive && x.ValidFrom <= DateTime.Today && x.ValidTo >= DateTime.Today);
        var validPlan = await _db.InsurancePlans.AsNoTracking().AnyAsync(x => x.Id == planId
            && x.InsuranceContractId == contractId && x.Status == InsuranceRecordStatus.Active
            && x.ValidFrom <= DateTime.Today && x.ValidTo >= DateTime.Today);
        if (!validContract || !validVersion || !validPlan)
        {
            if (showMessages) MessageBox.Show(UiText.T("العقد أو الخطة أو إصدار العقد غير صالح بتاريخ الفاتورة.", "The contract, plan, or contract version is not valid on the invoice date."));
            return false;
        }
        var result = await _coverageService.CalculateAsync(versionId, planId, _lines.Select(x => new InsuranceCoverageLineInput
        {
            ProductId = x.Product.Id, Quantity = x.Quantity, UnitPrice = x.UnitPrice, ApprovedQuantity = x.Quantity
        }));
        if (!result.Success || result.Data == null)
        {
            if (showMessages) MessageBox.Show(result.Message, UiText.T("خطأ", "Error"));
            return false;
        }
        if (result.Data.Lines.Any(x => x.IsExcluded || string.Equals(x.DecisionReason, "No matching coverage rule was configured.", StringComparison.OrdinalIgnoreCase)))
        {
            if (showMessages) MessageBox.Show(UiText.T("يوجد منتج غير مشمول في قاعدة التغطية المحددة.", "One or more products are not included in the selected coverage rules."));
            return false;
        }
        _calculation = result.Data;
        for (var i = 0; i < _lines.Count; i++)
        {
            var calculated = result.Data.Lines[i];
            _lines[i].DiscountPercent = calculated.InsuranceDiscountPercent;
            _lines[i].DiscountAmount = calculated.InsuranceDiscountAmount;
            _lines[i].CoveredAmount = calculated.CoveredAmount;
            _lines[i].PatientAmount = calculated.PatientAmount;
        }
        LinesGrid.Items.Refresh();
        DiscountTotalText.Text = result.Data.Lines.Sum(x => x.InsuranceDiscountAmount).ToString("0.000", CultureInfo.CurrentCulture);
        InsuranceTotalText.Text = result.Data.CoveredAmount.ToString("0.000", CultureInfo.CurrentCulture);
        PatientTotalText.Text = result.Data.PatientAmount.ToString("0.000", CultureInfo.CurrentCulture);
        return true;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        RaccoonWarehouse.Security.PermissionWindowGuard.DemandRule("InsuranceClaims.Create;SalesInvoice.Post");
        if (!await CalculateAsync(true) || _calculation == null || !TryParseIds(out var customerId, out var policyId, out var companyId, out var contractId, out var planId, out var versionId))
            return;
        if (_userSession.CurrentCashierSession is not CashierSessionReadDto session)
        {
            MessageBox.Show(UiText.T("لا توجد جلسة كاشير مفتوحة.", "There is no open cashier session."));
            return;
        }
        _loading.Show();
        try
        {
            var invoiceLines = _lines.Select((line, index) => new InvoiceLineWriteDto
            {
                ProductId = line.Product.Id,
                ProductUnitId = line.ProductUnitId,
                ProductName = line.Product.Name,
                UnitName = line.Product.DefaultSaleUnit?.Unit?.Name,
                Quantity = line.Quantity,
                QuantityPerUnitSnapshot = line.Product.DefaultSaleUnit?.QuantityPerUnit ?? 1m,
                BaseQuantity = line.Quantity * (line.Product.DefaultSaleUnit?.QuantityPerUnit ?? 1m),
                UnitPrice = line.UnitPrice,
                LineDiscountAmount = line.DiscountAmount,
                LineSubTotal = line.Quantity * line.UnitPrice - line.DiscountAmount,
                ExpiryDate = line.Product.CurrentExpiryDate ?? DateTime.Today,
                TaxExempt = line.Product.TaxExempt ?? false,
                TaxRate = line.Product.TaxRate ?? 0m,
                UnitCost = line.Product.CurrentPurchasePrice
            }).ToList();
            var total = _calculation.PatientAmount + _calculation.CoveredAmount;
            var invoice = new InvoiceWriteDto
            {
                InvoiceNumber = InvoiceNumberTextBox.Text.Trim(), InvoiceType = InvoiceType.Sale,
                PaymentType = _calculation.PatientAmount > 0 ? PaymentType.Cash : PaymentType.InsuranceReceivable,
                CustomerId = customerId, IsInsuranceSale = true, InsuranceMemberPolicyId = policyId,
                InsuranceCompanyId = companyId, InsuranceContractId = contractId, InsuranceContractVersionId = versionId,
                PatientResponsibility = _calculation.PatientAmount, InsuranceResponsibility = _calculation.CoveredAmount,
                TotalAmount = total, SubTotal = total, TotalTax = 0m, NetSales = total,
                InvoiceLines = invoiceLines, Payments = new List<InvoicePaymentWriteDto>(),
                CasherId = session.CashierId, CashierSessionId = session.Id, IsPOS = true,
                Status = InvoiceStatus.Completed, CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now
            };
            if (_calculation.PatientAmount > 0) invoice.Payments.Add(new InvoicePaymentWriteDto { PaymentType = PaymentType.Cash, Amount = _calculation.PatientAmount });
            if (_calculation.CoveredAmount > 0) invoice.Payments.Add(new InvoicePaymentWriteDto { PaymentType = PaymentType.InsuranceReceivable, Amount = _calculation.CoveredAmount });
            var checkout = await _checkoutService.CompleteAsync(new SaleCheckoutRequest
            {
                Invoice = invoice,
                Session = session,
                StockMovementsFactory = invoiceId => invoiceLines.Select(line => new StockMovementPostDto
                {
                    ProductId = line.ProductId, ProductUnitId = line.ProductUnitId, Quantity = -line.Quantity,
                    BaseQuantity = -line.BaseQuantity, QuantityPerUnitSnapshot = line.QuantityPerUnitSnapshot,
                    UnitPrice = line.UnitPrice, SalePrice = line.UnitPrice, PurchasePrice = line.UnitCost,
                    ExpiryDate = line.ExpiryDate, TransactionType = TransactionType.Sale, InvoiceId = invoiceId,
                    CustomerId = customerId, CasherId = session.CashierId, CashierSessionId = session.Id,
                    TransactionDate = DateTime.Now, Notes = $"Insurance invoice #{invoice.InvoiceNumber}"
                })
            });
            if (!checkout.Success || checkout.Data == null)
            {
                MessageBox.Show(checkout.Message, UiText.T("خطأ", "Error"));
                return;
            }
            var saved = checkout.Data.SavedInvoice;
            var coverage = new InsuranceInvoiceCoverage
            {
                InvoiceId = saved.Id, InsuranceCompanyId = companyId, InsuranceContractId = contractId,
                InsuranceContractVersionId = versionId, InsuranceMemberPolicyId = policyId,
                GrossAmount = _lines.Sum(x => x.Quantity * x.UnitPrice), DiscountAmount = _calculation.InsuranceDiscountAmount,
                InsurerAmount = _calculation.CoveredAmount, PatientAmount = _calculation.PatientAmount,
                DeductibleAmount = _calculation.DeductibleAmount, CopayAmount = _calculation.CopayAmount,
                CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now
            };
            var coverageLines = new List<InsuranceInvoiceCoverageLine>();
            var savedLines = saved.InvoiceLines?.ToList() ?? new List<InvoiceLineWriteDto>();
            for (var i = 0; i < _calculation.Lines.Count; i++)
            {
                var calculated = _calculation.Lines[i];
                coverageLines.Add(new InsuranceInvoiceCoverageLine
                {
                    InsuranceInvoiceCoverageId = coverage.Id, InvoiceLineId = savedLines.ElementAtOrDefault(i)?.Id ?? 0,
                    ProductId = calculated.ProductId, Quantity = calculated.Quantity, ContractedUnitPrice = calculated.ContractedUnitPrice,
                    InsuranceDiscountPercent = calculated.InsuranceDiscountPercent, InsuranceDiscountAmount = calculated.InsuranceDiscountAmount,
                    GrossAmount = calculated.GrossAmount, CoveredAmount = calculated.CoveredAmount, PatientAmount = calculated.PatientAmount,
                    ExcludedAmount = calculated.ExcludedAmount, DeductibleAmount = calculated.DeductibleAmount, CopayAmount = calculated.CopayAmount,
                    ApprovedQuantity = calculated.ApprovedQuantity, RequiresAuthorization = calculated.RequiresAuthorization,
                    IsExcluded = calculated.IsExcluded, DecisionReason = calculated.DecisionReason,
                    CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now
                });
            }
            var coverageResult = await _insuranceService.SaveInvoiceCoverageAsync(coverage, coverageLines);
            if (!coverageResult.Success) { MessageBox.Show(coverageResult.Message); return; }
            await _insuranceService.CreateClaimAsync(saved.Id, coverage.Id);
            MessageBox.Show(UiText.T("تم حفظ فاتورة التأمين وإنشاء المطالبة.", "Insurance invoice and claim saved successfully."), UiText.T("نجاح", "Success"));
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UiText.T("خطأ", "Error"));
        }
        finally { _loading.Hide(); }
    }

    private bool TryParseIds(out int customerId, out int policyId, out int companyId, out int contractId, out int planId, out int versionId)
    {
        customerId = 0; policyId = 0; companyId = 0; contractId = 0; planId = 0; versionId = 0;
        return int.TryParse(CustomerIdTextBox.Text, out customerId)
            && int.TryParse(PolicyIdTextBox.Text, out policyId)
            && int.TryParse(CompanyIdTextBox.Text, out companyId)
            && int.TryParse(ContractIdTextBox.Text, out contractId)
            && int.TryParse(PlanIdTextBox.Text, out planId)
            && int.TryParse(VersionIdTextBox.Text, out versionId)
            && customerId > 0 && policyId > 0 && companyId > 0 && contractId > 0 && planId > 0 && versionId > 0;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
