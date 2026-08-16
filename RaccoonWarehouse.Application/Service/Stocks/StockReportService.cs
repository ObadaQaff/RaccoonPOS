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
using RaccoonWarehouse.Domain.StockAdjustments;
using RaccoonWarehouse.Domain.Stock.Filters;
using RaccoonWarehouse.Domain.StockItems;
using RaccoonWarehouse.Domain.StockLots;
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

        public async Task<List<CurrentStockDto>> GetCurrentStockAsync(string? searchText = null)
        {
            var repo = _uow.GetRepository<Stock>();

            IQueryable<Stock> query = repo.AsQueryable()
                .AsNoTracking()
                .Include(s => s.Product)
                    .ThenInclude(p => p.ProductUnits)
                        .ThenInclude(pu => pu.Unit)
                .Include(s => s.ProductUnit)
                    .ThenInclude(pu => pu.Unit);

            var normalizedSearch = string.IsNullOrWhiteSpace(searchText)
                ? null
                : searchText.Trim();

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                query = query.Where(s =>
                    s.Product != null &&
                    ((s.Product.Name != null && s.Product.Name.Contains(normalizedSearch)) ||
                     s.Product.ITEMCODE.ToString().Contains(normalizedSearch)));
            }

            var stocks = await query.ToListAsync();
            var nearestLots = await GetNearestLotsByProductAsync();

            return stocks
                .GroupBy(s => s.ProductId)
                .Select(group =>
                {
                    var sample = group.First();
                    var saleUnit = GetSaleUnit(sample.Product, sample.ProductUnit);
                    var nearestLot = nearestLots.TryGetValue(group.Key, out var lot) ? lot : null;
                    var totalBaseQuantity = group.Sum(GetNormalizedStockQuantity);
                    var saleUnitFactor = GetUnitFactor(saleUnit);

                    return new CurrentStockDto
                    {
                        ProductId = group.Key,
                        ProductName = sample.Product?.Name,
                        ITEMCODE = sample.Product?.ITEMCODE.ToString(),
                        UnitName = saleUnit?.Unit?.Name ?? sample.ProductUnit?.Unit?.Name,
                        Quantity = saleUnitFactor > 0 ? totalBaseQuantity / saleUnitFactor : totalBaseQuantity,
                        MinimumQuantity = sample.Product?.MiniQuantity is decimal minimumQuantity
                            ? (saleUnitFactor > 0 ? minimumQuantity / saleUnitFactor : minimumQuantity)
                            : null,
                        PurchasePrice = nearestLot?.PurchasePrice ?? sample.PurchasePrice,
                        SalePrice = nearestLot?.SalePrice ?? sample.SalePrice,
                        NearestExpiryDate = nearestLot?.ExpiryDate
                    };
                })
                .OrderBy(x => x.ProductName)
                .ToList();
        }

        public async Task<List<StockMovementDto>> GetStockMovementsAsync(DateTime? from, DateTime? to, int? productId = null)
        {
            var transactionRepo = _uow.GetRepository<StockTransaction>();

            IQueryable<StockTransaction> query = transactionRepo.AsQueryable()
                .AsNoTracking();

            if (from.HasValue)
                query = query.Where(x => x.TransactionDate >= from.Value);

            if (to.HasValue)
                query = query.Where(x => x.TransactionDate <= to.Value);

            if (productId.HasValue && productId.Value > 0)
                query = query.Where(x => x.ProductId == productId.Value);

            var rows = await query
                .OrderByDescending(x => x.TransactionDate)
                .Select(x => new
                {
                    StockItemId = x.Id,
                    StockDocumentId = x.InvoiceId ?? x.VoucherId ?? x.StockId ?? 0,
                    Date = x.TransactionDate,
                    InvoiceNumber = x.Invoice != null ? x.Invoice.InvoiceNumber : null,
                    InvoiceType = x.Invoice != null ? (InvoiceType?)x.Invoice.InvoiceType : null,
                    VoucherNumber = x.Voucher != null ? x.Voucher.VoucherNumber : null,
                    HasVoucher = x.VoucherId.HasValue,
                    x.StockId,
                    x.TransactionType,
                    ProductId = x.ProductId,
                    ProductName = x.Product.Name,
                    BaseUnitName = x.Product.ProductUnits!
                        .Where(pu => pu.IsBaseUnit)
                        .Select(pu => pu.Unit != null ? pu.Unit.Name : null)
                        .FirstOrDefault(),
                    ProductUnitName = x.ProductUnit.Unit != null ? x.ProductUnit.Unit.Name : null,
                    Quantity = x.BaseQuantity != 0
                        ? x.BaseQuantity
                        : x.Quantity * (x.QuantityPerUnitSnapshot > 0
                            ? x.QuantityPerUnitSnapshot
                            : (x.ProductUnit.QuantityPerUnit > 0 ? x.ProductUnit.QuantityPerUnit : 1m)),
                    PurchasePrice = x.StockLot != null ? x.StockLot.PurchasePrice : x.ProductUnit.PurchasePrice,
                    SalePrice = x.UnitPrice,
                    ExpiryDate = x.ExpiryDate,
                    BatchStatus = x.StockLot != null ? (BatchStatus?)x.StockLot.Status : null,
                    Reference = x.ReferenceNumber,
                    CreatedBy = x.Casher != null ? x.Casher.Name : (x.Customer != null ? x.Customer.Name : string.Empty)
                })
                .ToListAsync();

            return rows
                .Select(x => new StockMovementDto
                {
                    StockItemId = x.StockItemId,
                    StockDocumentId = x.StockDocumentId,
                    Date = x.Date,
                    DocumentNumber = x.InvoiceNumber ?? x.VoucherNumber ?? (x.StockId.HasValue ? $"STK-{x.StockId}" : string.Empty),
                    DocumentType = ResolveDocumentType(x.InvoiceType, x.TransactionType, x.HasVoucher, x.StockId.HasValue),
                    ProductId = x.ProductId,
                    ProductName = x.ProductName,
                    UnitName = x.BaseUnitName ?? x.ProductUnitName,
                    Quantity = x.Quantity,
                    PurchasePrice = x.PurchasePrice,
                    SalePrice = x.SalePrice,
                    ExpiryDate = x.ExpiryDate,
                    BatchStatus = x.BatchStatus?.ToString(),
                    Reference = x.Reference,
                    CreatedBy = x.CreatedBy
                })
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
            var nearestLots = await GetNearestLotsByProductAsync();

            return stocks
                .GroupBy(s => s.ProductId)
                .Select(group =>
                {
                    var sample = group.First();
                    var nearestLot = nearestLots.TryGetValue(group.Key, out var lot) ? lot : null;
                    return new LowStockDto
                    {
                        ProductId = group.Key,
                        ProductName = sample.Product?.Name,
                        ITEMCODE = sample.Product?.ITEMCODE.ToString(),
                        UnitName = GetBaseUnit(sample.Product, sample.ProductUnit)?.Unit?.Name ?? sample.ProductUnit?.Unit?.Name,
                        CurrentQuantity = group.Sum(GetNormalizedStockQuantity),
                        MinimumQuantity = sample.Product?.MiniQuantity ?? 0m,
                        NearestExpiryDate = nearestLot?.ExpiryDate
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
            var nearestLots = await GetNearestLotsByProductAsync();
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
                row.NearestExpiryDate = nearestLots.TryGetValue(row.ProductId, out var lot) ? lot.ExpiryDate : null;
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
                .AsNoTracking()
                .Where(x => x.TransactionDate >= from && x.TransactionDate <= to);

            if (!filter.IncludeInvoices)
                transactionsQ = transactionsQ.Where(x => x.InvoiceId == null);

            if (filter.ProductId.HasValue)
                transactionsQ = transactionsQ.Where(x => x.ProductId == filter.ProductId.Value);

            var transactions = await transactionsQ
                .Select(x => new
                {
                    x.ProductId,
                    BaseUnitId = x.Product.ProductUnits!
                        .Where(pu => pu.IsBaseUnit)
                        .Select(pu => (int?)pu.Id)
                        .FirstOrDefault(),
                    x.ProductUnitId,
                    ProductName = x.Product.Name,
                    ItemCode = x.Product.ITEMCODE,
                    BaseUnitName = x.Product.ProductUnits!
                        .Where(pu => pu.IsBaseUnit)
                        .Select(pu => pu.Unit != null ? pu.Unit.Name : null)
                        .FirstOrDefault(),
                    ProductUnitName = x.ProductUnit.Unit != null ? x.ProductUnit.Unit.Name : null,
                    MinimumQuantity = x.Product.MiniQuantity,
                    Quantity = x.BaseQuantity != 0
                        ? x.BaseQuantity
                        : x.Quantity * (x.QuantityPerUnitSnapshot > 0
                            ? x.QuantityPerUnitSnapshot
                            : (x.ProductUnit.QuantityPerUnit > 0 ? x.ProductUnit.QuantityPerUnit : 1m))
                })
                .ToListAsync();

            foreach (var item in transactions)
            {
                if (!dict.TryGetValue(item.ProductId, out var row))
                {
                    row = new InventoryMovementSummaryRowDto
                    {
                        ProductId = item.ProductId,
                        ProductUnitId = item.BaseUnitId ?? item.ProductUnitId,
                        ProductName = item.ProductName,
                        ITEMCODE = item.ItemCode?.ToString(),
                        UnitName = item.BaseUnitName ?? item.ProductUnitName,
                        MinimumQuantity = item.MinimumQuantity ?? 0m
                    };
                    dict[item.ProductId] = row;
                }

                var signed = item.Quantity;

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
            var nearestLots = await GetNearestLotsByProductAsync();

            return stocks
                .GroupBy(s => s.ProductId)
                .Select(group =>
                {
                    var sample = group.First();
                    var baseUnit = GetBaseUnit(sample.Product, sample.ProductUnit);
                    var quantity = group.Sum(GetNormalizedStockQuantity);
                    var totalValue = group.Sum(stock => stock.Quantity * stock.PurchasePrice);
                    var unitCost = quantity > 0
                        ? Math.Round(totalValue / quantity, 3)
                        : GetBaseUnitCost(sample.Product, sample.ProductUnit);
                    var nearestLot = nearestLots.TryGetValue(group.Key, out var lot) ? lot : null;

                    return new StockValuationRowDto
                    {
                        ProductId = group.Key,
                        ITEMCODE = sample.Product?.ITEMCODE.ToString() ?? string.Empty,
                        ProductName = sample.Product?.Name,
                        UnitName = baseUnit?.Unit?.Name ?? sample.ProductUnit?.Unit?.Name,
                        Quantity = quantity,
                        UnitCost = unitCost,
                        TotalValue = totalValue,
                        MinimumQuantity = sample.Product?.MiniQuantity ?? 0m,
                        NearestExpiryDate = nearestLot?.ExpiryDate
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
                .Include(x => x.StockLot)
                .Include(x => x.StockAdjustment)
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
                    AdjustmentType = x.StockAdjustment?.AdjustmentType.ToString() ?? "Adjustment",
                    BatchAction = x.StockLot?.Status.ToString() ?? string.Empty,
                    Notes = x.Notes,
                    CreatedBy = x.Casher?.Name ?? string.Empty,
                    SourceReference = x.ReferenceNumber ?? (x.StockId.HasValue ? $"STK-{x.StockId}" : (x.InvoiceId.HasValue ? $"INV-{x.InvoiceId}" : $"TRX-{x.Id}"))
                })
                .OrderByDescending(x => x.TransactionDate)
                .ToList();
        }

        public async Task<List<PriceListRowDto>> GetPriceListAsync()
        {
            var productRepo = _uow.GetRepository<Product>();
            var lotLookup = await GetNearestLotsByProductUnitAsync();
            var stockLookup = await _uow.GetRepository<Stock>().GetAllAsQueryable()
                .AsNoTracking()
                .ToDictionaryAsync(stock => (stock.ProductId, stock.ProductUnitId));
            var products = await productRepo.GetAllAsQueryable()
                .Include(p => p.ProductUnits)
                    .ThenInclude(pu => pu.Unit)
                .ToListAsync();

            return products
                .SelectMany(product => (product.ProductUnits ?? new List<ProductUnit>())
                    .Select(unit =>
                    {
                        lotLookup.TryGetValue((product.Id, unit.Id), out var lot);
                        stockLookup.TryGetValue((product.Id, unit.Id), out var stock);
                        return new PriceListRowDto
                        {
                            ProductId = product.Id,
                            ItemID = product.ITEMCODE.ToString(),
                            ItemName = product.Name,
                            Barcode = product.ITEMCODE.ToString(),
                            UnitName = unit.Unit?.Name ?? string.Empty,
                            PurchasePrice = stock?.PurchasePrice ?? unit.PurchasePrice,
                            SalePrice = lot?.SalePrice ?? unit.SalePrice,
                            ExpiryDate = lot?.ExpiryDate,
                            IsDefaultSaleUnit = unit.IsDefaultSaleUnit,
                            IsDefaultPurchaseUnit = unit.IsDefaultPurchaseUnit
                        };
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

        private static ProductUnit? GetSaleUnit(Product? product, ProductUnit? fallbackUnit = null)
        {
            return ProductUnitSelector.GetDefaultSaleUnit(product?.ProductUnits) ?? fallbackUnit;
        }

        private async Task<Dictionary<int, StockLot>> GetNearestLotsByProductAsync()
        {
            var lotRepo = _uow.GetRepository<StockLot>();
            var today = DateTime.Today;
            var lots = await lotRepo.GetAllAsQueryable()
                .Where(l => l.RemainingQuantity > 0 &&
                            (!l.ExpiryDate.HasValue || l.ExpiryDate.Value >= today))
                .ToListAsync();

            return lots
                .GroupBy(l => l.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(l => l.ExpiryDate == null ? 1 : 0)
                        .ThenBy(l => l.ExpiryDate)
                        .ThenBy(l => l.CreatedDate)
                        .First());
        }

        private async Task<Dictionary<(int ProductId, int ProductUnitId), StockLot>> GetNearestLotsByProductUnitAsync()
        {
            var lotRepo = _uow.GetRepository<StockLot>();
            var today = DateTime.Today;
            var lots = await lotRepo.GetAllAsQueryable()
                .Where(l => l.RemainingQuantity > 0 &&
                            (!l.ExpiryDate.HasValue || l.ExpiryDate.Value >= today))
                .ToListAsync();

            return lots
                .GroupBy(l => (l.ProductId, l.ProductUnitId))
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(l => l.ExpiryDate == null ? 1 : 0)
                        .ThenBy(l => l.ExpiryDate)
                        .ThenBy(l => l.CreatedDate)
                        .First());
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

        private static string ResolveDocumentType(InvoiceType? invoiceType, TransactionType transactionType, bool hasVoucher, bool hasStockDocument)
        {
            if (invoiceType.HasValue)
                return invoiceType.Value.ToString();

            if (hasVoucher)
                return "Voucher";

            if (hasStockDocument)
                return transactionType == TransactionType.Purchase ? "Stock In" : "Stock Out";

            return transactionType.ToString();
        }

    }

    public interface IStockReportService
    {
        Task<List<CurrentStockDto>> GetCurrentStockAsync(string? searchText = null);
        Task<List<StockMovementDto>> GetStockMovementsAsync(DateTime? from, DateTime? to, int? productId = null);
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
