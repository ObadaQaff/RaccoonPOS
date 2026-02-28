using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.FinancialTransactions.DTOs
{
    public class FinancialPostDto :IBaseDto
    {


        public int Id { get; set; }
        public TransactionDirection Direction { get; set; }
        public PaymentMethod Method { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }

        public FinancialSourceType SourceType { get; set; }
        public int? SourceId { get; set; }

        public int? CashierSessionId { get; set; }
        public int? CashierId { get; set; }

        public string? Notes { get; set; }
        
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
