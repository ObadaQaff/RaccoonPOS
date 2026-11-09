using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Employees.DTOs
{
    public class EmployeeListFilterDto
    {
        public string? SearchText { get; set; }
        public EmployeeStatus? Status { get; set; }
        public int? BranchId { get; set; }
        public int? DepartmentId { get; set; }
        public string? JobTitle { get; set; }
        public bool OnlyActive { get; set; }
    }
}
