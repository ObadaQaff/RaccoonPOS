using RaccoonWarehouse.Application.Service.Employees;
using System.Windows;

namespace RaccoonWarehouse.Employees;

public partial class CreateBranchWindow : Window
{
    private readonly IEmployeeService _employees;
    public int? CreatedBranchId { get; private set; }

    public CreateBranchWindow(IEmployeeService employees)
    {
        _employees = employees;
        InitializeComponent();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var result = await _employees.CreateBranchAsync(Code.Text, Name.Text);
        if (!result.Success)
        {
            MessageBox.Show(string.Join(Environment.NewLine, result.Errors), "بيانات غير صحيحة", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        CreatedBranchId = result.Data;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
