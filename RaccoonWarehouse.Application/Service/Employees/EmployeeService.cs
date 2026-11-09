using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Modules;
using RaccoonWarehouse.Core.Permissions;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Employees;
using RaccoonWarehouse.Domain.Employees.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Branches;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Application.Service.Modules;

namespace RaccoonWarehouse.Application.Service.Employees;

public interface IEmployeeService
{
    Task<IReadOnlyList<EmployeeListItemDto>> SearchAsync(string? search, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<EmployeeDetailsDto?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeLookupDto>> GetBranchesAsync(CancellationToken cancellationToken = default);
    Task<Result<int>> CreateBranchAsync(string code, string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeLookupDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<Result<int>> SaveBranchAssignmentAsync(EmployeeBranchAssignmentEditDto model, CancellationToken cancellationToken = default);
    Task<Result> DeactivateBranchAssignmentAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<int>> SaveAsync(EmployeeEditDto model, CancellationToken cancellationToken = default);
    Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _db;
    private readonly IModuleStateStore _modules;
    private readonly IPermissionService? _permissions;

    public EmployeeService(ApplicationDbContext db, IModuleStateStore modules, IPermissionService? permissions = null)
    {
        _db = db;
        _modules = modules;
        _permissions = permissions;
    }

    private void RequireEnabled()
    {
        if (!_modules.IsEnabled(ModuleStateStore.EmployeesModuleKey))
            throw new InvalidOperationException("Employees module is disabled.");
    }
    private void Require(PermissionLevel level) { if (_permissions != null && !_permissions.HasPermission("Employees", level)) throw new UnauthorizedAccessException("Employees permission denied."); }

    public async Task<IReadOnlyList<EmployeeListItemDto>> SearchAsync(string? search, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        RequireEnabled();
        Require(PermissionLevel.View);
        var query = _db.Employees.AsNoTracking().AsQueryable();
        if (!includeInactive)
            query = query.Where(x => x.Status == EmployeeStatus.Active);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(x => x.FullName.Contains(search) || x.PhoneNumber.Contains(search) || (x.NationalId != null && x.NationalId.Contains(search)));
        }

        var rows = await query.OrderBy(x => x.FullName).Take(200).ToListAsync(cancellationToken);
        var branchIds = rows.Select(x => x.PrimaryBranchId).Distinct().ToList();
        var branchNames = await _db.Branches.AsNoTracking().Where(x => branchIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        return rows.Select(x => new EmployeeListItemDto(x.Id, x.FullName, x.PhoneNumber, x.NationalId, x.JobTitle, x.Status, branchNames.TryGetValue(x.PrimaryBranchId, out var branchName) ? branchName : null)).ToList();
    }

    public async Task<EmployeeDetailsDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        RequireEnabled();
        Require(PermissionLevel.View);
        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (employee == null) return null;

        var branches = await _db.EmployeeBranchAssignments.AsNoTracking().Include(x => x.Branch)
            .Where(x => x.EmployeeId == id).OrderByDescending(x => x.IsActive).ThenBy(x => x.StartDate)
            .Select(x => new EmployeeBranchDto(x.Id, x.BranchId, x.Branch!.Name, x.StartDate, x.EndDate, x.IsActive)).ToListAsync(cancellationToken);
        var shifts = await _db.EmployeeShiftAssignments.AsNoTracking().Include(x => x.ShiftTemplate)
            .Where(x => x.EmployeeId == id).OrderBy(x => x.DayOfWeek).ThenByDescending(x => x.IsActive)
            .Select(x => new EmployeeShiftDto(x.Id, x.ShiftTemplateId, x.ShiftTemplate!.Name, x.DayOfWeek, x.ShiftTemplate.StartTime, x.ShiftTemplate.EndTime, x.ShiftTemplate.IsOvernight, x.StartDate, x.EndDate, x.IsActive)).ToListAsync(cancellationToken);
        return new EmployeeDetailsDto(ToEditDto(employee), branches, shifts);
    }

    public async Task<IReadOnlyList<EmployeeLookupDto>> GetBranchesAsync(CancellationToken cancellationToken = default)
    {
        RequireEnabled();
        Require(PermissionLevel.View);
        return await _db.Branches.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new EmployeeLookupDto(x.Id, x.Name)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeLookupDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        RequireEnabled();
        Require(PermissionLevel.View);
        return await _db.Set<User>().AsNoTracking().OrderBy(x => x.Name).Select(x => new EmployeeLookupDto(x.Id, x.Name)).ToListAsync(cancellationToken);
    }

    public async Task<Result<int>> CreateBranchAsync(string code, string name, CancellationToken cancellationToken = default)
    {
        RequireEnabled();
        Require(PermissionLevel.Manage);

        code = code.Trim();
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Result<int>.ValidationFail(new List<string> { "Branch code and name are required." });

        if (await _db.Branches.AsNoTracking().AnyAsync(x => x.Code == code, cancellationToken))
            return Result<int>.ValidationFail(new List<string> { "A branch with this code already exists." });

        var branch = new Branch
        {
            Code = code,
            Name = name,
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        _db.Branches.Add(branch);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<int>.Ok(branch.Id, "Branch created.");
    }

    public async Task<Result<int>> SaveBranchAssignmentAsync(EmployeeBranchAssignmentEditDto model, CancellationToken cancellationToken = default)
    {
        RequireEnabled();
        Require(PermissionLevel.Manage);
        if (model.EndDate.HasValue && model.EndDate < model.StartDate)
            return Result<int>.ValidationFail(new List<string> { "Branch assignment end date cannot be before its start date." });
        var entity = model.Id == 0 ? new EmployeeBranchAssignment { CreatedDate = DateTime.Now } : await _db.EmployeeBranchAssignments.FindAsync(new object[] { model.Id }, cancellationToken) ?? throw new InvalidOperationException("Branch assignment was not found.");
        if (model.Id == 0) _db.EmployeeBranchAssignments.Add(entity);
        entity.EmployeeId = model.EmployeeId; entity.BranchId = model.BranchId; entity.StartDate = model.StartDate; entity.EndDate = model.EndDate; entity.IsActive = model.IsActive; entity.UpdatedDate = DateTime.Now;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<int>.Ok(entity.Id, "Branch assignment saved.");
    }

    public async Task<Result> DeactivateBranchAssignmentAsync(int id, CancellationToken cancellationToken = default)
    {
        RequireEnabled(); Require(PermissionLevel.Manage); var entity = await _db.EmployeeBranchAssignments.FindAsync(new object[] { id }, cancellationToken); if (entity == null) return Result.Fail("Branch assignment was not found."); entity.IsActive = false; entity.UpdatedDate = DateTime.Now; await _db.SaveChangesAsync(cancellationToken); return Result.Ok("Branch assignment deactivated.");
    }

    public async Task<Result<int>> SaveAsync(EmployeeEditDto model, CancellationToken cancellationToken = default)
    {
        RequireEnabled();
        Require(PermissionLevel.Manage);
        if (string.IsNullOrWhiteSpace(model.FullName) || string.IsNullOrWhiteSpace(model.PhoneNumber))
            return Result<int>.ValidationFail(new List<string> { "Full name and phone number are required." });
        if (model.ContractEndDate.HasValue && model.ContractEndDate.Value.Date < model.ContractStartDate.Date)
            return Result<int>.ValidationFail(new List<string> { "Contract end date cannot be before the start date." });
        if (model.PrimaryBranchId <= 0 || !await _db.Branches.AsNoTracking().AnyAsync(x => x.Id == model.PrimaryBranchId, cancellationToken))
            return Result<int>.ValidationFail(new List<string> { "A valid primary branch is required." });

        Employee entity;
        if (model.Id == 0)
        {
            entity = new Employee { CreatedDate = DateTime.Now };
            _db.Employees.Add(entity);
        }
        else
        {
            entity = await _db.Employees.FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken)
                ?? throw new InvalidOperationException("Employee was not found.");
        }

        entity.FullName = model.FullName.Trim();
        entity.PhoneNumber = model.PhoneNumber.Trim();
        entity.NationalId = string.IsNullOrWhiteSpace(model.NationalId) ? null : model.NationalId.Trim();
        entity.Address = model.Address?.Trim(); entity.Email = model.Email?.Trim(); entity.EmploymentDate = model.EmploymentDate;
        entity.Status = model.Status; entity.JobTitle = model.JobTitle?.Trim(); entity.MonthlyBasicSalary = model.MonthlyBasicSalary;
        entity.SocialSecurityEnrolled = model.SocialSecurityEnrolled; entity.SocialSecurityNumber = model.SocialSecurityNumber?.Trim(); entity.SocialSecurityEnrollmentDate = model.SocialSecurityEnrollmentDate;
        entity.ContractType = model.ContractType; entity.ContractStartDate = model.ContractStartDate; entity.ContractEndDate = model.ContractEndDate;
        entity.PrimaryBranchId = model.PrimaryBranchId; entity.UserId = model.UserId; entity.UpdatedDate = DateTime.Now;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<int>.Ok(entity.Id, "Employee saved.");
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        RequireEnabled();
        Require(PermissionLevel.Manage);
        var employee = await _db.Employees.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (employee == null) return Result.Fail("Employee was not found.");
        employee.Status = EmployeeStatus.Inactive; employee.UpdatedDate = DateTime.Now;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok("Employee deactivated.");
    }

    private static EmployeeEditDto ToEditDto(Employee x) => new()
    {
        Id = x.Id, FullName = x.FullName, PhoneNumber = x.PhoneNumber, NationalId = x.NationalId, Address = x.Address, Email = x.Email,
        EmploymentDate = x.EmploymentDate, Status = x.Status, JobTitle = x.JobTitle, MonthlyBasicSalary = x.MonthlyBasicSalary,
        SocialSecurityEnrolled = x.SocialSecurityEnrolled, SocialSecurityNumber = x.SocialSecurityNumber, SocialSecurityEnrollmentDate = x.SocialSecurityEnrollmentDate,
        ContractType = x.ContractType, ContractStartDate = x.ContractStartDate, ContractEndDate = x.ContractEndDate, PrimaryBranchId = x.PrimaryBranchId, UserId = x.UserId
    };
}
