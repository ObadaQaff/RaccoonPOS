using RaccoonWarehouse.Domain.Enums;
namespace RaccoonWarehouse.Domain.HR;
public sealed record AttendanceRowDto(int Id, int EmployeeId, string EmployeeName, DateTime WorkDate, DateTime? CheckIn, DateTime? CheckOut, decimal WorkedHours, int LateMinutes, int EarlyLeaveMinutes, AttendanceStatus Status);
public sealed class AttendanceEditDto { public int Id { get; set; } public int EmployeeId { get; set; } public int? ShiftAssignmentId { get; set; } public DateTime WorkDate { get; set; } = DateTime.Today; public DateTime? CheckIn { get; set; } public DateTime? CheckOut { get; set; } public string? CorrectionReason { get; set; } }
public sealed class LeaveRequestEditDto { public int Id { get; set; } public int EmployeeId { get; set; } public int LeaveTypeId { get; set; } public DateTime StartDate { get; set; } = DateTime.Today; public DateTime EndDate { get; set; } = DateTime.Today; public string? Reason { get; set; } }
public sealed record PayrollSummaryDto(int Id, int EmployeeId, string EmployeeName, DateTime PeriodMonth, PayrollStatus Status, decimal GrossAmount, decimal NetAmount);
