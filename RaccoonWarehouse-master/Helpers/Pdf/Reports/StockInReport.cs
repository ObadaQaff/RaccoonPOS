using RaccoonWarehouse.Domain.StockDocuments.DTOs;
using RaccoonWarehouse.Helpers.Pdf;
using System.Collections.Generic;

namespace RaccoonWarehouse.Helpers.Pdf.Reports
{
    public class StockInReportDocument : BaseReportDocument
    {
        private readonly StockDocumentReadDto _doc;

        public StockInReportDocument(StockDocumentReadDto doc)
        {
            _doc = doc;
        }

        public override string ArabicTitle => "سند إدخال بضاعة";
        public override string EnglishTitle => "Stock In Document";

        public override Dictionary<string, string> InfoFields =>
            new Dictionary<string, string>
            {
                { "رقم السند", _doc.DocumentNumber },
                { "التاريخ", _doc.CreatedDate.ToString("yyyy/MM/dd") },
                { "النوع", "إدخال بضاعة" },
                { "المورد", _doc.Supplier?.Name ?? "-" },
                { "ملاحظات", _doc.Notes ?? "-" }
            };

        public override List<string> TableHeaders =>
            new List<string>
            {
                "المنتج",
                "الوحدة",
                "الكمية",
                "سعر الشراء",
                "سعر البيع",
                "تاريخ الانتهاء"
            };

        public override List<List<string>> TableRows
        {
            get
            {
                var rows = new List<List<string>>();
                foreach (var item in _doc.Items)
                {
                    rows.Add(new List<string>
                    {
                        item.Product?.Name ?? "",
                        item.ProductUnit?.Unit?.Name ?? "",
                        item.Quantity.ToString("0.##"),
                        item.PurchasePrice.ToString("N2"),
                        item.SalePrice.ToString("N2"),
                        item.ExpiryDate?.ToString("yyyy/MM/dd") ?? "-"
                    });
                }
                return rows;
            }
        }
    }
}
