using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Accounting.Enums;

namespace RaccoonWarehouse.Domain.Accounting.Accounts.DTOs
{
    public class AccountReadDto : IBaseDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public AccountType AccountType { get; set; }
        public bool IsPosting { get; set; }
        public bool IsActive { get; set; }
        public int? ParentAccountId { get; set; }
        public string? ParentAccountName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
