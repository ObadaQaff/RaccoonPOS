using RaccoonWarehouse.Application.Service.Insurance;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Insurance;
using RaccoonWarehouse.Helpers.Localization;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace RaccoonWarehouse.Insurance;

public partial class InsuranceSettlementWindow : Window
{
    private readonly IInsuranceManagementService _service;
    private readonly ILoadingService _loading;
    private readonly ObservableCollection<InsuranceClaimSummaryDto> _claims = new();

    public InsuranceSettlementWindow(IInsuranceManagementService service, ILoadingService loading)
    {
        _service = service;
        _loading = loading;

        InitializeComponent();
        ClaimsGrid.ItemsSource = _claims;
        BatchFromDatePicker.SelectedDate = DateTime.Today.AddMonths(-1);
        BatchToDatePicker.SelectedDate = DateTime.Today;
        UiText.ApplyWindow(this);
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var canPay = true;
        var canBatch = true;
        PaymentClaimIdTextBox.IsEnabled = canPay;
        ReceiptNumberTextBox.IsEnabled = canPay;
        PaymentAmountTextBox.IsEnabled = canPay;
        PaymentCompanyIdTextBox.IsEnabled = canPay;
        PaymentReferenceTextBox.IsEnabled = canPay;
        await LoadAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading.Show();
        try
        {
            var result = await _service.GetClaimsAsync();
            if (!result.Success)
            {
                MessageBox.Show(result.Message, UiText.T("خطأ", "Error"));
                return;
            }
            _claims.Clear();
            foreach (var claim in result.Data ?? new List<InsuranceClaimSummaryDto>())
                if (claim.OutstandingAmount > 0)
                    _claims.Add(claim);
        }
        finally
        {
            _loading.Hide();
        }
    }

    private async void RecordPayment_Click(object sender, RoutedEventArgs e)
    {
        RaccoonWarehouse.Navigation.WindowManager.ShowDialog<RaccoonWarehouse.Vouchers.CreateVoucher>(
            RaccoonWarehouse.Navigation.WindowSizeType.LargeRectangle,
            window => window.InitializeInsuranceReceipt());
        await LoadAsync();
        return;

        if (!int.TryParse(PaymentClaimIdTextBox.Text, out var claimId)
            || !int.TryParse(PaymentCompanyIdTextBox.Text, out var companyId)
            || !decimal.TryParse(PaymentAmountTextBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount)
            || amount <= 0
            || string.IsNullOrWhiteSpace(ReceiptNumberTextBox.Text))
        {
            MessageBox.Show(UiText.T("أدخل رقم المطالبة والشركة والمبلغ ورقم الإيصال بشكل صحيح.", "Enter valid claim, company, amount, and receipt values."));
            return;
        }

        var result = await _service.RecordPaymentAsync(new InsuranceClaimPaymentWriteDto
        {
            InsuranceCompanyId = companyId,
            ReceiptNumber = ReceiptNumberTextBox.Text.Trim(),
            PaymentDate = DateTime.Today,
            Amount = amount,
            ReferenceNumber = PaymentReferenceTextBox.Text.Trim(),
            Allocations = new List<InsuranceClaimPaymentAllocationDto>
            {
                new() { InsuranceClaimId = claimId, Amount = amount }
            }
        });
        MessageBox.Show(result.Message, result.Success ? UiText.T("تم", "Success") : UiText.T("خطأ", "Error"));
        if (result.Success)
            await LoadAsync();
#pragma warning restore CS0162
    }

    private async void CreateBatch_Click(object sender, RoutedEventArgs e)
    {if (!int.TryParse(BatchCompanyIdTextBox.Text, out var companyId)
            || BatchFromDatePicker.SelectedDate is not DateTime from
            || BatchToDatePicker.SelectedDate is not DateTime to)
        {
            MessageBox.Show(UiText.T("أدخل الشركة والفترة بشكل صحيح.", "Enter a valid company and date range."));
            return;
        }

        var result = await _service.CreateClaimBatchAsync(companyId, from.Date, to.Date.AddDays(1).AddTicks(-1));
        MessageBox.Show(result.Message, result.Success ? UiText.T("تم", "Success") : UiText.T("خطأ", "Error"));
        if (result.Success)
            await LoadAsync();
    }

    private async void Aging_Click(object sender, RoutedEventArgs e)
    {var result = await _service.GetReceivableAgingAsync();
        if (!result.Success)
        {
            MessageBox.Show(result.Message, UiText.T("خطأ", "Error"));
            return;
        }

        var text = result.Data == null || result.Data.Count == 0
            ? UiText.T("لا توجد ذمم مستحقة.", "No outstanding receivables.")
            : string.Join(Environment.NewLine, result.Data.Select(x =>
                $"Company {x.InsuranceCompanyId} | {x.Bucket} | {x.Amount:0.000} | Claims: {x.ClaimCount}"));
        MessageBox.Show(text, UiText.T("أعمار الذمم", "Receivable aging"));
    }
}
