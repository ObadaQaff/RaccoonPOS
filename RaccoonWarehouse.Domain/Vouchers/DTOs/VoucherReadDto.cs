using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Cashiers;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Checks;
using RaccoonWarehouse.Domain.Checks.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Users.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Vouchers.DTOs
{
    public class VoucherReadDto: IBaseDto
    {
        public int Id { get; set; }
        public string? VoucherNumber { get; set; }
        public decimal Amount { get; set; }
        public VoucherType VoucherType { get; set; }
        public PaymentType PaymentType { get; set; }
        public int? CasherId { get; set; }
        public int? SupplierId { get; set; }
        public int? CustomerId { get; set; }
        public string? Notes { get; set; }
        public ICollection<CheckReadDto>? Checks { get; set; } = new List<CheckReadDto>();
        public CashierSessionReadDto? CashierSession { get; set; }
        public int? CashierSessionId { get; set; }
        public UserReadDto? User { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
