using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Permissions.DTOs
{
    public class RolePermissionWriteDto : IBaseDto
    {
        public int Id { get; set; }
        public UserRole Role { get; set; }
        public string PermissionKey { get; set; } = string.Empty;
        public bool IsAllowed { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
