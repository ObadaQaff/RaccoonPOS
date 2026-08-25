using System;
using System.Collections.Generic;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public static class AccountingTextLocalizer
    {
        public static string ToArabic(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value ?? string.Empty;

            var result = value;
            result = result.Replace("Reversal of JE", "\u0639\u0643\u0633 \u0627\u0644\u0642\u064a\u062f", StringComparison.OrdinalIgnoreCase);
            result = result.Replace("Repost voucher", "\u0625\u0639\u0627\u062f\u0629 \u062a\u0631\u062d\u064a\u0644 \u0633\u0646\u062f", StringComparison.OrdinalIgnoreCase);
            result = result.Replace("Repost invoice", "\u0625\u0639\u0627\u062f\u0629 \u062a\u0631\u062d\u064a\u0644 \u0641\u0627\u062a\u0648\u0631\u0629", StringComparison.OrdinalIgnoreCase);
            result = result.Replace("Repost stock document", "\u0625\u0639\u0627\u062f\u0629 \u062a\u0631\u062d\u064a\u0644 \u0633\u0646\u062f \u0645\u062e\u0632\u0648\u0646", StringComparison.OrdinalIgnoreCase);
            result = result.Replace("after update", "\u0628\u0639\u062f \u0627\u0644\u062a\u062d\u062f\u064a\u062b", StringComparison.OrdinalIgnoreCase);
            result = result.Replace("POSJournal", "\u0642\u064a\u062f \u0646\u0642\u0637\u0629 \u0627\u0644\u0628\u064a\u0639", StringComparison.OrdinalIgnoreCase);
            result = result.Replace("Journal", "\u0642\u064a\u062f \u064a\u0648\u0645\u064a\u0629", StringComparison.OrdinalIgnoreCase);
            foreach (var translation in Translations)
                result = result.Replace(translation.Key, translation.Value, StringComparison.OrdinalIgnoreCase);

            return result;
        }

        public static string ReferenceLabel(string? referenceType, int? referenceId)
        {
            if (string.Equals(referenceType?.Trim(), "Reversal", StringComparison.OrdinalIgnoreCase))
                return referenceId.HasValue && referenceId.Value > 0
                    ? $"\u0639\u0643\u0633 \u0627\u0644\u0642\u064a\u062f #{referenceId.Value}"
                    : "\u0639\u0643\u0633 \u0627\u0644\u0642\u064a\u062f";

            var label = referenceType?.Trim() switch
            {
                "Invoice" => "فاتورة",
                "Voucher" => "سند",
                "StockDocument" => "سند مخزون",
                "FinancialTransaction" => "حركة مالية",
                "StockAdjustment" => "تسوية مخزون",
                _ => ToArabic(referenceType)
            };

            return string.IsNullOrWhiteSpace(label)
                ? string.Empty
                : referenceId.HasValue && referenceId.Value > 0
                    ? $"{label} #{referenceId.Value}"
                    : label;
        }

        private static readonly IReadOnlyDictionary<string, string> Translations =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["POS"] = "نقطة البيع",
                ["Stock document In"] = "سند إدخال مخزون",
                ["Stock document Out"] = "سند إخراج مخزون",
                ["Voucher Payment"] = "سند دفع",
                ["Voucher Receipt"] = "سند قبض",
                ["Payment voucher"] = "سند دفع",
                ["Receipt voucher"] = "سند قبض",
                ["FinancialTransaction"] = "حركة مالية",
                ["StockAdjustment"] = "تسوية مخزون",
                ["PaymentVoucher"] = "سند دفع",
                ["ReceiptVoucher"] = "سند قبض",
                ["PosSaleInvoice"] = "فاتورة مبيعات نقطة البيع",
                ["SaleInvoice"] = "فاتورة مبيعات",
                ["PurchaseInvoice"] = "فاتورة مشتريات",
                ["SaleReturn"] = "مرتجع مبيعات",
                ["PurchaseReturn"] = "مرتجع مشتريات",
                ["SessionOpening"] = "افتتاح الجلسة",
                ["SessionClosing"] = "إغلاق الجلسة",
                ["PostInvoiceJournal"] = "ترحيل قيد الفاتورة",
                ["PostFinancialTransaction"] = "ترحيل حركة مالية",
                ["Purchase return"] = "مرتجع مشتريات",
                ["Sales return"] = "مرتجع مبيعات",
                ["Purchase invoice"] = "فاتورة مشتريات",
                ["Invoice"] = "فاتورة",
                ["Exchange"] = "استبدال",
                ["Purchase"] = "مشتريات",
                ["Sale"] = "مبيعات",
                ["collection"] = "تحصيل الفاتورة",
                ["refund"] = "رد قيمة المرتجع",
                ["payment"] = "دفع الفاتورة",
                ["return settlement"] = "تسوية المرتجع",
                ["sale settlement"] = "تسوية المبيعات",
                ["settlement"] = "تسوية",
                ["discount"] = "خصم",
                ["sales tax"] = "ضريبة المبيعات",
                ["input tax"] = "ضريبة المدخلات",
                ["tax reversal"] = "عكس الضريبة",
                ["tax"] = "ضريبة",
                ["cost of goods sold"] = "تكلفة البضاعة المباعة",
                ["inventory release"] = "إخراج من المخزون",
                ["inventory recovery"] = "استرداد المخزون",
                ["inventory reversal"] = "عكس المخزون",
                ["cost reversal"] = "عكس التكلفة",
                ["return"] = "مرتجع",
                ["sales"] = "مبيعات",
                ["purchase"] = "مشتريات",
                ["Stock adjustment"] = "تسوية مخزون",
                ["Increase"] = "زيادة",
                ["Decrease"] = "نقص",
                ["Replace"] = "استبدال",
                ["CloseAndRecreate"] = "إغلاق وإعادة إنشاء"
            };
    }
}
