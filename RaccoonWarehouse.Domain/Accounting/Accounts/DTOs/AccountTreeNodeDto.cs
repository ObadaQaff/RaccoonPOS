namespace RaccoonWarehouse.Domain.Accounting.Accounts.DTOs
{
    /// <summary>
    /// Represents an account node for visual chart of accounts tree rendering.
    /// </summary>
    public class AccountTreeNodeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public int? Level { get; set; }
        public string? Nature { get; set; }
        public string? Category { get; set; }
        public bool IsPosting { get; set; }
        public bool IsActive { get; set; }
        public List<AccountTreeNodeDto> Children { get; set; } = new();
    }
}
