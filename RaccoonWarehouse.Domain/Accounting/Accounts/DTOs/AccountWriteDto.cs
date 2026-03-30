using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Accounting.Enums;

namespace RaccoonWarehouse.Domain.Accounting.Accounts.DTOs
{
    public class AccountWriteDto : IBaseDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public AccountType AccountType { get; set; }
        public bool IsPosting { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public int? ParentAccountId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
