using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Interface;
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
using RaccoonWarehouse.Domain.StockTransactions;
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

        public async Task<List<CurrentStockDto>> GetCurrentStockAsync()
        {
            var repo = _uow.GetRepository<Stock>();

            var stocks = await repo.AsQueryable()
                .Include(s => s.Product)
                    .ThenInclude(p => p.ProductUnits)
                        .ThenInclude(pu => pu.Unit)
                .Include(s => s.ProductUnit)
                    .ThenInclude(pu => pu.Unit)
                .ToListAsync();

            return stocks
                .GroupBy(s => s.ProductId)
                .Select(group =>
                {
                    var sample = group.First();
                    var baseUnit = GetBaseUnit(sample.Product, sample.ProductUnit);

                    return new CurrentStockDto
                    {
                        ProductId = group.Key,
                        ProductName = sample.Product?.Name,
                        ITEMCODE = sample.Product?.ITEMCODE.ToString(),
                        UnitName = baseUnit?.Unit?.Name ?? sample.ProductUnit?.Unit?.Name,
                        Quantity = group.Sum(GetNormalizedStockQuantity),
                        MinimumQuantity = sample.Product?.MiniQuantity
                    };
                })
                .OrderBy(x => x.ProductName)
                .ToList();
        }

        public async Task<List<StockMovementDto>> GetStockMovementsAsync(DateTime? from, DateTime? to)
        {
            var transactionRepo = _uow.GetRepository<StockTransaction>();

            IQueryable<StockTransaction> query = transactionRepo.AsQueryable()
                .Include(x => x.Product)
                    .ThenInclude(p => p.ProductUnits)
                        .ThenInclude(pu => pu.Unit)
                .Include(x => x.ProductUnit)
                    .ThenInclude(pu => pu.Unit)
                .Include(x => x.Invoice)
                .Include(x => x.Casher)
                .Include(x => x.Customer);

            if (from.HasValue)
                query = query.Where(x => x.TransactionDate >= from.Value);

            if (to.HasValue)
                query = query.Where(x => x.TransactionDate <= to.Value);

            return (await query.ToListAsync())
                .Select(x => new StockMovementDto
                {
                    StockItemId = x.Id,
                    StockDocumentId = x.InvoiceId ?? x.VoucherId ?? x.StockId ?? 0,
                    Date = x.TransactionDate,
                    DocumentNumber = x.Invoice?.InvoiceNumber ?? x.Voucher?.VoucherNumber ?? (x.StockId.HasValue ? $"STK-{x.StockId}" : string.Empty),
                    DocumentType = ResolveDocumentType(x),
                    ProductId = x.ProductId,
                    ProductName = x.Product?.Name,
                    UnitName = GetBaseUnit(x.Product, x.ProductUnit)?.Unit?.Name ?? x.ProductUnit?.Unit?.Name,
                    Quantity = x.BaseQuantity != 0 ? x.BaseQuantity : x.Quantity * GetUnitFactor(x.ProductUnit, x.QuantityPerUnitSnapshot),
                    PurchasePrice = x.ProductUnit?.PurchasePrice ?? 0m,
                    SalePrice = x.UnitPrice,
                    CreatedBy = x.Casher?.Name ?? x.Customer?.Name ?? string.Empty
                })
                .OrderByDescending(x => x.Date)
                .ToList();
        }

        public async Task<List<LowStockDto>> GetLowStockAsync()
        {
            var repo = _uow.GetRepository<Stock>();

            var stocks = await repo.AsQueryable()
                .Include(s => s.Product)
                    .ThenInclude(p => p.ProductUnits)
                        .ThenInclude(pu => pu.Unit)
                .Include(s => s.ProductUnit)
                    .ThenInclude(pu => pu.Unit)
                .Where(s => s.Product.MiniQuantity != null)
                .ToListAsync();

            return stocks
                .GroupBy(s => s.ProductId)
                .Select(group =>
                {
                    var sample = group.First();
                    return new LowStockDto
                    {
                        ProductId = group.Key,
                        ProductName = sample.Product?.Name,
                        ITEMCODE = sample.Product?.ITEMCODE.ToString(),
                        UnitName = GetBaseUnit(sample.Product, sample.ProductUnit)?.Unit?.Name ?? sample.ProductUnit?.Unit?.Name,
                        CurrentQuantity = group.Sum(GetNormalizedStockQuantity),
                        MinimumQuantity = sample.Product?.MiniQuantity ?? 0m
                    };
                })
                .Where(x => x.CurrentQuantity <= x.MinimumQuantity)
                .OrderBy(x => x.ProductName)
                .ToList();
        }

        public async Task<List<StockBalanceByDateDto>> GetStockBalanceByDateAsync(DateTime date, bool includeInvoices = true)
        {
            var end = date.Date.AddDays(1).AddTicks(-1);
            var transactionRepo = _uow.GetRepository<StockTransaction>();
            IQueryable<StockTransaction> query = transactionRepo.AsQueryable()
                .Include(x => x.Product)
                    .ThenInclude(p => p.ProductUnits)
                        .ThenInclude(pu => pu.Unit)
                .Include(x => x.ProductUnit)
                    .ThenInclude(pu => pu.Unit)
                .Where(x => x.TransactionDate <= end);

            if (!includeInvoices)
                query = query.Where(x => x.InvoiceId == null);

            var transactions = await query.ToListAsync();

            var dict = new Dictionary<int, StockBalanceByDateDto>();

            foreach (var item in transactions)
            {
                if (!dict.TryGetValue(item.ProductId, out var row))
                {
                    var baseUnit = GetBaseUnit(item.Product, item.ProductUnit);
                    row = new StockBalanceByDateDto
                    {
                        ProductId = item.ProductId,
                        ProductUnitId = baseUnit?.Id ?? item.ProductUnitId,
                        ProductName = item.Product?.Name,
                        ITEMCODE = item.Product?.ITEMCODE.ToString(),
                        UnitName = baseUnit?.Unit?.Name ?? item.ProductUnit?.Unit?.Name,
                        MinimumQuantity = item.Product?.MiniQuantity ?? 0m
                    };
                    dict[item.ProductId] = row;
                }

                row.Quantity += item.BaseQuantity != 0
                    ? item.BaseQuantity
                    : item.Quantity * GetUnitFactor(item.ProductUnit, item.QuantityPerUnitSnapshot);
            }

            foreach (var row in dict.Values)
            {
                row.StatusText = (row.MinimumQuantity > 0 && row.Quantity <= row.MinimumQuantity)
                    ? "تحت الحد الأدنى"
                    : "طبيعي";
            }

            return dict.Values
                .OrderBy(x => x.ProductName)
                .ThenBy(x => x.UnitName)
                .ToList();
        }

        public async Task<List<InventoryMovementSummaryRowDto>> GetInventoryMovementSummaryAsync(InventoryMovementSummaryFilterDto filter)
        {
            if (filter.From > filter.To)
                throw new ArgumentException("Invalid date range");

            var from = filter.From.Date;
            var to = filter.To.Date.AddDays(1).AddTicks(-1);

            var dict = new Dictionary<int, InventoryMovementSummaryRowDto>();
            var transactionRepo = _uow.GetRepository<StockTransaction>();
            var transactionsQ = transactionRepo.AsQueryable()
                .Include(x => x.Product)
                    .ThenInclude(p => p.ProductUnits)
                        .ThenInclude(pu => pu.Unit)
                .Include(x => x.ProductUnit)
                    .ThenInclude(pu => pu.Unit)
                .Where(x => x.TransactionDate >= from && x.TransactionDate <= to);

            if (!filter.IncludeInvoices)
                transactionsQ = transactionsQ.Where(x => x.InvoiceId == null);

            if (filter.ProductId.HasValue)
                transactionsQ = transactionsQ.Where(x => x.ProductId == filter.ProductId.Value);

            var transactions = await transactionsQ.ToListAsync();

            foreach (var item in transactions)
            {
                if (!dict.TryGetValue(item.ProductId, out var row))
                {
                    var baseUnit = GetBaseUnit(item.Product, item.ProductUnit);
                    row = new InventoryMovementSummaryRowDto
                    {
                        ProductId = item.ProductId,
                        ProductUnitId = baseUnit?.Id ?? item.ProductUnitId,
                        ProductName = item.Product?.Name,
                        ITEMCODE = item.Product?.ITEMCODE.ToString(),
                        UnitName = baseUnit?.Unit?.Name ?? item.ProductUnit?.Unit?.Name,
                        MinimumQuantity = item.Product?.MiniQuantity ?? 0m
                    };
                    dict[item.ProductId] = row;
                }

                var signed = item.BaseQuantity != 0
                    ? item.BaseQuantity
                    : item.Quantity * GetUnitFactor(item.ProductUnit, item.QuantityPerUnitSnapshot);

                if (signed >= 0)
                    row.InQty += signed;
                else
                    row.OutQty += -signed;
            }

            foreach (var row in dict.Values)
            {
                row.StatusText = (row.MinimumQuantity > 0 && row.NetQty <= row.MinimumQuantity)
                    ? "قريب/تحت الحد"
                    : "طبيعي";
            }

            return dict.Values
                .OrderBy(x => x.ProductName)
                .ThenBy(x => x.UnitName)
                .ToList();
        }

        public async Task<List<ProductProfitRowDto>> GetProductProfitAsync(ProductProfitFilterDto filter)
        {
            if (filter.From > filter.To)
                throw new ArgumentException("Invalid date range");

            var from = filter.From.Date;
            var to = filter.To.Date.AddDays(1).AddTicks(-1);

            var lineRepo = _uow.GetRepository<InvoiceLine>();
            var linesQ = lineRepo.GetAllAsQueryable()
                .Include(l => l.Invoice)
                .Include(l => l.Product)
                    .ThenInclude(p => p.ProductUnits)
                        .ThenInclude(pu => pu.Unit)
                .Include(l => l.ProductUnit)
                    .ThenInclude(pu => pu.Unit)
                .Where(l => l.Invoice != null &&
                            l.Invoice.CreatedDate >= from &&
                            l.Invoice.CreatedDate <= to);

            if (filter.ProductId.HasValue)
                linesQ = linesQ.Where(l => l.ProductId == filter.ProductId.Value);

            if (!filter.IncludeReturns)
                linesQ = linesQ.Where(l => l.Invoice!.InvoiceType == InvoiceType.Sale);

            var lines = await linesQ.ToListAsync();
            var dict = new Dictionary<string, ProductProfitRowDto>();

            foreach (var line in lines)
            {
                var sign = line.Invoice?.InvoiceType == InvoiceType.Return ? -1m : 1m;
                var invoiceSubTotal = line.Invoice?.SubTotal ?? 0m;
                var invoiceDiscount = line.Invoice?.DiscountAmount ?? 0m;
                var allocatedDiscount = 0m;

                if (invoiceSubTotal > 0 && invoiceDiscount > 0)
                    allocatedDiscount = (line.LineSubTotal / invoiceSubTotal) * invoiceDiscount;

                var key = filter.GroupByUnit
                    ? $"{line.ProductId}:{line.ProductUnitId}"
                    : $"{line.ProductId}";

                if (!dict.TryGetValue(key, out var row))
                {
                    var baseUnit = GetBaseUnit(line.Product, line.ProductUnit);
                    row = new ProductProfitRowDto
                    {
                        ProductId = line.ProductId,
                        ProductName = line.Product?.Name,
                        ITEMCODE = line.Product?.ITEMCODE.ToString(),
                        UnitName = filter.GroupByUnit
                            ? line.ProductUnit?.Unit?.Name
                            : baseUnit?.Unit?.Name
                    };
                    dict[key] = row;
                }

                row.SalesQty += sign * (filter.GroupByUnit ? line.Quantity : GetNormalizedInvoiceLineQuantity(line));
                row.SubTotal += sign * line.LineSubTotal;
                row.Discount += sign * allocatedDiscount;
                row.Tax += sign * line.TaxAmount;
                row.COGS += sign * (line.Quantity * line.UnitCost);
            }

            foreach (var row in dict.Values)
            {
                row.NetSales = row.SubTotal - row.Discount;
                row.GrossProfit = row.NetSales - row.COGS;
                row.Margin = row.NetSales == 0 ? 0 : Math.Round((row.GrossProfit / row.NetSales) * 100m, 2);
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
            var transactionRepo = _uow.GetRepository<StockTransaction>();

            var products = await productRepo.GetAllAsQueryable().ToListAsync();

            var stockMovements = await transactionRepo.GetAllAsQueryable()
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    LastDate = g.Max(x => x.TransactionDate)
                })
                .ToListAsync();

            var stocks = await stockRepo.GetAllAsQueryable()
                .Include(x => x.ProductUnit)
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Qty = g.Sum(x => x.Quantity * (x.ProductUnit != null && x.ProductUnit.QuantityPerUnit > 0 ? x.ProductUnit.QuantityPerUnit : 1m))
                })
                .ToListAsync();

            var stockDict = stocks.ToDictionary(x => x.ProductId, x => x.Qty);
            var result = new List<InactiveProductRowDto>();

            foreach (var product in products)
            {
                var lastStockDate = stockMovements.FirstOrDefault(x => x.ProductId == product.Id)?.LastDate;
                var lastMovement = lastStockDate;

                if (lastMovement != null && lastMovement > cutoffDate)
                    continue;

                var currentStock = stockDict.TryGetValue(product.Id, out var qty) ? qty : 0m;
                if (filter.IncludeZeroStockOnly && currentStock > 0)
                    continue;

                result.Add(new InactiveProductRowDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ITEMCODE = product.ITEMCODE.ToString(),
                    CurrentStock = currentStock,
                    MinimumQuantity = product.MiniQuantity ?? 0,
                    LastMovementDate = lastMovement,
                    DaysSinceLastMovement = lastMovement == null ? 999 : (today - lastMovement.Value).Days
                });
            }

            return result
                .OrderByDescending(x => x.DaysSinceLastMovement)
                .ToList();
        }

        public async Task<List<StockValuationRowDto>> GetStockValuationAsync()
        {
            var repo = _uow.GetRepository<Stock>();

            var stocks = await repo.AsQueryable()
                .Include(s => s.Product)
                    .ThenInclude(p => p.ProductUnits)
                        .ThenInclude(pu => pu.Unit)
                .Include(s => s.ProductUnit)
                    .ThenInclude(pu => pu.Unit)
                .ToListAsync();

            return stocks
                .GroupBy(s => s.ProductId)
                .Select(group =>
                {
                    var sample = group.First();
                    var baseUnit = GetBaseUnit(sample.Product, sample.ProductUnit);
                    var unitCost = GetBaseUnitCost(sample.Product, sample.ProductUnit);
                    var quantity = group.Sum(GetNormalizedStockQuantity);

                    return new StockValuationRowDto
                    {
                        ProductId = group.Key,
                        ITEMCODE = sample.Product?.ITEMCODE.ToString() ?? string.Empty,
                        ProductName = sample.Product?.Name,
                        UnitName = baseUnit?.Unit?.Name ?? sample.ProductUnit?.Unit?.Name,
                        Quantity = quantity,
                        UnitCost = unitCost,
                        TotalValue = quantity * unitCost,
                        MinimumQuantity = sample.Product?.MiniQuantity ?? 0m
                    };
                })
                .OrderByDescending(x => x.TotalValue)
                .ThenBy(x => x.ProductName)
                .ToList();
        }

        public async Task<List<StockVarianceRowDto>> GetStockVarianceAsync()
        {
            var repo = _uow.GetRepository<Stock>();

            var stocks = await repo.AsQueryable()
                .Include(s => s.Product)
                    .ThenInclude(p => p.ProductUnits)
                        .ThenInclude(pu => pu.Unit)
                .Include(s => s.ProductUnit)
                    .ThenInclude(pu => pu.Unit)
                .Where(s => s.Product != null)
                .ToListAsync();

            return stocks
                .GroupBy(s => s.ProductId)
                .Select(group =>
                {
                    var sample = group.First();
                    var baseUnit = GetBaseUnit(sample.Product, sample.ProductUnit);
                    var currentQty = group.Sum(GetNormalizedStockQuantity);
                    var minQty = sample.Product?.MiniQuantity ?? 0m;
                    var variance = currentQty - minQty;

                    return new StockVarianceRowDto
                    {
                        ProductId = group.Key,
                        ITEMCODE = sample.Product?.ITEMCODE.ToString() ?? string.Empty,
                        ProductName = sample.Product?.Name,
                        UnitName = baseUnit?.Unit?.Name ?? sample.ProductUnit?.Unit?.Name,
                        CurrentQuantity = currentQty,
                        MinimumQuantity = minQty,
                        VarianceQuantity = variance,
                        StatusText = variance < 0 ? "عجز" : variance == 0 ? "متوازن" : "فائض"
                    };
                })
                .OrderBy(x => x.StatusText)
                .ThenBy(x => x.ProductName)
                .ToList();
        }

        public async Task<List<StockAdjustmentRowDto>> GetStockAdjustmentsAsync(DateTime? from, DateTime? to)
        {
            var transactionRepo = _uow.GetRepository<StockTransaction>();

            IQueryable<StockTransaction> query = transactionRepo.AsQueryable()
                .Include(x => x.Product)
                    .ThenInclude(p => p.ProductUnits)
                        .ThenInclude(pu => pu.Unit)
                .Include(x => x.ProductUnit)
                    .ThenInclude(pu => pu.Unit)
                .Include(x => x.Casher)
                .Where(x => x.TransactionType == TransactionType.Adjustment);

            if (from.HasValue)
                query = query.Where(x => x.TransactionDate >= from.Value);

            if (to.HasValue)
                query = query.Where(x => x.TransactionDate <= to.Value);

            return (await query.ToListAsync())
                .Select(x => new StockAdjustmentRowDto
                {
                    TransactionId = x.Id,
                    TransactionDate = x.TransactionDate,
                    ITEMCODE = x.Product?.ITEMCODE.ToString() ?? string.Empty,
                    ProductName = x.Product?.Name,
                    UnitName = GetBaseUnit(x.Product, x.ProductUnit)?.Unit?.Name ?? x.ProductUnit?.Unit?.Name,
                    Quantity = x.BaseQuantity != 0 ? x.BaseQuantity : x.Quantity * GetUnitFactor(x.ProductUnit, x.QuantityPerUnitSnapshot),
                    UnitPrice = x.UnitPrice,
                    Notes = x.Notes,
                    CreatedBy = x.Casher?.Name ?? string.Empty,
                    SourceReference = x.StockId.HasValue ? $"STK-{x.StockId}" : (x.InvoiceId.HasValue ? $"INV-{x.InvoiceId}" : $"TRX-{x.Id}")
                })
                .OrderByDescending(x => x.TransactionDate)
                .ToList();
        }

        public async Task<List<PriceListRowDto>> GetPriceListAsync()
        {
            var productRepo = _uow.GetRepository<Product>();
            var products = await productRepo.GetAllAsQueryable()
                .Include(p => p.ProductUnits)
                    .ThenInclude(pu => pu.Unit)
                .ToListAsync();

            return products
                .SelectMany(product => (product.ProductUnits ?? new List<ProductUnit>())
                    .Select(unit => new PriceListRowDto
                    {
                        ProductId = product.Id,
                        ItemID = product.ITEMCODE.ToString(),
                        ItemName = product.Name,
                        Barcode = product.ITEMCODE.ToString(),
                        UnitName = unit.Unit?.Name ?? string.Empty,
                        PurchasePrice = unit.PurchasePrice,
                        SalePrice = unit.SalePrice,
                        IsDefaultSaleUnit = unit.IsDefaultSaleUnit,
                        IsDefaultPurchaseUnit = unit.IsDefaultPurchaseUnit
                    }))
                .OrderBy(x => x.ItemName)
                .ThenByDescending(x => x.IsDefaultSaleUnit)
                .ThenBy(x => x.UnitName)
                .ToList();
        }

        public async Task<List<ItemCostDetailRowDto>> GetItemCostDetailsAsync()
        {
            var stocks = await GetStockValuationAsync();

            return stocks
                .Select(x => new ItemCostDetailRowDto
                {
                    ProductId = x.ProductId,
                    ItemID = x.ITEMCODE,
                    ItemName = x.ProductName ?? string.Empty,
                    Barcode = x.ITEMCODE,
                    UnitName = x.UnitName ?? string.Empty,
                    Quantity = x.Quantity,
                    Cost = x.UnitCost,
                    Total = x.TotalValue,
                    MinimumQuantity = x.MinimumQuantity
                })
                .OrderByDescending(x => x.Total)
                .ThenBy(x => x.ItemName)
                .ToList();
        }

        private static ProductUnit? GetBaseUnit(Product? product, ProductUnit? fallbackUnit = null)
        {
            return ProductUnitSelector.GetBaseUnit(product?.ProductUnits) ?? fallbackUnit;
        }

        private static decimal GetUnitFactor(ProductUnit? unit, decimal snapshotFactor = 0m)
        {
            if (snapshotFactor > 0)
                return snapshotFactor;

            if (unit?.QuantityPerUnit > 0)
                return unit.QuantityPerUnit;

            return 1m;
        }

        private static decimal GetNormalizedStockQuantity(Stock stock)
        {
            return stock.Quantity * GetUnitFactor(stock.ProductUnit);
        }

        private static decimal GetNormalizedInvoiceLineQuantity(InvoiceLine line)
        {
            var factor = GetUnitFactor(line.ProductUnit, line.QuantityPerUnitSnapshot);
            return line.BaseQuantity > 0 ? line.BaseQuantity : line.Quantity * factor;
        }

        private static decimal GetBaseUnitCost(Product? product, ProductUnit? fallbackUnit = null)
        {
            var baseUnit = GetBaseUnit(product, fallbackUnit);

            if (baseUnit?.PurchasePrice > 0)
                return baseUnit.PurchasePrice;

            if (fallbackUnit?.PurchasePrice > 0)
            {
                var factor = GetUnitFactor(fallbackUnit);
                return factor > 0 ? fallbackUnit.PurchasePrice / factor : fallbackUnit.PurchasePrice;
            }

            return 0m;
        }

        private static string ResolveDocumentType(StockTransaction transaction)
        {
            if (transaction.InvoiceId.HasValue)
                return transaction.Invoice?.InvoiceType.ToString() ?? transaction.TransactionType.ToString();

            if (transaction.VoucherId.HasValue)
                return "Voucher";

            if (transaction.StockId.HasValue)
                return transaction.TransactionType == TransactionType.Purchase ? "Stock In" : "Stock Out";

            return transaction.TransactionType.ToString();
        }

    }

    public interface IStockReportService
    {
        Task<List<CurrentStockDto>> GetCurrentStockAsync();
        Task<List<StockMovementDto>> GetStockMovementsAsync(DateTime? from, DateTime? to);
        Task<List<LowStockDto>> GetLowStockAsync();
        Task<List<StockBalanceByDateDto>> GetStockBalanceByDateAsync(DateTime date, bool includeInvoices = true);
        Task<List<InventoryMovementSummaryRowDto>> GetInventoryMovementSummaryAsync(InventoryMovementSummaryFilterDto filter);
        Task<List<ProductProfitRowDto>> GetProductProfitAsync(ProductProfitFilterDto filter);
        Task<List<InactiveProductRowDto>> GetInactiveProductsAsync(InactiveProductsFilterDto filter);
        Task<List<StockValuationRowDto>> GetStockValuationAsync();
        Task<List<StockVarianceRowDto>> GetStockVarianceAsync();
        Task<List<StockAdjustmentRowDto>> GetStockAdjustmentsAsync(DateTime? from, DateTime? to);
        Task<List<PriceListRowDto>> GetPriceListAsync();
        Task<List<ItemCostDetailRowDto>> GetItemCostDetailsAsync();
    }
}
