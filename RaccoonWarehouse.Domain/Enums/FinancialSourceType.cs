using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Enums
{
    public enum FinancialSourceType
    {
        Manual = 1,          // إدخال يدوي من نافذة قبض/صرف
        PosSaleInvoice = 2,  // فاتورة بيع من POS
        SaleInvoice = 3,     // فاتورة بيع عادية
        PurchaseInvoice = 4, // فاتورة شراء
        ReceiptVoucher = 5,  // سند قبض
        PaymentVoucher = 6,  // سند صرف
        SaleReturn = 7,      // مرتجع بيع
        PurchaseReturn = 8,  // مرتجع شراء
        Expense = 9,         // مصروف
        SessionOpening = 10,
        SessionClosing = 11

    }

}
