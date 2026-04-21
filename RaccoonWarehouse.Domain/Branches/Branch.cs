using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Branches
{
    public class Branch : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ArabicName { get; set; }
        public string? EnglishName { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; } = true;
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
