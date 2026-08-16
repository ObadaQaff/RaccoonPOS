namespace RaccoonWarehouse.Domain.CostCenters.DTOs
{
    public class CostCenterNodeDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public bool IsActive { get; set; }
        public List<CostCenterNodeDto> Children { get; set; } = new();
    }
}
