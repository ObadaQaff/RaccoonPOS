namespace RaccoonWarehouse.Domain.Reports.Delegates.Filters
{
    public class DelegateReportFilterDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? DelegateId { get; set; }
        public int? BranchId { get; set; }
        public int? RegionId { get; set; }
    }
}
