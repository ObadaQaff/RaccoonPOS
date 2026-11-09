using RaccoonWarehouse.Application.Service.Insurance;
using RaccoonWarehouse.Domain.Insurance;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RaccoonWarehouse.Insurance;

public sealed class PosInsuranceDetailsWindow : Window
{
    private readonly IInsuranceManagementService _service;
    private readonly decimal _total;
    private readonly ComboBox _company = new() { IsEditable = true, IsTextSearchEnabled = false, StaysOpenOnEdit = true };
    private readonly ComboBox _customer = new() { IsEditable = true, IsTextSearchEnabled = false, StaysOpenOnEdit = true };
    private readonly TextBox _phone = new();
    private readonly TextBox _member = new();
    private readonly TextBox _policy = new();
    private readonly TextBox _percent = new();
    private readonly TextBlock _contract = new();
    private readonly TextBlock _summary = new() { FontSize = 18, TextWrapping = TextWrapping.Wrap };
    private readonly Button _apply;
    private readonly PosInsuranceBillingDto? _original;
    private bool _busy;
    public PosInsuranceBillingDto? Details { get; private set; }
    public bool Removed { get; private set; }

    public PosInsuranceDetailsWindow(IInsuranceManagementService service, IEnumerable<UserReadDto> customers,
        decimal total, PosInsuranceBillingDto? current, int? customerId)
    {

        _service = service; _total = total; _original = current;
        Title = UiText.T("بيانات التأمين", "Insurance details");
        Width = 900; Height = 730; MinWidth = 720; MinHeight = 620;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = InsuranceUi.Background; FlowDirection = UiText.CurrentFlowDirection;
        SetResourceReference(FlowDirectionProperty, "CurrentFlowDirection");
        var layout = new DockPanel { Margin = new Thickness(24) };
        var hero = InsuranceUi.Hero(Title, UiText.T("أدخل بيانات المريض واختر شركة التأمين", "Enter patient details and select the insurance company"));
        DockPanel.SetDock(hero, Dock.Top); layout.Children.Add(hero);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,16,0,0) };
        _apply = InsuranceUi.Button(UiText.T("تطبيق التأمين", "Apply insurance"), true);
        _apply.IsEnabled = false; _apply.Click += Apply;
        var remove = InsuranceUi.Button(UiText.T("إزالة التأمين", "Remove insurance"));
        remove.IsEnabled = current != null; remove.Click += (_,_) => { Removed = true; DialogResult = true; };
        var cancel = InsuranceUi.Button(UiText.T("إلغاء", "Cancel")); cancel.Click += (_,_) => DialogResult = false;
        actions.Children.Add(_apply); actions.Children.Add(remove); actions.Children.Add(cancel);
        DockPanel.SetDock(actions, Dock.Bottom); layout.Children.Add(actions);
        var fields = new Grid(); fields.ColumnDefinitions.Add(new()); fields.ColumnDefinitions.Add(new());
        for (var i=0;i<5;i++) fields.RowDefinitions.Add(new() { Height = GridLength.Auto });
        void Field(int row, int column, string label, FrameworkElement input)
        {
            var panel = new StackPanel { Margin = new Thickness(10,8,10,8) };
            panel.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0,0,0,7) });
            input.MinHeight = 40; panel.Children.Add(input); Grid.SetRow(panel,row); Grid.SetColumn(panel,column); fields.Children.Add(panel);
        }
        Field(0,0,UiText.T("اسم المريض *", "Patient name *"),_customer);
        Field(0,1,UiText.T("الهاتف", "Phone"),_phone);
        Field(1,0,UiText.T("شركة التأمين *", "Insurance company *"),_company);
        Field(1,1,UiText.T("رقم العضوية *", "Member number *"),_member);
        Field(2,0,UiText.T("رقم البوليصة", "Policy number"),_policy);
        Field(2,1,UiText.T("نسبة خصم العميل (%) *", "Customer discount (%) *"),_percent);
        Field(3,0,UiText.T("الخصم التعاقدي للشركة (%)", "Company contractual discount (%)"),_contract);
        var summaryCard = InsuranceUi.Card(_summary); Grid.SetRow(summaryCard,4); Grid.SetColumnSpan(summaryCard,2); fields.Children.Add(summaryCard);
        layout.Children.Add(InsuranceUi.Card(new ScrollViewer { Content = fields, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }));
        Content = layout;
        var customerList = customers.ToList();
        InsuranceUi.Search(_customer,customerList,x=>x.Name ?? string.Empty);
        _customer.DisplayMemberPath = "Name";
        _customer.SelectedItem = customerList.FirstOrDefault(x=>x.Id == (current?.CustomerId ?? customerId));
        if (current != null) _customer.Text = current.CustomerName;
        _phone.Text = current?.Phone ?? string.Empty; _member.Text = current?.MemberNumber ?? string.Empty;
        _policy.Text = current?.PolicyNumber ?? string.Empty; _percent.Text = (current?.CustomerDiscountPercent ?? 0m).ToString("0.######");
        _percent.TextChanged += (_,_) => UpdateSummary();
        _company.SelectionChanged += (_,_) => Dispatcher.BeginInvoke(new Action(UpdateSummary));
        _company.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler((_,_)=>UpdateSummary()));
        Loaded += Load;
        Closing += (_,e)=> { if (_busy) e.Cancel=true; };
    }

    private InsuranceCompanyDto? SelectedCompany => _company.SelectedItem is InsuranceCompanyDto c
        && string.Equals(_company.Text,c.Name,StringComparison.OrdinalIgnoreCase) ? c : null;
    private async void Load(object sender, RoutedEventArgs e)
    {
        _busy=true;
        try
        {
            var result=await _service.GetCompaniesAsync(activeOnly:true);
            if (!result.Success) throw new InvalidOperationException(result.Message);
            var companies=result.Data ?? new List<InsuranceCompanyDto>();
            _company.DisplayMemberPath="Name";
            InsuranceUi.Search(_company,companies,x=>x.Name);
            _company.SelectedItem=companies.FirstOrDefault(x=>x.Id==_original?.InsuranceCompanyId);
            UpdateSummary();
        }
        catch(Exception ex) { MessageBox.Show(this,UiText.T("تعذر تحميل شركات التأمين.","Could not load insurance companies.")+"\n"+ex.Message,Title); }
        finally { _busy=false; }
    }
    private void UpdateSummary()
    {
        _apply.IsEnabled=false;
        var company=SelectedCompany; _contract.Text=company?.ContractualDiscountPercent.ToString("0.######") ?? "—";
        if(company==null || !decimal.TryParse(_percent.Text,out var percent))
        { _summary.Text=UiText.T("اختر شركة التأمين وأدخل نسبة خصم صحيحة.","Select an insurance company and enter a valid discount percentage."); return; }
        if (_total < 0m || percent < 0m || percent > 100m
            || company.ContractualDiscountPercent < 0m || company.ContractualDiscountPercent > percent)
        {
            _summary.Text=UiText.T("النسب من 0 إلى 100، والخصم التعاقدي لا يتجاوز خصم العميل.","Percentages must be 0–100; contractual discount cannot exceed customer discount.");
            return;
        }
        try
        {
            var c=PosInsuranceBillingService.Calculate(_total,percent,company.ContractualDiscountPercent);
            _summary.Text=$"{UiText.T("الإجمالي الأصلي","Original total")}: {c.OriginalTotal:0.000}   |   {UiText.T("خصم العميل","Customer discount")}: {c.CustomerDiscountAmount:0.000}\n"
                +$"{UiText.T("يدفع المريض","Patient pays")}: {c.PatientAmount:0.000}   |   {UiText.T("الخصم التعاقدي","Contractual discount")}: {c.ContractualDiscountAmount:0.000}\n"
                +$"{UiText.T("ذمة شركة التأمين","Insurance receivable")}: {c.InsurerAmount:0.000}   |   {UiText.T("صافي الفاتورة","Invoice total")}: {c.InvoiceTotal:0.000}";
            _apply.IsEnabled=true;
        }
        catch(ArgumentException) { _summary.Text=UiText.T("النسب من 0 إلى 100، والخصم التعاقدي لا يتجاوز خصم العميل.","Percentages must be 0–100; contractual discount cannot exceed customer discount."); }
    }
    private void Apply(object sender,RoutedEventArgs e)
    {
        if(SelectedCompany is not InsuranceCompanyDto company || !decimal.TryParse(_percent.Text,out var percent)
            || string.IsNullOrWhiteSpace(_customer.Text) || string.IsNullOrWhiteSpace(_member.Text))
        { MessageBox.Show(this,UiText.T("أكمل اسم المريض ورقم العضوية وشركة التأمين والنسبة.","Complete patient name, member number, insurance company and percentage."),Title); return; }
        if (_total < 0m || percent < 0m || percent > 100m
            || company.ContractualDiscountPercent < 0m || company.ContractualDiscountPercent > percent)
        { UpdateSummary(); return; }
        try { Details=PosInsuranceBillingService.Calculate(_total,percent,company.ContractualDiscountPercent); }
        catch(ArgumentException) { UpdateSummary(); return; }
        Details.InsuranceCompanyId=company.Id; Details.CustomerName=_customer.Text.Trim();
        Details.CustomerId=_customer.SelectedItem is UserReadDto u && string.Equals(u.Name,_customer.Text,StringComparison.OrdinalIgnoreCase) ? u.Id : null;
        Details.Phone=_phone.Text.Trim(); Details.MemberNumber=_member.Text.Trim(); Details.PolicyNumber=_policy.Text.Trim();
        DialogResult=true;
    }
}

