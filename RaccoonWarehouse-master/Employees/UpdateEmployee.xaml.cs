using System.Windows;
using RaccoonWarehouse.Application.Service.Employees;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Application.Service.Payroll;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Employees.Payroll;
namespace RaccoonWarehouse.Employees;
public partial class UpdateEmployee : Window
{
    private readonly EmployeeEditorControl _editor;
    public UpdateEmployee(IEmployeeService employees,IUserService users,EmployeePayrollService payroll,IPermissionService permissions,PayrollCalculator calculator)
    {
        RaccoonWarehouse.Security.PermissionWindowGuard.Demand(GetType());
        InitializeComponent();
        _editor=new EmployeeEditorControl(employees,users,payroll,permissions,calculator,false,()=>{DialogResult=true;Close();return Task.CompletedTask;});
        EditorHost.Content=_editor;

    }
        public async void Initialize(int employeeId)
        {try{await _editor.LoadAsync(employeeId);}catch(Exception ex){PayrollUi.Error(ex);Close();}}

}
