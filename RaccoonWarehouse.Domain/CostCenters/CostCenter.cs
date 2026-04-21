using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.CostCenters
{
    public class CostCenter : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ArabicName { get; set; }
        public string? EnglishName { get; set; }
        public int? ParentCostCenterId { get; set; }
        public CostCenter? ParentCostCenter { get; set; }
        public ICollection<CostCenter> Children { get; set; } = new List<CostCenter>();
        public int Level { get; set; }
        public bool IsActive { get; set; } = true;
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
