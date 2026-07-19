namespace RaccoonWarehouse.Domain.Accounting.Accounts.DTOs
{
    /// <summary>
    /// Payload used to create a new account node under a parent account.
    /// </summary>
    public class CreateAccountNodeDto
    {
        public int? ParentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Nature { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
