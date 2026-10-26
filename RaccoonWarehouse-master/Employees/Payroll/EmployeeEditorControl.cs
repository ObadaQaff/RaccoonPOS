using System.Windows;
using System.Windows.Controls;
using RaccoonWarehouse.Application.Service.Employees;
using RaccoonWarehouse.Application.Service.Payroll;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Employees.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Payroll;
namespace RaccoonWarehouse.Employees.Payroll;

public sealed class EmployeeEditorControl : UserControl
{
    private readonly IEmployeeService _employees;private readonly IUserService _users;private readonly EmployeePayrollService _payroll;private readonly IPermissionService _permissions;private readonly PayrollCalculator _calculator;
    private readonly bool _readOnly;private readonly Func<Task> _saved;
    private readonly Dictionary<string,TextBox> _text=new();private readonly Dictionary<string,DatePicker> _dates=new();
    private readonly PasswordBox _sscNumber=new(){Margin=new Thickness(0,4,0,8),Padding=new Thickness(7)};
    private readonly CheckBox _clearSsc=new(){Content="حذف رقم الضمان المحفوظ صراحة"};
    private readonly CheckBox _newSalary=new(){Content="حفظ إصدار جديد لإعداد الراتب بتاريخ السريان المختار"};
    private readonly CheckBox _covered=new(){Content="الموظف مشمول بالضمان الاجتماعي"};
    private readonly TabControl _tabs=new();private readonly StackPanel _salaryPanel=PayrollUi.Fields();
    private readonly ComboBox _status=PayrollUi.Choices(PayrollUi.Statuses),_gender=PayrollUi.Choices(new[]{new PayrollUi.Choice(null,"غير محدد"),new PayrollUi.Choice(EmployeeGender.Male,"ذكر"),new PayrollUi.Choice(EmployeeGender.Female,"أنثى"),new PayrollUi.Choice(EmployeeGender.Unspecified,"غير مصنف")});
    private readonly ComboBox _employment=PayrollUi.Choices(new[]{new PayrollUi.Choice(null,"غير محدد"),new PayrollUi.Choice(EmploymentType.FullTime,"دوام كامل"),new PayrollUi.Choice(EmploymentType.PartTime,"دوام جزئي")});
    private readonly ComboBox _branch=new(){DisplayMemberPath="Name",SelectedValuePath="Id",Margin=new Thickness(0,3,0,8)},_manager=new(){DisplayMemberPath="FullName",SelectedValuePath="Id",Margin=new Thickness(0,3,0,8)},_user=new(){DisplayMemberPath="Name",SelectedValuePath="Id",Margin=new Thickness(0,3,0,8)};
    private readonly ComboBox _hourly=PayrollUi.Choices(new[]{new PayrollUi.Choice(HourlyRateMethod.DailyWage,"أجر اليوم ÷ الساعات اليومية"),new PayrollUi.Choice(HourlyRateMethod.MonthlyHours,"الأجر الشهري ÷ الساعات الشهرية المعتمدة")});
    private readonly ComboBox _overtime=PayrollUi.Choices(new[]{new PayrollUi.Choice(OvertimeMode.Manual,"إدخال الإضافي مباشرة"),new PayrollUi.Choice(OvertimeMode.MonthlyAggregate,"تلقائي من الساعات المدخلة — يلزم المراجعة")});
    private readonly ComboBox _category=new(){DisplayMemberPath="Name",SelectedValuePath="Category",Margin=new Thickness(0,3,0,8)};
    private readonly TextBox _daily=PayrollUi.Output(),_hour=PayrollUi.Output();
    private readonly Button _save;private readonly PayrollPeriodControl _period;
    private readonly StackPanel _personal=PayrollUi.Fields(),_employmentPanel=PayrollUi.Fields(),_hours=PayrollUi.Fields();
    private EmployeeReadDto? _original;private EmployeeSalaryVersion? _salary;private PayrollPolicy? _policy;private List<PayrollPolicy> _policies=new();private bool _loading=true,_canConfigure,_canViewPayroll;
    public EmployeeEditorControl(IEmployeeService employees,IUserService users,EmployeePayrollService payroll,IPermissionService permissions,PayrollCalculator calculator,bool readOnly,Func<Task> saved)
    {
        (_employees,_users,_payroll,_permissions,_calculator,_readOnly,_saved)=(employees,users,payroll,permissions,calculator,readOnly,saved);
        FlowDirection=FlowDirection.RightToLeft;
        Text(_personal,"اسم الموظف *","FullName");Text(_personal,"الرقم الوطني الأردني (اختياري — عشرة أرقام)","NationalId");PayrollUi.Field(_personal,"رقم الضمان الاجتماعي (مشفر؛ اتركه فارغاً للاحتفاظ بالمحفوظ)",_sscNumber);_personal.Children.Add(_clearSsc);
        _personal.Children.Add(PayrollUi.AsyncButton("عرض رقم الضمان للمخولين",async()=>{if(_original==null)throw new ArgumentException("احفظ الموظف أولاً.");var value=await _payroll.RevealSocialSecurityNumberAsync(_original.Id);MessageBox.Show(value??"غير مسجل","رقم الضمان");}));
        Date(_personal,"تاريخ الميلاد","Birth");PayrollUi.Field(_personal,"الجنس",_gender);Text(_personal,"رقم الهاتف","Phone");Text(_personal,"هاتف إضافي","AltPhone");Text(_personal,"البريد الإلكتروني","Email");Text(_personal,"العنوان","Address");Text(_personal,"ملاحظات","Notes");
        Text(_employmentPanel,"الرقم الوظيفي *","Code");Text(_employmentPanel,"المسمى الوظيفي","JobTitle");Date(_employmentPanel,"تاريخ التعيين","Hire");Date(_employmentPanel,"تاريخ انتهاء العمل","Termination");PayrollUi.Field(_employmentPanel,"الفرع",_branch);PayrollUi.Field(_employmentPanel,"نوع التوظيف",_employment);PayrollUi.Field(_employmentPanel,"حالة الموظف",_status);Text(_employmentPanel,"رقم القسم (اختياري)","Department");PayrollUi.Field(_employmentPanel,"المدير المباشر",_manager);PayrollUi.Field(_employmentPanel,"المستخدم المرتبط — لا يغير صلاحيات المستخدم",_user);Text(_employmentPanel,"سبب تغيير البيانات الوظيفية","EmploymentReason");
        Text(_hours,"ساعات العمل اليومية الأساسية","DailyHours");Text(_hours,"أيام العمل الأسبوعية","WeeklyDays");Text(_hours,"ساعات العمل الشهرية المعتمدة","MonthlyHours");PayrollUi.Field(_hours,"طريقة احتساب أجر الساعة",_hourly);PayrollUi.Field(_hours,"طريقة احتساب الإضافي",_overtime);
        _hours.Children.Add(new TextBlock{Text="هذه إعدادات حساب الراتب وليست سجلات حضور. الدوام اليومي الطويل لا يحول تلقائياً إلى ساعات عادية؛ تطبق حدود السياسة القانونية وتراجع ساعات الإضافي اليومية.",TextWrapping=TextWrapping.Wrap});
        Date(_salaryPanel,"سريان إعداد الراتب (أول الشهر)","Effective");_salaryPanel.Children.Add(_newSalary);Text(_salaryPanel,"سبب إصدار إعداد الراتب","SalaryReason");Text(_salaryPanel,"الراتب الأساسي الشهري (د.أ)","Basic");Text(_salaryPanel,"البدلات الثابتة (د.أ)","Fixed");_salaryPanel.Children.Add(_covered);Text(_salaryPanel,"الأجر الخاضع للضمان (د.أ) — مستقل عن الإضافي والإجازات","Eligible");_text["Category"]=PayrollUi.Number();PayrollUi.Field(_salaryPanel,"فئة اشتراك الضمان والسياسة",_category);Text(_salaryPanel,"معامل الإضافي العادي","NormalMultiplier");Text(_salaryPanel,"معامل إضافي الراحة والعطل","HolidayMultiplier");PayrollUi.Field(_salaryPanel,"أجر اليوم المحسوب (د.أ)",_daily);PayrollUi.Field(_salaryPanel,"أجر الساعة المحسوب (د.أ)",_hour);
        _period=new PayrollPeriodControl(payroll);
        Tab("البيانات الرئيسية",PayrollUi.Scroll(_personal));Tab("المعلومات الوظيفية",PayrollUi.Scroll(_employmentPanel));Tab("الدوام وساعات العمل",PayrollUi.Scroll(_hours));Tab("الراتب والمستحقات",PayrollUi.Scroll(_salaryPanel));Tab("الإضافي والخصومات",_period.AdjustmentsTab);Tab("الضمان الاجتماعي",_period.SocialSecurityTab);Tab("ملخص الراتب",_period.SummaryTab);
        var root=new DockPanel{Margin=new Thickness(18)};var footer=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Center};
        _save=PayrollUi.AsyncButton("حفظ ملف الموظف",SaveAsync);_save.Visibility=readOnly?Visibility.Collapsed:Visibility.Visible;footer.Children.Add(_save);footer.Children.Add(PayrollUi.Button("إغلاق",()=>Window.GetWindow(this)?.Close()));footer.Children.Add(PayrollUi.AsyncButton("السجل الوظيفي",async()=>{if(_original==null)return;var history=await _payroll.EmploymentHistoryAsync(_original.Id);MessageBox.Show(string.Join("\n",history.Select(x=>$"{x.CreatedDate:yyyy-MM-dd HH:mm}: {x.Reason}\nقبل: {PayrollUi.EmploymentText(x.BeforeJson)}\nبعد: {PayrollUi.EmploymentText(x.AfterJson)}")),"السجل الوظيفي");}));DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);root.Children.Add(_tabs);Content=root;
        _newSalary.Checked+=(_,_)=>{if(!_loading&&_salary!=null&&_dates["Effective"].SelectedDate==_salary.EffectiveFrom){var current=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1);_dates["Effective"].SelectedDate=_salary.EffectiveFrom==current?current.AddMonths(1):current;}EnableSalary();};_newSalary.Unchecked+=(_,_)=>EnableSalary();
        _dates["Effective"].SelectedDateChanged+=(_,_)=>{if(!_loading){RefreshPolicy();RefreshWages();}};
        _category.SelectionChanged+=(_,_)=>{if(_category.SelectedItem is PayrollPolicy policy){_text["Category"].Text=policy.Category;RefreshPolicy();RefreshWages();}};
        _salaryPanel.Children.Add(PayrollUi.AsyncButton("سجل إعدادات الراتب المؤرخة",async()=>{if(_original==null)return;var rows=await _payroll.SalaryHistoryAsync(_original.Id);var window=new Window{Title="سجل إعدادات الراتب",Width=850,Height=500,FlowDirection=FlowDirection.RightToLeft,WindowStartupLocation=WindowStartupLocation.CenterScreen};var grid=new DataGrid{AutoGenerateColumns=false,IsReadOnly=true,CanUserAddRows=false,ItemsSource=rows};foreach(var item in new Dictionary<string,string>{{"EffectiveFrom","السريان"},{"BasicMonthlySalary","الراتب الأساسي"},{"FixedAllowances","البدلات الثابتة"},{"StandardDailyHours","الساعات اليومية"},{"StandardMonthlyHours","الساعات الشهرية"},{"ChangeReason","سبب التغيير"}})grid.Columns.Add(new DataGridTextColumn{Header=item.Value,Binding=new System.Windows.Data.Binding(item.Key){StringFormat=item.Key=="EffectiveFrom"?"yyyy-MM-dd":null},Width=new DataGridLength(1,DataGridLengthUnitType.Star)});window.Content=grid;RaccoonWarehouse.Security.PermissionWindowGuard.ShowDialog(window, "Employees.View");}));
        foreach(var key in new[]{"Basic","Fixed","DailyHours","WeeklyDays","MonthlyHours","NormalMultiplier","HolidayMultiplier"})_text[key].TextChanged+=(_,_)=>RefreshWages();_hourly.SelectionChanged+=(_,_)=>RefreshWages();
    }
    private void Text(Panel p,string label,string key){var box=PayrollUi.Number();if(new[]{"FullName","Address","Notes","JobTitle","EmploymentReason","SalaryReason"}.Contains(key))box.FlowDirection=FlowDirection.RightToLeft;_text[key]=box;PayrollUi.Field(p,label,box);}
    private void Date(Panel p,string label,string key){var date=new DatePicker{Margin=new Thickness(0,3,0,8)};_dates[key]=date;PayrollUi.Field(p,label,date);}
    private void Tab(string label,UIElement content)=>_tabs.Items.Add(new TabItem{Header=label,Content=content,Padding=new Thickness(10,7,10,7)});
    public async Task LoadAsync(int id=0)
    {
        _loading=true;IsEnabled=false;
        try
        {
            _canViewPayroll=await _permissions.HasCurrentUserPermissionAsync("Payroll.View");_canConfigure=!_readOnly&&await _permissions.HasCurrentUserPermissionAsync("Payroll.Configure");
            if(id>0){var result=await _employees.GetByIdAsync(id);if(!result.Success)throw new ArgumentException(result.Message);_original=result.Data;}
            var d=_original??new EmployeeReadDto{Status=EmployeeStatus.Active,EmploymentType=EmploymentType.FullTime};
            foreach(var pair in new Dictionary<string,string?>{{"Code",d.Code},{"FullName",d.FullName},{"NationalId",d.NationalId},{"Phone",d.PhoneNumber},{"AltPhone",d.AlternatePhoneNumber},{"Email",d.Email},{"Address",d.Address},{"Notes",d.Notes},{"JobTitle",d.JobTitle},{"Department",d.DepartmentId?.ToString()}})_text[pair.Key].Text=pair.Value??"";
            _dates["Birth"].SelectedDate=d.DateOfBirth;_dates["Hire"].SelectedDate=d.HireDate;_dates["Termination"].SelectedDate=d.TerminationDate;
            _status.SelectedValue=d.Status;_gender.SelectedValue=d.Gender;_employment.SelectedValue=d.EmploymentType;
            var branches=await _payroll.BranchesAsync();if(d.BranchId.HasValue&&branches.All(x=>x.Id!=d.BranchId))branches.Add(new RaccoonWarehouse.Domain.Branches.Branch{Id=d.BranchId.Value,Name=$"فرع سابق ({d.BranchId})"});branches.Insert(0,new RaccoonWarehouse.Domain.Branches.Branch{Id=0,Name="بدون فرع"});_branch.ItemsSource=branches;_branch.SelectedValue=d.BranchId??0;
            var users=await _users.GetAllAsync();var userList=users.Data?.ToList()??new();if(d.UserId.HasValue&&userList.All(x=>x.Id!=d.UserId))userList.Add(new RaccoonWarehouse.Domain.Users.DTOs.UserReadDto{Id=d.UserId.Value,Name=$"مستخدم مرتبط سابق ({d.UserId})"});userList.Insert(0,new RaccoonWarehouse.Domain.Users.DTOs.UserReadDto{Id=0,Name="بدون مستخدم"});_user.ItemsSource=userList;_user.SelectedValue=d.UserId??0;
            var managers=(await _employees.GetListAsync()).Data?.Where(x=>x.Id!=id).ToList()??new();if(d.ManagerId.HasValue&&managers.All(x=>x.Id!=d.ManagerId))managers.Add(new EmployeeReadDto{Id=d.ManagerId.Value,FullName=$"مدير سابق ({d.ManagerId})"});managers.Insert(0,new EmployeeReadDto{Id=0,FullName="بدون مدير"});_manager.ItemsSource=managers;_manager.SelectedValue=d.ManagerId??0;
            if(_canViewPayroll)
            {
                var month=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1);var policies=await _payroll.PoliciesAsync();_policies=policies;
                _category.ItemsSource=policies.GroupBy(x=>x.Category).Select(x=>x.First()).ToList();
                _salary=id>0?await _payroll.SalaryAsync(id,month):null;
                _policy=policies.FirstOrDefault(x=>x.EffectiveFrom<=month&&x.EffectiveTo>=month.AddMonths(1).AddDays(-1)&&(_salary==null||x.Category==_salary.ContributionCategory));
                var config=_salary??(_policy!=null?_payroll.SalaryTemplate(d.BasicSalary??0m,_policy):null);
                if(config!=null)PopulateSalary(config);else _text["Basic"].Text=d.BasicSalary?.ToString()??"";
                _newSalary.IsChecked=id==0&&_canConfigure&&config!=null;
                _period.SetEmployee(id);if(id>0)await _period.LoadAsync();
            }
            for(int i=2;i<7;i++)((TabItem)_tabs.Items[i]).IsEnabled=_canViewPayroll;
            _employmentPanel.IsEnabled=!_readOnly;
            foreach(var key in new[]{"FullName","Phone","AltPhone","Email","Address","Notes"})_text[key].IsReadOnly=_readOnly;
            _dates["Birth"].IsEnabled=!_readOnly;_gender.IsEnabled=!_readOnly;
            _sscNumber.IsEnabled=_clearSsc.IsEnabled=!_readOnly&&await _permissions.HasCurrentUserPermissionAsync("Employees.SensitiveEdit");
            _text["NationalId"].IsReadOnly=!await _permissions.HasCurrentUserPermissionAsync("Employees.SensitiveEdit");
            _newSalary.IsEnabled=_canConfigure;_save.IsEnabled=!_readOnly;_loading=false;EnableSalary();RefreshWages();
        }
        finally{_loading=false;IsEnabled=true;}
    }
    public async Task SelectPayrollPeriodAsync(int year,int month){_tabs.SelectedIndex=4;await _period.SelectPeriodAsync(year,month);}
    private void PopulateSalary(EmployeeSalaryVersion c)
    {
        foreach(var pair in new Dictionary<string,decimal>{{"Basic",c.BasicMonthlySalary},{"Fixed",c.FixedAllowances},{"DailyHours",c.StandardDailyHours},{"WeeklyDays",c.WorkingDaysPerWeek},{"MonthlyHours",c.StandardMonthlyHours},{"NormalMultiplier",c.NormalOvertimeMultiplier},{"HolidayMultiplier",c.HolidayOvertimeMultiplier}})_text[pair.Key].Text=PayrollUi.Value(pair.Value);
        _text["Eligible"].Text=c.SocialSecurityEligibleWage?.ToString()??"";_text["Category"].Text=c.ContributionCategory;_category.SelectedValue=c.ContributionCategory;_text["SalaryReason"].Text="";_dates["Effective"].SelectedDate=c.EffectiveFrom;_covered.IsChecked=c.SocialSecurityCovered;_hourly.SelectedValue=c.HourlyRateMethod;_overtime.SelectedValue=c.OvertimeMode;
    }
    private void EnableSalary(){RefreshPolicy();var enabled=_canConfigure&&_newSalary.IsChecked==true;foreach(var box in _text.Where(x=>new[]{"Basic","Fixed","Eligible","Category","SalaryReason","NormalMultiplier","HolidayMultiplier"}.Contains(x.Key)))box.Value.IsReadOnly=!enabled;_hours.IsEnabled=enabled;_category.IsEnabled=enabled;_covered.IsEnabled=enabled;_dates["Effective"].IsEnabled=enabled;}
    private EmployeeSalaryVersion ReadSalary()
    {
        var weekly=PayrollUi.Decimal(_text["WeeklyDays"],"أيام العمل الأسبوعية");if(weekly!=decimal.Truncate(weekly))throw new ArgumentException("أيام العمل الأسبوعية عدد صحيح.");
        return new EmployeeSalaryVersion{EffectiveFrom=_dates["Effective"].SelectedDate??throw new ArgumentException("تاريخ السريان مطلوب."),BasicMonthlySalary=PayrollUi.Decimal(_text["Basic"],"الراتب"),FixedAllowances=PayrollUi.Decimal(_text["Fixed"],"البدلات الثابتة"),StandardDailyHours=PayrollUi.Decimal(_text["DailyHours"],"الساعات اليومية"),WorkingDaysPerWeek=(int)weekly,StandardMonthlyHours=PayrollUi.Decimal(_text["MonthlyHours"],"الساعات الشهرية"),HourlyRateMethod=_hourly.SelectedValue is HourlyRateMethod h?h:HourlyRateMethod.DailyWage,OvertimeMode=_overtime.SelectedValue is OvertimeMode o?o:OvertimeMode.Manual,NormalOvertimeMultiplier=PayrollUi.Decimal(_text["NormalMultiplier"],"معامل الإضافي"),HolidayOvertimeMultiplier=PayrollUi.Decimal(_text["HolidayMultiplier"],"معامل العطل"),SocialSecurityCovered=_covered.IsChecked==true,SocialSecurityEligibleWage=PayrollUi.Optional(_text["Eligible"],"أجر الضمان"),ContributionCategory=_text["Category"].Text.Trim(),ChangeReason=_text["SalaryReason"].Text.Trim()};
    }
    private void RefreshPolicy()
    {
        var date=_newSalary.IsChecked==true?_dates["Effective"].SelectedDate??DateTime.Today:new DateTime(DateTime.Today.Year,DateTime.Today.Month,1);
        _policy=_policies.Where(p=>p.Category==_text["Category"].Text&&p.EffectiveFrom<=date&&p.EffectiveTo>=date).OrderByDescending(p=>p.EffectiveFrom).FirstOrDefault();
    }
    private void RefreshWages(){if(_loading||_policy==null)return;try{var rates=_calculator.WageRates(ReadSalary(),_policy);_daily.Text=PayrollUi.Value(rates.Daily);_hour.Text=PayrollUi.Value(rates.Hourly);}catch{_daily.Text=_hour.Text="—";}}
    private static int? Selected(ComboBox box)=>box.SelectedValue is int id&&id>0?id:null;
    private async Task SaveAsync()
    {
        var department=PayrollUi.Optional(_text["Department"],"رقم القسم");if(department.HasValue&&(department<=0||department!=decimal.Truncate(department.Value)))throw new ArgumentException("رقم القسم موجب وصحيح أو فارغ.");
        var dto=new EmployeeCreateDto{Id=_original?.Id??0,Code=_text["Code"].Text.Trim(),FullName=_text["FullName"].Text.Trim(),NationalId=_text["NationalId"].Text.Trim(),SocialSecurityNumber=string.IsNullOrWhiteSpace(_sscNumber.Password)?null:_sscNumber.Password,ClearSocialSecurityNumber=_clearSsc.IsChecked==true,PhoneNumber=_text["Phone"].Text.Trim(),AlternatePhoneNumber=_text["AltPhone"].Text.Trim(),Email=_text["Email"].Text.Trim(),Address=_text["Address"].Text.Trim(),Notes=_text["Notes"].Text.Trim(),JobTitle=_text["JobTitle"].Text.Trim(),DepartmentId=department.HasValue?(int)department:null,BranchId=Selected(_branch),ManagerId=Selected(_manager),UserId=Selected(_user),Gender=_gender.SelectedValue as EmployeeGender?,EmploymentType=_employment.SelectedValue as EmploymentType?,Status=_status.SelectedValue is EmployeeStatus status?status:EmployeeStatus.Active,HireDate=_dates["Hire"].SelectedDate,TerminationDate=_dates["Termination"].SelectedDate,DateOfBirth=_dates["Birth"].SelectedDate,EmploymentChangeReason=_text["EmploymentReason"].Text.Trim(),BasicSalary=_original?.BasicSalary,CreatedDate=_original?.CreatedDate??DateTime.Now,CreatedBy=_original?.CreatedBy};
        var salary=_canConfigure&&_newSalary.IsChecked==true?ReadSalary():null;if(_original==null&&salary!=null)dto.BasicSalary=salary.BasicMonthlySalary;
        await _payroll.SaveProfileAsync(dto,salary);_sscNumber.Clear();MessageBox.Show("تم حفظ ملف الموظف وإعداداته.","الموظفون");await _saved();
    }
}
