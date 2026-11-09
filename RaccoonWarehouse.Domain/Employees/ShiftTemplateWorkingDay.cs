using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Employees;

public sealed class ShiftTemplateWorkingDay : BaseEntity
{
    public int ShiftTemplateId { get; set; }
    public ShiftTemplate? ShiftTemplate { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
}
