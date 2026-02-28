using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Cashiers;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Users.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.FinancialTransactions.DTOs
{
    public class FinancialTransactionWriteDto : IBaseDto
    {
        public int Id { get; set; }

        public string TransactionNumber { get; set; } = null!;

        // Core
        public TransactionDirection Direction { get; set; }     // In / Out
        public PaymentMethod Method { get; set; }               // Cash/Visa/Bank...
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }

        // Source (Flexible link)
        public FinancialSourceType SourceType { get; set; }
        public int? SourceId { get; set; }                      // رقم الفاتورة/السند/الخ

        // Session / Cashier
        public int? CashierSessionId { get; set; }
        public CashierSessionWriteDto? CashierSession { get; set; }

        public int? CashierId { get; set; }
        public UserWriteDto? Cashier { get; set; }

        // Extra
        public string? Notes { get; set; }

        public FinancialTransactionStatus Status { get; set; }   // Posted / Voided

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }



        /*
        public int Id { get; set; }
        public string TransactionNumber { get; set; }
        public FinancialTransactionType Type { get; set; }
        public PaymentMethod Method { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public int? InvoiceId { get; set; }
        public int? VoucherId { get; set; }
        public int? CasherId { get; set; }
        public CashierSessionWriteDto? CashierSession { get; set; }
        public int? CashierSessionId { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }*/
    }

}