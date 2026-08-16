using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Orders.DTOs;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.StockLots;
using RaccoonWarehouse.Domain.StockTransactions;

namespace RaccoonWarehouse.Application.Service.Orders
{
    public interface IEndpointOrderStatusService
    {
        Task<Result> ApplyStatusAsync(
            int invoiceId,
            InvoiceStatus targetStatus,
            CancellationToken cancellationToken = default,
            bool synchronizeBox = true);

        Task<Result> UpdateDetailsAsync(
            EndpointOrderEditDto request,
            CancellationToken cancellationToken = default);
    }

    public sealed class EndpointOrderStatusService : IEndpointOrderStatusService
    {
        private static readonly SemaphoreSlim TransitionLock = new(1, 1);

        private readonly ApplicationDbContext _context;
        private readonly IStockService _stockService;
        private readonly IAccountingService _accountingService;
        private readonly IMapper _mapper;
        private readonly IBoxCartApiService _boxCartApiService;

        public EndpointOrderStatusService(
            ApplicationDbContext context,
            IStockService stockService,
            IAccountingService accountingService,
            IMapper mapper,
            IBoxCartApiService? boxCartApiService = null)
        {
            _context = context;
            _stockService = stockService;
            _accountingService = accountingService;
            _mapper = mapper;
            _boxCartApiService = boxCartApiService ?? new BoxCartApiService();
        }

