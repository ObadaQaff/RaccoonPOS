using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Reports.Employees.Filters
{
    public class EmployeeReportFilterDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? EmployeeId { get; set; }
        public int? BranchId { get; set; }
        public int? DepartmentId { get; set; }
        public EmployeeStatus? Status { get; set; }
    }
}
