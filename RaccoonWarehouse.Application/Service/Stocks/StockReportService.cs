using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.Reports.Products.Dtos;
using RaccoonWarehouse.Domain.Reports.Products.Filters;
using RaccoonWarehouse.Domain.Reports.Stocks.Dtos;
using RaccoonWarehouse.Domain.Reports.Stocks.Filters;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Domain.Stock.Filters;
using RaccoonWarehouse.Domain.StockItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.Stocks
{
    public class StockReportService : IStockReportService
    {
        private readonly IUOW _uow;

        public StockReportService(IUOW uow)
        {
            _uow = uow;
        }

        // ───────────────────────────────────────────────
        // 1. CURRENT STOCK REPORT
        // ───────────────────────────────────────────────
        public async Task<List<CurrentStockDto>> GetCurrentStockAsync()
        {
            var repo = _uow.GetRepository<Stock>();

            var stocks = await repo.GetAllWithIncludeAsync(
                s => s.Product,
                s => s.ProductUnit,
                s => s.ProductUnit.Unit
            );

            return stocks.Select(s => new CurrentStockDto
            {
                ProductId = s.ProductId,
                ProductName = s.Product?.Name,
                ITEMCODE = s.Product?.ITEMCODE.ToString(),
                UnitName = s.ProductUnit?.Unit?.Name,
                Quantity = s.Quantity,
                MinimumQuantity = s.Product?.MiniQuantity
            }).ToList();
        }


        // ───────────────────────────────────────────────
        // 2. STOCK MOVEMENTS REPORT
        // ───────────────────────────────────────────────
        public async Task<List<StockMovementDto>> GetStockMovementsAsync(DateTime? from, DateTime? to)
        {
            var stockItemRepo = _uow.GetRepository<StockItem>();
            var invoiceRepo = _uow.GetRepository<Invoice>();
            var lineRepo = _uow.GetRepository<InvoiceLine>();

            // ===============================
            // 1️⃣ حركات المستودع (كما هي)
            // ===============================

            IQueryable<StockItem> stockQuery = stockItemRepo.AsQueryable()
                .Include(i => i.Stock)
                .Include(i => i.Product)
                .Include(i => i.ProductUnit)
                    .ThenInclude(pu => pu.Unit);

            if (from.HasValue)
                stockQuery = stockQuery.Where(x => x.Stock.CreatedDate >= from.Value);

            if (to.HasValue)
                stockQuery = stockQuery.Where(x => x.Stock.CreatedDate <= to.Value);

          

            var stockItems = await stockQuery.ToListAsync();

            var stockMovements = stockItems.Select(x => new StockMovementDto
            {
                StockItemId = x.Id,
                StockDocumentId = x.StockId,
                Date = x.Stock?.CreatedDate ?? DateTime.MinValue,
                DocumentNumber = x.Stock?.DocumentNumber,
                DocumentType = x.Stock?.Type.ToString(),

                ProductId = x.ProductId,
                ProductName = x.Product?.Name,
                UnitName = x.ProductUnit?.Unit?.Name,
                Quantity = x.Quantity,

                PurchasePrice = x.PurchasePrice,
                SalePrice = x.SalePrice,
                CreatedBy = x.Stock?.Supplier?.Name ?? ""
            }).ToList();


            // ===============================
            // 2️⃣ حركات الفواتير (كخروج)
            // ===============================

            var invoicesQuery = invoiceRepo.AsQueryable()
                .Where(i => i.InvoiceType == InvoiceType.Sale);

            if (from.HasValue)
                invoicesQuery = invoicesQuery.Where(i => i.CreatedDate >= from.Value);

            if (to.HasValue)
                invoicesQuery = invoicesQuery.Where(i => i.CreatedDate <= to.Value);

            var invoiceIds = await invoicesQuery.Select(i => i.Id).ToListAsync();

            var lines = await lineRepo.AsQueryable()
                .Include(l => l.Product)
                .Include(l => l.ProductUnit).ThenInclude(pu => pu.Unit)
                .Where(l => invoiceIds.Contains(l.InvoiceId))
                .ToListAsync();

            var invoicesMap = await invoicesQuery
                .Select(i => new { i.Id, i.InvoiceNumber, i.CreatedDate })
                .ToDictionaryAsync(x => x.Id, x => x);

            var invoiceMovements = lines.Select(l =>
            {
                var inv = invoicesMap[l.InvoiceId];

                return new StockMovementDto
                {
                    StockItemId = 0, // لأنها ليست StockItem
                    StockDocumentId = l.InvoiceId,

                    Date = inv.CreatedDate,
                    DocumentNumber = inv.InvoiceNumber,
                    DocumentType = "Sale Invoice",

                    ProductId = l.ProductId,
                    ProductName = l.Product?.Name,
                    UnitName = l.ProductUnit?.Unit?.Name,

                    // 🔥 أهم نقطة: الفاتورة = خروج
                    Quantity = -l.Quantity,

                    PurchasePrice = l.UnitCost,
                    SalePrice = l.UnitPrice,
                    CreatedBy = "Invoice"
                };
            }).ToList();


            // ===============================
            // 3️⃣ دمج المصدرين
            // ===============================

            var final = stockMovements
                .Concat(invoiceMovements)
                .OrderByDescending(x => x.Date)
                .ToList();

            return final;
        }

        // ───────────────────────────────────────────────
        // 3. LOW STOCK REPORT
        // ───────────────────────────────────────────────
        public async Task<List<LowStockDto>> GetLowStockAsync()
        {
            var repo = _uow.GetRepository<Stock>();

            var query = repo.AsQueryable()
                .Include(s => s.Product)
                .Include(s => s.ProductUnit).ThenInclude(pu => pu.Unit)
                .Where(s => s.Product.MiniQuantity != null &&
                            s.Quantity <= s.Product.MiniQuantity);

            var stocks = await query.ToListAsync();

            return stocks.Select(s => new LowStockDto
            {
                ProductId = s.ProductId,
                ProductName = s.Product.Name,
                ITEMCODE = s.Product.ITEMCODE.ToString(),
                UnitName = s.ProductUnit.Unit.Name,
                CurrentQuantity = s.Quantity,
                MinimumQuantity = s.Product.MiniQuantity ?? 0
            }).ToList();
        }


    public async Task<List<StockBalanceByDateDto>> GetStockBalanceByDateAsync(DateTime date, bool includeInvoices = true)
        {
            // نخليها لنهاية اليوم
            var end = date.Date.AddDays(1).AddTicks(-1);

            // ✅ 1) حركات المخزون من StockItem (الطريقة القديمة)
            var stockItemRepo = _uow.GetRepository<StockItem>();
            var stockItemsQ = stockItemRepo.AsQueryable()
                .Include(i => i.Stock)
                .Include(i => i.Product)
                .Include(i => i.ProductUnit).ThenInclude(pu => pu.Unit)
                .Where(i => i.Stock != null && i.Stock.CreatedDate <= end);

            var stockItems = await stockItemsQ.ToListAsync();

            // نجمعها حسب (ProductId, ProductUnitId)
            var dict = new Dictionary<(int productId, int unitId), StockBalanceByDateDto>();

            foreach (var it in stockItems)
            {
                var key = (it.ProductId, it.ProductUnitId);

                if (!dict.TryGetValue(key, out var row))
                {
                    row = new StockBalanceByDateDto
                    {
                        ProductId = it.ProductId,
                        ProductUnitId = it.ProductUnitId,
                        ProductName = it.Product?.Name,
                        ITEMCODE = it.Product?.ITEMCODE.ToString(),
                        UnitName = it.ProductUnit?.Unit?.Name,
                        MinimumQuantity = it.Product?.MiniQuantity ?? 0m
                    };
                    dict[key] = row;
                }

                // ✅ هنا أهم سطر: نحول نوع سند المخزون إلى (+) أو (-)
                // عدّل الدالة تحت حسب enum تبعك
                var signedQty = GetSignedStockQty(it.Stock!.Type, it.Quantity);
                row.Quantity += signedQty;
            }

            // ✅ 2) (اختياري) إضافة الفواتير كمصدر حركة خروج/دخول
            if (includeInvoices)
            {
                var invoiceRepo = _uow.GetRepository<Invoice>();
                var invoiceLineRepo = _uow.GetRepository<InvoiceLine>();

                var invIds = await invoiceRepo.GetAllAsQueryable()
                    .Where(x => x.CreatedDate <= end)
                    .Select(x => new { x.Id, x.InvoiceType })
                    .ToListAsync();

                if (invIds.Count > 0)
                {
                    var idList = invIds.Select(x => x.Id).ToList();
                    var lines = await invoiceLineRepo.GetAllAsQueryable()
                        .Where(l => idList.Contains(l.InvoiceId))
                        .Include(l => l.Product)
                        .Include(l => l.ProductUnit).ThenInclude(pu => pu.Unit)
                        .ToListAsync();

                    // نعمل lookup سريع لنوع الفاتورة
                    var typeById = invIds.ToDictionary(x => x.Id, x => x.InvoiceType);

                    foreach (var l in lines)
                    {
                        var key = (l.ProductId, l.ProductUnitId);

                        if (!dict.TryGetValue(key, out var row))
                        {
                            row = new StockBalanceByDateDto
                            {
                                ProductId = l.ProductId,
                                ProductUnitId = l.ProductUnitId,
                                ProductName = l.Product?.Name,
                                ITEMCODE = l.Product?.ITEMCODE.ToString(),
                                UnitName = l.ProductUnit?.Unit?.Name,
                                MinimumQuantity = l.Product?.MiniQuantity ?? 0m
                            };
                            dict[key] = row;
                        }

                        // ✅ بيع = خروج (-) ، مرتجع = دخول (+)
                        var invType = typeById[l.InvoiceId];
                        var signedQty = GetSignedInvoiceQty(invType, l.Quantity);
                        row.Quantity += signedQty;
                    }
                }
            }

            // ✅ StatusText
            foreach (var r in dict.Values)
            {
                r.StatusText = (r.MinimumQuantity > 0 && r.Quantity <= r.MinimumQuantity)
                    ? "تحت الحد الأدنى"
                    : "طبيعي";
            }

            return dict.Values
                .OrderBy(x => x.ProductName)
                .ThenBy(x => x.UnitName)
                .ToList();
        }

        /// <summary>
        /// ✅ عدّل هنا حسب enum تبع Stock.Type
        /// الفكرة: سند إدخال = + ، سند إخراج = -
        /// </summary>
        private static decimal GetSignedStockQty(object stockTypeEnum, decimal qty)
        {
            // إذا Stock.Type هو Enum: بنحوله لاسم ونقرر
            var name = stockTypeEnum?.ToString()?.ToLowerInvariant() ?? "";

            // غيّر الكلمات حسب مشروعك: (in/entry/purchase/adjustin) مقابل (out/issue/sale/adjustout)
            if (name.Contains("in") || name.Contains("entry") || name.Contains("purchase"))
                return +qty;

            if (name.Contains("out") || name.Contains("issue") || name.Contains("sale"))
                return -qty;

            // الافتراضي: صفر (أو اعتبره +)
            return 0m;
        }

        /// <summary>
        /// ✅ عدّل حسب enum InvoiceType عندك
        /// Sale = - , Return = +
        /// </summary>
        //private static decimal GetSignedInvoiceQty(object invoiceTypeEnum, decimal qty)
        //{
        //    var name = invoiceTypeEnum?.ToString()?.ToLowerInvariant() ?? "";

        //    if (name.Contains("sale"))
        //        return -qty;

        //    if (name.Contains("return"))
        //        return +qty;

        //    return 0m;
        //}




        public async Task<List<InventoryMovementSummaryRowDto>> GetInventoryMovementSummaryAsync(InventoryMovementSummaryFilterDto filter)
        {
            if (filter.From > filter.To)
                throw new ArgumentException("Invalid date range");

            var from = filter.From.Date;
            var to = filter.To.Date.AddDays(1).AddTicks(-1);

            var dict = new Dictionary<(int productId, int unitId), InventoryMovementSummaryRowDto>();

            // ===============================
            // 1) StockItems source (old)
            // ===============================
            var stockItemRepo = _uow.GetRepository<StockItem>();
            var stockItemsQ = stockItemRepo.AsQueryable()
                .Include(i => i.Stock)
                .Include(i => i.Product)
                .Include(i => i.ProductUnit).ThenInclude(pu => pu.Unit)
                .Where(i => i.Stock != null &&
                            i.Stock.CreatedDate >= from &&
                            i.Stock.CreatedDate <= to);

            if (filter.ProductId.HasValue)
                stockItemsQ = stockItemsQ.Where(i => i.ProductId == filter.ProductId.Value);

            var stockItems = await stockItemsQ.ToListAsync();

            foreach (var it in stockItems)
            {
                var key = (it.ProductId, it.ProductUnitId);

                if (!dict.TryGetValue(key, out var row))
                {
                    row = new InventoryMovementSummaryRowDto
                    {
                        ProductId = it.ProductId,
                        ProductUnitId = it.ProductUnitId,
                        ProductName = it.Product?.Name,
                        ITEMCODE = it.Product?.ITEMCODE.ToString(),
                        UnitName = it.ProductUnit?.Unit?.Name,
                        MinimumQuantity = it.Product?.MiniQuantity ?? 0m
                    };
                    dict[key] = row;
                }

                // ✅ حدد اتجاه الحركة من نوع السند
                var signed = GetSignedStockQty(it.Stock!.Type, it.Quantity);
                if (signed >= 0) row.InQty += signed;
                else row.OutQty += (-signed);
            }

            // ===============================
            // 2) Invoices source (Sale/Return)
            // ===============================
            if (filter.IncludeInvoices)
            {
                var invoiceRepo = _uow.GetRepository<Invoice>();
                var lineRepo = _uow.GetRepository<InvoiceLine>();

                var invQ = invoiceRepo.GetAllAsQueryable()
                    .Where(x => x.CreatedDate >= from && x.CreatedDate <= to);

                // Optional filter product via lines (later) but keep invQ simple

                var invTypes = await invQ
                    .Select(x => new { x.Id, x.InvoiceType })
                    .ToListAsync();

                if (invTypes.Count > 0)
                {
                    var ids = invTypes.Select(x => x.Id).ToList();
                    var typeById = invTypes.ToDictionary(x => x.Id, x => x.InvoiceType);

                    IQueryable<InvoiceLine> linesQ = lineRepo.GetAllAsQueryable()
                        .Where(l => ids.Contains(l.InvoiceId))   // ✅ IMPORTANT: keep only invoice lines for invoices in range
                        .Include(l => l.Product)
                        .Include(l => l.ProductUnit)
                            .ThenInclude(pu => pu.Unit);

                    if (filter.ProductId.HasValue)
                        linesQ = linesQ.Where(l => l.ProductId == filter.ProductId.Value);

                    var lines = await linesQ.ToListAsync();

                    foreach (var l in lines)
                    {
                        // ✅ SAFE: avoid key not found
                        if (!typeById.TryGetValue(l.InvoiceId, out var invType))
                            continue; // or treat as 0 movement

                        var key = (l.ProductId, l.ProductUnitId);

                        if (!dict.TryGetValue(key, out var row))
                        {
                            row = new InventoryMovementSummaryRowDto
                            {
                                ProductId = l.ProductId,
                                ProductUnitId = l.ProductUnitId,
                                ProductName = l.Product?.Name,
                                ITEMCODE = l.Product?.ITEMCODE.ToString(),
                                UnitName = l.ProductUnit?.Unit?.Name,
                                MinimumQuantity = l.Product?.MiniQuantity ?? 0m
                            };
                            dict[key] = row;
                        }

                        // ✅ بيع = خروج, مرتجع = دخول
                        var signed = GetSignedInvoiceQty(invType, l.Quantity);
                        if (signed >= 0) row.InQty += signed;
                        else row.OutQty += (-signed);
                    }
                }
            }

            // Status
            foreach (var r in dict.Values)
            {
                r.StatusText = (r.MinimumQuantity > 0 && (r.NetQty <= r.MinimumQuantity))
                    ? "قريب/تحت الحد"
                    : "طبيعي";
            }

            return dict.Values
                .OrderBy(x => x.ProductName)
                .ThenBy(x => x.UnitName)
                .ToList();
        }

        // ========= Helpers =========
        // ✅ عدّل المنطق حسب Enum الحقيقي عندك (أفضل تعملها switch)
        // حالياً string contains عشان ما نكسّر مشروعك
        //private static decimal GetSignedStockQty(object stockTypeEnum, decimal qty)
        //{
        //    var name = stockTypeEnum?.ToString()?.ToLowerInvariant() ?? "";

        //    // داخل
        //    if (name.Contains("in") || name.Contains("entry") || name.Contains("purchase") || name.Contains("receive"))
        //        return +qty;

        //    // خارج
        //    if (name.Contains("out") || name.Contains("issue") || name.Contains("sale") || name.Contains("dispatch"))
        //        return -qty;

        //    return 0m;
        //}

        private static decimal GetSignedInvoiceQty(object invoiceTypeEnum, decimal qty)
        {
            var name = invoiceTypeEnum?.ToString()?.ToLowerInvariant() ?? "";

            if (name.Contains("sale"))
                return -qty;

            if (name.Contains("return"))
                return +qty;

            return 0m;
        }



        public async Task<List<ProductProfitRowDto>> GetProductProfitAsync(ProductProfitFilterDto filter)
        {
            if (filter.From > filter.To)
                throw new ArgumentException("Invalid date range");

            var from = filter.From.Date;
            var to = filter.To.Date.AddDays(1).AddTicks(-1);

            var lineRepo = _uow.GetRepository<InvoiceLine>();

            // ✅ نجيب بيانات خام (Projection) ثم نجمعها بالذاكرة (أسلم وأسهل)
            var q = lineRepo.GetAllAsQueryable()
                .Include(l => l.Invoice)
                .Include(l => l.Product)
                .Include(l => l.ProductUnit).ThenInclude(pu => pu.Unit)
                .Where(l => l.Invoice != null &&
                            l.Invoice.CreatedDate >= from &&
                            l.Invoice.CreatedDate <= to);

            if (filter.ProductId.HasValue)
                q = q.Where(l => l.ProductId == filter.ProductId.Value);

            if (!filter.IncludeReturns)
                q = q.Where(l => l.Invoice!.InvoiceType == InvoiceType.Sale);

            var raw = await q.Select(l => new
            {
                l.ProductId,
                ProductName = l.Product!.Name,
                ItemCode = l.Product!.ITEMCODE.ToString(),
                UnitName = l.ProductUnit!.Unit!.Name,

                Qty = l.Quantity,
                LineSub = l.LineSubTotal,
                Tax = l.TaxAmount,
                UnitCost = l.UnitCost,

                InvType = l.Invoice!.InvoiceType,
                InvSubTotal = l.Invoice!.SubTotal,
                InvDiscount = (l.Invoice!.DiscountAmount ?? 0m),

                // إذا بدك GroupByUnit
                l.ProductUnitId
            }).ToListAsync();

            // ✅ تجميع
            var dict = new Dictionary<string, ProductProfitRowDto>();

            foreach (var x in raw)
            {
                // ✅ بيع = + ، مرتجع = -
                var sign = (x.InvType == InvoiceType.Return) ? -1m : 1m;

                // ✅ توزيع الخصم على السطر: (LineSub / InvoiceSubTotal) * InvoiceDiscount
                var invSub = x.InvSubTotal;
                var invDisc = x.InvDiscount;

                decimal allocatedDiscount = 0m;
                if (invSub > 0 && invDisc > 0)
                    allocatedDiscount = (x.LineSub / invSub) * invDisc;

                // ✅ قيم السطر (موقعة حسب نوع الفاتورة)
                var qty = sign * x.Qty;
                var sub = sign * x.LineSub;
                var disc = sign * allocatedDiscount;
                var tax = sign * x.Tax;
                var cogs = sign * (x.Qty * x.UnitCost);

                // ✅ مفتاح التجميع
                var key = filter.GroupByUnit
                    ? $"{x.ProductId}:{x.ProductUnitId}"
                    : $"{x.ProductId}";

                if (!dict.TryGetValue(key, out var row))
                {
                    row = new ProductProfitRowDto
                    {
                        ProductId = x.ProductId,
                        ProductName = x.ProductName,
                        ITEMCODE = x.ItemCode,
                        UnitName = filter.GroupByUnit ? x.UnitName : null
                    };
                    dict[key] = row;
                }

                row.SalesQty += qty;
                row.SubTotal += sub;
                row.Discount += disc;
                row.Tax += tax;
                row.COGS += cogs;
            }

            // ✅ مشتقات
            foreach (var r in dict.Values)
            {
                r.NetSales = r.SubTotal - r.Discount;     // قبل الضريبة
                r.GrossProfit = r.NetSales - r.COGS;
                r.Margin = (r.NetSales == 0) ? 0 : Math.Round((r.GrossProfit / r.NetSales) * 100m, 2);
            }

            return dict.Values
                .OrderByDescending(x => x.GrossProfit)
                .ToList();
        }

        public async Task<List<InactiveProductRowDto>> GetInactiveProductsAsync(InactiveProductsFilterDto filter)
        {
            var today = filter.AsOfDate?.Date ?? DateTime.Today;
            var cutoffDate = today.AddDays(-filter.DaysWithoutMovement);

            var productRepo = _uow.GetRepository<Product>();
            var stockRepo = _uow.GetRepository<Stock>();
            var stockItemRepo = _uow.GetRepository<StockItem>();
            var invoiceLineRepo = _uow.GetRepository<InvoiceLine>();

            // Get all products
            var products = await productRepo.GetAllAsQueryable().ToListAsync();

            // Last stock movement
            var stockMovements = await stockItemRepo.GetAllAsQueryable()
                .Include(x => x.Stock)
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    LastDate = g.Max(x => x.Stock.CreatedDate)
                })
                .ToListAsync();

            // Last invoice movement
            var invoiceMovements = await invoiceLineRepo.GetAllAsQueryable()
                .Include(x => x.Invoice)
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    LastDate = g.Max(x => x.Invoice.CreatedDate)
                })
                .ToListAsync();

            // Current stock
            var stocks = await stockRepo.GetAllAsQueryable()
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Qty = g.Sum(x => x.Quantity)
                })
                .ToListAsync();

            var stockDict = stocks.ToDictionary(x => x.ProductId, x => x.Qty);

            var result = new List<InactiveProductRowDto>();

            foreach (var p in products)
            {
                var lastStockDate = stockMovements
                    .FirstOrDefault(x => x.ProductId == p.Id)?.LastDate;

                var lastInvoiceDate = invoiceMovements
                    .FirstOrDefault(x => x.ProductId == p.Id)?.LastDate;

                DateTime? lastMovement = null;

                if (lastStockDate.HasValue && lastInvoiceDate.HasValue)
                    lastMovement = lastStockDate > lastInvoiceDate ? lastStockDate : lastInvoiceDate;
                else
                    lastMovement = lastStockDate ?? lastInvoiceDate;

                if (lastMovement == null || lastMovement <= cutoffDate)
                {
                    var currentStock = stockDict.ContainsKey(p.Id) ? stockDict[p.Id] : 0;

                    if (filter.IncludeZeroStockOnly && currentStock > 0)
                        continue;

                    var days = lastMovement == null
                        ? 999
                        : (today - lastMovement.Value).Days;

                    result.Add(new InactiveProductRowDto
                    {
                        ProductId = p.Id,
                        ProductName = p.Name,
                        ITEMCODE = p.ITEMCODE.ToString(),
                        CurrentStock = currentStock,
                        MinimumQuantity = p.MiniQuantity ?? 0,
                        LastMovementDate = lastMovement,
                        DaysSinceLastMovement = days
                    });
                }
            }

            return result
                .OrderByDescending(x => x.DaysSinceLastMovement)
                .ToList();
        }
      

        // ✅ Helper: يتأكد إن نوع السند هو "جرد"
        private static bool IsInventoryCountType(object stockTypeEnum)
        {
            var name = stockTypeEnum?.ToString()?.ToLowerInvariant() ?? "";

            // عدّل الكلمات حسب enum عندك
            // مثال: InventoryCount / StockTake / Count / Inventory
            return name.Contains("count")
                || name.Contains("inventory")
                || name.Contains("stocktake")
                || name.Contains("stock_take");
        }


    }




    // ───────────────────────────────────────────────
    // INTERFACE
    // ───────────────────────────────────────────────
    public interface IStockReportService
    {
        Task<List<CurrentStockDto>> GetCurrentStockAsync();
        Task<List<StockMovementDto>> GetStockMovementsAsync(DateTime? from, DateTime? to);
        Task<List<LowStockDto>> GetLowStockAsync();
        Task<List<StockBalanceByDateDto>> GetStockBalanceByDateAsync(DateTime date, bool includeInvoices = true);
        Task<List<InventoryMovementSummaryRowDto>> GetInventoryMovementSummaryAsync(InventoryMovementSummaryFilterDto filter);
        Task<List<ProductProfitRowDto>> GetProductProfitAsync(ProductProfitFilterDto filter);
        Task<List<InactiveProductRowDto>> GetInactiveProductsAsync(InactiveProductsFilterDto filter);

    }

}
