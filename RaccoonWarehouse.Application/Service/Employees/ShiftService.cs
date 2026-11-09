using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Core.Permissions;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Employees;
using RaccoonWarehouse.Domain.Employees.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Application.Service.Modules;

namespace RaccoonWarehouse.Application.Service.Employees;

public interface IShiftService
{
    Task<IReadOnlyList<ShiftTemplateListItemDto>> GetTemplatesAsync(CancellationToken cancellationToken = default);
    Task<Result<int>> SaveTemplateAsync(ShiftTemplateEditDto model, CancellationToken cancellationToken = default);
    Task<Result> DeactivateTemplateAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<int>> SaveAssignmentAsync(EmployeeShiftAssignmentEditDto model, CancellationToken cancellationToken = default);
    Task<Result> DeactivateAssignmentAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class ShiftService : IShiftService
{
    private readonly ApplicationDbContext _db;
    private readonly IModuleStateStore _modules;
    private readonly IPermissionService? _permissions;
    public ShiftService(ApplicationDbContext db, IModuleStateStore modules, IPermissionService? permissions = null) { _db = db; _modules = modules; _permissions = permissions; }
    private void RequireEnabled() { if (!_modules.IsEnabled(ModuleStateStore.EmployeesModuleKey)) throw new InvalidOperationException("Employees module is disabled."); }
    private void Require(PermissionLevel level)
    {
        if (_permissions != null
            && !_permissions.HasPermission("Employees.ShiftManagement", level)
            && !_permissions.HasPermission("Shifts", level))
            throw new UnauthorizedAccessException("Shift permission denied.");
    }

    public async Task<IReadOnlyList<ShiftTemplateListItemDto>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        RequireEnabled();
        Require(PermissionLevel.View);
        return await _db.ShiftTemplates.AsNoTracking().Include(x => x.WorkingDays).OrderBy(x => x.Name)
            .Select(x => new ShiftTemplateListItemDto(x.Id, x.Name, x.StartTime, x.EndTime, x.IsOvernight, x.IsActive, x.WorkingDays.OrderBy(d => d.DayOfWeek).Select(d => d.DayOfWeek).ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<int>> SaveTemplateAsync(ShiftTemplateEditDto model, CancellationToken cancellationToken = default)
    {
        RequireEnabled();
        Require(PermissionLevel.Admin);
        if (string.IsNullOrWhiteSpace(model.Name)) return Result<int>.ValidationFail(new List<string> { "Shift name is required." });
        if (!model.IsOvernight && model.EndTime <= model.StartTime) return Result<int>.ValidationFail(new List<string> { "End time must be after start time." });
        var entity = model.Id == 0 ? new ShiftTemplate { CreatedDate = DateTime.Now } : await _db.ShiftTemplates.Include(x => x.WorkingDays).FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken) ?? throw new InvalidOperationException("Shift was not found.");
        if (model.Id == 0) _db.ShiftTemplates.Add(entity);
        entity.Name = model.Name.Trim(); entity.StartTime = model.StartTime; entity.EndTime = model.EndTime; entity.IsOvernight = model.IsOvernight; entity.IsActive = model.IsActive; entity.UpdatedDate = DateTime.Now;
        if (model.Id != 0) _db.ShiftTemplateWorkingDays.RemoveRange(entity.WorkingDays);
        entity.WorkingDays = model.WorkingDays.Distinct().Select(day => new ShiftTemplateWorkingDay { DayOfWeek = day, CreatedDate = DateTime.Now }).ToList();
        await _db.SaveChangesAsync(cancellationToken); return Result<int>.Ok(entity.Id, "Shift saved.");
    }

    public async Task<Result> DeactivateTemplateAsync(int id, CancellationToken cancellationToken = default) { RequireEnabled(); var entity = await _db.ShiftTemplates.FindAsync(new object[] { id }, cancellationToken); if (entity == null) return Result.Fail("Shift was not found."); entity.IsActive = false; entity.UpdatedDate = DateTime.Now; await _db.SaveChangesAsync(cancellationToken); return Result.Ok("Shift deactivated."); }
    public async Task<Result<int>> SaveAssignmentAsync(EmployeeShiftAssignmentEditDto model, CancellationToken cancellationToken = default) { RequireEnabled(); Require(PermissionLevel.Manage); if (model.EndDate.HasValue && model.EndDate < model.StartDate) return Result<int>.ValidationFail(new List<string> { "Assignment end date cannot be before its start date." }); var e = model.Id == 0 ? new EmployeeShiftAssignment { CreatedDate = DateTime.Now } : await _db.EmployeeShiftAssignments.FindAsync(new object[] { model.Id }, cancellationToken) ?? throw new InvalidOperationException("Assignment was not found."); if (model.Id == 0) _db.EmployeeShiftAssignments.Add(e); e.EmployeeId = model.EmployeeId; e.ShiftTemplateId = model.ShiftTemplateId; e.DayOfWeek = model.DayOfWeek; e.StartDate = model.StartDate; e.EndDate = model.EndDate; e.IsActive = model.IsActive; e.UpdatedDate = DateTime.Now; await _db.SaveChangesAsync(cancellationToken); return Result<int>.Ok(e.Id, "Assignment saved."); }
    public async Task<Result> DeactivateAssignmentAsync(int id, CancellationToken cancellationToken = default) { RequireEnabled(); Require(PermissionLevel.Manage); var e = await _db.EmployeeShiftAssignments.FindAsync(new object[] { id }, cancellationToken); if (e == null) return Result.Fail("Assignment was not found."); e.IsActive = false; e.UpdatedDate = DateTime.Now; await _db.SaveChangesAsync(cancellationToken); return Result.Ok("Assignment deactivated."); }
}
