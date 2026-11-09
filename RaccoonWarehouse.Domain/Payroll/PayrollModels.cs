using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Employees;

namespace RaccoonWarehouse.Domain.Payroll;

public enum EmploymentType { FullTime = 1, PartTime = 2 }
public enum HourlyRateMethod { DailyWage = 1, MonthlyHours = 2 }
public enum OvertimeMode { Manual = 1, MonthlyAggregate = 2 }
public enum PayrollState { Draft = 0, Approved = 1 }
public enum ProrationMethod { CalendarMonth = 1, FixedDivisor = 2 }
public enum PayrollAdjustmentKind { Deduction = 1, Bonus = 2, Allowance = 3, EmployerCost = 4, StatutoryDeduction = 5 }
public enum DeductionBasis { None = 0, AdvanceRecovery = 1, OverpaymentRecovery = 2, CourtOrder = 3, OtherLawful = 4 }

public class PayrollPolicy : BaseEntity
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public DateTime EffectiveFrom { get; set; }
    public DateTime EffectiveTo { get; set; }
    public decimal SalaryDayDivisor { get; set; }
    public decimal EmployeeSscPercent { get; set; }
    public decimal EmployerSscPercent { get; set; }
    public decimal InsurableWageCeiling { get; set; }
    public decimal DefaultDailyHours { get; set; }
    public int DefaultWorkingDaysPerWeek { get; set; }
    public decimal DefaultMonthlyHours { get; set; }
    public decimal LegalDailyHours { get; set; }
    public decimal LegalWeeklyHours { get; set; }
    public decimal MinimumNormalMultiplier { get; set; }
    public decimal MinimumHolidayMultiplier { get; set; }
    public decimal AdvanceDeductionMaximumPercent { get; set; }
    public int WeekStartsOn { get; set; }
    public ProrationMethod ProrationMethod { get; set; }
    public bool IncludeFixedAllowancesInWageBasis { get; set; }
    public bool IsLegallyReviewed { get; set; }
    public string SourceReference { get; set; } = "";
    public int? ReviewedBy { get; set; }
    public string? ReviewReason { get; set; }
}

public class EmployeeSalaryVersion : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateTime EffectiveFrom { get; set; }
    public decimal BasicMonthlySalary { get; set; }
    public decimal FixedAllowances { get; set; }
    public decimal StandardDailyHours { get; set; }
    public int WorkingDaysPerWeek { get; set; }
    public decimal StandardMonthlyHours { get; set; }
    public HourlyRateMethod HourlyRateMethod { get; set; }
    public OvertimeMode OvertimeMode { get; set; }
    public decimal NormalOvertimeMultiplier { get; set; }
    public decimal HolidayOvertimeMultiplier { get; set; }
    public bool SocialSecurityCovered { get; set; }
    public decimal? SocialSecurityEligibleWage { get; set; }
    public string ContributionCategory { get; set; } = "";
    public string ChangeReason { get; set; } = "";
    public int? ChangedBy { get; set; }
}

public class EmployeePayroll : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public int Year { get; set; }
    public int Month { get; set; }
    public int SalaryVersionId { get; set; }
    public EmployeeSalaryVersion SalaryVersion { get; set; } = null!;
    public int PolicyId { get; set; }
    public PayrollPolicy Policy { get; set; } = null!;
    public PayrollState State { get; set; }
    public Guid Revision { get; set; }
    public string InputsJson { get; set; } = "";
    public string SnapshotJson { get; set; } = "";
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public int? ModifiedBy { get; set; }
    public ICollection<PayrollAdjustment> Adjustments { get; set; } = new List<PayrollAdjustment>();
}

public class PayrollAdjustment : BaseEntity
{
    public int EmployeePayrollId { get; set; }
    public EmployeePayroll EmployeePayroll { get; set; } = null!;
    public PayrollAdjustmentKind Kind { get; set; }
    public decimal Amount { get; set; }
    public DeductionBasis DeductionBasis { get; set; }
    public string Reason { get; set; } = "";
    public string? LegalReference { get; set; }
    public string? Notes { get; set; }
    public int? AuthorizedBy { get; set; }
}

// Corrections retain the complete approved snapshot and its original authorization.
public class PayrollCorrection : BaseEntity
{
    public int EmployeePayrollId { get; set; }
    public EmployeePayroll EmployeePayroll { get; set; } = null!;
    public Guid OriginalRevision { get; set; }
    public string OriginalInputsJson { get; set; } = "";
    public string OriginalSnapshotJson { get; set; } = "";
    public string OriginalAdjustmentsJson { get; set; } = "";
    public DateTime? OriginalApprovedAt { get; set; }
    public int? OriginalApprovedBy { get; set; }
    public string Reason { get; set; } = "";
    public int? CorrectedBy { get; set; }
}

public class EmployeeEmploymentHistory : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public string BeforeJson { get; set; } = "";
    public string AfterJson { get; set; } = "";
    public string Reason { get; set; } = "";
    public int? ChangedBy { get; set; }
}
