using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RaccoonWarehouse.Application.Service.Employees;
using RaccoonWarehouse.Application.Service.Payroll;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Domain.Employees.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Navigation;
namespace RaccoonWarehouse.Employees.Payroll;

public sealed class EmployeePayrollWorkspaceWindow : Window
{
    private readonly IEmployeeService _employees;private readonly IPermissionService _permissions;
    private readonly TextBox _search=PayrollUi.Number();private readonly DataGrid _grid=new(){AutoGenerateColumns=false,CanUserAddRows=false,IsReadOnly=true};
    private List<EmployeeReadDto> _items=new();
    public EmployeePayrollWorkspaceWindow(IEmployeeService employees,IPermissionService permissions)
    {
        RaccoonWarehouse.Security.PermissionWindowGuard.Demand(GetType());
        (_employees,_permissions)=(employees,permissions);Title="ملفات الموظفين والرواتب";Width=1200;Height=780;MinWidth=850;MinHeight=500;FlowDirection=FlowDirection.RightToLeft;WindowStartupLocation=WindowStartupLocation.CenterScreen;
        if(System.Windows.Application.Current.TryFindResource("AppPageBackgroundBrush") is System.Windows.Media.Brush bg)Background=bg;
        var root=new DockPanel{Margin=new Thickness(22)};var header=new TextBlock{Text=Title,FontSize=26,FontWeight=FontWeights.Bold,Margin=new Thickness(0,0,0,18)};DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
        var tools=new WrapPanel();tools.Children.Add(PayrollUi.AsyncButton("تحديث",LoadAsync));tools.Children.Add(PayrollUi.AsyncButton("إنشاء موظف",async()=>{await Require("Employees.Create");WindowManager.ShowDialog<CreateEmployee>();await LoadAsync();}));tools.Children.Add(PayrollUi.AsyncButton("تعديل الموظف",async()=>{await Require("Employees.Edit");var id=Selected();WindowManager.ShowDialog<UpdateEmployee>(WindowSizeType.LargeRectangle,w=>w.Initialize(id));await LoadAsync();}));tools.Children.Add(PayrollUi.AsyncButton("التفاصيل والتبويبات",async()=>{await Require("Employees.View");WindowManager.ShowDialog<EmployeeDetails>(WindowSizeType.LargeRectangle,w=>w.Initialize(Selected()));}));tools.Children.Add(PayrollUi.AsyncButton("الرواتب الشهرية",async()=>{await Require("Payroll.View");WindowManager.ShowDialog<PayrollMonthWindow>();}));tools.Children.Add(PayrollUi.AsyncButton("سياسات الرواتب والضمان",async()=>{await Require("Payroll.Configure");WindowManager.ShowDialog<PayrollPolicyWindow>();}));DockPanel.SetDock(tools,Dock.Top);root.Children.Add(tools);
        var searchPanel=PayrollUi.Fields();PayrollUi.Field(searchPanel,"البحث بالاسم أو الرقم الوظيفي أو الهاتف",_search);DockPanel.SetDock(searchPanel,Dock.Top);root.Children.Add(searchPanel);_search.TextChanged+=(_,_)=>Filter();
        Column("الرقم الوظيفي","Code");Column("اسم الموظف","FullName");Column("المسمى الوظيفي","JobTitle");Column("الهاتف","PhoneNumber");Column("تاريخ التعيين","HireDate");
        var state=new DataGridTemplateColumn{Header="الحالة",Width=110};var template=new DataTemplate();var text=new FrameworkElementFactory(typeof(TextBlock));text.SetBinding(TextBlock.TextProperty,new Binding("Status"){Converter=new StatusConverter()});template.VisualTree=text;state.CellTemplate=template;_grid.Columns.Add(state);
        root.Children.Add(_grid);Content=root;Loaded+=async(_,_)=>{try{await LoadAsync();}catch(Exception ex){PayrollUi.Error(ex);}};
    }
    private async Task Require(string key){if(!await _permissions.HasCurrentUserPermissionAsync(key))throw new UnauthorizedAccessException("ليس لديك الصلاحية المطلوبة.");}
    private int Selected()=>_grid.SelectedItem is EmployeeReadDto e?e.Id:throw new ArgumentException("اختر الموظف أولاً.");
    private void Column(string title,string path)=>_grid.Columns.Add(new DataGridTextColumn{Header=title,Binding=new Binding(path){StringFormat=path=="HireDate"?"yyyy-MM-dd":null},Width=new DataGridLength(1,DataGridLengthUnitType.Star)});
    private async Task LoadAsync(){await Require("Employees.View");var result=await _employees.GetListAsync();if(!result.Success)throw new ArgumentException(result.Message);_items=result.Data;Filter();}
    private void Filter(){var search=_search.Text.Trim();_grid.ItemsSource=_items.Where(x=>search.Length==0||x.FullName.Contains(search,StringComparison.OrdinalIgnoreCase)||x.Code.Contains(search,StringComparison.OrdinalIgnoreCase)||x.PhoneNumber?.Contains(search)==true).ToList();}
    private sealed class StatusConverter : IValueConverter
    {
        public object Convert(object value,Type t,object p,System.Globalization.CultureInfo c)=>PayrollUi.Statuses.FirstOrDefault(x=>Equals(x.Value,value))?.Name??"غير محدد";
        public object ConvertBack(object v,Type t,object p,System.Globalization.CultureInfo c)=>throw new NotSupportedException();
    }
}
