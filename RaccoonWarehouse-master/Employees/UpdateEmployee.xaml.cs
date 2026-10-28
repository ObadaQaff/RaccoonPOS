using System.Windows;
using RaccoonWarehouse.Application.Service.Employees;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Application.Service.Payroll;
using RaccoonWarehouse.Employees.Payroll;
namespace RaccoonWarehouse.Employees;
public partial class UpdateEmployee : Window
{
    private readonly EmployeeEditorControl _editor;
    public UpdateEmployee(IEmployeeService employees,IUserService users,EmployeePayrollService payroll,PayrollCalculator calculator)
    {

        InitializeComponent();
        _editor=new EmployeeEditorControl(employees,users,payroll,calculator,false,()=>{DialogResult=true;Close();return Task.CompletedTask;});
        EditorHost.Content=_editor;

    }
        public async void Initialize(int employeeId)
        {try{await _editor.LoadAsync(employeeId);}catch(Exception ex){PayrollUi.Error(ex);Close();}}

}
