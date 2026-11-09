using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RaccoonWarehouse.Application.Service.Payroll;
using RaccoonWarehouse.Domain.Payroll;
using RaccoonWarehouse.Navigation;
namespace RaccoonWarehouse.Employees.Payroll;

public sealed class PayrollMonthWindow : Window
{
    private readonly EmployeePayrollService _service;private readonly Func<int,int,int,Task>? _openEmployee;
    private readonly DatePicker _period=new(){SelectedDate=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1)};
    private readonly DataGrid _grid=new(){AutoGenerateColumns=false,IsReadOnly=true,CanUserAddRows=false};
    private readonly TextBlock _message=new(){TextWrapping=TextWrapping.Wrap,Margin=new Thickness(8)};
    private sealed record Row(int EmployeeId,string EmployeeName,string State,decimal? EmployeeSsc,decimal? EmployerSsc,decimal? NetSalary,decimal? EmployerCost,PayrollRecord? Record);
    public PayrollMonthWindow(EmployeePayrollService service,Func<int,int,int,Task>? openEmployee = null)
    {
        _service=service;_openEmployee=openEmployee;Title="الرواتب الشهرية — مسودات وكشوف معتمدة";Width=1180;Height=740;MinWidth=850;MinHeight=500;FlowDirection=FlowDirection.RightToLeft;WindowStartupLocation=WindowStartupLocation.CenterScreen;
        var root=new DockPanel{Margin=new Thickness(22)};var top=PayrollUi.Fields();top.Children.Add(new TextBlock{Text=Title,FontSize=24,FontWeight=FontWeights.Bold});PayrollUi.Field(top,"شهر وسنة الرواتب",_period);
        var tools=new WrapPanel();tools.Children.Add(PayrollUi.AsyncButton("تحميل الموظفين والكشوف",LoadAsync));tools.Children.Add(PayrollUi.AsyncButton("إنشاء المسودات الناقصة",GenerateAsync));tools.Children.Add(PayrollUi.AsyncButton("فتح تبويبات الموظف والراتب",OpenAsync));tools.Children.Add(PayrollUi.Button("عرض النسخة المحفوظة",ShowSnapshot));top.Children.Add(tools);top.Children.Add(_message);DockPanel.SetDock(top,Dock.Top);root.Children.Add(top);
        Add("الموظف","EmployeeName");Add("الحالة","State");Add("اقتطاع الموظف للضمان","EmployeeSsc");Add("حصة الشركة","EmployerSsc");Add("صافي الراتب (د.أ)","NetSalary");Add("تكلفة صاحب العمل (د.أ)","EmployerCost");root.Children.Add(_grid);Content=root;
        Loaded+=async(_,_)=>{try{await LoadAsync();}catch(Exception ex){PayrollUi.Error(ex);}};
        _period.SelectedDateChanged+=(_,_)=>{_grid.ItemsSource=null;_message.Text="تم تغيير الشهر؛ اضغط تحميل.";};
    }
    private (int Year,int Month) Period(){var d=_period.SelectedDate??throw new ArgumentException("اختر الشهر والسنة.");return(d.Year,d.Month);}
    public Task RefreshAsync()=>LoadAsync();
    private void Add(string label,string path)=>_grid.Columns.Add(new DataGridTextColumn{Header=label,Binding=new Binding(path){StringFormat=path is "EmployeeName" or "State"?null:"0.000"},Width=new DataGridLength(1,DataGridLengthUnitType.Star)});
    private async Task LoadAsync()
    {
        var(y,m)=Period();var employees=await _service.EligibleAsync(y,m);var records=await _service.RecordsAsync(y,m);var rows=records.Select(r=>new Row(r.Calculation.EmployeeId,r.EmployeeName,r.State==PayrollState.Approved?"معتمد":"مسودة",r.Calculation.EmployeeSsc,r.Calculation.EmployerSsc,r.Calculation.NetSalary,r.Calculation.EmployerCost,r)).ToList();
        rows.AddRange(employees.Where(x=>records.All(r=>r.Calculation.EmployeeId!=x.Id)).Select(x=>new Row(x.Id,x.FullName,"لم يحتسب",null,null,null,null,null)));_grid.ItemsSource=rows.OrderBy(x=>x.EmployeeName).ToList();_message.Text="الاعتماد لا يعني دفع الراتب ولا ينشئ قيداً محاسبياً. افتح الموظف لمراجعة الساعات والخصومات قبل الاعتماد.";
    }
    private async Task GenerateAsync()
    {
        var(y,m)=Period();var employees=await _service.EligibleAsync(y,m);var existing=await _service.RecordsAsync(y,m);var errors=new List<string>();int created=0;
        foreach(var employee in employees.Where(x=>existing.All(r=>r.Calculation.EmployeeId!=x.Id)))
        {
            try{await _service.SaveDraftAsync(employee.Id,y,m,new PayrollInputs());created++;}
            catch(Exception ex){errors.Add(employee.FullName+": "+PayrollUi.EmployeeError(ex.Message));}
        }
        await LoadAsync();_message.Text=$"تم إنشاء {created} مسودة.\n"+string.Join("\n",errors);
    }
    private async Task OpenAsync(){if(_grid.SelectedItem is not Row row)throw new ArgumentException("اختر الموظف.");var(y,m)=Period();if(_openEmployee!=null)await _openEmployee(row.EmployeeId,y,m);else WindowManager.ShowDialog<EmployeeDetails>(WindowSizeType.LargeRectangle,w=>w.InitializePayroll(row.EmployeeId,y,m));await LoadAsync();}
    private void ShowSnapshot(){if(_grid.SelectedItem is not Row{Record:not null} row){MessageBox.Show("اختر كشفاً محفوظاً.");return;}PayrollUi.ShowSnapshot(row.Record.Calculation);}
}
