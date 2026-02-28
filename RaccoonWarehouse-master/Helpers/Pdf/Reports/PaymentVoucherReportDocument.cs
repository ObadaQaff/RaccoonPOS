using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using RaccoonWarehouse.Helpers.Pdf;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
using System.Collections.Generic;
using System.Linq;

namespace RaccoonWarehouse.Helpers.Pdf.Reports
{
    public class PaymentVoucherReportDocument : BaseReportDocument
    {
        private readonly VoucherWriteDto _voucher;

        public PaymentVoucherReportDocument(VoucherWriteDto voucher)
        {
            _voucher = voucher;
        }

        public override string ArabicTitle => "سند دفع";
        public override string EnglishTitle => "Payment Voucher";

        public override Dictionary<string, string> InfoFields =>
            new Dictionary<string, string>
            {
                { "رقم السند", _voucher.VoucherNumber },
                { "التاريخ", _voucher.CreatedDate.ToString("yyyy/MM/dd") },
                { "المستفيد / الجهة", _voucher.CustomerId?.ToString() ?? "-" },
                { "المبلغ", _voucher.Amount.ToString("N2") },
                { "طريقة الدفع", _voucher.PaymentType.ToString() },
                { "ملاحظات", _voucher.Notes ?? "-" }
            };

        public override List<string> TableHeaders =>
            new List<string>
            {
                "رقم الشيك",
                "البنك",
                "المبلغ",
                "تاريخ الاستحقاق",
                "ملاحظات"
            };

        public override List<List<string>> TableRows
        {
            get
            {
                if (_voucher.Checks == null || _voucher.Checks.Count == 0)
                    return new List<List<string>>();

                return _voucher.Checks.Select(c => new List<string>
                {
                    c.CheckNumber,
                    c.BankName,
                    c.Amount.ToString("N2"),
                    c.DueDate.ToString("yyyy/MM/dd"),
                    c.Notes ?? ""
                }).ToList();
            }
        }
    }
}
    