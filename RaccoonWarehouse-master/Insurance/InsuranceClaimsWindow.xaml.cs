using RaccoonWarehouse.Application.Service.Insurance;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Domain.Insurance;
using RaccoonWarehouse.Helpers.Localization;
using System.Collections.ObjectModel;
using System.Windows;

namespace RaccoonWarehouse.Insurance;

public partial class InsuranceClaimsWindow : Window
{
    private readonly IInsuranceManagementService _service;
    private readonly ILoadingService _loading;
    private readonly IPermissionService _permissions;
    private readonly ObservableCollection<InsuranceClaimSummaryDto> _claims = new();

    public InsuranceClaimsWindow(IInsuranceManagementService service, ILoadingService loading, IPermissionService permissions)
    {
        _service = service;
        _loading = loading;
        _permissions = permissions;
        InitializeComponent();
        ClaimsGrid.ItemsSource = _claims;
        StatusComboBox.ItemsSource = new[]
        {
            InsuranceClaimStatus.PartiallyApproved,
            InsuranceClaimStatus.Approved,
            InsuranceClaimStatus.Rejected
        };
        StatusComboBox.SelectedItem = InsuranceClaimStatus.Approved;
        UiText.ApplyWindow(this);
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var canApprove = await _permissions.HasCurrentUserPermissionAsync("InsuranceClaims.Approve");
        var canReject = await _permissions.HasCurrentUserPermissionAsync("InsuranceClaims.Reject");
        SaveDecisionButton.IsEnabled = canApprove || canReject;
        await LoadAsync();
    }
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private void ClaimsGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ClaimsGrid.SelectedItem is not InsuranceClaimSummaryDto claim)
            return;
        ApprovedTextBox.Text = claim.ApprovedAmount.ToString("0.######");
        RejectedTextBox.Text = claim.RejectedAmount.ToString("0.######");
        DeductionTextBox.Text = claim.DeductionAmount.ToString("0.######");
        StatusComboBox.SelectedItem = claim.Status == InsuranceClaimStatus.Rejected
            ? InsuranceClaimStatus.Rejected
            : claim.ApprovedAmount > 0 ? InsuranceClaimStatus.Approved : InsuranceClaimStatus.PartiallyApproved;
    }

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
                _claims.Add(claim);
            ClaimsEmptyStatePanel.Visibility = _claims.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        finally
        {
            _loading.Hide();
        }
    }

    private async void SaveDecision_Click(object sender, RoutedEventArgs e)
    {
        if (ClaimsGrid.SelectedItem is not InsuranceClaimSummaryDto claim)
        {
            MessageBox.Show(UiText.T("اختر مطالبة أولاً.", "Select a claim first."));
            return;
        }
        if (!decimal.TryParse(ApprovedTextBox.Text, out var approved)
            || !decimal.TryParse(RejectedTextBox.Text, out var rejected)
            || !decimal.TryParse(DeductionTextBox.Text, out var deduction))
        {
            MessageBox.Show(UiText.T("أدخل مبالغ صحيحة.", "Enter valid amounts."));
            return;
        }

        var selectedStatus = (InsuranceClaimStatus)StatusComboBox.SelectedItem!;
        var permissionKey = selectedStatus == InsuranceClaimStatus.Rejected
            ? "InsuranceClaims.Reject"
            : "InsuranceClaims.Approve";
        if (!await _permissions.HasCurrentUserPermissionAsync(permissionKey))
        {
            MessageBox.Show(UiText.T("لا تملك صلاحية تسجيل هذا القرار.", "You do not have permission to record this decision."));
            return;
        }

        var result = await _service.ReviewClaimAsync(new InsuranceClaimReviewDto
        {
            ClaimId = claim.Id,
            Status = selectedStatus,
            ApprovedAmount = approved,
            RejectedAmount = rejected,
            DeductionAmount = deduction,
            RejectionReason = ReasonTextBox.Text
        });
        MessageBox.Show(result.Message, result.Success ? UiText.T("تم", "Success") : UiText.T("خطأ", "Error"));
        if (result.Success)
            await LoadAsync();
    }
}
