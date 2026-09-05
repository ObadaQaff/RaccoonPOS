using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Invoices.DTOs
{
    public class InvoicePaymentReadDto : IBaseDto
    {
        public int Id { get; set; }
        public PaymentType PaymentType { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
