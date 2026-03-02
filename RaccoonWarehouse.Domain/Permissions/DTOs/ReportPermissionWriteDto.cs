using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Permissions.DTOs
{
    public class ReportPermissionWriteDto : IBaseDto
    {
        public int Id { get; set; }
        public string ReportKey { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool CanView { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
