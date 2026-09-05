using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Invoices
{
    public class InvoicePayment : BaseEntity
    {
        public int InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }
        public PaymentType PaymentType { get; set; }
        public decimal Amount { get; set; }
    }
}
