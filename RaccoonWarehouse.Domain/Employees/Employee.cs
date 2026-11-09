using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Branches;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users;

namespace RaccoonWarehouse.Domain.Employees;

public sealed class Employee : BaseEntity
{
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
    public DateTime ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public int PrimaryBranchId { get; set; }
    public Branch? PrimaryBranch { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public ICollection<EmployeeBranchAssignment> BranchAssignments { get; set; } = new List<EmployeeBranchAssignment>();
    public ICollection<EmployeeShiftAssignment> ShiftAssignments { get; set; } = new List<EmployeeShiftAssignment>();
}
