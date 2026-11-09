using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Branches;

namespace RaccoonWarehouse.Domain.Employees;

public sealed class EmployeeBranchAssignment : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}
