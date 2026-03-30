using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Delegates.DTOs
{
    public class DelegateListFilterDto
    {
        public string? SearchText { get; set; }
        public DelegateStatus? Status { get; set; }
        public DelegateType? DelegateType { get; set; }
        public int? RegionId { get; set; }
        public bool OnlyActive { get; set; }
    }
}
