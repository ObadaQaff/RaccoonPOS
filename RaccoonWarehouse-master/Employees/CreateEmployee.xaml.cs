using System.Windows;
using RaccoonWarehouse.Application.Service.Employees;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Application.Service.Payroll;
using RaccoonWarehouse.Employees.Payroll;
namespace RaccoonWarehouse.Employees;
public partial class CreateEmployee : Window
{
    private readonly EmployeeEditorControl _editor;
    public CreateEmployee(IEmployeeService employees,IUserService users,EmployeePayrollService payroll,PayrollCalculator calculator)
    {

        InitializeComponent();
        _editor=new EmployeeEditorControl(employees,users,payroll,calculator,false,()=>{DialogResult=true;Close();return Task.CompletedTask;});
        EditorHost.Content=_editor;
            Loaded+=async(_,_)=>{try{await _editor.LoadAsync();}catch(Exception ex){PayrollUi.Error(ex);Close();}};
    }

}
