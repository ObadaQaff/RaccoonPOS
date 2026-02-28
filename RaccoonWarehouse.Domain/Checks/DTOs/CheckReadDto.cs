using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Vouchers;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Checks.DTOs
{
    public class CheckReadDto : IBaseDto
    {
        public int Id { get; set; }
        public string CheckNumber { get; set; }
        public string BankName { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }

        public int? VoucherId { get; set; }
        public VoucherReadDto? Voucher { get; set; }
        public int? InvoiceId { get; set; }
        public InvoiceReadDto? Invoice { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

    }
}
