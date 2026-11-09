using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RaccoonWarehouse.Application.Service.Payroll;
using RaccoonWarehouse.Domain.Payroll;
namespace RaccoonWarehouse.Employees.Payroll;

public sealed class PayrollPolicyWindow : Window
{
    private readonly EmployeePayrollService _service;private readonly DataGrid _grid=new(){AutoGenerateColumns=false,IsReadOnly=true,CanUserAddRows=false,MinHeight=160};
    public PayrollPolicyWindow(EmployeePayrollService service) : this(service, true)
    {
    }

    internal PayrollPolicyWindow(EmployeePayrollService service,bool enforcePermission)
    {
        if(enforcePermission)RaccoonWarehouse.Security.PermissionWindowGuard.Demand(GetType());
        _service=service;Title="إصدارات سياسات الرواتب والضمان";Width=1100;Height=650;FlowDirection=FlowDirection.RightToLeft;WindowStartupLocation=WindowStartupLocation.CenterScreen;
        var root=new DockPanel{Margin=new Thickness(20)};var top=PayrollUi.Fields();top.Children.Add(new TextBlock{Text=Title,FontSize=24,FontWeight=FontWeights.Bold});top.Children.Add(new TextBlock{Text="كل تعديل مالي إصدار مؤرخ جديد. راجع العقد وطريقة أجر الساعة والخصم والسقف وفئة الشمول من المصادر الرسمية قبل اعتماد السياسة.",TextWrapping=TextWrapping.Wrap});var buttons=new WrapPanel();buttons.Children.Add(PayrollUi.AsyncButton("تحميل",LoadAsync));buttons.Children.Add(PayrollUi.AsyncButton("إضافة إصدار من السياسة المحددة",AddAsync));buttons.Children.Add(PayrollUi.AsyncButton("توثيق مراجعة السياسة",ReviewAsync));top.Children.Add(buttons);DockPanel.SetDock(top,Dock.Top);root.Children.Add(top);
        Add("اسم السياسة","Name");Add("الفئة","Category");Add("السريان","EffectiveFrom","yyyy-MM-dd");Add("حتى","EffectiveTo","yyyy-MM-dd");Add("نسبة الموظف","EmployeeSscPercent","0.##");Add("نسبة الشركة","EmployerSscPercent","0.##");Add("سقف الأجر (د.أ)","InsurableWageCeiling","0.000");Add("مصدر التحقق","SourceReference");root.Children.Add(_grid);Content=root;Loaded+=async(_,_)=>{try{await _service.Require("Payroll.Configure");await LoadAsync();}catch(Exception ex){PayrollUi.Error(ex);Close();}};
    }
    private void Add(string title,string path,string? format=null)=>_grid.Columns.Add(new DataGridTextColumn{Header=title,Binding=new Binding(path){StringFormat=format},Width=new DataGridLength(1,DataGridLengthUnitType.Star)});
    public Task RefreshAsync()=>LoadAsync();
    private async Task LoadAsync()=>_grid.ItemsSource=await _service.PoliciesAsync();
    private async Task ReviewAsync(){if(_grid.SelectedItem is not PayrollPolicy row)throw new ArgumentException("اختر السياسة.");var reason=PayrollUi.Reason("مرجع مراجعة السياسة والعقد وقواعد الضمان");if(reason==null)return;await _service.ReviewPolicyAsync(row.Id,reason);await LoadAsync();}
    private async Task AddAsync()
    {
        if(_grid.SelectedItem is not PayrollPolicy selected)throw new ArgumentException("اختر سياسة لنسخ إعداداتها إلى إصدار جديد.");
        var policy=JsonSerializer.Deserialize<PayrollPolicy>(JsonSerializer.Serialize(selected))!;policy.Id=0;policy.EffectiveFrom=selected.EffectiveFrom.AddMonths(1);
        var w=new Window{Title="إصدار سياسة راتب وضمان جديد",Width=780,Height=820,FlowDirection=FlowDirection.RightToLeft,WindowStartupLocation=WindowStartupLocation.CenterScreen};var panel=PayrollUi.Fields();
        TextBox name=PayrollUi.Number(policy.Name),category=PayrollUi.Number(policy.Category),source=PayrollUi.Number(policy.SourceReference);DatePicker from=new DatePicker{SelectedDate=policy.EffectiveFrom},to=new DatePicker{SelectedDate=policy.EffectiveTo};PayrollUi.Field(panel,"اسم السياسة",name);PayrollUi.Field(panel,"فئة الشمول (تحدد سياسة الاستثناء إن وجدت)",category);PayrollUi.Field(panel,"تاريخ السريان",from);PayrollUi.Field(panel,"تاريخ نهاية السريان",to);
        var values=new Dictionary<string,TextBox>();
        foreach(var pair in new Dictionary<string,string>{{"SalaryDayDivisor","قاسم الأجر اليومي وخصم الإجازة"},{"EmployeeSscPercent","نسبة الموظف للضمان (%)"},{"EmployerSscPercent","نسبة الشركة للضمان (%)"},{"InsurableWageCeiling","سقف الأجر الخاضع للضمان (د.أ)"},{"LegalDailyHours","الحد القانوني للساعات اليومية العادية"},{"LegalWeeklyHours","الحد القانوني للساعات الأسبوعية العادية"},{"MinimumNormalMultiplier","أدنى معامل إضافي عادي"},{"MinimumHolidayMultiplier","أدنى معامل إضافي العطل"},{"AdvanceDeductionMaximumPercent","حد قسط استرداد السلفة (%)"},{"DefaultDailyHours","الساعات اليومية المقترحة"},{"DefaultWorkingDaysPerWeek","أيام العمل الأسبوعية المقترحة"},{"DefaultMonthlyHours","الساعات الشهرية المقترحة"},{"WeekStartsOn","بداية أسبوع الجدول الافتراضي (الأحد 0 إلى السبت 6)"}})
        {var box=PayrollUi.Number(System.Convert.ToString(typeof(PayrollPolicy).GetProperty(pair.Key)!.GetValue(policy),System.Globalization.CultureInfo.InvariantCulture)??"");box.Tag=pair.Value;values[pair.Key]=box;PayrollUi.Field(panel,pair.Value,box);}
        var prorate=PayrollUi.Choices(new[]{new PayrollUi.Choice(ProrationMethod.CalendarMonth,"الأيام المستحقة ÷ أيام الشهر الفعلية"),new PayrollUi.Choice(ProrationMethod.FixedDivisor,"الأيام المستحقة ÷ قاسم الأجر المعتمد")});prorate.SelectedValue=policy.ProrationMethod;PayrollUi.Field(panel,"توزيع الراتب للفترة الجزئية",prorate);var fixedBasis=new CheckBox{Content="إدخال البدلات الثابتة في أساس أجر اليوم والساعة وخصم الإجازة",IsChecked=policy.IncludeFixedAllowancesInWageBasis};panel.Children.Add(fixedBasis);PayrollUi.Field(panel,"مرجع رسمي موثق للمعاملات والسقف وطريقة الأجر",source);
        panel.Children.Add(PayrollUi.AsyncButton("حفظ الإصدار الجديد",async()=>{
            foreach(var pair in values){var property=typeof(PayrollPolicy).GetProperty(pair.Key)!;var value=PayrollUi.Decimal(pair.Value,pair.Value.Tag?.ToString()??"القيمة");if(property.PropertyType==typeof(int)){if(value!=decimal.Truncate(value))throw new ArgumentException("القيمة يجب أن تكون عدداً صحيحاً.");property.SetValue(policy,(int)value);}else property.SetValue(policy,value);}
            policy.Name=name.Text.Trim();policy.Category=category.Text.Trim();policy.SourceReference=source.Text.Trim();policy.EffectiveFrom=from.SelectedDate??throw new ArgumentException("تاريخ السريان مطلوب.");policy.EffectiveTo=to.SelectedDate??throw new ArgumentException("تاريخ الانتهاء مطلوب.");policy.ProrationMethod=(ProrationMethod)prorate.SelectedValue;policy.IncludeFixedAllowancesInWageBasis=fixedBasis.IsChecked==true;await _service.AddPolicyAsync(policy);w.DialogResult=true;
        }));w.Content=PayrollUi.Scroll(panel);RaccoonWarehouse.Security.PermissionWindowGuard.ShowDialog(w, "Payroll.View");await LoadAsync();
    }
}
