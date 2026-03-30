using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Employees.DTOs
{
    public class EmployeeCreateDto : IBaseDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AlternatePhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? NationalId { get; set; }
        public DateTime? HireDate { get; set; }
        public DateTime? TerminationDate { get; set; }
        public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
        public EmployeeGender? Gender { get; set; }
        public string? JobTitle { get; set; }
        public int? DepartmentId { get; set; }
        public int? BranchId { get; set; }
        public int? ManagerId { get; set; }
        public decimal? BasicSalary { get; set; }
        public string? Notes { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
