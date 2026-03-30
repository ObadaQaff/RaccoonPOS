namespace RaccoonWarehouse.Domain.Reports.Employees.Dtos
{
    public class EmployeeAnalyticsDto
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int InactiveEmployees { get; set; }
        public int SuspendedEmployees { get; set; }
        public int TerminatedEmployees { get; set; }
        public Dictionary<int, int> CountByBranch { get; set; } = new();
        public Dictionary<int, int> CountByDepartment { get; set; } = new();
        public Dictionary<string, int> CountByJobTitle { get; set; } = new();
        public List<string> RecentlyHiredEmployees { get; set; } = new();
        public DateTime? LastActivityDate { get; set; }
    }
}
