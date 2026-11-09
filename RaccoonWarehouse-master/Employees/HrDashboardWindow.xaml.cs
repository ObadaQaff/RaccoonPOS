using RaccoonWarehouse.Application.Service.HR;
using RaccoonWarehouse.Domain.HR;
using System.Collections.ObjectModel;
using System.Windows;

namespace RaccoonWarehouse.Employees;
public partial class HrDashboardWindow : Window
{
    private readonly IHrService _hr; private readonly ObservableCollection<AttendanceRowDto> _attendance = new(); private readonly ObservableCollection<PayrollSummaryDto> _payroll = new();
    public HrDashboardWindow(IHrService hr) { _hr = hr; InitializeComponent(); AttendanceGrid.ItemsSource = _attendance; PayrollGrid.ItemsSource = _payroll; AttendanceFrom.SelectedDate = DateTime.Today; AttendanceTo.SelectedDate = DateTime.Today; PayrollMonth.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); Loaded += async (_, _) => await LoadAsync(); }
    private async Task LoadAsync() { await LoadAttendanceAsync(); await LoadLeavesAsync(); await LoadPayrollAsync(); }
    private async Task LoadAttendanceAsync() { var id = int.TryParse(AttendanceEmployeeId.Text, out var value) ? value : (int?)null; var rows = await _hr.GetAttendanceAsync(AttendanceFrom.SelectedDate ?? DateTime.Today, AttendanceTo.SelectedDate ?? DateTime.Today, id); _attendance.Clear(); foreach (var row in rows) _attendance.Add(row); }
    private async Task LoadLeavesAsync() { LeavesGrid.ItemsSource = await _hr.GetLeavesAsync(AttendanceFrom.SelectedDate ?? DateTime.Today, AttendanceTo.SelectedDate ?? DateTime.Today); }
    private async Task LoadPayrollAsync() { var rows = await _hr.GetPayrollAsync(PayrollMonth.SelectedDate ?? DateTime.Today); _payroll.Clear(); foreach (var row in rows) _payroll.Add(row); }
    private async void LoadAttendance_Click(object sender, RoutedEventArgs e) => await LoadAttendanceAsync(); private async void LoadLeaves_Click(object sender, RoutedEventArgs e) => await LoadLeavesAsync(); private async void LoadPayroll_Click(object sender, RoutedEventArgs e) => await LoadPayrollAsync();
    private async void RequestLeave_Click(object sender, RoutedEventArgs e) { if (!int.TryParse(LeaveEmployeeId.Text, out var employeeId) || !int.TryParse(LeaveTypeId.Text, out var typeId)) return; var result = await _hr.SaveLeaveAsync(new RaccoonWarehouse.Domain.HR.LeaveRequestEditDto { EmployeeId = employeeId, LeaveTypeId = typeId, StartDate = LeaveStart.SelectedDate ?? DateTime.Today, EndDate = LeaveEnd.SelectedDate ?? DateTime.Today }); if (!result.Success) MessageBox.Show(string.Join(Environment.NewLine, result.Errors)); else await LoadLeavesAsync(); }
    private async void GeneratePayroll_Click(object sender, RoutedEventArgs e) { var result = await _hr.GeneratePayrollAsync(PayrollMonth.SelectedDate ?? DateTime.Today); if (!result.Success) MessageBox.Show(result.Message); else await LoadPayrollAsync(); }
}
