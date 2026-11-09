using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Permissions
{
    public class PermissionDefinition : BaseEntity
    {
        public string Key { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LegacyReportKey { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
