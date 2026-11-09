using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Modules;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Core.Permissions;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.HR;
using System.Text.Json;

namespace RaccoonWarehouse.Application.Service.HR;

public interface IHrService
{
    Task<IReadOnlyList<AttendanceRowDto>> GetAttendanceAsync(DateTime from, DateTime to, int? employeeId = null, CancellationToken cancellationToken = default);
    Task<Result<int>> SaveAttendanceAsync(AttendanceEditDto model, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequest>> GetLeavesAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<Result<int>> SaveLeaveAsync(LeaveRequestEditDto model, CancellationToken cancellationToken = default);
    Task<Result> DecideLeaveAsync(int id, bool approve, string? reason, CancellationToken cancellationToken = default);
    Task<Result<int>> GeneratePayrollAsync(DateTime periodMonth, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PayrollSummaryDto>> GetPayrollAsync(DateTime periodMonth, CancellationToken cancellationToken = default);
    Task<Result> SetPayrollStatusAsync(int periodId, PayrollStatus status, CancellationToken cancellationToken = default);
}

public sealed class HrService : IHrService
{
    private readonly ApplicationDbContext _db; private readonly IModuleStateStore _modules; private readonly IPermissionService? _permissions;
    public HrService(ApplicationDbContext db, IModuleStateStore modules, IPermissionService? permissions = null) { _db = db; _modules = modules; _permissions = permissions; }
    private void RequireEnabled() { if (!_modules.IsEnabled(ModuleStateStore.HrModuleKey) || !_modules.IsEnabled(ModuleStateStore.EmployeesModuleKey)) throw new InvalidOperationException("HR module is disabled or Employees is unavailable."); Require(PermissionLevel.View); }
    private void Require(PermissionLevel level) { if (_permissions != null && !_permissions.HasPermission("HR", level)) throw new UnauthorizedAccessException("HR permission denied."); }

    public async Task<IReadOnlyList<AttendanceRowDto>> GetAttendanceAsync(DateTime from, DateTime to, int? employeeId = null, CancellationToken cancellationToken = default)
    {
        RequireEnabled(); var query = _db.AttendanceRecords.AsNoTracking().Include(x => x.Employee).Where(x => x.WorkDate >= from.Date && x.WorkDate <= to.Date); if (employeeId.HasValue) query = query.Where(x => x.EmployeeId == employeeId.Value);
        return await query.OrderByDescending(x => x.WorkDate).Select(x => new AttendanceRowDto(x.Id, x.EmployeeId, x.Employee!.FullName, x.WorkDate, x.CheckIn, x.CheckOut, x.WorkedHours, x.LateMinutes, x.EarlyLeaveMinutes, x.Status)).ToListAsync(cancellationToken);
    }

    public async Task<Result<int>> SaveAttendanceAsync(AttendanceEditDto model, CancellationToken cancellationToken = default)
    {
        RequireEnabled(); Require(PermissionLevel.Manage); if (model.CheckIn.HasValue && model.CheckOut.HasValue) { var end = model.CheckOut.Value < model.CheckIn.Value ? model.CheckOut.Value.AddDays(1) : model.CheckOut.Value; var hours = (decimal)(end - model.CheckIn.Value).TotalHours; if (hours < 0 || hours > 24) return Result<int>.ValidationFail(new List<string> { "Invalid attendance duration." }); }
        var entity = model.Id == 0 ? await _db.AttendanceRecords.FirstOrDefaultAsync(x => x.EmployeeId == model.EmployeeId && x.WorkDate == model.WorkDate.Date, cancellationToken) ?? new AttendanceRecord { CreatedDate = DateTime.Now } : await _db.AttendanceRecords.FindAsync(new object[] { model.Id }, cancellationToken) ?? throw new InvalidOperationException("Attendance record was not found.");
        if (entity.Id == 0) _db.AttendanceRecords.Add(entity); entity.EmployeeId = model.EmployeeId; entity.ShiftAssignmentId = model.ShiftAssignmentId; entity.WorkDate = model.WorkDate.Date; entity.CheckIn = model.CheckIn; entity.CheckOut = model.CheckOut; entity.CorrectionReason = model.CorrectionReason; entity.Status = model.CorrectionReason == null ? AttendanceStatus.Present : AttendanceStatus.Corrected;
        if (model.CheckIn.HasValue && model.CheckOut.HasValue) { var end = model.CheckOut.Value < model.CheckIn.Value ? model.CheckOut.Value.AddDays(1) : model.CheckOut.Value; entity.WorkedHours = (decimal)(end - model.CheckIn.Value).TotalHours; } else entity.WorkedHours = 0; entity.UpdatedDate = DateTime.Now;
        await _db.SaveChangesAsync(cancellationToken); return Result<int>.Ok(entity.Id, "Attendance saved.");
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetLeavesAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default) { RequireEnabled(); return await _db.LeaveRequests.AsNoTracking().Include(x => x.Employee).Include(x => x.LeaveType).Where(x => x.StartDate <= to.Date && x.EndDate >= from.Date).OrderByDescending(x => x.StartDate).ToListAsync(cancellationToken); }
    public async Task<Result<int>> SaveLeaveAsync(LeaveRequestEditDto model, CancellationToken cancellationToken = default)
    {
        RequireEnabled(); Require(PermissionLevel.Manage); if (model.EndDate.Date < model.StartDate.Date) return Result<int>.ValidationFail(new List<string> { "Leave end date cannot be before start date." });
        var overlap = await _db.LeaveRequests.AnyAsync(x => x.EmployeeId == model.EmployeeId && x.Status == LeaveRequestStatus.Approved && x.Id != model.Id && x.StartDate <= model.EndDate && x.EndDate >= model.StartDate, cancellationToken); if (overlap) return Result<int>.Fail("Approved leave overlaps an existing leave.");
        var entity = model.Id == 0 ? new LeaveRequest { CreatedDate = DateTime.Now } : await _db.LeaveRequests.FindAsync(new object[] { model.Id }, cancellationToken) ?? throw new InvalidOperationException("Leave request was not found."); if (model.Id == 0) _db.LeaveRequests.Add(entity); entity.EmployeeId = model.EmployeeId; entity.LeaveTypeId = model.LeaveTypeId; entity.StartDate = model.StartDate.Date; entity.EndDate = model.EndDate.Date; entity.Days = (decimal)(model.EndDate.Date - model.StartDate.Date).TotalDays + 1; entity.Reason = model.Reason; entity.Status = LeaveRequestStatus.Pending; entity.UpdatedDate = DateTime.Now; await _db.SaveChangesAsync(cancellationToken); return Result<int>.Ok(entity.Id, "Leave request saved.");
    }
    public async Task<Result> DecideLeaveAsync(int id, bool approve, string? reason, CancellationToken cancellationToken = default) { RequireEnabled(); Require(PermissionLevel.Admin); var entity = await _db.LeaveRequests.FindAsync(new object[] { id }, cancellationToken); if (entity == null) return Result.Fail("Leave request was not found."); if (approve && await _db.LeaveRequests.AnyAsync(x => x.Id != id && x.EmployeeId == entity.EmployeeId && x.Status == LeaveRequestStatus.Approved && x.StartDate <= entity.EndDate && x.EndDate >= entity.StartDate, cancellationToken)) return Result.Fail("Approved leave overlaps an existing leave."); entity.Status = approve ? LeaveRequestStatus.Approved : LeaveRequestStatus.Rejected; entity.DecisionReason = reason; entity.UpdatedDate = DateTime.Now; await _db.SaveChangesAsync(cancellationToken); return Result.Ok("Leave decision saved."); }

    public async Task<Result<int>> GeneratePayrollAsync(DateTime periodMonth, CancellationToken cancellationToken = default)
    {
        RequireEnabled(); Require(PermissionLevel.Manage);
        var month = new DateTime(periodMonth.Year, periodMonth.Month, 1);
        var employees = await _db.Employees.AsNoTracking()
            .Where(x => x.Status == EmployeeStatus.Active)
            .ToListAsync(cancellationToken);

        if (employees.Count == 0)
            return Result<int>.Fail("لا يوجد موظفون نشطون لإنشاء كشف رواتب. / No active employees are available to generate payroll.");

        var period = await _db.PayrollPeriods
            .Include(x => x.Payrolls)
            .SingleOrDefaultAsync(x => x.PeriodMonth == month, cancellationToken);

        if (period != null && (period.Status != PayrollStatus.Draft || period.Payrolls.Count > 0))
            return Result<int>.Fail("Payroll already exists for this period.");

        var rule = await _db.SocialSecurityRules.AsNoTracking().Where(x => x.IsActive && x.EffectiveFrom <= month && (x.EffectiveTo == null || x.EffectiveTo >= month)).OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(cancellationToken);
        period ??= new PayrollPeriod { PeriodMonth = month, CreatedDate = DateTime.Now };
        if (period.Id == 0)
            _db.PayrollPeriods.Add(period);

        foreach (var employee in employees)
        {
            var adjustments = await _db.HrAdjustments.AsNoTracking().Where(x => x.EmployeeId == employee.Id && x.Approved && x.PeriodMonth == month).ToListAsync(cancellationToken);
            var allowances = adjustments.Where(x => x.Type == HrAdjustmentType.Allowance).Sum(x => x.Amount); var bonuses = adjustments.Where(x => x.Type == HrAdjustmentType.Bonus).Sum(x => x.Amount); var overtime = adjustments.Where(x => x.Type == HrAdjustmentType.Overtime).Sum(x => x.Amount); var deductions = adjustments.Where(x => x.Type == HrAdjustmentType.Deduction).Sum(x => x.Amount); var gross = employee.MonthlyBasicSalary + allowances + bonuses + overtime; var baseAmount = Math.Clamp(gross, rule?.MinimumBase ?? 0, rule?.MaximumBase ?? decimal.MaxValue); var employeeSs = baseAmount * (rule?.EmployeeRate ?? 0) / 100; var employerSs = baseAmount * (rule?.EmployerRate ?? 0) / 100; var net = gross - deductions - employeeSs;
            var snapshot = JsonSerializer.Serialize(new { employee.MonthlyBasicSalary, allowances, bonuses, overtime, deductions, employeeSs, employerSs, rule?.EmployeeRate, rule?.EmployerRate }); period.Payrolls.Add(new EmployeePayroll { EmployeeId = employee.Id, EmployeeNameSnapshot = employee.FullName, BasicSalary = employee.MonthlyBasicSalary, Allowances = allowances, Bonuses = bonuses, Overtime = overtime, Deductions = deductions, EmployeeSocialSecurity = employeeSs, EmployerSocialSecurity = employerSs, GrossAmount = gross, NetAmount = net, CalculationSnapshotJson = snapshot, CreatedDate = DateTime.Now });
        }
        await _db.SaveChangesAsync(cancellationToken); return Result<int>.Ok(period.Id, "Payroll draft generated.");
    }
    public async Task<IReadOnlyList<PayrollSummaryDto>> GetPayrollAsync(DateTime periodMonth, CancellationToken cancellationToken = default) { RequireEnabled(); var month = new DateTime(periodMonth.Year, periodMonth.Month, 1); return await _db.EmployeePayrolls.AsNoTracking().Include(x => x.PayrollPeriod).Where(x => x.PayrollPeriod!.PeriodMonth == month).OrderBy(x => x.EmployeeNameSnapshot).Select(x => new PayrollSummaryDto(x.Id, x.EmployeeId, x.EmployeeNameSnapshot, x.PayrollPeriod!.PeriodMonth, x.PayrollPeriod.Status, x.GrossAmount, x.NetAmount)).ToListAsync(cancellationToken); }
    public async Task<Result> SetPayrollStatusAsync(int periodId, PayrollStatus status, CancellationToken cancellationToken = default) { RequireEnabled(); Require(PermissionLevel.Admin); var period = await _db.PayrollPeriods.FindAsync(new object[] { periodId }, cancellationToken); if (period == null) return Result.Fail("Payroll period was not found."); if (status == PayrollStatus.Paid && period.Status != PayrollStatus.Approved) return Result.Fail("Payroll must be approved before payment."); period.Status = status; if (status == PayrollStatus.Approved) period.ApprovedAt = DateTime.Now; if (status == PayrollStatus.Paid) period.PaidAt = DateTime.Now; await _db.SaveChangesAsync(cancellationToken); return Result.Ok("Payroll status saved."); }
}
