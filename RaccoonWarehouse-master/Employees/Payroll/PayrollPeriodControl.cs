using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RaccoonWarehouse.Application.Service.Payroll;
using RaccoonWarehouse.Domain.Payroll;
namespace RaccoonWarehouse.Employees.Payroll;

public sealed class PayrollPeriodControl
{
    public UIElement AdjustmentsTab { get; }
    public UIElement SocialSecurityTab { get; }
    public UIElement SummaryTab { get; }
    private readonly EmployeePayrollService _service;
    private int _employeeId;
    private PayrollRecord? _record;
    private readonly DatePicker _period=new(){SelectedDate=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1)};
    private readonly StackPanel _inputs=PayrollUi.Fields();
    private readonly Dictionary<string,TextBox> _numbers=new();
    private readonly CheckBox _review=new(){Content="راجعت افتراضات الفترة والساعات اليومية وتصنيف الإضافي وأسباب الخصم"};
    private readonly CheckBox _sscReview=new(){Content="راجعت الأجر الخاضع للضمان وحالة الشمول لهذا الشهر"};
    private readonly TextBox _overtimeReason=PayrollUi.Number(),_sscReason=PayrollUi.Number();
    private readonly ObservableCollection<PayrollAdjustmentInput> _adjustments=new();
    private readonly DataGrid _grid=new(){AutoGenerateColumns=false,CanUserAddRows=false,IsReadOnly=true,MinHeight=140};
    private readonly StackPanel _calculated=PayrollUi.Fields();
    private readonly StackPanel _summary=PayrollUi.Fields(),_ssc=PayrollUi.Fields();
    private readonly TextBlock _state=new(){Text="اختر شهر الراتب",Margin=new Thickness(8)};
    private readonly Button _save,_approve,_correct,_calculate;
    private bool _busy;
    public PayrollPeriodControl(EmployeePayrollService service)
    {
        _service=service;
        var root=PayrollUi.Fields();PayrollUi.Field(root,"شهر الراتب (يستخدم الشهر والسنة فقط)",_period);
        root.Children.Add(PayrollUi.AsyncButton("تحميل الشهر / الكشف المحفوظ",LoadAsync));root.Children.Add(_state);
        Add("الإجمالي الفعلي لساعات الشهر (يشمل ساعات العطل)","Worked");Add("ساعات الإضافي العادي — للوضع اليدوي","Normal");Add("ساعات إضافي الراحة الأسبوعية والعطل — مدخلة صراحة","Holiday");Add("تصحيح ساعات الإضافي العادي (اختياري)","Override");
        PayrollUi.Field(_inputs,"سبب تصحيح تصنيف الإضافي",_overtimeReason);Add("أيام الإجازة غير المدفوعة","Leave");Add("الأجر الخاضع للضمان لهذا الشهر (تعديل اختياري)","SscOverride");PayrollUi.Field(_inputs,"سبب تعديل أجر الضمان",_sscReason);
        _inputs.Children.Add(new TextBlock{Text="لا يثبت إجمالي ساعات الشهر وحده الإضافي اليومي. راجع التصنيف ولا تستنتج ساعات العطل تلقائياً. الخصومات تتطلب أساساً قانونياً ومرجعاً موثقاً.",TextWrapping=TextWrapping.Wrap,Foreground=System.Windows.Media.Brushes.DarkRed,Margin=new Thickness(0,8,0,8)});
        _grid.ItemsSource=_adjustments;Column("النوع","Kind");Column("المبلغ","Amount");Column("السبب","Reason");Column("المرجع القانوني","LegalReference");
        _grid.LoadingRow+=(_,e)=>{if(e.Row.Item is PayrollAdjustmentInput item)e.Row.ToolTip=item.Notes;};
        _inputs.Children.Add(_grid);var actions=new WrapPanel();actions.Children.Add(PayrollUi.Button("إضافة خصم / مكافأة / بدل",AddAdjustment));actions.Children.Add(PayrollUi.Button("حذف السطر المحدد",()=>{if(_grid.SelectedItem is PayrollAdjustmentInput value)_adjustments.Remove(value);}));_inputs.Children.Add(actions);
        root.Children.Add(_calculated);
        _inputs.Children.Add(_review);_inputs.Children.Add(_sscReview);root.Children.Add(_inputs);
        var buttons=new WrapPanel();_calculate=PayrollUi.AsyncButton("احتساب ومعاينة",CalculateAsync);_save=PayrollUi.AsyncButton("حفظ المسودة",SaveAsync);_approve=PayrollUi.AsyncButton("اعتماد الكشف",ApproveAsync);_correct=PayrollUi.AsyncButton("فتح تصحيح موثق",CorrectAsync);
        buttons.Children.Add(_calculate);buttons.Children.Add(_save);buttons.Children.Add(_approve);buttons.Children.Add(_correct);buttons.Children.Add(PayrollUi.AsyncButton("سجل التصحيحات",HistoryAsync));root.Children.Add(buttons);
        AdjustmentsTab=PayrollUi.Scroll(root);SocialSecurityTab=PayrollUi.Scroll(_ssc);SummaryTab=PayrollUi.Scroll(_summary);
        foreach(var key in new[]{"Worked","Normal","Holiday","Leave"})_numbers[key].Text="0";
        _period.SelectedDateChanged+=(_,_)=>{_state.Text="تم تغيير الشهر؛ اضغط تحميل قبل الاحتساب أو الحفظ.";_save.IsEnabled=_approve.IsEnabled=_calculate.IsEnabled=_correct.IsEnabled=false;_summary.Children.Clear();_ssc.Children.Clear();};
        SetApproved(false);
    }
    private void Add(string label,string key){var input=PayrollUi.Number();_numbers[key]=input;PayrollUi.Field(_inputs,label,input);}
    private void Column(string label,string property)
    {
        if(property=="Kind")_grid.Columns.Add(new DataGridTemplateColumn{Header=label,CellTemplate=KindTemplate(),Width=120});
        else _grid.Columns.Add(new DataGridTextColumn{Header=label,Binding=new Binding(property),Width=new DataGridLength(1,DataGridLengthUnitType.Star)});
    }
    private static DataTemplate KindTemplate()
    {
        var template=new DataTemplate();var text=new FrameworkElementFactory(typeof(TextBlock));text.SetBinding(TextBlock.TextProperty,new Binding("Kind"){Converter=new KindConverter()});template.VisualTree=text;return template;
    }
    private sealed class KindConverter : IValueConverter
    {
        public object Convert(object value,Type t,object p,System.Globalization.CultureInfo c)=>value switch{PayrollAdjustmentKind.Deduction=>"خصم",PayrollAdjustmentKind.StatutoryDeduction=>"اقتطاع قانوني",PayrollAdjustmentKind.Bonus=>"مكافأة",PayrollAdjustmentKind.Allowance=>"بدل إضافي",PayrollAdjustmentKind.EmployerCost=>"تكلفة على الشركة",_=>""};
        public object ConvertBack(object v,Type t,object p,System.Globalization.CultureInfo c)=>throw new NotSupportedException();
    }
    public void SetEmployee(int id){_employeeId=id;_record=null;}
    public async Task SelectPeriodAsync(int year,int month){_period.SelectedDate=new DateTime(year,month,1);await LoadAsync();}
    private (int Year,int Month) Period(){var d=_period.SelectedDate??throw new ArgumentException("اختر شهر الراتب.");if(_employeeId<=0)throw new ArgumentException("احفظ ملف الموظف أولاً.");return(d.Year,d.Month);}
    private PayrollInputs Read()=>new(){TotalWorkedHours=PayrollUi.Decimal(_numbers["Worked"],"الساعات"),NormalOvertimeHours=PayrollUi.Decimal(_numbers["Normal"],"الإضافي"),HolidayOvertimeHours=PayrollUi.Decimal(_numbers["Holiday"],"إضافي العطل"),NormalOvertimeOverride=PayrollUi.Optional(_numbers["Override"],"تصحيح الإضافي"),OvertimeReviewReason=_overtimeReason.Text,UnpaidLeaveDays=PayrollUi.Decimal(_numbers["Leave"],"أيام الإجازة"),InsurableWageOverride=PayrollUi.Optional(_numbers["SscOverride"],"أجر الضمان"),InsurableWageReason=_sscReason.Text,AssumptionsReviewed=_review.IsChecked==true,InsurableWageReviewed=_sscReview.IsChecked==true,Adjustments=_adjustments.ToList()};
    private async Task Run(Func<Task> action){if(_busy)return;_busy=true;try{await action();}finally{_busy=false;}}
    public async Task LoadAsync()=>await Run(async()=>{
        var (y,m)=Period();_record=(await _service.RecordsAsync(y,m,_employeeId)).SingleOrDefault();var value=_record?.Inputs??new PayrollInputs();
        _numbers["Worked"].Text=PayrollUi.Value(value.TotalWorkedHours);_numbers["Normal"].Text=PayrollUi.Value(value.NormalOvertimeHours);_numbers["Holiday"].Text=PayrollUi.Value(value.HolidayOvertimeHours);_numbers["Leave"].Text=PayrollUi.Value(value.UnpaidLeaveDays);
        _numbers["Override"].Text=value.NormalOvertimeOverride?.ToString()??"";_numbers["SscOverride"].Text=value.InsurableWageOverride?.ToString()??"";_overtimeReason.Text=value.OvertimeReviewReason??"";_sscReason.Text=value.InsurableWageReason??"";_review.IsChecked=value.AssumptionsReviewed;_sscReview.IsChecked=value.InsurableWageReviewed;
        _adjustments.Clear();foreach(var row in value.Adjustments)_adjustments.Add(row);
        SetApproved(_record?.State==PayrollState.Approved);_state.Text=_record==null?"لا يوجد كشف محفوظ لهذا الشهر":_record.State==PayrollState.Approved?"معتمد — تعرض النسخة التاريخية المحفوظة":"مسودة";
        if(_record!=null)Render(_record.Calculation);else{_summary.Children.Clear();_ssc.Children.Clear();}
    });
    private void SetApproved(bool value){_inputs.IsEnabled=!value;_save.IsEnabled=!value;_calculate.IsEnabled=!value;_approve.IsEnabled=!value&&_record!=null;_correct.IsEnabled=value;}
    private async Task CalculateAsync()=>await Run(async()=>{var(y,m)=Period();var preview=await _service.PreviewAsync(_employeeId,y,m,Read());Render(preview.Result);});
    private async Task SaveAsync()=>await Run(async()=>{var(y,m)=Period();if(_record!=null&&(_record.Year!=y||_record.Month!=m))throw new ArgumentException("حمّل الشهر المختار قبل الحفظ.");_record=await _service.SaveDraftAsync(_employeeId,y,m,Read(),_record?.Id??0,_record?.Revision);Render(_record.Calculation);_state.Text="تم حفظ المسودة";SetApproved(false);});
    private async Task ApproveAsync(){if(_record==null)throw new ArgumentException("احفظ المسودة أولاً.");if(MessageBox.Show("سيعتمد الكشف المحفوظ فقط. هل راجعت جميع القيم والخصومات وأساس الضمان؟ لا تعد الرواتب مدفوعة ولا ترحل محاسبياً تلقائياً.","اعتماد الراتب",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;await _service.ApproveAsync(_record.Id,_record.Revision);await LoadAsync();}
    private async Task CorrectAsync(){if(_record==null)return;var reason=PayrollUi.Reason("سبب تصحيح كشف معتمد");if(reason==null)return;await _service.CorrectAsync(_record.Id,_record.Revision,reason);await LoadAsync();}
    private async Task HistoryAsync(){if(_record==null)return;var rows=await _service.CorrectionsAsync(_record.Id);if(rows.Count==0){MessageBox.Show("لا توجد تصحيحات محفوظة.");return;}var w=new Window{Title="سجل التصحيحات",Width=700,Height=500,FlowDirection=FlowDirection.RightToLeft,WindowStartupLocation=WindowStartupLocation.CenterScreen};var panel=PayrollUi.Fields();foreach(var row in rows){panel.Children.Add(new TextBlock{Text=$"{row.CreatedDate:yyyy-MM-dd HH:mm} — المستخدم {row.CorrectedBy}: {row.Reason}",TextWrapping=TextWrapping.Wrap});panel.Children.Add(PayrollUi.Button("عرض النسخة المعتمدة السابقة",()=>PayrollUi.ShowSnapshot(System.Text.Json.JsonSerializer.Deserialize<PayrollCalculation>(row.OriginalSnapshotJson)!)));}w.Content=PayrollUi.Scroll(panel);w.ShowDialog();}
    private void Value(Panel panel,string label,decimal amount){var box=PayrollUi.Output();box.Text=PayrollUi.Money(amount);PayrollUi.Field(panel,label,box);}
    private void Render(PayrollCalculation r)
    {
        _summary.Children.Clear();_ssc.Children.Clear();_calculated.Children.Clear();
        Value(_calculated,"قيمة الإضافي العادي (محسوبة)",r.NormalOvertime);Value(_calculated,"قيمة إضافي العطل (محسوبة)",r.HolidayOvertime);Value(_calculated,"خصم الإجازة غير المدفوعة (محسوب)",r.UnpaidLeaveDeduction);
        Value(_summary,"الراتب الأساسي المستحق",r.BasicSalary);Value(_summary,"البدلات الثابتة المستحقة",r.FixedAllowances);Value(_summary,"قيمة الإضافي العادي",r.NormalOvertime);Value(_summary,"قيمة إضافي العطل",r.HolidayOvertime);Value(_summary,"المكافآت",r.Bonuses);Value(_summary,"البدلات الإضافية",r.AdditionalAllowances);Value(_summary,"إجمالي المستحقات قبل الخصومات",r.GrossEarnings);Value(_summary,"خصم الإجازة غير المدفوعة",r.UnpaidLeaveDeduction);Value(_summary,"الخصومات والاقتطاعات الأخرى",r.AdditionalDeductions);Value(_summary,"اقتطاع الموظف للضمان",r.EmployeeSsc);Value(_summary,"صافي الراتب",r.NetSalary);Value(_summary,"تكاليف أخرى على الشركة",r.OtherEmployerCosts);Value(_summary,"إجمالي تكلفة صاحب العمل",r.EmployerCost);
        _summary.Children.Add(new TextBlock{Text=r.CalculationAssumptions,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,0)});
        Value(_ssc,"الأجر الخاضع للضمان بعد تطبيق السقف",r.InsurableWage);Value(_ssc,"السقف الساري",r.InsurableWageCeiling);
        _ssc.Children.Add(new TextBlock{Text=$"نسبة الموظف: {r.EmployeeSscPercent:0.##}% — نسبة الشركة: {r.EmployerSscPercent:0.##}%"});
        Value(_ssc,"اقتطاع الموظف",r.EmployeeSsc);Value(_ssc,"حصة الشركة",r.EmployerSsc);Value(_ssc,"إجمالي اشتراكات الضمان",r.TotalSsc);
        _ssc.Children.Add(new TextBlock{Text="حصة الشركة لا تخصم من صافي الموظف. لا يغيّر الإضافي أو الإجازة الأجر الخاضع تلقائياً.\n"+r.PolicySourceReference,TextWrapping=TextWrapping.Wrap});
    }
    private void AddAdjustment()
    {
        var w=new Window{Title="إضافة مستحق أو خصم",Width=600,Height=650,FlowDirection=FlowDirection.RightToLeft,WindowStartupLocation=WindowStartupLocation.CenterScreen};var panel=PayrollUi.Fields();
        var kind=PayrollUi.Choices(new[]{new PayrollUi.Choice(PayrollAdjustmentKind.Deduction,"خصم إضافي"),new PayrollUi.Choice(PayrollAdjustmentKind.StatutoryDeduction,"اقتطاع قانوني"),new PayrollUi.Choice(PayrollAdjustmentKind.Bonus,"مكافأة"),new PayrollUi.Choice(PayrollAdjustmentKind.Allowance,"بدل إضافي"),new PayrollUi.Choice(PayrollAdjustmentKind.EmployerCost,"تكلفة على الشركة")});kind.SelectedIndex=0;
        var basis=PayrollUi.Choices(new[]{new PayrollUi.Choice(DeductionBasis.None,"لا ينطبق على المستحقات"),new PayrollUi.Choice(DeductionBasis.AdvanceRecovery,"استرداد سلفة ضمن الحد القانوني"),new PayrollUi.Choice(DeductionBasis.OverpaymentRecovery,"استرداد مبلغ دفع بالزيادة"),new PayrollUi.Choice(DeductionBasis.CourtOrder,"حكم قضائي"),new PayrollUi.Choice(DeductionBasis.OtherLawful,"حسم قانوني موثق آخر")});basis.SelectedIndex=0;
        TextBox amount=PayrollUi.Number(),reason=PayrollUi.Number(),reference=PayrollUi.Number(),notes=PayrollUi.Number();PayrollUi.Field(panel,"النوع",kind);PayrollUi.Field(panel,"المبلغ (د.أ)",amount);PayrollUi.Field(panel,"السبب",reason);PayrollUi.Field(panel,"الأساس القانوني للخصم",basis);PayrollUi.Field(panel,"رقم المستند أو المرجع القانوني",reference);PayrollUi.Field(panel,"ملاحظات",notes);
        panel.Children.Add(PayrollUi.Button("إضافة",()=>{try{var value=PayrollUi.Decimal(amount,"المبلغ");if(value<=0m||string.IsNullOrWhiteSpace(reason.Text))throw new ArgumentException("أدخل مبلغاً موجباً وسبباً.");_adjustments.Add(new PayrollAdjustmentInput{Kind=(PayrollAdjustmentKind)kind.SelectedValue,Amount=value,Reason=reason.Text.Trim(),DeductionBasis=(DeductionBasis)basis.SelectedValue,LegalReference=reference.Text.Trim(),Notes=notes.Text.Trim()});w.DialogResult=true;}catch(Exception ex){PayrollUi.Error(ex);}}));w.Content=PayrollUi.Scroll(panel);w.ShowDialog();
    }
}
