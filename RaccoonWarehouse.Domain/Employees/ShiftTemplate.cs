using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Employees;

public sealed class ShiftTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsOvernight { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ShiftTemplateWorkingDay> WorkingDays { get; set; } = new List<ShiftTemplateWorkingDay>();
    public ICollection<EmployeeShiftAssignment> Assignments { get; set; } = new List<EmployeeShiftAssignment>();
}
