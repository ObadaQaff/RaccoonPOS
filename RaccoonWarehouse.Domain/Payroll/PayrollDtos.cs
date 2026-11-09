namespace RaccoonWarehouse.Domain.Payroll;

public sealed class PayrollInputs
{
    public decimal TotalWorkedHours { get; set; }
    public decimal NormalOvertimeHours { get; set; }
    public decimal HolidayOvertimeHours { get; set; }
    public decimal? NormalOvertimeOverride { get; set; }
    public string? OvertimeReviewReason { get; set; }
    public decimal UnpaidLeaveDays { get; set; }
    public decimal? InsurableWageOverride { get; set; }
    public string? InsurableWageReason { get; set; }
    public bool InsurableWageReviewed { get; set; }
    public bool AssumptionsReviewed { get; set; }
    public List<PayrollAdjustmentInput> Adjustments { get; set; } = new();
}
public sealed class PayrollAdjustmentInput
{
    public PayrollAdjustmentKind Kind { get; set; }
    public decimal Amount { get; set; }
    public DeductionBasis DeductionBasis { get; set; }
    public string Reason { get; set; } = "";
    public string? LegalReference { get; set; }
    public string? Notes { get; set; }
}
public sealed class PayrollCalculation
{
    public string EmployeeName { get; set; } = "";
    public string EmployeeCode { get; set; } = "";
    public string PolicySourceReference { get; set; } = "";
    public decimal InsurableWageCeiling { get; set; }
    public decimal NormalMultiplier { get; set; }
    public decimal HolidayMultiplier { get; set; }
    public DateTime SalaryEffectiveFrom { get; set; }
    public DateTime PolicyEffectiveFrom { get; set; }
    public int EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal DailyWage { get; set; }
    public decimal HourlyWage { get; set; }
    public int EligibleCalendarDays { get; set; }
    public decimal EmploymentFraction { get; set; }
    public decimal OrdinaryHoursForPeriod { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal FixedAllowances { get; set; }
    public decimal NormalOvertimeHours { get; set; }
    public decimal HolidayOvertimeHours { get; set; }
    public decimal NormalOvertime { get; set; }
    public decimal HolidayOvertime { get; set; }
    public decimal Bonuses { get; set; }
    public decimal AdditionalAllowances { get; set; }
    public decimal GrossEarnings { get; set; }
    public decimal UnpaidLeaveDeduction { get; set; }
    public decimal AdditionalDeductions { get; set; }
    public decimal InsurableWage { get; set; }
    public decimal EmployeeSscPercent { get; set; }
    public decimal EmployerSscPercent { get; set; }
    public decimal EmployeeSsc { get; set; }
    public decimal EmployerSsc { get; set; }
    public decimal TotalSsc => EmployeeSsc + EmployerSsc;
    public decimal NetSalary { get; set; }
    public decimal OtherEmployerCosts { get; set; }
    public decimal EmployerCost { get; set; }
    public string CalculationAssumptions { get; set; } = "";
}
public sealed record PayrollRecord(int Id, string EmployeeName, int Year, int Month, PayrollState State,
    Guid Revision, PayrollInputs Inputs, PayrollCalculation Calculation, int PolicyId, int SalaryVersionId);
