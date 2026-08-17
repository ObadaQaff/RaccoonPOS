using RaccoonWarehouse.Application.Service.FinancialTransactions;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.FinancialTransactions.DTOs;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.StockTransactions.DTOs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.Sales
{
    public sealed class SaleCheckoutRequest
    {
        public InvoiceWriteDto Invoice { get; init; } = new();
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
        private readonly IInvoiceService _invoiceService;
        private readonly IStockService _stockService;
        private readonly IFinancialTransactionService _financialService;
        private readonly IAccountingOperationService _accountingOperationService;
        private readonly IUOW _uow;

        public SaleCheckoutService(
            IInvoiceService invoiceService,
            IStockService stockService,
            IFinancialTransactionService financialService,
            IAccountingOperationService accountingOperationService,
            IUOW uow)
        {
            _invoiceService = invoiceService;
            _stockService = stockService;
            _financialService = financialService;
            _accountingOperationService = accountingOperationService;
            _uow = uow;
        }

        public async Task<Result<SaleCheckoutResult>> CompleteAsync(SaleCheckoutRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var timing = Stopwatch.StartNew();
            var stepTiming = Stopwatch.StartNew();
            await using var transaction = await _uow.BeginTransactionAsync();
            try
            {
                var invoiceResult = request.Invoice.Id > 0
                    ? await _invoiceService.UpdateAsync(request.Invoice)
                    : await _invoiceService.CreateAsync(request.Invoice);
                LogTiming("invoice save", timing, stepTiming);

                if (!invoiceResult.Success || invoiceResult.Data == null)
                {
                    await transaction.RollbackAsync();
                    return Result<SaleCheckoutResult>.Fail(invoiceResult.Message ?? "Failed to save the invoice.");
                }

                var savedInvoice = invoiceResult.Data;
                var savedInvoiceId = savedInvoice.Id > 0 ? savedInvoice.Id : request.Invoice.Id;
                if (request.Invoice.DeferAccountingPosting)
                    await _accountingOperationService.EnqueueInvoiceAsync(request.Invoice);
                var movementResult = await _stockService.PostMovementsAsync(request.StockMovementsFactory(savedInvoiceId));
                LogTiming("stock posting", timing, stepTiming);
                if (!movementResult.Success)
                {
                    await transaction.RollbackAsync();
                    return Result<SaleCheckoutResult>.Fail(movementResult.Message ?? "Failed to update stock.");
                }

                if (savedInvoice.PaymentType != PaymentType.Credit)
                {
                    await _accountingOperationService.EnqueueFinancialAsync(
                        BuildFinancialPost(savedInvoice, savedInvoiceId, request.Session),
                        savedInvoice.InvoiceNumber ?? savedInvoiceId.ToString());
                }

                await transaction.CommitAsync();
                LogTiming("checkout transaction commit", timing, stepTiming);
                PosPerformanceLogger.Write("checkout total", timing.ElapsedMilliseconds, timing.ElapsedMilliseconds);

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
                throw;
            }
        }

        private static FinancialPostDto BuildFinancialPost(InvoiceWriteDto invoice, int invoiceId, CashierSessionReadDto session)
        {
            return new FinancialPostDto
            {
                Direction = ResolveDirection(invoice.InvoiceType, invoice.TotalAmount),
                Method = MapPaymentMethod(invoice.PaymentType ?? PaymentType.Cash),
                Amount = Math.Abs(invoice.TotalAmount),
                TransactionDate = DateTime.Now,
                SourceType = MapSourceTypeByInvoiceType(invoice.InvoiceType),
                SourceId = invoiceId,
                CashierSessionId = session.Id,
                CashierId = session.CashierId,
                Notes = $"{invoice.InvoiceType} Invoice #{invoice.InvoiceNumber}"
            };
        }

        private static FinancialSourceType MapSourceTypeByInvoiceType(InvoiceType invoiceType)
        {
            return invoiceType switch
            {
                InvoiceType.Sale => FinancialSourceType.PosSaleInvoice,
                InvoiceType.Return => FinancialSourceType.SaleReturn,
                InvoiceType.Exchange => FinancialSourceType.PosSaleInvoice,
                _ => FinancialSourceType.Manual
            };
        }

        private static TransactionDirection ResolveDirection(InvoiceType invoiceType, decimal totalAmount)
        {
            if (invoiceType == InvoiceType.Return)
                return TransactionDirection.Out;

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
