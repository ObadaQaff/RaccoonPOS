namespace RaccoonWarehouse.Domain.Accounting.Accounts.DTOs
{
    /// <summary>
    /// Payload used to update mutable account node fields.
    /// </summary>
    public class UpdateAccountNodeDto
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
