using RaccoonWarehouse.Domain.Enums;

namespace RaccoonWarehouse.Domain.Accounting.JournalEntries.DTOs
{
    public class JournalEntryPaymentDetailDto
    {
        public PaymentType PaymentType { get; set; }
        public decimal Amount { get; set; }
    }
}
