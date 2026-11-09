using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RaccoonWarehouse.Domain.Payroll;
using RaccoonWarehouse.Domain.Enums;
namespace RaccoonWarehouse.Employees.Payroll;

internal static class PayrollUi
{
    public static readonly Brush ReadOnlyBrush=new SolidColorBrush(Color.FromRgb(231,239,242));
    public static TextBox Number(string value="")=>new(){Text=value,MinWidth=130,Padding=new Thickness(7),Margin=new Thickness(0,3,0,6),FlowDirection=FlowDirection.LeftToRight};
    public static TextBox Output()=>new(){IsReadOnly=true,Background=ReadOnlyBrush,Padding=new Thickness(7),Margin=new Thickness(0,3,0,6),MinWidth=130,FlowDirection=FlowDirection.LeftToRight};
    public static StackPanel Fields()=>new(){Margin=new Thickness(18)};
    public static void Field(Panel panel,string label,FrameworkElement input){panel.Children.Add(new TextBlock{Text=label,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,5,0,0)});panel.Children.Add(input);}
    public static Button Button(string label,Action action,[System.Runtime.CompilerServices.CallerArgumentExpression("action")] string expression="",[System.Runtime.CompilerServices.CallerFilePath] string file=""){var b=new Button{Content=label,Padding=new Thickness(14,7,14,7),Margin=new Thickness(5)};if(System.Windows.Application.Current.TryFindResource("AppSecondaryButtonStyle") is Style style)b.Style=style;b.Click+=(_,_)=>action();return b;}
    public static Button AsyncButton(string label,Func<Task> action,[System.Runtime.CompilerServices.CallerArgumentExpression("action")] string expression="",[System.Runtime.CompilerServices.CallerFilePath] string file=""){var b=Button(label,()=>{},expression,file);bool busy=false;b.Click+=async(_,_)=>{if(busy||!b.IsEnabled)return;busy=true;var owner=Window.GetWindow(b);if(owner!=null)owner.IsEnabled=false;try{await action();}catch(Exception e){Error(e);}finally{busy=false;if(owner!=null)owner.IsEnabled=true;}};return b;}
    public static void Error(Exception ex)
    {
        var message=EmployeeError(ex is Microsoft.EntityFrameworkCore.DbUpdateException?"تعذر حفظ البيانات. قد تكون مكررة أو تغيرت من مستخدم آخر؛ أعد التحميل.":ex is OverflowException?"القيمة الرقمية خارج النطاق المسموح.":ex.Message);
        if(!message.Any(c=>c>='\u0600'&&c<='\u06ff'))message="تعذر تنفيذ العملية. راجع البيانات وإعداد قاعدة البيانات ثم أعد المحاولة.";
        MessageBox.Show(message,"تنبيه",MessageBoxButton.OK,MessageBoxImage.Warning);
    }
    public static string EmployeeError(string message)=>message switch {
        "Full name is required."=>"اسم الموظف مطلوب.","Code is required."=>"الرقم الوظيفي مطلوب.","Employee code must be unique."=>"الرقم الوظيفي مستخدم مسبقاً.",
        "Identity number must be unique."=>"الرقم الوطني مستخدم مسبقاً.","Phone number format is not valid."=>"صيغة الهاتف غير صالحة.","Email format is not valid."=>"صيغة البريد الإلكتروني غير صالحة.",
        "The selected user is already linked to another employee."=>"المستخدم مرتبط بموظف آخر.","Employee not found."=>"الموظف غير موجود.",_=>message};
    public static decimal Decimal(TextBox box,string name,bool optional=false)
    {
        var text=new string(box.Text.Trim().Select(c=>char.IsDigit(c)?(char)('0'+(int)char.GetNumericValue(c)):c=='٫'?'.':c).ToArray());
        if(optional&&text.Length==0)return 0m;
        if(!decimal.TryParse(text,NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var value))
            throw new ArgumentException($"أدخل قيمة رقمية صحيحة للحقل: {name}.");return value;
    }
    public static decimal? Optional(TextBox box,string name)=>string.IsNullOrWhiteSpace(box.Text)?null:Decimal(box,name);
    public static string Money(decimal value)=>value.ToString("0.000",CultureInfo.InvariantCulture)+" د.أ";
    public static string Value(decimal value)=>value.ToString("0.######",CultureInfo.InvariantCulture);
    public sealed record Choice(object? Value,string Name);
    public static ComboBox Choices(IEnumerable<Choice> values)=>new(){ItemsSource=values.ToList(),DisplayMemberPath="Name",SelectedValuePath="Value",MinWidth=140,Margin=new Thickness(0,3,0,8)};
    public static Choice[] Statuses=>new[]{new Choice(EmployeeStatus.Active,"فعال"),new Choice(EmployeeStatus.Inactive,"غير فعال"),new Choice(EmployeeStatus.Suspended,"موقوف"),new Choice(EmployeeStatus.Terminated,"منتهي الخدمة")};
    public static ScrollViewer Scroll(UIElement child)=>new(){Content=child,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
    public static void ShowSnapshot(PayrollCalculation r)
    {
        var w=new Window{Title=$"كشف الراتب المحفوظ — {r.EmployeeName} — {r.Year}/{r.Month}",Width=650,Height=760,FlowDirection=FlowDirection.RightToLeft,WindowStartupLocation=WindowStartupLocation.CenterScreen};var panel=Fields();
        foreach(var item in new Dictionary<string,decimal>{{"الراتب الأساسي",r.BasicSalary},{"البدلات الثابتة",r.FixedAllowances},{"الإضافي العادي",r.NormalOvertime},{"إضافي العطل",r.HolidayOvertime},{"المكافآت",r.Bonuses},{"البدلات الإضافية",r.AdditionalAllowances},{"إجمالي المستحقات",r.GrossEarnings},{"خصم الإجازة",r.UnpaidLeaveDeduction},{"الخصومات الأخرى",r.AdditionalDeductions},{"الأجر الخاضع للضمان",r.InsurableWage},{"اقتطاع الموظف",r.EmployeeSsc},{"حصة الشركة",r.EmployerSsc},{"صافي الراتب",r.NetSalary},{"تكلفة صاحب العمل",r.EmployerCost}}){var box=Output();box.Text=Money(item.Value);Field(panel,item.Key,box);}panel.Children.Add(new TextBlock{Text=r.CalculationAssumptions,TextWrapping=TextWrapping.Wrap});w.Content=Scroll(panel);w.ShowDialog();
    }
    public static string EmploymentText(string json)
    {
        if(string.IsNullOrWhiteSpace(json))return "لا توجد بيانات سابقة";
        var labels=new Dictionary<string,string>{{"Code","الرقم الوظيفي"},{"JobTitle","المسمى الوظيفي"},{"HireDate","التعيين"},{"TerminationDate","انتهاء العمل"},{"BranchId","الفرع"},{"DepartmentId","القسم"},{"ManagerId","المدير"},{"Status","الحالة"},{"EmploymentType","نوع التوظيف"}};
        using var doc=System.Text.Json.JsonDocument.Parse(json);
        return string.Join("، ",doc.RootElement.EnumerateObject().Select(x=>(labels.GetValueOrDefault(x.Name)??"بيان")+": "+(x.Name=="Status"&&x.Value.ValueKind==System.Text.Json.JsonValueKind.Number&&x.Value.TryGetInt32(out var status)?Statuses.FirstOrDefault(c=>Equals(c.Value,(EmployeeStatus)status))?.Name:x.Name=="EmploymentType"&&x.Value.ValueKind==System.Text.Json.JsonValueKind.Number&&x.Value.TryGetInt32(out var type)?type==1?"دوام كامل":"دوام جزئي":x.Value.ToString())));
    }
    public static string? Reason(string title)
    {
        var w=new Window{Title=title,Width=540,Height=260,WindowStartupLocation=WindowStartupLocation.CenterScreen,FlowDirection=FlowDirection.RightToLeft};
        var p=Fields();var box=Number();box.AcceptsReturn=true;box.Height=80;Field(p,"السبب والمرجع",box);
        var save=Button("تأكيد",()=>{if(string.IsNullOrWhiteSpace(box.Text)){MessageBox.Show("السبب مطلوب.");return;}w.DialogResult=true;});p.Children.Add(save);w.Content=p;return w.ShowDialog()==true?box.Text.Trim():null;
    }
}