internal static class InsuranceUi
{
    internal static Brush Background => new SolidColorBrush(Color.FromRgb(237,242,245));
    internal static Border Card(UIElement content) => new() { Background=Brushes.White,CornerRadius=new CornerRadius(18),Padding=new Thickness(18),Margin=new Thickness(0,8,0,0),Child=content };
    internal static Border Hero(string title,string subtitle)
    {
        var p=new StackPanel(); p.Children.Add(new TextBlock { Text=title,FontSize=28,FontWeight=FontWeights.Bold,Foreground=Brushes.White });
        p.Children.Add(new TextBlock { Text=subtitle,Margin=new Thickness(0,8,0,0),Foreground=Brushes.White,TextWrapping=TextWrapping.Wrap });
        return new Border { Background=new SolidColorBrush(Color.FromRgb(20,85,76)),CornerRadius=new CornerRadius(20),Padding=new Thickness(24),Margin=new Thickness(0,0,0,12),Child=p };
    }
    internal static Button Button(string text,bool primary=false) => new() { Content=text,Padding=new Thickness(18,10,18,10),MinHeight=42,Margin=new Thickness(0,0,12,0),
        Background=primary?new SolidColorBrush(Color.FromRgb(20,85,76)):Brushes.White,Foreground=primary?Brushes.White:Brushes.Black };
    internal static void Search<T>(ComboBox combo,List<T> all,Func<T,string> name)
    {
        combo.ItemsSource=all; var filtering=false;
        combo.AddHandler(TextBox.TextChangedEvent,new TextChangedEventHandler((_,_)=>
        {
            if(filtering || !combo.IsKeyboardFocusWithin || combo.SelectedItem is T selected && name(selected)==combo.Text) return;
            var text=combo.Text;
            filtering=true;
            try
            {
                var editor=combo.Template.FindName("PART_EditableTextBox",combo) as TextBox;
                var caret=editor?.CaretIndex ?? text.Length;
                combo.SelectedItem=null;
                combo.ItemsSource=all.Where(x=>name(x).Contains(text,StringComparison.OrdinalIgnoreCase)).Take(30).ToList();
                combo.Text=text; combo.IsDropDownOpen=true;
                if(editor!=null) editor.CaretIndex=Math.Min(caret,text.Length);
            }
            finally { filtering=false; }
        }));
    }
}
