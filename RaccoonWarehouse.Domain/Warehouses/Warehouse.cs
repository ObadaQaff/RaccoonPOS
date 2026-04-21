using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Warehouses
{
    public class Warehouse : BaseEntity
    {
        public string? Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Description { get; set; }
        public int? PhoneNumber { get; set; }
        public int? BranchId { get; set; }
        public WarehouseStatus Status { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
