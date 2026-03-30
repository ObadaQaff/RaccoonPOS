using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Users;

namespace RaccoonWarehouse.Domain.Delegates
{
    public class Delegate : BaseEntity, ISoftDelete
    {
        public int? UserId { get; set; }
        public User? User { get; set; }
        public string Code { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AlternatePhoneNumber { get; set; }
        public DelegateStatus Status { get; set; } = DelegateStatus.Active;
        public DelegateType DelegateType { get; set; } = DelegateType.General;
        public int? RegionId { get; set; }
        public string? AreaName { get; set; }
        public DateTime? HireDate { get; set; }
        public string? Notes { get; set; }
        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
