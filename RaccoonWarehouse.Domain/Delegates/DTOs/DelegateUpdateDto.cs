using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Delegates.DTOs
{
    public class DelegateUpdateDto : IBaseDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AlternatePhoneNumber { get; set; }
        public DelegateStatus Status { get; set; }
        public DelegateType DelegateType { get; set; }
        public int? RegionId { get; set; }
        public string? AreaName { get; set; }
        public DateTime? HireDate { get; set; }
        public string? Notes { get; set; }
        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
