using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Employees;

public sealed class EmployeeShiftAssignment : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int ShiftTemplateId { get; set; }
    public ShiftTemplate? ShiftTemplate { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}
