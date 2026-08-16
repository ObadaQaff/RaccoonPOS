namespace RaccoonWarehouse.Domain.Reports.Accounting.Filters
{
    public class UserStatementFilterDto
    {
        public int UserId { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public bool IncludePostedOnly { get; set; } = true;
    }
}
