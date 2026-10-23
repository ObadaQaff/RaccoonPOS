using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Insurance;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Common.Loading;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Insurance;
using RaccoonWarehouse.Helpers.Localization;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;

namespace RaccoonWarehouse.Insurance;

public partial class InsuranceClaimsWindow : Window
{
    private readonly IInsuranceManagementService _service;
    private readonly ILoadingService _loading;
    private readonly IPermissionService _permissions;
    private readonly ApplicationDbContext _db;
    private readonly ObservableCollection<ClaimRow> _claims=new();
    private bool _busy;
    private bool _canReview;
    public sealed record StatusChoice(InsuranceClaimStatus? Value,string Label);
    public sealed class ClaimRow
    {
        public InsuranceClaimSummaryDto Claim { get; init; } = new();
        public string ClaimNumber=>Claim.ClaimNumber;
        public string InvoiceNumber { get; init; }=string.Empty;
        public string CompanyName { get; init; }=string.Empty;
        public string CustomerName { get; init; }=string.Empty;
        public string MemberNumber { get; init; }=string.Empty;
        public DateTime CreatedDate { get; init; }
        public string StatusLabel=>StatusText(Claim.Status);
        public decimal SubmittedAmount=>Claim.SubmittedAmount;
        public decimal ApprovedAmount=>Claim.ApprovedAmount;
        public decimal PaidAmount=>Claim.PaidAmount;
        public decimal OutstandingAmount=>Claim.OutstandingAmount;
    }
    public InsuranceClaimsWindow(IInsuranceManagementService service,ILoadingService loading,IPermissionService permissions,ApplicationDbContext db)
    {
        _service=service;_loading=loading;_permissions=permissions;_db=db;InitializeComponent();
        ClaimsGrid.ItemsSource=_claims;
        var choices=Enum.GetValues<InsuranceClaimStatus>().Select(x=>new StatusChoice(x,StatusText(x))).ToList();
        StatusComboBox.ItemsSource=choices.Where(x=>x.Value is InsuranceClaimStatus.Approved or InsuranceClaimStatus.PartiallyApproved or InsuranceClaimStatus.Rejected).ToList();
        StatusComboBox.SelectedIndex=0;
        ClaimStatusFilter.ItemsSource=new[]{new StatusChoice(null,UiText.T("الكل","All"))}.Concat(choices).ToList();
        ClaimStatusFilter.SelectedIndex=0;
        FromFilter.SelectedDate=DateTime.Today.AddYears(-1);ToFilter.SelectedDate=DateTime.Today;
        UiText.ApplyWindow(this);Closing+=(_,e)=>{if(_busy)e.Cancel=true;};
    }
    private static string StatusText(InsuranceClaimStatus s)=>s switch
    {
        InsuranceClaimStatus.Draft=>UiText.T("مسودة","Draft"),
        InsuranceClaimStatus.Prepared=>UiText.T("مجهزة","Prepared"),
        InsuranceClaimStatus.ReadyForSubmission=>UiText.T("جاهزة للإرسال","Ready for submission"),
        InsuranceClaimStatus.Submitted=>UiText.T("مرسلة","Submitted"),
        InsuranceClaimStatus.UnderReview=>UiText.T("قيد المراجعة","Under review"),
        InsuranceClaimStatus.Approved=>UiText.T("معتمدة","Approved"),
        InsuranceClaimStatus.PartiallyApproved=>UiText.T("معتمدة جزئياً","Partially approved"),
        InsuranceClaimStatus.Rejected=>UiText.T("مرفوضة","Rejected"),
        InsuranceClaimStatus.PartiallyPaid=>UiText.T("مدفوعة جزئياً","Partially paid"),
        InsuranceClaimStatus.PartiallySettled=>UiText.T("مسددة جزئياً","Partially settled"),
        InsuranceClaimStatus.Paid=>UiText.T("مدفوعة","Paid"),
        InsuranceClaimStatus.Closed=>UiText.T("مغلقة","Closed"),
        InsuranceClaimStatus.Cancelled=>UiText.T("ملغاة","Cancelled"),_=>s.ToString()
    };
    private async void Window_Loaded(object sender,RoutedEventArgs e)
    {
        try
        {
            _canReview=await _permissions.HasCurrentUserPermissionAsync("InsuranceClaims.Approve")
                ||await _permissions.HasCurrentUserPermissionAsync("InsuranceClaims.Reject");
            if(!await _permissions.HasCurrentUserPermissionAsync("InsuranceClaims.View")) { Close();return; }
            var companies=await _service.GetCompaniesAsync();
            if(!companies.Success)throw new InvalidOperationException(companies.Message);
            var list=new[]{new InsuranceCompanyDto { Id=0,Name=UiText.T("كل الشركات","All companies") }}.Concat(companies.Data??new()).ToList();
            InsuranceUi.Search(CompanyFilter,list,x=>x.Name);CompanyFilter.SelectedItem=list[0];
            await LoadAsync();
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,UiText.T("خطأ","Error"));}
    }
    private async void Refresh_Click(object sender,RoutedEventArgs e)=>await LoadAsync();
    private void ClaimsGrid_SelectionChanged(object sender,System.Windows.Controls.SelectionChangedEventArgs e)
    {
        var claim=(ClaimsGrid.SelectedItem as ClaimRow)?.Claim;
        SaveDecisionButton.IsEnabled=!_busy&&_canReview&&claim!=null
            &&claim.Status is not (InsuranceClaimStatus.Paid or InsuranceClaimStatus.Closed or InsuranceClaimStatus.Cancelled);
        ApprovedTextBox.Text=(claim==null?0m:claim.Status is InsuranceClaimStatus.Prepared or InsuranceClaimStatus.ReadyForSubmission or InsuranceClaimStatus.Submitted ? claim.SubmittedAmount : claim.ApprovedAmount).ToString("0.######");
        RejectedTextBox.Text=(claim?.RejectedAmount??0m).ToString("0.######");
        DeductionTextBox.Text=(claim?.DeductionAmount??0m).ToString("0.######");ReasonTextBox.Clear();
        if(claim!=null) StatusComboBox.SelectedItem=((IEnumerable<StatusChoice>)StatusComboBox.ItemsSource)
            .First(x=>x.Value==(claim.Status==InsuranceClaimStatus.Rejected?InsuranceClaimStatus.Rejected
                :claim.Status==InsuranceClaimStatus.PartiallyApproved?InsuranceClaimStatus.PartiallyApproved:InsuranceClaimStatus.Approved));
    }
    private async Task LoadAsync()
    {
        if(_busy)return;_busy=true;RefreshButton.IsEnabled=SaveDecisionButton.IsEnabled=false;_loading.Show();
        try
        {
            if(!FromFilter.SelectedDate.HasValue||!ToFilter.SelectedDate.HasValue||FromFilter.SelectedDate>ToFilter.SelectedDate)
                throw new ArgumentException(UiText.T("اختر فترة صحيحة.","Select a valid date range."));
            if(CompanyFilter.SelectedItem is not InsuranceCompanyDto company || company.Name!=CompanyFilter.Text)
                throw new ArgumentException(UiText.T("اختر شركة من القائمة.","Select a company from the list."));
            var status=(ClaimStatusFilter.SelectedItem as StatusChoice)?.Value;
            var result=await _service.GetClaimsAsync(company.Id>0?company.Id:null,status);
            if(!result.Success)throw new InvalidOperationException(result.Message);
            var periodFrom=FromFilter.SelectedDate.Value.Date;var to=ToFilter.SelectedDate.Value.Date.AddDays(1);
            var claimIds=(result.Data??new List<InsuranceClaimSummaryDto>()).Select(x=>x.Id).ToList();
            var records=await (from c in _db.InsuranceClaims.AsNoTracking()
                join coverage in _db.InsuranceInvoiceCoverages.AsNoTracking() on c.InsuranceInvoiceCoverageId equals coverage.Id
                join insurer in _db.InsuranceCompanies.AsNoTracking() on c.InsuranceCompanyId equals insurer.Id
                join invoice in _db.Set<RaccoonWarehouse.Domain.Invoices.Invoice>().AsNoTracking() on c.InvoiceId equals invoice.Id
                where claimIds.Contains(c.Id)&&c.CreatedDate>=periodFrom&&c.CreatedDate<to
                select new { c.Id,c.CreatedDate,insurer.Name,invoice.InvoiceNumber,coverage.SnapshotJson,
                    invoice.CustomerId, c.InsuranceMemberPolicyId }).ToDictionaryAsync(x=>x.Id);
            var customerIds=records.Values.Where(x=>x.CustomerId.HasValue).Select(x=>x.CustomerId!.Value).Distinct().ToList();
            var customers=await _db.Set<RaccoonWarehouse.Domain.Users.User>().AsNoTracking().Where(x=>customerIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,x=>x.Name);
            var policyIds=records.Values.Where(x=>x.InsuranceMemberPolicyId.HasValue).Select(x=>x.InsuranceMemberPolicyId!.Value).ToList();
            var members=await _db.InsuranceMemberPolicies.AsNoTracking().Where(x=>policyIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,x=>x.MemberNumber);
            _claims.Clear();
            foreach(var claim in result.Data??new List<InsuranceClaimSummaryDto>())
            {
                if(!records.TryGetValue(claim.Id,out var r))continue;
                PosInsuranceBillingDto? snapshot=null;
                if(!string.IsNullOrWhiteSpace(r.SnapshotJson))try{snapshot=JsonSerializer.Deserialize<PosInsuranceBillingDto>(r.SnapshotJson);}catch(JsonException){}
                _claims.Add(new ClaimRow { Claim=claim,CreatedDate=r.CreatedDate,CompanyName=r.Name,InvoiceNumber=r.InvoiceNumber,
                    CustomerName=snapshot?.CustomerName??(r.CustomerId.HasValue?customers.GetValueOrDefault(r.CustomerId.Value):null)??"—",
                    MemberNumber=snapshot?.MemberNumber??(r.InsuranceMemberPolicyId.HasValue?members.GetValueOrDefault(r.InsuranceMemberPolicyId.Value):null)??"—" });
            }
            ClaimsEmptyStatePanel.Visibility=_claims.Count==0?Visibility.Visible:Visibility.Collapsed;
            ClaimsTotals.Text=$"{UiText.T("المطالبات","Claims")}: {_claims.Count} | {UiText.T("المقدم","Submitted")}: {_claims.Sum(x=>x.SubmittedAmount):0.000} | "
                +$"{UiText.T("المدفوع","Paid")}: {_claims.Sum(x=>x.PaidAmount):0.000} | {UiText.T("المتبقي","Outstanding")}: {_claims.Sum(x=>x.OutstandingAmount):0.000}";
        }
        catch(Exception ex){MessageBox.Show(this,UiText.T("تعذر تحميل المطالبات.","Could not load claims.")+"\n"+ex.Message,Title);}
        finally{_loading.Hide();_busy=false;RefreshButton.IsEnabled=true;SaveDecisionButton.IsEnabled=false;}
    }
    private async void SaveDecision_Click(object sender,RoutedEventArgs e)
    {
        if(_busy||ClaimsGrid.SelectedItem is not ClaimRow row)return;
        if(!decimal.TryParse(ApprovedTextBox.Text,out var approved)||!decimal.TryParse(RejectedTextBox.Text,out var rejected)
            ||!decimal.TryParse(DeductionTextBox.Text,out var deduction)||StatusComboBox.SelectedItem is not StatusChoice choice||!choice.Value.HasValue)
        {MessageBox.Show(this,UiText.T("أدخل مبالغ صحيحة.","Enter valid amounts."));return;}
        _busy=true;RefreshButton.IsEnabled=SaveDecisionButton.IsEnabled=false;
        try
        {
            var key=choice.Value==InsuranceClaimStatus.Rejected?"InsuranceClaims.Reject":"InsuranceClaims.Approve";
            if(!await _permissions.HasCurrentUserPermissionAsync(key))throw new InvalidOperationException(UiText.T("لا تملك صلاحية تسجيل هذا القرار.","You do not have permission to record this decision."));
            _loading.Show();
            var result=await _service.ReviewClaimAsync(new InsuranceClaimReviewDto { ClaimId=row.Claim.Id,Status=choice.Value.Value,
                ApprovedAmount=approved,RejectedAmount=rejected,DeductionAmount=deduction,RejectionReason=ReasonTextBox.Text });
            if(!result.Success)throw new InvalidOperationException(result.Message);
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,Title);}
        finally{_loading.Hide();_busy=false;RefreshButton.IsEnabled=true;}
        await LoadAsync();
    }
}
