using RaccoonWarehouse.Domain.Stock.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RaccoonWarehouse.Helpers.Pdf.Reports
{
    public class StockMovementsSummaryReportDocument : BaseReportDocument
    {
        private readonly IReadOnlyCollection<StockMovementDto> _rows;
        private readonly DateTime? _from;
        private readonly DateTime? _to;

        public StockMovementsSummaryReportDocument(IEnumerable<StockMovementDto> rows, DateTime? from, DateTime? to)
        {
            _rows = rows.ToList();
            _from = from;
            _to = to;
        }

        public override string ArabicTitle => "تفصيل حركة المخزون";
        public override string EnglishTitle => "Stock Movements Report";

        public override Dictionary<string, string> InfoFields =>
            new()
            {
                { "من تاريخ", _from?.ToString("yyyy/MM/dd") ?? "-" },
                { "إلى تاريخ", _to?.ToString("yyyy/MM/dd") ?? "-" },
                { "عدد الحركات", _rows.Count.ToString() },
                { "إجمالي الكمية", _rows.Sum(x => x.Quantity).ToString("N3") }
            };

        public override List<string> TableHeaders =>
            new()
            {
                "التاريخ",
                "رقم المستند",
                "نوع المستند",
                "الصنف",
                "الوحدة",
                "الكمية",
                "تم بواسطة"
            };

        public override List<List<string>> TableRows =>
            _rows.Select(x => new List<string>
            {
                x.Date.ToString("yyyy/MM/dd HH:mm"),
                x.DocumentNumber ?? "-",
                x.DocumentType ?? "-",
                x.ProductName ?? "-",
                x.UnitName ?? "-",
                x.Quantity.ToString("N3"),
                x.CreatedBy ?? "-"
            }).ToList();
    }
}
