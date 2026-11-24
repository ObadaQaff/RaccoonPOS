using System.Globalization;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Helpers.Pdf;

public static class FinancialTransactionPrintDocument
{
    private static string T(string ar, string en) => UiText.T(ar, en);

    public static PurchasingDocument Create(
        FinancialTransactionReadDto transaction,
        bool isReceipt,
        string? partyName,
        string? partyPhone)
    {
        var title = isReceipt ? T("سند قبض", "Receipt Voucher") : T("سند دفع", "Payment Voucher");
        var partyLabel = isReceipt ? T("استلمنا من: ", "Received from: ") : T("دفعنا إلى: ", "Paid to: ");
        var amount = transaction.Amount.ToString("0.000", CultureInfo.InvariantCulture);

        return new PurchasingDocument
        {
            FileName = isReceipt ? "ReceiptTransaction" : "PaymentTransaction",
            Title = title,
            Number = transaction.TransactionNumber,
            Branding = DocumentPrintRuntime.Branding(),
            Information = new()
            {
                T("التاريخ: ", "Date: ") + transaction.TransactionDate.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture),
                partyLabel + (string.IsNullOrWhiteSpace(partyName) ? T("غير محدد", "Not specified") : partyName),
                T("هاتف الجهة: ", "Party phone: ") + (string.IsNullOrWhiteSpace(partyPhone) ? "—" : partyPhone),
                T("طريقة الدفع: ", "Payment method: ") + transaction.Method,
                T("رقم المرجع: ", "Reference: ") + transaction.TransactionNumber,
                T("المبلغ: ", "Amount: ") + amount
            },
            Totals = new() { T("قيمة السند: ", "Voucher amount: ") + amount },
            Notes = transaction.Notes,
            ClosingText = T("توقيع المستلم: __________________    توقيع الموظف: __________________", "Recipient signature: __________________    Employee signature: __________________")
        };
    }
}
