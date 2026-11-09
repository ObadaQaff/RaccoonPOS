using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Employees.DTOs;

public sealed record EmployeeListItemDto(
    int Id,
    string FullName,
    string PhoneNumber,
    string? NationalId,
    string? JobTitle,
    EmployeeStatus Status,
    string? PrimaryBranchName);

public sealed class EmployeeEditDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public DateTime? EmploymentDate { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public string? JobTitle { get; set; }
    public decimal MonthlyBasicSalary { get; set; }
    public bool SocialSecurityEnrolled { get; set; }
    public string? SocialSecurityNumber { get; set; }
    public DateTime? SocialSecurityEnrollmentDate { get; set; }
    public EmploymentContractType ContractType { get; set; } = EmploymentContractType.Indefinite;
    public DateTime ContractStartDate { get; set; } = DateTime.Today;
    public DateTime? ContractEndDate { get; set; }
    public int PrimaryBranchId { get; set; }
    public int? UserId { get; set; }
}

public sealed record EmployeeDetailsDto(
    EmployeeEditDto Employee,
    IReadOnlyList<EmployeeBranchDto> Branches,
    IReadOnlyList<EmployeeShiftDto> Shifts);

public sealed record EmployeeBranchDto(int Id, int BranchId, string BranchName, DateTime StartDate, DateTime? EndDate, bool IsActive);
public sealed class EmployeeBranchAssignmentEditDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int BranchId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}
public sealed record EmployeeShiftDto(int Id, int ShiftTemplateId, string ShiftName, DayOfWeek DayOfWeek, TimeSpan StartTime, TimeSpan EndTime, bool IsOvernight, DateTime StartDate, DateTime? EndDate, bool IsActive);
public sealed record EmployeeLookupDto(int Id, string Name);

public sealed class ShiftTemplateEditDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsOvernight { get; set; }
    public bool IsActive { get; set; } = true;
    public List<DayOfWeek> WorkingDays { get; set; } = new();
}

public sealed record ShiftTemplateListItemDto(int Id, string Name, TimeSpan StartTime, TimeSpan EndTime, bool IsOvernight, bool IsActive, IReadOnlyList<DayOfWeek> WorkingDays);

public sealed class EmployeeShiftAssignmentEditDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int ShiftTemplateId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}
