using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Accounting.JournalEntries;
using RaccoonWarehouse.Domain.Accounting.Operations;
using RaccoonWarehouse.Domain.Invoices;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Insurance;
using RaccoonWarehouse.Domain.Reports.Accounting.Filters;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RaccoonWarehouse.Insurance;

public sealed class InsuranceCompanyStatementWindow : Window
{
    private readonly ApplicationDbContext _db;
    private readonly IAccountingService _accounting;
    private readonly InsuranceCompanyDto _company;
    private readonly DatePicker _from=new() { SelectedDate=new DateTime(DateTime.Today.Year,1,1),Width=150 };
    private readonly DatePicker _to=new() { SelectedDate=DateTime.Today,Width=150 };
    private readonly TextBox _postingStatus=new() { IsReadOnly=true,TextWrapping=TextWrapping.Wrap,
        Background=new SolidColorBrush(Color.FromRgb(255,246,218)),BorderThickness=new Thickness(0),
        Padding=new Thickness(12),Margin=new Thickness(0,0,0,12),MaxHeight=160,
        VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Visibility=Visibility.Collapsed };
    private readonly TextBlock _totals=new() { FontSize=17,Margin=new Thickness(8),TextWrapping=TextWrapping.Wrap };
    private readonly DataGrid _grid=new() { AutoGenerateColumns=false,IsReadOnly=true,CanUserAddRows=false,EnableRowVirtualization=true };
    private readonly Button _refresh;
    private bool _busy;
    public InsuranceCompanyStatementWindow(ApplicationDbContext db,IAccountingService accounting,InsuranceCompanyDto company)
    {
        _db=db; _accounting=accounting; _company=company;
        Title=UiText.T("كشف حساب شركة التأمين","Insurance company statement");
        Width=1150; Height=780; MinWidth=850; MinHeight=580; WindowStartupLocation=WindowStartupLocation.CenterOwner;
        FlowDirection=UiText.CurrentFlowDirection; Background=InsuranceUi.Background;
        SetResourceReference(FlowDirectionProperty,"CurrentFlowDirection");
        _grid.Style=TryFindResource("AppModernDataGridStyle") as Style;
        var layout=new DockPanel { Margin=new Thickness(24) };
        var hero=InsuranceUi.Hero(Title,company.Name); DockPanel.SetDock(hero,Dock.Top);layout.Children.Add(hero);
        var filters=new WrapPanel { Margin=new Thickness(0,8,0,12) };
        filters.Children.Add(new TextBlock { Text=UiText.T("من","From"),Margin=new Thickness(8) }); filters.Children.Add(_from);
        filters.Children.Add(new TextBlock { Text=UiText.T("إلى","To"),Margin=new Thickness(8) }); filters.Children.Add(_to);
        _refresh=InsuranceUi.Button(UiText.T("عرض","Show"),true); _refresh.Margin=new Thickness(12,0,0,0);
        _refresh.Click+=async(_,_)=>await LoadAsync(); filters.Children.Add(_refresh);
        DockPanel.SetDock(filters,Dock.Top);layout.Children.Add(filters);
        DockPanel.SetDock(_postingStatus,Dock.Top);layout.Children.Add(_postingStatus);
        DockPanel.SetDock(_totals,Dock.Bottom);layout.Children.Add(_totals);
        void Column(string a,string e,string path,int width=130)=>_grid.Columns.Add(new DataGridTextColumn {
            Header=UiText.T(a,e),Binding=new Binding(path) { StringFormat=path is "Debit" or "Credit" or "RunningBalance"?"N3":path=="EntryDate"?"yyyy-MM-dd":null },Width=width });
        Column("التاريخ","Date","EntryDate"); Column("القيد","Journal","EntryNumber",180);
        Column("البيان","Description","Description",350); Column("مدين","Debit","Debit");
        Column("دائن","Credit","Credit"); Column("الرصيد","Balance","RunningBalance");
        layout.Children.Add(InsuranceUi.Card(_grid));Content=layout;
        Loaded+=async(_,_)=>await LoadAsync();Closing+=(_,e)=>{if(_busy)e.Cancel=true;};
    }
    private async Task LoadAsync()
    {
        if(_busy)return; _busy=true;_refresh.IsEnabled=false;
        try
        {
            if(!_from.SelectedDate.HasValue || !_to.SelectedDate.HasValue || _from.SelectedDate>_to.SelectedDate)
                throw new ArgumentException(UiText.T("اختر فترة صحيحة.","Select a valid date range."));
            var id=await _db.InsuranceCompanies.AsNoTracking().Where(x=>x.Id==_company.Id).Select(x=>x.ReceivableAccountId).SingleAsync();
            await LoadPostingStatusAsync(id,_to.SelectedDate.Value.Date.AddDays(1));
            if(!id.HasValue) { _grid.ItemsSource=null; _totals.Text=UiText.T("لا توجد حركات محاسبية للشركة بعد.","No accounting movements exist for this company yet.");return; }
            var result=await _accounting.GetGeneralLedgerAsync(new GeneralLedgerFilterDto {
                AccountId=id,From=_from.SelectedDate.Value.Date,To=_to.SelectedDate.Value.Date.AddDays(1).AddTicks(-1),
                IncludeOpeningBalance=true,IncludePostedOnly=true });
            if(!result.Success)throw new InvalidOperationException(result.Message);
            var ledger=result.Data?.SingleOrDefault();_grid.ItemsSource=ledger?.Rows;
            _totals.Text=$"{UiText.T("رصيد افتتاحي","Opening balance")}: {ledger?.OpeningBalance??0m:0.000} | "
                +$"{UiText.T("مدين","Debit")}: {ledger?.TotalDebit??0m:0.000} | {UiText.T("دائن","Credit")}: {ledger?.TotalCredit??0m:0.000} | "
                +$"{UiText.T("الرصيد المستحق","Amount due")}: {ledger?.ClosingBalance??0m:0.000}";
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,Title);}
        finally{_busy=false;_refresh.IsEnabled=true;}
    }
    private async Task LoadPostingStatusAsync(int? accountId,DateTime until)
    {
        _postingStatus.Visibility=Visibility.Collapsed;_postingStatus.Clear();
        try
        {
            // Include older unposted invoices: they also explain an incorrect opening balance.
            var invoices=await _db.Set<Invoice>().AsNoTracking()
                .Where(x=>x.IsInsuranceSale&&x.InsuranceCompanyId==_company.Id&&x.InsuranceResponsibility>0m
                    &&(x.Status==InvoiceStatus.Completed||x.Status==InvoiceStatus.Posted)&&x.CreatedDate<until)
                .Select(x=>new{x.Id,x.InvoiceNumber}).ToListAsync();
            var ids=invoices.Select(x=>x.Id).ToList();
            if(ids.Count==0)return;
            var posted=await _db.Set<JournalEntry>().AsNoTracking()
                .Where(x=>x.ReferenceType=="Invoice"&&x.ReferenceId.HasValue&&ids.Contains(x.ReferenceId.Value)
                    &&x.Status==JournalEntryStatus.Posted&&x.Lines.Any(line=>line.AccountId==accountId&&line.Debit>0m))
                .Select(x=>x.ReferenceId!.Value).ToListAsync();
            var missing=invoices.Where(x=>!posted.Contains(x.Id)).ToList();
            if(missing.Count==0)return;
            var missingIds=missing.Select(x=>x.Id).ToList();
            var operations=await _db.Set<AccountingOperation>().AsNoTracking()
                .Where(x=>x.ReferenceType=="Invoice"&&x.OperationType=="PostInvoiceJournal"&&missingIds.Contains(x.ReferenceId))
                .OrderByDescending(x=>x.UpdatedDate).ToListAsync();
            var lines=new List<string> { UiText.T("هذه الفواتير لم يظهر لها قيد في حساب الشركة. الرصيد أدناه يشمل القيود المرحلة فقط.",
                "These invoices have no posting on this company's account. The balance below includes posted entries only.") };
            foreach(var invoice in missing.Take(20))
            {
                var op=operations.FirstOrDefault(x=>x.ReferenceId==invoice.Id);
                var state=op?.Status switch {
                    AccountingOperationStatus.Pending=>UiText.T("بانتظار الترحيل","Pending posting"),
                    AccountingOperationStatus.Processing=>UiText.T("جاري الترحيل","Posting in progress"),
                    AccountingOperationStatus.Failed=>UiText.T("فشل الترحيل","Posting failed"),
                    AccountingOperationStatus.Completed=>UiText.T("العملية مكتملة دون حركة في حساب هذه الشركة","Marked complete without a movement on this company's account"),
                    _=>UiText.T("لا توجد عملية ترحيل مسجلة","No posting operation found") };
                lines.Add($"{UiText.T("فاتورة","Invoice")} #{invoice.InvoiceNumber} (ID {invoice.Id}): {state}"
                    +(string.IsNullOrWhiteSpace(op?.LastError)?string.Empty:" — "+op.LastError));
            }
            if(missing.Count>20)lines.Add(UiText.T($"وفواتير أخرى: {missing.Count-20}",$"Additional invoices: {missing.Count-20}"));
            lines.Add(UiText.T("راجع العمليات المحاسبية لمعالجة الخطأ وإعادة المحاولة.","Review Accounting Operations to resolve the error and retry."));
            _postingStatus.Text=string.Join(Environment.NewLine,lines);_postingStatus.Visibility=Visibility.Visible;
        }
        catch(Exception ex)
        {
            _postingStatus.Text=UiText.T("تعذر فحص حالة الترحيل: ","Could not inspect posting status: ")+ex.Message;
            _postingStatus.Visibility=Visibility.Visible;
        }
    }

}