        public async Task<Result> ApplyStatusAsync(
            int invoiceId,
            InvoiceStatus targetStatus,
            CancellationToken cancellationToken = default,
            bool synchronizeBox = true)
        {
            if (invoiceId <= 0)
                return Result.Fail("Invoice id is required.");

            if (targetStatus is not InvoiceStatus.Unknown
                and not InvoiceStatus.InProcess
                and not InvoiceStatus.Completed
                and not InvoiceStatus.Cancelled)
            {
                return Result.Fail(
                    "Endpoint orders support only Unknown, In Process, Completed, and Cancelled statuses.");
            }

            await TransitionLock.WaitAsync(cancellationToken);
            try
            {
                var invoice = await _context.Set<Invoice>()
                    .Include(item => item.InvoiceLines)
                    .FirstOrDefaultAsync(item => item.Id == invoiceId, cancellationToken);

                if (invoice == null)
                    return Result.Fail("Invoice was not found.");

                if (invoice.InvoiceType != InvoiceType.appCart)
                    return Result.Fail("Only app-cart orders can use this status workflow.");

                var oldStatus = invoice.Status ?? InvoiceStatus.Unknown;
                var stockDeductedNow = false;

                if (targetStatus is InvoiceStatus.Unknown
                    or InvoiceStatus.InProcess
                    or InvoiceStatus.Completed)
                {
                    var holdResult = await EnsureStockDeductedAsync(invoice, cancellationToken);
                    if (!holdResult.Success)
                        return Result.Fail(holdResult.Message, holdResult.Errors);

                    stockDeductedNow = holdResult.Data;
                }

                if (targetStatus == InvoiceStatus.Cancelled)
                {
                    var reverseResult = await _accountingService.ReverseJournalByReferenceAsync(
                        "Invoice",
                        invoice.Id,
                        $"Endpoint order #{invoice.InvoiceNumber} changed to {targetStatus}");

                    if (!reverseResult.Success)
                        return Result.Fail(reverseResult.Message, reverseResult.Errors);

                    await RestoreStockAsync(invoice, cancellationToken);
                }
                else if (targetStatus is InvoiceStatus.Unknown or InvoiceStatus.InProcess &&
                          oldStatus is InvoiceStatus.Completed or InvoiceStatus.Posted)
                {
                    var reverseResult = await _accountingService.ReverseJournalByReferenceAsync(
                        "Invoice",
                        invoice.Id,
                        $"Endpoint order #{invoice.InvoiceNumber} returned to {targetStatus}");

                    if (!reverseResult.Success)
                        return Result.Fail(reverseResult.Message, reverseResult.Errors);
                }

                invoice.Status = targetStatus;
                invoice.ClosedAt = targetStatus == InvoiceStatus.Completed
                    ? DateTime.Now
                    : null;
                invoice.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync(cancellationToken);

                if (targetStatus == InvoiceStatus.Completed)
                {
                    var accountingDto = _mapper.Map<InvoiceWriteDto>(invoice);
                    accountingDto.Status = targetStatus;
                    accountingDto.InvoiceType = InvoiceType.Sale;
                    accountingDto.PaymentType ??= PaymentType.Credit;

                    var postResult = await _accountingService.PostInvoiceEntryAsync(accountingDto);
                    if (!postResult.Success)
                    {
                        invoice.Status = oldStatus;
                        invoice.ClosedAt = null;
                        invoice.UpdatedDate = DateTime.Now;
                        await _context.SaveChangesAsync(cancellationToken);

                        if (stockDeductedNow && oldStatus == InvoiceStatus.Cancelled)
                            await RestoreStockAsync(invoice, cancellationToken);

                        return Result.Fail(postResult.Message, postResult.Errors);
                    }
                }

                if (synchronizeBox)
                {
                    var cartId = GetBoxCartId(invoice.OriginalInvoiceId);
                    if (cartId.HasValue)
                    {
                        var boxResult = await _boxCartApiService.UpdateCartStatusAsync(
                            cartId.Value,
                            MapBoxCartStatus(targetStatus),
                            cancellationToken);
                        if (!boxResult.Success)
                            return Result.Fail(boxResult.Message, boxResult.Errors);
                    }
                }

                return Result.Ok($"Endpoint order status changed to {targetStatus}.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result.Fail($"Failed to update endpoint order status: {ex.Message}");
            }
            finally
            {
                TransitionLock.Release();
            }
        }

        public async Task<Result> UpdateDetailsAsync(
            EndpointOrderEditDto request,
            CancellationToken cancellationToken = default)
        {
            if (request == null || request.InvoiceId <= 0)
                return Result.Fail("Invoice id is required.");

            if (request.Lines == null || request.Lines.Count == 0)
                return Result.Fail("Endpoint order must contain at least one line.");

            if (request.Lines.Any(line =>
                    line.ProductId <= 0 ||
                    line.ProductUnitId <= 0 ||
                    line.Quantity <= 0 ||
                    line.UnitPrice < 0))
            {
                return Result.Fail(
                    "Each order line requires a valid product, unit, positive quantity, and non-negative price.");
            }

            await TransitionLock.WaitAsync(cancellationToken);
            try
            {
                var invoice = await _context.Set<Invoice>()
                    .Include(item => item.InvoiceLines)
                    .FirstOrDefaultAsync(item => item.Id == request.InvoiceId, cancellationToken);
                if (invoice == null)
                    return Result.Fail("Invoice was not found.");

                if (invoice.InvoiceType != InvoiceType.appCart)
                    return Result.Fail("Only app-cart orders can use this edit workflow.");

                if (invoice.Status is not InvoiceStatus.Unknown
                    and not InvoiceStatus.InProcess
                    and not InvoiceStatus.OnHold)
                {
                    return Result.Fail("Only unknown, in-process, or legacy on-hold orders can be edited.");
                }

                var invoiceLines = invoice.InvoiceLines?.OrderBy(line => line.Id).ToList()
                                   ?? new List<Domain.InvoiceLines.InvoiceLine>();
                if (HasSameDetails(invoiceLines, request.Lines))
                    return Result.Ok("No order detail changes were detected.");

                var originalLines = invoiceLines.Select(EndpointLineSnapshot.FromLine).ToList();
                var requestedUnitIds = request.Lines.Select(line => line.ProductUnitId).Distinct().ToList();
                var units = await _context.Set<Domain.ProductUnits.ProductUnit>()
                    .AsNoTracking()
                    .Include(unit => unit.Product)
                    .Where(unit => requestedUnitIds.Contains(unit.Id))
                    .ToDictionaryAsync(unit => unit.Id, cancellationToken);
                if (units.Count != requestedUnitIds.Count ||
                    request.Lines.Any(line =>
                        !units.TryGetValue(line.ProductUnitId, out var unit) ||
                        unit.ProductId != line.ProductId))
                {
                    return Result.Fail("One or more selected product units are invalid.");
                }

                await RestoreStockAsync(invoice, cancellationToken);
                _context.Set<Domain.InvoiceLines.InvoiceLine>().RemoveRange(invoiceLines);
                invoice.InvoiceLines = request.Lines.Select(line =>
                    CreateInvoiceLine(invoice, line, units[line.ProductUnitId])).ToList();
                ApplyInvoiceTotals(invoice);
                await _context.SaveChangesAsync(cancellationToken);

                var reserveResult = await EnsureStockDeductedAsync(invoice, cancellationToken);
                if (!reserveResult.Success)
                {
                    _context.Set<Domain.InvoiceLines.InvoiceLine>().RemoveRange(invoice.InvoiceLines);
                    invoice.InvoiceLines = originalLines.Select(snapshot => snapshot.ToLine(invoice.Id)).ToList();
                    ApplyInvoiceTotals(invoice);
                    await _context.SaveChangesAsync(cancellationToken);
                    await EnsureStockDeductedAsync(invoice, cancellationToken);
                    return Result.Fail(reserveResult.Message, reserveResult.Errors);
                }

                return Result.Ok("Endpoint order details were updated.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result.Fail($"Failed to update endpoint order details: {ex.Message}");
            }
            finally
            {
                TransitionLock.Release();
            }
        }

        public static int MapBoxCartStatus(InvoiceStatus status)
        {
            return status switch
            {
                InvoiceStatus.Unknown => BoxCartApiService.UnknownStatus,
                InvoiceStatus.InProcess => BoxCartApiService.InProcessStatus,
                InvoiceStatus.Completed => BoxCartApiService.CompletedStatus,
                InvoiceStatus.Cancelled => BoxCartApiService.CancelledStatus,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Unsupported endpoint order status.")
            };
        }

        public static int? GetBoxCartId(string? originalInvoiceId)
        {
            const string prefix = "BOX-CART-";
            if (string.IsNullOrWhiteSpace(originalInvoiceId) ||
                !originalInvoiceId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return int.TryParse(originalInvoiceId[prefix.Length..], out var cartId) && cartId > 0
                ? cartId
                : null;
        }

        public static int? GetBoxCartItemId(string? originalInvoiceId)
        {
            const string prefix = "BOX-CART-ITEM-";
            if (string.IsNullOrWhiteSpace(originalInvoiceId) ||
                !originalInvoiceId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return int.TryParse(originalInvoiceId[prefix.Length..], out var itemId) && itemId > 0
                ? itemId
                : null;
        }

        private static Domain.InvoiceLines.InvoiceLine CreateInvoiceLine(
            Invoice invoice,
            EndpointOrderLocalLineDto edit,
            Domain.ProductUnits.ProductUnit unit)
        {
            var factor = unit.QuantityPerUnit > 0 ? unit.QuantityPerUnit : 1m;
            var line = new Domain.InvoiceLines.InvoiceLine
            {
                InvoiceId = invoice.Id,
                ProductId = edit.ProductId,
                ProductUnitId = edit.ProductUnitId,
                Quantity = edit.Quantity,
                QuantityPerUnitSnapshot = factor,
                BaseQuantity = edit.Quantity * factor,
                UnitPrice = edit.UnitPrice,
                UnitCost = unit.PurchasePrice,
                TaxExempt = unit.Product?.TaxExempt ?? false,
                TaxRate = unit.Product?.TaxRate ?? 0m,
                ExpiryDate = DateTime.Today.AddYears(10),
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };
            ApplyLineTotals(line);
            return line;
        }

        private static void ApplyLineTotals(Domain.InvoiceLines.InvoiceLine line)
        {
            var gross = line.Quantity * line.UnitPrice;
            var taxRate = line.TaxExempt ? 0m : line.TaxRate;
            var divisor = 1m + taxRate / 100m;
            line.LineSubTotal = line.TaxExempt || divisor <= 0m
                ? gross
                : Math.Round(gross / divisor, 3);
            line.TaxAmount = Math.Round(gross - line.LineSubTotal, 3);
            line.ProfitBeforeTax = line.LineSubTotal - line.CostTotal;
            line.Profit = line.ProfitBeforeTax;
        }

        private static void ApplyInvoiceTotals(Invoice invoice)
        {
            var lines = invoice.InvoiceLines?.ToList() ?? new List<Domain.InvoiceLines.InvoiceLine>();
            invoice.SubTotal = lines.Sum(line => line.LineSubTotal);
            invoice.TotalTax = lines.Sum(line => line.TaxAmount);
            invoice.TotalAmount = lines.Sum(line => line.LineTotal) - (invoice.DiscountAmount ?? 0m);
            invoice.TotalCOGS = lines.Sum(line => line.CostTotal);
            invoice.NetSales = invoice.SubTotal - (invoice.DiscountAmount ?? 0m);
            invoice.GrossProfit = invoice.NetSales - invoice.TotalCOGS;
            invoice.UpdatedDate = DateTime.Now;
        }

        private static bool HasSameDetails(
            IReadOnlyList<Domain.InvoiceLines.InvoiceLine> savedLines,
            IReadOnlyList<EndpointOrderLocalLineDto> requestedLines)
        {
            if (savedLines.Count != requestedLines.Count)
                return false;

            for (var index = 0; index < savedLines.Count; index++)
            {
                var saved = savedLines[index];
                var requested = requestedLines[index];
                if (saved.ProductId != requested.ProductId ||
                    saved.ProductUnitId != requested.ProductUnitId ||
                    saved.Quantity != requested.Quantity ||
                    saved.UnitPrice != requested.UnitPrice)
                {
                    return false;
                }
            }

            return true;
        }

        private sealed record EndpointLineSnapshot(
            int ProductId,
            int ProductUnitId,
            string? OriginalInvoiceId,
            decimal Quantity,
            decimal QuantityPerUnitSnapshot,
            decimal BaseQuantity,
            decimal UnitPrice,
            decimal UnitCost,
            bool TaxExempt,
            decimal TaxRate,
            DateTime ExpiryDate,
            DateTime CreatedDate)
        {
            public static EndpointLineSnapshot FromLine(Domain.InvoiceLines.InvoiceLine line)
            {
                return new EndpointLineSnapshot(
                    line.ProductId,
                    line.ProductUnitId,
                    line.OriginalInvoiceId,
                    line.Quantity,
                    line.QuantityPerUnitSnapshot,
                    line.BaseQuantity,
                    line.UnitPrice,
                    line.UnitCost,
                    line.TaxExempt,
                    line.TaxRate,
                    line.ExpiryDate,
                    line.CreatedDate);
            }

            public Domain.InvoiceLines.InvoiceLine ToLine(int invoiceId)
            {
                var line = new Domain.InvoiceLines.InvoiceLine
                {
                    InvoiceId = invoiceId,
                    ProductId = ProductId,
                    ProductUnitId = ProductUnitId,
                    OriginalInvoiceId = OriginalInvoiceId,
                    Quantity = Quantity,
                    QuantityPerUnitSnapshot = QuantityPerUnitSnapshot,
                    BaseQuantity = BaseQuantity,
                    UnitPrice = UnitPrice,
                    UnitCost = UnitCost,
                    TaxExempt = TaxExempt,
                    TaxRate = TaxRate,
                    ExpiryDate = ExpiryDate,
                    CreatedDate = CreatedDate,
                    UpdatedDate = DateTime.Now
                };
                ApplyLineTotals(line);
                return line;
            }
        }

        private async Task<Result<bool>> EnsureStockDeductedAsync(
            Invoice invoice,
            CancellationToken cancellationToken)
        {
            var netBaseQuantity = await _context.Set<StockTransaction>()
                .AsNoTracking()
                .Where(item => item.InvoiceId == invoice.Id)
                .SumAsync(item => (decimal?)item.BaseQuantity, cancellationToken) ?? 0m;

            if (netBaseQuantity < 0)
                return Result<bool>.Ok(false, "Stock was already deducted.");

            var lines = invoice.InvoiceLines?.Where(line => line.Quantity > 0).ToList()
                        ?? new List<Domain.InvoiceLines.InvoiceLine>();
            if (lines.Count == 0)
                return Result<bool>.Fail("Endpoint order has no stock lines.");

            var allocationResult = await _stockService.AllocateOutgoingAsync(
                lines.Select(line => new StockAllocationRequestDto
                {
                    ProductId = line.ProductId,
                    ProductUnitId = line.ProductUnitId,
                    Quantity = line.Quantity
                }));

            if (!allocationResult.Success)
                return Result<bool>.Fail(allocationResult.Message, allocationResult.Errors);

            var movementResult = await _stockService.PostMovementsAsync(lines.Select(line =>
            {
                var factor = line.QuantityPerUnitSnapshot > 0 ? line.QuantityPerUnitSnapshot : 1m;
                var baseQuantity = line.BaseQuantity != 0 ? line.BaseQuantity : line.Quantity * factor;

                return new StockMovementPostDto
                {
                    ProductId = line.ProductId,
                    ProductUnitId = line.ProductUnitId,
                    Quantity = -line.Quantity,
                    QuantityPerUnitSnapshot = factor,
                    BaseQuantity = -Math.Abs(baseQuantity),
                    UnitPrice = line.UnitPrice,
                    PurchasePrice = line.UnitCost,
                    SalePrice = line.UnitPrice,
                    ExpiryDate = line.ExpiryDate,
                    TransactionType = TransactionType.Sale,
                    InvoiceId = invoice.Id,
                    CustomerId = invoice.CustomerId,
                    TransactionDate = DateTime.Now,
                    Notes = $"Endpoint order #{invoice.InvoiceNumber} stock hold",
                    ReferenceNumber = $"ENDPOINT-HOLD-{invoice.Id}"
                };
            }));

            return movementResult.Success
                ? Result<bool>.Ok(true, movementResult.Message)
                : Result<bool>.Fail(movementResult.Message, movementResult.Errors);
        }

        private async Task RestoreStockAsync(Invoice invoice, CancellationToken cancellationToken)
        {
            var transactions = await _context.Set<StockTransaction>()
                .Where(item => item.InvoiceId == invoice.Id)
                .OrderBy(item => item.Id)
                .ToListAsync(cancellationToken);

            if (transactions.Sum(item => item.BaseQuantity) >= 0)
                return;

            var outgoing = transactions.Where(item => item.BaseQuantity < 0).ToList();
            var lotIds = outgoing
                .Where(item => item.StockLotId.HasValue)
                .Select(item => item.StockLotId!.Value)
                .Distinct()
                .ToList();
            var lots = await _context.Set<StockLot>()
                .Where(item => lotIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, cancellationToken);
            var now = DateTime.Now;

            foreach (var transaction in outgoing)
            {
                if (!transaction.StockLotId.HasValue ||
                    !lots.TryGetValue(transaction.StockLotId.Value, out var lot))
                {
                    continue;
                }

                var restoredBaseQuantity = Math.Abs(transaction.BaseQuantity);
                lot.RemainingBaseQuantity += restoredBaseQuantity;
                lot.RemainingQuantity = lot.RemainingBaseQuantity /
                                        (lot.QuantityPerUnitSnapshot > 0 ? lot.QuantityPerUnitSnapshot : 1m);
                lot.Status = BatchStatus.Active;
                lot.ClosedDate = null;
                lot.ClosedReason = null;
                lot.UpdatedDate = now;

                _context.Set<StockTransaction>().Add(new StockTransaction
                {
                    ProductId = transaction.ProductId,
                    ProductUnitId = transaction.ProductUnitId,
                    StockLotId = lot.Id,
                    Quantity = Math.Abs(transaction.Quantity),
                    QuantityPerUnitSnapshot = transaction.QuantityPerUnitSnapshot,
                    BaseQuantity = restoredBaseQuantity,
                    UnitPrice = transaction.UnitPrice,
                    ExpiryDate = transaction.ExpiryDate,
                    TransactionType = TransactionType.Return,
                    InvoiceId = invoice.Id,
                    CustomerId = invoice.CustomerId,
                    TransactionDate = now,
                    Notes = $"Endpoint order #{invoice.InvoiceNumber} stock release",
                    ReferenceNumber = $"ENDPOINT-RELEASE-{invoice.Id}",
                    CreatedDate = now,
                    UpdatedDate = now
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            await SyncStockSummariesAsync(outgoing, now, cancellationToken);
        }

        private async Task SyncStockSummariesAsync(
            IEnumerable<StockTransaction> transactions,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var keys = transactions
                .Select(item => new { item.ProductId, item.ProductUnitId })
                .Distinct()
                .ToList();

            foreach (var key in keys)
            {
                var lots = await _context.Set<StockLot>()
                    .Where(item =>
                        item.ProductId == key.ProductId &&
                        item.ProductUnitId == key.ProductUnitId &&
                        item.Status == BatchStatus.Active &&
                        item.RemainingQuantity > 0 &&
                        (!item.ExpiryDate.HasValue || item.ExpiryDate.Value >= now.Date))
                    .OrderBy(item => item.ExpiryDate == null ? 1 : 0)
                    .ThenBy(item => item.ExpiryDate)
                    .ThenBy(item => item.CreatedDate)
                    .ToListAsync(cancellationToken);

                var stock = await _context.Set<Stock>()
                    .FirstOrDefaultAsync(item =>
                        item.ProductId == key.ProductId &&
                        item.ProductUnitId == key.ProductUnitId,
                        cancellationToken);
                var currentLot = lots.OrderByDescending(item => item.CreatedDate).FirstOrDefault();

                if (stock == null)
                {
                    stock = new Stock
                    {
                        ProductId = key.ProductId,
                        ProductUnitId = key.ProductUnitId,
                        CreatedDate = now
                    };
                    _context.Set<Stock>().Add(stock);
                }

                stock.Quantity = lots.Sum(item => item.RemainingQuantity);
                stock.PurchasePrice = currentLot?.PurchasePrice ?? 0m;
                stock.SalePrice = currentLot?.SalePrice ?? 0m;
                stock.LastMovementDate = now;
                stock.UpdatedDate = now;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
