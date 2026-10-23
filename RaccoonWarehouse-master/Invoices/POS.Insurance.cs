using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Insurance;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Insurance;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using RaccoonWarehouse.Insurance;
using System.Windows;

namespace RaccoonWarehouse.Invoices;

public partial class POS
{
    private PosInsuranceBillingDto? _posInsuranceDetails;
    private InvoiceWriteDto? _insuranceBasket;
    private bool _insurancePaymentReady;
    private bool _openingInsurancePayment;

    private async Task OpenPosInsuranceDetailsAsync()
    {
        if(_isProcessingPayment || _openingInsurancePayment) return;
        CommitActiveInvoiceEdits(); RecalculateTotals();
        if(_currentInvoice.InvoiceType!=InvoiceType.Sale || _currentInvoice.IsInsuranceSale || _currentInvoice.TotalAmount<=0m)
        { MessageBox.Show(this,UiText.T("أضف أصنافاً إلى فاتورة بيع جديدة أولاً.","Add items to a new sales invoice first.")); return; }
        var window=new PosInsuranceDetailsWindow(_serviceProvider.GetRequiredService<IInsuranceManagementService>(),
            _allCustomers ?? Enumerable.Empty<UserReadDto>(),Math.Round(_currentInvoice.TotalAmount,3),_posInsuranceDetails,_currentInvoice.CustomerId) { Owner=this };
        if(window.ShowDialog()!=true) return;
        _posInsuranceDetails=window.Removed?null:window.Details; _insuranceBasket=_currentInvoice;
        _currentInvoice.Payments=new List<InvoicePaymentWriteDto>(); RecalculateTotals();
        await Task.CompletedTask;
    }

    private void UpdateInsuranceSummary()
    {
        if(!ReferenceEquals(_insuranceBasket,_currentInvoice) || _currentInvoice.InvoiceType!=InvoiceType.Sale)
            _posInsuranceDetails=null;
        if(_posInsuranceDetails==null) { NetTotalText.ToolTip=null; return; }
        if (_currentInvoice.TotalAmount < 0m)
        { StatusTotal.Text=UiText.T("تحقق من كميات وأسعار الفاتورة.","Check invoice quantities and prices."); return; }
        var c=PosInsuranceBillingService.Calculate(Math.Round(_currentInvoice.TotalAmount,3),
            _posInsuranceDetails.CustomerDiscountPercent,_posInsuranceDetails.ContractualDiscountPercent);
        NetTotalText.Text=c.InvoiceTotal.ToString("0.000");
        TotalTextBlock.Text=c.InvoiceTotal.ToString("0.000");
        var summary=$"{UiText.T("المريض","Patient")}: {c.PatientAmount:0.000} | {UiText.T("التأمين","Insurance")}: {c.InsurerAmount:0.000} | {UiText.T("الخصم التعاقدي","Contractual discount")}: {c.ContractualDiscountAmount:0.000}";
        NetTotalText.ToolTip=summary; StatusTotal.Text=summary;
    }

    private async Task OpenInsurancePaymentAsync()
    {
        if(_posInsuranceDetails==null || _openingInsurancePayment || _isProcessingPayment) return;
        _openingInsurancePayment=true;
        try
        {
            CommitActiveInvoiceEdits();
            if(DiscountTextBox.IsKeyboardFocusWithin && !ApplyInvoiceDiscountInput()) return;
            if(!ValidateDistributedInvoiceDiscount() || !await EnsureAllFractionalSalesConfiguredAsync()) return;
            PrepareInvoiceForSave();
            var c=PosInsuranceBillingService.Calculate(Math.Round(_currentInvoice.TotalAmount,3),
                _posInsuranceDetails.CustomerDiscountPercent,_posInsuranceDetails.ContractualDiscountPercent);
            if(c.OriginalTotal<=0m) return;
            var payments=new List<InvoicePaymentWriteDto>();
            if(c.PatientAmount>0m)
            {
                var allowed=new[]{PaymentType.Cash,PaymentType.Visa,PaymentType.Master,PaymentType.Debit,PaymentType.Check,PaymentType.MobilePayment}
                    .ToDictionary(x=>x,x=>c.PatientAmount);
                var window=new ChangeInvoicePaymentWindow(null,_allCustomers ?? Enumerable.Empty<UserReadDto>(),
                    _posInsuranceDetails.CustomerId,c.PatientAmount,allowedPaymentAmounts:allowed) { Owner=this,
                    Title=UiText.T("دفع حصة المريض","Patient payment") };
                if(window.ShowDialog()!=true) return;
                payments=window.SelectedPayments.Select(x=>new InvoicePaymentWriteDto { PaymentType=x.PaymentType,Amount=x.Amount }).ToList();
            }
            _currentInvoice.Payments=payments;
            _currentInvoice.PaymentType=payments.FirstOrDefault()?.PaymentType ?? PaymentType.InsuranceReceivable;
            _insurancePaymentReady=true;
            try { await ProcessPaymentAsync(_currentInvoice.PaymentType.Value); }
            finally { _insurancePaymentReady=false; }
        }
        catch(Exception ex) { MessageBox.Show(this,UiText.T("تعذر تجهيز دفع التأمين.","Could not prepare insurance payment.")+"\n"+ex.Message); }
        finally { _openingInsurancePayment=false; }
    }
}
