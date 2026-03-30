using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Reports.Employees.Dtos
{
    public class EmployeeReportRowDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public EmployeeStatus Status { get; set; }
        public int? BranchId { get; set; }
        public int? DepartmentId { get; set; }
        public string? JobTitle { get; set; }
        public DateTime? HireDate { get; set; }
        public string? LinkedUserName { get; set; }
    }
}
