using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Permissions
{
    public class ReportPermission : BaseEntity
    {
        public string ReportKey { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool CanView { get; set; }
    }
}
