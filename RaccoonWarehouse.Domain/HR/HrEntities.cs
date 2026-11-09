using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Employees;
using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.HR;

public sealed class AttendanceRecord : BaseEntity
{
    public int EmployeeId { get; set; } public Employee? Employee { get; set; }
    public int? ShiftAssignmentId { get; set; } public EmployeeShiftAssignment? ShiftAssignment { get; set; }
    public DateTime WorkDate { get; set; } public DateTime? CheckIn { get; set; } public DateTime? CheckOut { get; set; }
    public decimal WorkedHours { get; set; } public int LateMinutes { get; set; } public int EarlyLeaveMinutes { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present; public string? CorrectionReason { get; set; }
}

public sealed class LeaveType : BaseEntity { public string Name { get; set; } = string.Empty; public bool IsPaid { get; set; } public decimal DefaultAnnualBalance { get; set; } public bool IsActive { get; set; } = true; }
public sealed class LeaveRequest : BaseEntity
{
    public int EmployeeId { get; set; } public Employee? Employee { get; set; } public int LeaveTypeId { get; set; } public LeaveType? LeaveType { get; set; }
    public DateTime StartDate { get; set; } public DateTime EndDate { get; set; } public decimal Days { get; set; } public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending; public string? Reason { get; set; } public string? DecisionReason { get; set; }
}
public sealed class LeaveBalance : BaseEntity { public int EmployeeId { get; set; } public Employee? Employee { get; set; } public int LeaveTypeId { get; set; } public LeaveType? LeaveType { get; set; } public int Year { get; set; } public decimal EntitledDays { get; set; } public decimal UsedDays { get; set; } }
public sealed class HrAdjustment : BaseEntity { public int EmployeeId { get; set; } public Employee? Employee { get; set; } public DateTime PeriodMonth { get; set; } public HrAdjustmentType Type { get; set; } public decimal Amount { get; set; } public decimal Hours { get; set; } public bool Approved { get; set; } public string Reason { get; set; } = string.Empty; }
public sealed class SocialSecurityRule : BaseEntity { public string Name { get; set; } = string.Empty; public DateTime EffectiveFrom { get; set; } public DateTime? EffectiveTo { get; set; } public decimal EmployeeRate { get; set; } public decimal EmployerRate { get; set; } public decimal? MinimumBase { get; set; } public decimal? MaximumBase { get; set; } public bool IsActive { get; set; } = true; }
public sealed class PayrollPeriod : BaseEntity { public DateTime PeriodMonth { get; set; } public PayrollStatus Status { get; set; } = PayrollStatus.Draft; public DateTime? ApprovedAt { get; set; } public DateTime? PaidAt { get; set; } public string? ApprovalNote { get; set; } public ICollection<EmployeePayroll> Payrolls { get; set; } = new List<EmployeePayroll>(); }
public sealed class EmployeePayroll : BaseEntity { public int PayrollPeriodId { get; set; } public PayrollPeriod? PayrollPeriod { get; set; } public int EmployeeId { get; set; } public Employee? Employee { get; set; } public string EmployeeNameSnapshot { get; set; } = string.Empty; public decimal BasicSalary { get; set; } public decimal Allowances { get; set; } public decimal Bonuses { get; set; } public decimal Overtime { get; set; } public decimal UnpaidAbsence { get; set; } public decimal Deductions { get; set; } public decimal EmployeeSocialSecurity { get; set; } public decimal EmployerSocialSecurity { get; set; } public decimal GrossAmount { get; set; } public decimal NetAmount { get; set; } public string CalculationSnapshotJson { get; set; } = string.Empty; public ICollection<EmployeePayrollLine> Lines { get; set; } = new List<EmployeePayrollLine>(); }
public sealed class EmployeePayrollLine : BaseEntity { public int EmployeePayrollId { get; set; } public EmployeePayroll? EmployeePayroll { get; set; } public string Description { get; set; } = string.Empty; public decimal Amount { get; set; } }
