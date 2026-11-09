using RaccoonWarehouse.Application.Service.Insurance;
using RaccoonWarehouse.Domain.Insurance;
using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Audit;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.StockTransactions.DTOs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.Sales
{
    public sealed class SaleCheckoutRequest
    {
        public InvoiceWriteDto Invoice { get; init; } = new();
        public PosInsuranceBillingDto? InsuranceDetails { get; init; }
        public CashierSessionReadDto Session { get; init; } = new();
        public Func<int, IEnumerable<StockMovementPostDto>> StockMovementsFactory { get; init; } = _ => Array.Empty<StockMovementPostDto>();
    }

    public sealed class SaleCheckoutResult
    {
        public InvoiceWriteDto SavedInvoice { get; init; } = new();
        public InvoiceReadDto? FullInvoice { get; init; }
    }

    public interface ISaleCheckoutService
    {
        Task<Result<SaleCheckoutResult>> CompleteAsync(SaleCheckoutRequest request);
    }

    public sealed class SaleCheckoutService : ISaleCheckoutService
    {
        private readonly IAccountingService? _immediateAccounting;
        private readonly PosInsuranceBillingService? _insuranceBilling;
        private readonly IInvoiceService _invoiceService;
        private readonly IStockService _stockService;
        private readonly IFinancialTransactionService _financialService;
        private readonly IAccountingOperationService _accountingOperationService;
        private readonly IUOW _uow;
        private readonly IAuditLogService _auditLogService;
        private readonly IPosProductCatalogRefreshQueue? _posProductCatalogRefreshQueue;

        public SaleCheckoutService(
            IInvoiceService invoiceService,
            IStockService stockService,
            IFinancialTransactionService financialService,
            IAccountingOperationService accountingOperationService,
            IUOW uow,
            IAuditLogService auditLogService,
            IPosProductCatalogRefreshQueue? posProductCatalogRefreshQueue = null,
            PosInsuranceBillingService? insuranceBilling = null,
            IAccountingService? immediateAccounting = null)
        {
            _immediateAccounting = immediateAccounting;
            _insuranceBilling = insuranceBilling;
            _invoiceService = invoiceService;
            _stockService = stockService;
            _financialService = financialService;
            _accountingOperationService = accountingOperationService;
            _uow = uow;
            _auditLogService = auditLogService;
            _posProductCatalogRefreshQueue = posProductCatalogRefreshQueue;
        }

        public async Task<Result<SaleCheckoutResult>> CompleteAsync(SaleCheckoutRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var timing = Stopwatch.StartNew();
            var stepTiming = Stopwatch.StartNew();
            await using var transaction = await _uow.BeginTransactionAsync();
            try
            {
                var invoice = request.Invoice;
                PosInsuranceBillingDto? insuranceSnapshot = null;
                if (request.InsuranceDetails != null)
                {
                    if (_insuranceBilling == null) throw new InvalidOperationException("Insurance billing is unavailable.");
                    var prepared = await _insuranceBilling.PrepareAsync(invoice, request.InsuranceDetails);
                    if (!prepared.Success || prepared.Data == null)
                    {
                        await transaction.RollbackAsync();
                        if (request.InsuranceDetails != null) _insuranceBilling?.DiscardTrackingAfterRollback();
                        return Result<SaleCheckoutResult>.Fail(prepared.Message);
                    }
                    invoice = prepared.Data.Invoice;
                    insuranceSnapshot = prepared.Data.Snapshot;
                }
                // POS can retain an invoice id after a draft/held invoice was deleted or
                // rolled back. Do not try to update a row that no longer exists.
                if (invoice.Id > 0)
                {
                    var existingInvoiceResult = await _invoiceService.GetAllWriteDtoWithFilteringAndIncludeAsync(
                        row => row.Id == invoice.Id);
                    if (!existingInvoiceResult.Success || existingInvoiceResult.Data?.Any() != true)
                        invoice.Id = 0;
                }

                // A failed checkout can leave an unfinished draft inside the current transaction
                // boundary. Reuse that draft on retry instead of attempting to create the same
                // invoice number again. Completed/posted invoices are never reused here.
                if (invoice.Id <= 0
                    && invoice.IsPOS == true
                    && !string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
                {
                    var draftResult = await _invoiceService.GetAllWriteDtoWithFilteringAndIncludeAsync(
                        row => row.InvoiceNumber == invoice.InvoiceNumber
                            && row.IsPOS == true
                            && row.CasherId == invoice.CasherId
                            && row.CashierSessionId == invoice.CashierSessionId
                            && (row.Status == InvoiceStatus.Draft || row.Status == InvoiceStatus.OnHold));

                    var existingDraft = draftResult.Success ? draftResult.Data?.FirstOrDefault() : null;
                    if (existingDraft != null)
                        invoice.Id = existingDraft.Id;
                }

                var invoiceResult = invoice.Id > 0
                    ? await _invoiceService.UpdateAsync(invoice)
                    : await _invoiceService.CreateAsync(invoice);
                LogTiming("invoice save", timing, stepTiming);

                if (!invoiceResult.Success || invoiceResult.Data == null)
                {
                    await transaction.RollbackAsync();
                    if (request.InsuranceDetails != null) _insuranceBilling?.DiscardTrackingAfterRollback();
                    return Result<SaleCheckoutResult>.Fail(invoiceResult.Message ?? "Failed to save the invoice.");
                }

                var savedInvoice = invoiceResult.Data;
                var savedInvoiceId = savedInvoice.Id > 0 ? savedInvoice.Id : invoice.Id;
                if (insuranceSnapshot != null)
                {
                    if (_immediateAccounting == null)
                        throw new InvalidOperationException("Insurance accounting is unavailable.");
                    await _insuranceBilling!.SaveCoverageAndClaimAsync(savedInvoice, insuranceSnapshot);
                    // The claim must not commit before its company receivable reaches the ledger.
                    var posting = await _immediateAccounting.PostInvoiceEntryAsync(savedInvoice);
                    if (!posting.Success)
                    {
                        await transaction.RollbackAsync();
                        _insuranceBilling.DiscardTrackingAfterRollback();
                        return Result<SaleCheckoutResult>.Fail("Insurance accounting posting failed: " + posting.Message);
                    }
                }
                else if (invoice.DeferAccountingPosting)
                    await _accountingOperationService.EnqueueInvoiceAsync(savedInvoice);
                var stockMovements = request.StockMovementsFactory(savedInvoiceId)?.ToList()
                    ?? new List<StockMovementPostDto>();
                var affectedProductIds = stockMovements
                    .Select(movement => movement.ProductId)
                    .Where(productId => productId > 0)
                    .Distinct()
                    .ToList();
                var movementResult = await _stockService.PostMovementsAsync(
                    stockMovements,
                    refreshCatalog: false,
                    refreshStockSummary: false);
                LogTiming("stock posting", timing, stepTiming);
                if (!movementResult.Success)
                {
                    await transaction.RollbackAsync();
                    if (request.InsuranceDetails != null) _insuranceBilling?.DiscardTrackingAfterRollback();
                    return Result<SaleCheckoutResult>.Fail(movementResult.Message ?? "Failed to update stock.");
                }

                var paymentAllocations = savedInvoice.Payments?.Where(payment => payment.Amount > 0m).ToList()
                    ?? new List<InvoicePaymentWriteDto>();
                if (paymentAllocations.Count == 0 && savedInvoice.PaymentType.HasValue)
                {
                    paymentAllocations.Add(new InvoicePaymentWriteDto
                    {
                        PaymentType = savedInvoice.PaymentType.Value,
                        Amount = Math.Abs(savedInvoice.TotalAmount)
                    });
                }

                foreach (var payment in paymentAllocations.Where(payment => payment.Amount > 0m && payment.PaymentType is not (PaymentType.Credit or PaymentType.InsuranceReceivable)))
                {
                    await _accountingOperationService.EnqueueFinancialAsync(
                        BuildFinancialPost(savedInvoice, savedInvoiceId, request.Session, payment.PaymentType, payment.Amount),
                        savedInvoice.InvoiceNumber ?? savedInvoiceId.ToString());
                }

                // Persist patient receipt queue rows before committing the insurance transaction.
                if (insuranceSnapshot != null)
                    await _uow.CommitAsync();
                await transaction.CommitAsync();
                _posProductCatalogRefreshQueue?.Enqueue(
                    affectedProductIds,
                    refreshStockSummary: true);
                LogTiming("checkout transaction commit", timing, stepTiming);
                PosPerformanceLogger.Write("checkout total", timing.ElapsedMilliseconds, timing.ElapsedMilliseconds);
                await _auditLogService.WriteAsync(new AuditEvent(
                    "Invoice.Checkout",
                    EntityType: "Invoice",
                    EntityId: savedInvoiceId));

                return Result<SaleCheckoutResult>.Ok(
                    new SaleCheckoutResult
                    {
                        SavedInvoice = savedInvoice,
                        FullInvoice = null
                    },
                    "Sale checkout completed successfully.");
            }
            catch
            {
                await transaction.RollbackAsync();
                if (request.InsuranceDetails != null) _insuranceBilling?.DiscardTrackingAfterRollback();
                throw;
            }
        }

        private static FinancialPostDto BuildFinancialPost(
            InvoiceWriteDto invoice,
            int invoiceId,
            CashierSessionReadDto session,
            PaymentType paymentType,
            decimal amount)
        {
            return new FinancialPostDto
            {
                Direction = ResolveDirection(invoice.InvoiceType, invoice.TotalAmount),
                Method = MapPaymentMethod(paymentType),
                Amount = Math.Abs(amount),
                TransactionDate = DateTime.Now,
                SourceType = MapSourceTypeByInvoiceType(invoice.InvoiceType),
                SourceId = invoiceId,
                CashierSessionId = session.Id,
                CashierId = session.CashierId,
                Notes = string.IsNullOrWhiteSpace(invoice.Notes)
                    ? $"{invoice.InvoiceType} Invoice #{invoice.InvoiceNumber}"
                    : $"{invoice.InvoiceType} Invoice #{invoice.InvoiceNumber} | {invoice.Notes}"
            };
        }

        private static FinancialSourceType MapSourceTypeByInvoiceType(InvoiceType invoiceType)
        {
            return invoiceType switch
            {
                InvoiceType.Sale => FinancialSourceType.PosSaleInvoice,
                InvoiceType.Return => FinancialSourceType.SaleReturn,
                InvoiceType.PurchaseReturn => FinancialSourceType.PurchaseReturn,
                InvoiceType.Exchange => FinancialSourceType.PosSaleInvoice,
                _ => FinancialSourceType.Manual
            };
        }

        private static TransactionDirection ResolveDirection(InvoiceType invoiceType, decimal totalAmount)
        {
            if (invoiceType == InvoiceType.Return)
                return totalAmount >= 0 ? TransactionDirection.In : TransactionDirection.Out;

            if (invoiceType == InvoiceType.PurchaseReturn)
                return TransactionDirection.In;

            if (invoiceType == InvoiceType.Exchange)
                return totalAmount >= 0 ? TransactionDirection.In : TransactionDirection.Out;

            return TransactionDirection.In;
        }

        private static PaymentMethod MapPaymentMethod(PaymentType paymentType)
        {
            return paymentType switch
            {
                PaymentType.Cash => PaymentMethod.Cash,
                PaymentType.Visa => PaymentMethod.Visa,
                PaymentType.Master => PaymentMethod.Master,
                PaymentType.Debit => PaymentMethod.BankTransfer,
                PaymentType.Check => PaymentMethod.Check,
                PaymentType.MobilePayment => PaymentMethod.MobilePayment,
                PaymentType.Credit => PaymentMethod.Credit,
                PaymentType.InsuranceReceivable => PaymentMethod.Credit,
                _ => PaymentMethod.Cash
            };
        }

        private static void LogTiming(string step, Stopwatch totalTiming, Stopwatch stepTiming)
        {
            var stepMilliseconds = stepTiming.ElapsedMilliseconds;
            var totalMilliseconds = totalTiming.ElapsedMilliseconds;
            PosPerformanceLogger.Write(step, stepMilliseconds, totalMilliseconds);
            Debug.WriteLine($"[POS timing] {step}: {stepMilliseconds} ms (total {totalMilliseconds} ms)");
            stepTiming.Restart();
        }
    }
}
