using System.Windows;
using RaccoonWarehouse.Application.Service.Employees;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Application.Service.Payroll;
using RaccoonWarehouse.Employees.Payroll;
namespace RaccoonWarehouse.Employees;
public partial class EmployeeDetails : Window
{
    private readonly EmployeeEditorControl _editor;
    public EmployeeDetails(IEmployeeService employees,IUserService users,EmployeePayrollService payroll,PayrollCalculator calculator)
    {

        InitializeComponent();
        _editor=new EmployeeEditorControl(employees,users,payroll,calculator,true,()=>{DialogResult=true;Close();return Task.CompletedTask;});
        EditorHost.Content=_editor;

    }
        public async void Initialize(int employeeId)
        {try{await _editor.LoadAsync(employeeId);}catch(Exception ex){PayrollUi.Error(ex);Close();}}
        public async void InitializePayroll(int employeeId,int year,int month)
        {try{await _editor.LoadAsync(employeeId);await _editor.SelectPayrollPeriodAsync(year,month);}catch(Exception ex){PayrollUi.Error(ex);Close();}}

}
