using RaccoonWarehouse.Domain.Base;

namespace RaccoonWarehouse.Domain.Settings
{
    public class AppSetting : BaseEntity
    {
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string? Description { get; set; }
    }
}
