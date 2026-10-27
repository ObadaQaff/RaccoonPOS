using RaccoonWarehouse.Application.Service.Purchases;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Application.Service.Sales;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Checks;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Reports.Financial.Dtos;
using RaccoonWarehouse.Domain.Reports.Financial.Filters;
using RaccoonWarehouse.Domain.Reports.Sales.Dtos;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.Invoices
{
    public class InvoiceService : GenericService<Invoice, InvoiceWriteDto, InvoiceReadDto>, IInvoiceService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        private readonly IAccountingService? _accountingService;

        //POS Operations
        #region POS Operations
        //POS Invoice Creation
        public async Task<Result<InvoiceWriteDto>> CreatePOSInvoice(InvoiceWriteDto Dto)
        {



            return Result<InvoiceWriteDto>.Ok(null, "Not implemented yet");
        }

        #endregion


        public InvoiceService(ApplicationDbContext context, IUOW uow, IMapper mapper) : this(context, uow, mapper, null)
        {
        }

        public InvoiceService(ApplicationDbContext context, IUOW uow, IMapper mapper, IAccountingService? accountingService) : base(context, uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
            _accountingService = accountingService;
        }
        public override async Task<Result<InvoiceWriteDto>> CreateAsync(InvoiceWriteDto dto)
        {
            var totalTiming = Stopwatch.StartNew();
            var stepTiming = Stopwatch.StartNew();
            try
            {
                var invoiceRepo = _uow.GetRepository<Invoice>();

                var normalizedFalconInvoiceNumber = dto.FalconInvoiceNumber?.Trim();
                dto.FalconInvoiceNumber = string.IsNullOrWhiteSpace(normalizedFalconInvoiceNumber)
                    ? null
                    : normalizedFalconInvoiceNumber;

                dto.InvoiceNumber = dto.InvoiceNumber?.Trim() ?? string.Empty;

                if (dto.Id <= 0)
                {
                    if (string.IsNullOrWhiteSpace(dto.InvoiceNumber))
                        return Result<InvoiceWriteDto>.Fail(dto.InvoiceType == InvoiceType.Purchase
                            ? "Supplier invoice number is required / رقم فاتورة المورد مطلوب."
                            : "Invoice number is required / رقم الفاتورة مطلوب.");
                    if (dto.InvoiceType != InvoiceType.Purchase &&
                        (!int.TryParse(dto.InvoiceNumber, out var invoiceNumber) || invoiceNumber < 100 || invoiceNumber > 99999))
                        return Result<InvoiceWriteDto>.Fail("Invoice number must contain 3 to 5 digits / رقم الفاتورة يجب أن يتكون من 3 إلى 5 أرقام.");

                    var invoiceNumberExists = await invoiceRepo.GetAllAsQueryable()
                        .AnyAsync(invoice => invoice.InvoiceNumber == dto.InvoiceNumber);
                    if (invoiceNumberExists)
                        return Result<InvoiceWriteDto>.Fail("Invoice number already exists / رقم الفاتورة موجود مسبقاً.");
                }

                if (dto.InvoiceLines == null || !dto.InvoiceLines.Any())
                    return Result<InvoiceWriteDto>.Fail("Invoice must contain at least one line.");
                if (dto.InvoiceType == InvoiceType.PurchaseReturn
                    && !dto.InvoiceLines.Any(line => line.ProductId > 0 && line.ProductUnitId > 0 && (line.Quantity < 0 || line.FreeQuantity > 0)))
                    return Result<InvoiceWriteDto>.Fail("Purchase return must contain at least one returned quantity.");
                if (dto.InvoiceType == InvoiceType.Purchase && dto.InvoiceLines.Any(line => line.ExpiryDate == default))
                    return Result<InvoiceWriteDto>.Fail("Expiry date is required for every purchase invoice line.");
                var partyValidation = ValidateCreditParty(dto);
                if (partyValidation != null)
                    return Result<InvoiceWriteDto>.Fail(partyValidation);

                if (dto.InvoiceType == InvoiceType.Purchase)
                    PurchaseBonusCosting.Validate(dto.InvoiceLines);
                await PurchaseBonusReturns.PrepareAsync(_context, dto);
                var returnPaymentValidation = await ValidateReturnPaymentTermsAsync(dto, invoiceRepo);
                if (returnPaymentValidation != null)
                    return Result<InvoiceWriteDto>.Fail(returnPaymentValidation);

                await NormalizeInvoiceLinesAsync(dto.InvoiceLines);

                // 1) احسب قيم السطور (Tax/Cost/Profit) قبل الحفظ
                foreach (var l in dto.InvoiceLines)
                {
                    var returnSubtotal = l.LineSubTotal;
                    var returnTax = l.TaxAmount;
                    var grossLineTotal = GetGrossLineTotal(dto.InvoiceType, l.Quantity, l.UnitPrice);
                    l.LineDiscountAmount = Math.Clamp(l.LineDiscountAmount, 0m, grossLineTotal);
                    l.FreeQuantity = Math.Max(0m, l.FreeQuantity);
                    var lineTotal = grossLineTotal - l.LineDiscountAmount;
                    var taxRate = l.TaxExempt ? 0m : l.TaxRate;
                    var isPurchaseInvoice = dto.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn;
                    // Sales prices are tax-inclusive; purchases depend on the invoice setting.
                    var includesTax = !isPurchaseInvoice || dto.CostPriceIncludesTax;
                    var divisor = includesTax ? 1m + (taxRate / 100m) : 1m;

                    l.LineSubTotal = l.TaxExempt || divisor <= 0m
                        ? lineTotal
                        : Math.Round(lineTotal / divisor, 3);
                    l.TaxAmount = includesTax ? Math.Round(lineTotal - l.LineSubTotal, 3) : 0m;

                    if (dto.InvoiceType == InvoiceType.PurchaseReturn && l.SourcePurchaseLineId.HasValue)
                    {
                        l.LineSubTotal = returnSubtotal;
                        l.TaxAmount = returnTax;
                    }
                    var costTotal = l.Quantity * (l.PurchaseAcquisitionCost.HasValue && !isPurchaseInvoice
                        ? l.UnitCost : CalculateEffectivePurchaseCost(l.UnitCost, l.TaxRate, l.TaxExempt, includesTax));
                    var profitBeforeTax = l.LineSubTotal - costTotal;

                    l.ProfitBeforeTax = profitBeforeTax;
                    l.Profit = profitBeforeTax;
                }

                if (dto.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn)
                    ApplyPurchaseDiscountAndTax(dto.InvoiceLines, dto.DiscountAmount, dto.CostPriceIncludesTax);
                if (dto.InvoiceType == InvoiceType.Purchase)
                    PurchaseBonusCosting.Allocate(dto.InvoiceLines, dto.DiscountAmount, dto.CostPriceIncludesTax);

                // 2) احسب قيم الفاتورة
                // Sales totals must include both invoice-level and line-level discounts.
                // The line total is already normalized above, so use the same gross
                // semantics used for line tax/profit calculations.
                var grossSales = dto.InvoiceLines.Sum(x =>
                    GetGrossLineTotal(dto.InvoiceType, x.Quantity, x.UnitPrice) - x.LineDiscountAmount);
                dto.SubTotal = dto.InvoiceLines.Sum(x => x.LineSubTotal);
                dto.TotalTax = dto.InvoiceLines.Sum(x => x.TaxAmount);
                var calculatedTotal = dto.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn
                    ? dto.SubTotal - (dto.DiscountAmount ?? 0m) + dto.TotalTax
                    : grossSales - (dto.DiscountAmount ?? 0m);
                dto.TotalAmount = dto.InvoiceType == InvoiceType.Return
                    ? -calculatedTotal
                    : calculatedTotal;
                dto.TotalCOGS = dto.InvoiceLines.Sum(x => x.Quantity * CalculateEffectivePurchaseCost(
                    x.UnitCost,
                    x.TaxRate,
                    x.TaxExempt,
                    (dto.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn) && dto.CostPriceIncludesTax));
                if (dto.InvoiceType == InvoiceType.PurchaseReturn && dto.InvoiceLines.Any(x => x.SourcePurchaseLineId.HasValue))
                    dto.TotalCOGS = dto.InvoiceLines.Sum(x => x.PurchaseAcquisitionCost ?? 0m);
                dto.NetSales = dto.SubTotal - (dto.DiscountAmount ?? 0m); // قبل الضريبة
                dto.GrossProfit = dto.NetSales - dto.TotalCOGS;
                LogInvoiceTiming("invoice line normalization and totals", totalTiming, stepTiming);
                var insuranceValidation = ValidateInsuranceBilling(dto);
                if (insuranceValidation != null)
                    return Result<InvoiceWriteDto>.Fail(insuranceValidation);
                var paymentValidation = dto.Status is InvoiceStatus.OnHold or InvoiceStatus.Cancelled
                    ? null
                    : NormalizeAndValidatePayments(dto);
                if (paymentValidation != null)
                    return Result<InvoiceWriteDto>.Fail(paymentValidation);
                var checkValidation = ValidateChecks(dto);
                if (checkValidation != null)
                    return Result<InvoiceWriteDto>.Fail(checkValidation);
                if (dto.CreatedDate == default)
                    dto.CreatedDate = DateTime.Now;
                dto.UpdatedDate = DateTime.Now;

                // 3) أنشئ invoice بدون lines (مهم)
                await using var createTransaction = await _uow.BeginTransactionAsync();
                var invoice = _mapper.Map<Invoice>(dto);
                invoice.InvoiceLines = new List<InvoiceLine>();
                invoice.Checks = new List<Check>();
                invoice.Payments = new List<InvoicePayment>();

                if (invoice.Id > 0)
                {
                await _uow.CommitAsync(); // ✅ هسا صار عندك invoice.Id
                LogInvoiceTiming("invoice header database save", totalTiming, stepTiming);
                }

                // 4) أضف السطور وربط InvoiceId يدوي
                foreach (var l in dto.InvoiceLines)
                {
                    var line = _mapper.Map<InvoiceLine>(l);

                    line.Id = 0;
                    line.InvoiceId = invoice.Id;  // ✅ أهم سطر
                    line.Invoice = null;          // اختياري لتجنب tracking issues
                    line.Product = null;
                    line.ProductUnit = null;

                    line.Invoice = invoice;
                    invoice.InvoiceLines.Add(line);
                }

                foreach (var paymentDto in dto.Payments ?? Enumerable.Empty<InvoicePaymentWriteDto>())
                {
                    var payment = _mapper.Map<InvoicePayment>(paymentDto);
                    payment.Id = 0;
                    payment.InvoiceId = 0;
                    payment.Invoice = null;
                    payment.Invoice = invoice;
                    invoice.Payments.Add(payment);
                }

                foreach (var checkDto in dto.Checks ?? Enumerable.Empty<Domain.Checks.DTOs.CheckWriteDto>())
                {
                    var check = _mapper.Map<Check>(checkDto);
                    check.Id = 0;
                    check.InvoiceId = 0;
                    check.Invoice = null;
                    check.Invoice = invoice;
                    invoice.Checks.Add(check);
                }

                await invoiceRepo.AddAsync(invoice);
                await _uow.CommitAsync();
                await createTransaction.CommitAsync();
                LogInvoiceTiming("invoice graph database save", totalTiming, stepTiming);

                dto.Id = invoice.Id;
                foreach (var pair in dto.InvoiceLines.Zip(invoice.InvoiceLines))
                {
                    pair.First.Id = pair.Second.Id;
                    pair.First.InvoiceId = invoice.Id;
                }
                var shouldPostAccounting = _accountingService != null
                    && !dto.DeferAccountingPosting
                    && dto.Status is not InvoiceStatus.Draft
                    && dto.Status is not InvoiceStatus.OnHold;

                if (shouldPostAccounting)
                {
                    var journalResult = await _accountingService.PostInvoiceEntryAsync(dto);
                    LogInvoiceTiming("invoice accounting entry", totalTiming, stepTiming);
                    if (!journalResult.Success)
                    {
                        _context.Set<InvoiceLine>().RemoveRange(_context.Set<InvoiceLine>().Where(x => x.InvoiceId == invoice.Id));
                        _context.Set<InvoicePayment>().RemoveRange(_context.Set<InvoicePayment>().Where(x => x.InvoiceId == invoice.Id));
                        _context.Set<Check>().RemoveRange(_context.Set<Check>().Where(x => x.InvoiceId == invoice.Id));
                        _context.Set<Invoice>().Remove(invoice);
                        await _uow.CommitAsync();
                        LogInvoiceTiming("invoice rollback database save", totalTiming, stepTiming);
                        return Result<InvoiceWriteDto>.Fail($"Invoice creation was rolled back because accounting posting failed: {journalResult.Message}");
                    }
                }

                return Result<InvoiceWriteDto>.Ok(dto, "Invoice created successfully.");
            }
            catch (Exception ex)
            {
                return Result<InvoiceWriteDto>.Fail($"Error creating invoice: {ex.Message}");
            }
        }

        private static void LogInvoiceTiming(string step, Stopwatch totalTiming, Stopwatch stepTiming)
        {
            var stepMilliseconds = stepTiming.ElapsedMilliseconds;
            var totalMilliseconds = totalTiming.ElapsedMilliseconds;
            PosPerformanceLogger.Write(step, stepMilliseconds, totalMilliseconds);
            Debug.WriteLine($"[POS timing] {step}: {stepMilliseconds} ms (total {totalMilliseconds} ms)");
            stepTiming.Restart();
        }

        public override async Task<Result<InvoiceWriteDto>> UpdateAsync(InvoiceWriteDto dto)
        {
            try
            {
                if (dto.Id <= 0)
                    return Result<InvoiceWriteDto>.Fail("Invoice id is required.");

                if (dto.InvoiceLines == null || !dto.InvoiceLines.Any())
                    return Result<InvoiceWriteDto>.Fail("Invoice must contain at least one line.");
                if (dto.InvoiceType == InvoiceType.PurchaseReturn
                    && !dto.InvoiceLines.Any(line => line.ProductId > 0 && line.ProductUnitId > 0 && (line.Quantity < 0 || line.FreeQuantity > 0)))
                    return Result<InvoiceWriteDto>.Fail("Purchase return must contain at least one returned quantity.");
                if (dto.InvoiceType == InvoiceType.Purchase && dto.InvoiceLines.Any(line => line.ExpiryDate == default))
                    return Result<InvoiceWriteDto>.Fail("Expiry date is required for every purchase invoice line.");
                var partyValidation = ValidateCreditParty(dto);
                if (partyValidation != null)
                    return Result<InvoiceWriteDto>.Fail(partyValidation);

                var invoiceRepo = _uow.GetRepository<Invoice>();
                var lineRepo = _uow.GetRepository<InvoiceLine>();
                var normalizedFalconInvoiceNumber = dto.FalconInvoiceNumber?.Trim();
                dto.FalconInvoiceNumber = string.IsNullOrWhiteSpace(normalizedFalconInvoiceNumber)
                    ? null
                    : normalizedFalconInvoiceNumber;
                dto.InvoiceNumber = dto.InvoiceNumber?.Trim() ?? string.Empty;

                var existingInvoice = await invoiceRepo.GetAllAsQueryable()
                    .Include(x => x.InvoiceLines)
                    .Include(x => x.Payments)
                    .Include(x => x.Checks)
                    .FirstOrDefaultAsync(x => x.Id == dto.Id);

                if (existingInvoice == null)
                    return Result<InvoiceWriteDto>.Fail("Invoice not found.");

                if (string.IsNullOrWhiteSpace(dto.InvoiceNumber))
                    return Result<InvoiceWriteDto>.Fail(dto.InvoiceType == InvoiceType.Purchase
                        ? "Supplier invoice number is required / رقم فاتورة المورد مطلوب."
                        : "Invoice number is required / رقم الفاتورة مطلوب.");

                if (dto.InvoiceType != InvoiceType.Purchase &&
                    (!int.TryParse(dto.InvoiceNumber, out var invoiceNumber) || invoiceNumber < 100 || invoiceNumber > 99999))
                    return Result<InvoiceWriteDto>.Fail("Invoice number must contain 3 to 5 digits / رقم الفاتورة يجب أن يتكون من 3 إلى 5 أرقام.");

                // Invoice numbers are global because all invoice types share the Invoice table.
                // Updating an invoice must not allow it to take another invoice's number.
                var invoiceNumberUsedByAnotherInvoice = await invoiceRepo.GetAllAsQueryable()
                    .AnyAsync(invoice => invoice.InvoiceNumber == dto.InvoiceNumber && invoice.Id != dto.Id);
                if (invoiceNumberUsedByAnotherInvoice)
                    return Result<InvoiceWriteDto>.Fail("Invoice number already exists / رقم الفاتورة موجود مسبقاً.");

                if (dto.InvoiceType == InvoiceType.Purchase)
                    PurchaseBonusCosting.Validate(dto.InvoiceLines);
                await PurchaseBonusReturns.PrepareAsync(_context, dto);
                var returnPaymentValidation = await ValidateReturnPaymentTermsAsync(dto, invoiceRepo);
                if (returnPaymentValidation != null)
                    return Result<InvoiceWriteDto>.Fail(returnPaymentValidation);

                await NormalizeInvoiceLinesAsync(dto.InvoiceLines);
                ApplyInvoiceTotals(dto, existingInvoice.CreatedDate);
                var insuranceValidation = ValidateInsuranceBilling(dto);
                if (insuranceValidation != null)
                    return Result<InvoiceWriteDto>.Fail(insuranceValidation);
                var paymentValidation = dto.Status is InvoiceStatus.OnHold or InvoiceStatus.Cancelled
                    ? null
                    : NormalizeAndValidatePayments(dto);
                if (paymentValidation != null)
                    return Result<InvoiceWriteDto>.Fail(paymentValidation);
                var checkValidation = ValidateChecks(dto);
                if (checkValidation != null)
                    return Result<InvoiceWriteDto>.Fail(checkValidation);

                if (existingInvoice.InvoiceLines.Any(x => x.SourcePurchaseLineId.HasValue))
                    return Result<InvoiceWriteDto>.Fail("Posted bonus purchase returns cannot be edited / لا يمكن تعديل مرتجع بونص مرحّل.");
                var allocatedSourceIds = existingInvoice.InvoiceLines.Where(x => x.PurchaseAcquisitionCost.HasValue).Select(x => x.Id).ToList();
                if (allocatedSourceIds.Count > 0 && (await _context.Set<InvoiceLine>().AnyAsync(x => x.SourcePurchaseLineId.HasValue && allocatedSourceIds.Contains(x.SourcePurchaseLineId.Value))
                    || await _context.Set<RaccoonWarehouse.Domain.StockLots.StockLot>().AnyAsync(x => x.SourcePurchaseLineId.HasValue && allocatedSourceIds.Contains(x.SourcePurchaseLineId.Value)
                        && x.RemainingBaseQuantity != x.BaseQuantity)))
                    return Result<InvoiceWriteDto>.Fail("This purchase has consumed/returned stock; use a return instead / لهذه الفاتورة مخزون مصروف أو مرتجع؛ استخدم المرتجع.");

                var oldStatus = existingInvoice.Status;
                var hadPostedAccounting = oldStatus is not InvoiceStatus.OnHold
                    and not InvoiceStatus.Draft
                    and not InvoiceStatus.Cancelled
                    and not InvoiceStatus.Unknown
                    and not InvoiceStatus.InProcess;

                var existingLines = existingInvoice.InvoiceLines?.ToList() ?? new List<InvoiceLine>();
                var existingPayments = existingInvoice.Payments?.ToList() ?? new List<InvoicePayment>();
                var existingChecks = existingInvoice.Checks?.ToList() ?? new List<Check>();
                var existingJordanStatus = existingInvoice.JordanEInvoiceStatus;
                var existingJordanReference = existingInvoice.JordanEInvoiceReference;
                var existingJordanQr = existingInvoice.JordanEInvoiceQr;
                var existingJordanResponse = existingInvoice.JordanEInvoiceResponse;
                var existingJordanSubmittedAt = existingInvoice.JordanEInvoiceSubmittedAtUtc;
                var existingJordanLastAttemptAt = existingInvoice.JordanEInvoiceLastAttemptAtUtc;
                var existingJordanAttemptCount = existingInvoice.JordanEInvoiceAttemptCount;
                var existingSupplierNumber = existingInvoice.SupplierJoFotaraNumber;
                var existingSupplierUuid = existingInvoice.SupplierJoFotaraUuid;
                var existingSupplierQr = existingInvoice.SupplierJoFotaraQr;

                _mapper.Map(dto, existingInvoice);
                existingInvoice.CreatedDate = dto.CreatedDate;
                existingInvoice.UpdatedDate = dto.UpdatedDate;
                existingInvoice.JordanEInvoiceStatus = existingJordanStatus;
                existingInvoice.JordanEInvoiceReference = existingJordanReference;
                existingInvoice.JordanEInvoiceQr = existingJordanQr;
                existingInvoice.JordanEInvoiceResponse = existingJordanResponse;
                existingInvoice.JordanEInvoiceSubmittedAtUtc = existingJordanSubmittedAt;
                existingInvoice.JordanEInvoiceLastAttemptAtUtc = existingJordanLastAttemptAt;
                existingInvoice.JordanEInvoiceAttemptCount = existingJordanAttemptCount;
                existingInvoice.SupplierJoFotaraNumber = existingSupplierNumber;
                existingInvoice.SupplierJoFotaraUuid = existingSupplierUuid;
                existingInvoice.SupplierJoFotaraQr = existingSupplierQr;

                if (existingLines.Count > 0)
                    _context.Set<InvoiceLine>().RemoveRange(existingLines);
                if (existingPayments.Count > 0)
                    _context.Set<InvoicePayment>().RemoveRange(existingPayments);
                if (existingChecks.Count > 0)
                    _context.Set<Check>().RemoveRange(existingChecks);

                existingInvoice.InvoiceLines = new List<InvoiceLine>();
                existingInvoice.Payments = new List<InvoicePayment>();
                existingInvoice.Checks = new List<Check>();

                await using var updateTransaction = await _uow.BeginTransactionAsync();
                await _uow.CommitAsync();

                var savedLinePairs = new List<(InvoiceLineWriteDto Dto, InvoiceLine Entity)>();
                foreach (var lineDto in dto.InvoiceLines)
                {
                    var line = _mapper.Map<InvoiceLine>(lineDto);
                    line.Id = 0;
                    line.InvoiceId = existingInvoice.Id;
                    line.Invoice = null;
                    line.Product = null;
                    line.ProductUnit = null;
                    line.CreatedDate = lineDto.CreatedDate == default ? dto.CreatedDate : lineDto.CreatedDate;
                    line.UpdatedDate = dto.UpdatedDate;
                    await lineRepo.AddAsync(line);
                    savedLinePairs.Add((lineDto, line));
                }

                foreach (var paymentDto in dto.Payments ?? Enumerable.Empty<InvoicePaymentWriteDto>())
                {
                    var payment = _mapper.Map<InvoicePayment>(paymentDto);
                    payment.Id = 0;
                    payment.InvoiceId = existingInvoice.Id;
                    payment.Invoice = null;
                    await _uow.GetRepository<InvoicePayment>().AddAsync(payment);
                }

                var checkRepo = _uow.GetRepository<Check>();
                foreach (var checkDto in dto.Checks ?? Enumerable.Empty<Domain.Checks.DTOs.CheckWriteDto>())
                {
                    var check = _mapper.Map<Check>(checkDto);
                    check.Id = 0;
                    check.InvoiceId = existingInvoice.Id;
                    check.Invoice = null;
                    await checkRepo.AddAsync(check);
                }

                await _uow.CommitAsync();
                foreach (var pair in savedLinePairs)
                {
                    pair.Dto.Id = pair.Entity.Id;
                    pair.Dto.InvoiceId = existingInvoice.Id;
                }
                await updateTransaction.CommitAsync();

                var shouldPostAccounting = _accountingService != null
                    && dto.Status is not InvoiceStatus.Draft
                    && dto.Status is not InvoiceStatus.OnHold;

                if (shouldPostAccounting)
                {
                    if (hadPostedAccounting)
                    {
                        var reverseResult = await _accountingService.ReverseJournalByReferenceAsync(
                            "Invoice",
                            existingInvoice.Id,
                            $"Repost invoice #{existingInvoice.InvoiceNumber} after update");

                        if (!reverseResult.Success)
                            return Result<InvoiceWriteDto>.Fail($"Invoice update was blocked because accounting reversal failed: {reverseResult.Message}");
                    }

                    var repostResult = await _accountingService.PostInvoiceEntryAsync(dto);
                    if (!repostResult.Success)
                    {
                        return Result<InvoiceWriteDto>.Fail($"Invoice data was updated but accounting repost failed: {repostResult.Message}");
                    }
                }

                return Result<InvoiceWriteDto>.Ok(dto, "Invoice updated successfully.");
            }
            catch (Exception ex)
            {
                return Result<InvoiceWriteDto>.Fail($"Error updating invoice: {ex.Message}");
            }
        }

        public async Task<Result<bool>> SaveSupplierJoFotaraReferenceAsync(
            int invoiceId,
            string? number,
            string? uuid,
            string? qr)
        {
            var invoice = await _uow.GetRepository<Invoice>().GetByIdAsync(invoiceId);
            if (invoice == null)
                return Result<bool>.Fail("Invoice not found / الفاتورة غير موجودة.");
            if (invoice.InvoiceType != InvoiceType.Purchase)
                return Result<bool>.Fail("Supplier JoFotara references can only be saved on purchase invoices / يمكن حفظ مرجع جوافوترة للمورد على فواتير الشراء فقط.");

            invoice.SupplierJoFotaraNumber = string.IsNullOrWhiteSpace(number) ? null : number.Trim();
            invoice.SupplierJoFotaraUuid = string.IsNullOrWhiteSpace(uuid) ? null : uuid.Trim();
            invoice.SupplierJoFotaraQr = string.IsNullOrWhiteSpace(qr) ? null : qr.Trim();
            invoice.UpdatedDate = DateTime.Now;
            await _uow.CommitAsync();
            return Result<bool>.Ok(true, "Supplier JoFotara reference saved / تم حفظ مرجع جوافوترة للمورد.");
        }

        private static string? ValidateCreditParty(InvoiceWriteDto dto)
        {
            if (dto.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn)
            {
                return HasPaymentType(dto, PaymentType.Credit) && !dto.SupplierId.HasValue
                    ? "A supplier is required for credit purchase invoices."
                    : null;
            }

            if (HasPaymentType(dto, PaymentType.Credit) && !dto.CustomerId.HasValue)
                return "A customer is required for credit sales invoices.";

            return null;
        }

        private static string? ValidateInsuranceBilling(InvoiceWriteDto dto)
        {
            if (!dto.IsInsuranceSale)
                return null;
            if (dto.InvoiceType is not InvoiceType.Sale)
                return "Insurance billing is supported only for sales invoices.";
            if (dto.InsuranceCompanyId is not > 0)
                return "Insurance sales require an insurance company.";
            var hasLegacyMetadata = dto.InsuranceMemberPolicyId.HasValue || dto.InsuranceContractId.HasValue
                || dto.InsuranceContractVersionId.HasValue;
            if (hasLegacyMetadata && (!dto.CustomerId.HasValue || !dto.InsuranceMemberPolicyId.HasValue
                || !dto.InsuranceContractId.HasValue || !dto.InsuranceContractVersionId.HasValue))
                return "Insurance sales require complete membership and contract details.";
            if (dto.PatientResponsibility < 0m || dto.InsuranceResponsibility < 0m)
                return "Insurance responsibility amounts cannot be negative.";
            if (Math.Abs(dto.PatientResponsibility + dto.InsuranceResponsibility - dto.TotalAmount) > 0.01m)
                return "Patient and insurer responsibility must equal the invoice total.";

            var insurerPayments = dto.Payments?.Where(x => x.PaymentType == PaymentType.InsuranceReceivable)
                .Sum(x => x.Amount) ?? 0m;
            if (Math.Abs(insurerPayments - dto.InsuranceResponsibility) > 0.01m)
                return "The insurance receivable payment must equal the insurer responsibility.";
            return null;
        }

        private static bool HasPaymentType(InvoiceWriteDto dto, PaymentType paymentType) =>
            (dto.Payments?.Any(payment => payment.PaymentType == paymentType && payment.Amount > 0m) ?? false)
            || ((dto.Payments == null || dto.Payments.Count == 0) && dto.PaymentType == paymentType);

        private static string? NormalizeAndValidatePayments(InvoiceWriteDto dto)
        {
            var payments = dto.Payments?.Where(payment => payment.Amount > 0m).ToList()
                ?? new List<InvoicePaymentWriteDto>();

            if (payments.Count == 0)
            {
                if (!dto.PaymentType.HasValue)
                    return "A payment method is required.";

                payments.Add(new InvoicePaymentWriteDto
                {
                    PaymentType = dto.PaymentType.Value,
                    Amount = Math.Abs(dto.TotalAmount)
                });
            }

            if (payments.GroupBy(payment => payment.PaymentType).Any(group => group.Count() > 1))
                return "Each payment method can appear only once.";

            var expected = Math.Round(Math.Abs(dto.TotalAmount), 3);
            var actual = Math.Round(payments.Sum(payment => payment.Amount), 3);
            if (actual != expected)
                return $"Payment amounts must equal the invoice total ({expected:0.000}).";

            dto.Payments = payments;
            dto.PaymentType = payments[0].PaymentType;
            return ValidateCreditParty(dto);
        }

        private static async Task<string?> ValidateReturnPaymentTermsAsync(
            InvoiceWriteDto dto,
            IGenericRepository<Invoice> invoiceRepo)
        {
            if (dto.InvoiceType is not (InvoiceType.Return or InvoiceType.PurchaseReturn or InvoiceType.Exchange) ||
                string.IsNullOrWhiteSpace(dto.OriginalInvoiceId))
            {
                return null;
            }

            var original = await invoiceRepo.GetAllAsQueryable()
                .AsNoTracking()
                .Include(invoice => invoice.Payments)
                .FirstOrDefaultAsync(invoice => invoice.InvoiceNumber == dto.OriginalInvoiceId);

            if (original == null)
                return "The original invoice was not found.";

            var originalPayments = original.Payments?
                .Where(payment => payment.Amount > 0m)
                .GroupBy(payment => payment.PaymentType)
                .ToDictionary(group => group.Key, group => group.Sum(payment => payment.Amount))
                ?? new Dictionary<PaymentType, decimal>();

            if (originalPayments.Count == 0 && original.PaymentType.HasValue)
                originalPayments[original.PaymentType.Value] = Math.Abs(original.TotalAmount);

            var previousReturns = await invoiceRepo.GetAllAsQueryable()
                .AsNoTracking()
                .Include(invoice => invoice.Payments)
                .Where(invoice => invoice.OriginalInvoiceId == original.InvoiceNumber &&
                                  (invoice.InvoiceType == InvoiceType.Return ||
                                   invoice.InvoiceType == InvoiceType.PurchaseReturn))
                .ToListAsync();

            foreach (var previousReturn in previousReturns)
            {
                var payments = previousReturn.Payments?
                    .Where(payment => payment.Amount > 0m)
                    .ToList() ?? new List<InvoicePayment>();

                if (payments.Count == 0 && previousReturn.PaymentType.HasValue)
                {
                    payments.Add(new InvoicePayment
                    {
                        PaymentType = previousReturn.PaymentType.Value,
                        Amount = Math.Abs(previousReturn.TotalAmount)
                    });
                }

                foreach (var payment in payments)
                {
                    if (originalPayments.ContainsKey(payment.PaymentType))
                        originalPayments[payment.PaymentType] -= payment.Amount;
                }
            }

            var requestedPayments = dto.Payments?.Where(payment => payment.Amount > 0m).ToList()
                ?? new List<InvoicePaymentWriteDto>();
            if (requestedPayments.Count == 0 && dto.PaymentType.HasValue)
            {
                requestedPayments.Add(new InvoicePaymentWriteDto
                {
                    PaymentType = dto.PaymentType.Value,
                    Amount = Math.Abs(dto.TotalAmount)
                });
            }

            if (dto.InvoiceType is InvoiceType.Return or InvoiceType.PurchaseReturn)
            {
                if (requestedPayments.Any(payment => payment.PaymentType == PaymentType.Credit)
                    && !(dto.InvoiceType == InvoiceType.PurchaseReturn
                        && dto.InvoiceLines?.All(x => x.SourcePurchaseLineId.HasValue) == true
                        && original.PaymentType == PaymentType.Credit))
                    return "Credit payment is not allowed for returns.";

                var originalTotal = Math.Abs(original.TotalAmount);
                var previousReturnTotal = previousReturns.Sum(previousReturn => Math.Abs(previousReturn.TotalAmount));
                var remainingRefundableTotal = Math.Max(0m, originalTotal - previousReturnTotal);
                var requestedTotal = requestedPayments.Sum(payment => payment.Amount);
                if (requestedTotal > remainingRefundableTotal + 0.0005m)
                    return "The return amount exceeds the remaining refundable amount for the original invoice.";

                return null;
            }

            foreach (var payment in requestedPayments)
            {
                if (!originalPayments.TryGetValue(payment.PaymentType, out var remainingAmount))
                    return $"Payment method {payment.PaymentType} was not used on the original invoice.";

                if (payment.Amount > remainingAmount + 0.0005m)
                    return $"The return amount for {payment.PaymentType} exceeds the remaining refundable amount.";
            }

            return null;
        }

        private static string? ValidateChecks(InvoiceWriteDto dto)
        {
            var checkPayment = dto.Payments?.FirstOrDefault(payment =>
                payment.PaymentType == PaymentType.Check && payment.Amount > 0m);
            if (checkPayment == null && dto.PaymentType != PaymentType.Check)
                return null;

            var checks = dto.Checks?.ToList() ?? new List<Domain.Checks.DTOs.CheckWriteDto>();
            if (checks.Count == 0)
                return "At least one check is required for check payment.";
            if (checks.Any(x => string.IsNullOrWhiteSpace(x.CheckNumber) || x.Amount <= 0m || x.DueDate == default))
                return "Check number, positive amount, and due date are required.";
            if (checks.GroupBy(x => x.CheckNumber.Trim(), StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
                return "Check numbers cannot be duplicated within an invoice.";
            var expectedAmount = checkPayment?.Amount ?? Math.Abs(dto.TotalAmount);
            if (Math.Round(checks.Sum(x => x.Amount), 3) != Math.Round(expectedAmount, 3))
                return "The total check amount must equal the invoice total.";

            return null;
        }

        private async Task NormalizeInvoiceLinesAsync(IEnumerable<InvoiceLineWriteDto> lines)
        {
            var lineList = lines?.Where(l => l != null).ToList();
            if (lineList == null || lineList.Count == 0)
                return;

            var unitIds = lineList
                .Where(l => l.ProductUnitId > 0)
                .Select(l => l.ProductUnitId)
                .Distinct()
                .ToList();

            if (unitIds.Count == 0)
            {
                foreach (var line in lineList)
                {
                    line.QuantityPerUnitSnapshot = 1m;
                    line.BaseQuantity = line.Quantity;
                }

                return;
            }

            var unitRepo = _uow.GetRepository<ProductUnit>();
            var factors = await unitRepo.GetAllAsQueryable()
                .Where(pu => unitIds.Contains(pu.Id))
                .Select(pu => new { pu.Id, pu.QuantityPerUnit })
                .ToDictionaryAsync(
                    pu => pu.Id,
                    pu => pu.QuantityPerUnit > 0 ? pu.QuantityPerUnit : 1m);

            foreach (var line in lineList)
            {
                var factor = line.SourcePurchaseLineId.HasValue && line.QuantityPerUnitSnapshot > 0m
                    ? line.QuantityPerUnitSnapshot
                    : factors.TryGetValue(line.ProductUnitId, out var quantityPerUnit) ? quantityPerUnit : 1m;

                line.QuantityPerUnitSnapshot = factor;
                line.BaseQuantity = line.Quantity * factor;
                line.FreeQuantity = Math.Max(0m, line.FreeQuantity);
                line.LineDiscountAmount = Math.Max(0m, line.LineDiscountAmount);
            }
        }

        private static void ApplyInvoiceTotals(InvoiceWriteDto dto, DateTime originalCreatedDate)
        {
            foreach (var l in dto.InvoiceLines!)
            {
                var returnSubtotal = l.LineSubTotal;
                var returnTax = l.TaxAmount;
                var grossLineTotal = GetGrossLineTotal(dto.InvoiceType, l.Quantity, l.UnitPrice);
                l.LineDiscountAmount = Math.Clamp(l.LineDiscountAmount, 0m, grossLineTotal);
                l.FreeQuantity = Math.Max(0m, l.FreeQuantity);
                var lineTotal = grossLineTotal - l.LineDiscountAmount;
                var taxRate = l.TaxExempt ? 0m : l.TaxRate;
                var isPurchaseInvoice = dto.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn;
                // Sales prices are tax-inclusive; purchases depend on the invoice setting.
                var includesTax = !isPurchaseInvoice || dto.CostPriceIncludesTax;
                var divisor = includesTax ? 1m + (taxRate / 100m) : 1m;

                l.LineSubTotal = l.TaxExempt || divisor <= 0m
                    ? lineTotal
                    : Math.Round(lineTotal / divisor, 3);
                l.TaxAmount = includesTax ? Math.Round(lineTotal - l.LineSubTotal, 3) : 0m;

                if (dto.InvoiceType == InvoiceType.PurchaseReturn && l.SourcePurchaseLineId.HasValue)
                {
                    l.LineSubTotal = returnSubtotal;
                    l.TaxAmount = returnTax;
                }
                var costTotal = l.Quantity * (l.PurchaseAcquisitionCost.HasValue && !isPurchaseInvoice
                        ? l.UnitCost : CalculateEffectivePurchaseCost(l.UnitCost, l.TaxRate, l.TaxExempt, includesTax));
                l.ProfitBeforeTax = l.LineSubTotal - costTotal;
                l.Profit = l.ProfitBeforeTax;
            }

            if (dto.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn)
                ApplyPurchaseDiscountAndTax(dto.InvoiceLines!, dto.DiscountAmount, dto.CostPriceIncludesTax);
            if (dto.InvoiceType == InvoiceType.Purchase)
                PurchaseBonusCosting.Allocate(dto.InvoiceLines!, dto.DiscountAmount, dto.CostPriceIncludesTax);

            var grossSales = dto.InvoiceLines.Sum(x =>
                GetGrossLineTotal(dto.InvoiceType, x.Quantity, x.UnitPrice) - x.LineDiscountAmount);
            dto.SubTotal = dto.InvoiceLines.Sum(x => x.LineSubTotal);
            dto.TotalTax = dto.InvoiceLines.Sum(x => x.TaxAmount);
            var calculatedTotal = dto.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn
                ? dto.SubTotal - (dto.DiscountAmount ?? 0m) + dto.TotalTax
                : grossSales - (dto.DiscountAmount ?? 0m);
            dto.TotalAmount = dto.InvoiceType == InvoiceType.Return
                ? -calculatedTotal
                : calculatedTotal;
            dto.TotalCOGS = dto.InvoiceLines.Sum(x => x.Quantity * CalculateEffectivePurchaseCost(
                x.UnitCost,
                x.TaxRate,
                x.TaxExempt,
                (dto.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn) && dto.CostPriceIncludesTax));
            if (dto.InvoiceType == InvoiceType.PurchaseReturn && dto.InvoiceLines.Any(x => x.SourcePurchaseLineId.HasValue))
                dto.TotalCOGS = dto.InvoiceLines.Sum(x => x.PurchaseAcquisitionCost ?? 0m);
            dto.NetSales = dto.SubTotal - (dto.DiscountAmount ?? 0m);
            dto.GrossProfit = dto.NetSales - dto.TotalCOGS;
            dto.CreatedDate = originalCreatedDate == default ? DateTime.Now : originalCreatedDate;
            dto.UpdatedDate = DateTime.Now;
        }

        private void RecalculateInvoice(Invoice invoice)
        {
            if (invoice.InvoiceLines == null)
                invoice.InvoiceLines = new List<InvoiceLine>();

            // 1) Per-line calculations
            foreach (var line in invoice.InvoiceLines)
            {
                var factor = line.QuantityPerUnitSnapshot > 0 ? line.QuantityPerUnitSnapshot : 1m;
                line.QuantityPerUnitSnapshot = factor;
                line.BaseQuantity = line.Quantity * factor;
                var grossLineTotal = GetGrossLineTotal(invoice.InvoiceType, line.Quantity, line.UnitPrice);
                line.LineDiscountAmount = Math.Clamp(line.LineDiscountAmount, 0m, grossLineTotal);
                line.FreeQuantity = Math.Max(0m, line.FreeQuantity);
                var lineTotal = grossLineTotal - line.LineDiscountAmount;
                var rate = line.TaxExempt ? 0m : line.TaxRate;
                var isPurchaseInvoice = invoice.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn;
                // Sales prices are tax-inclusive; purchases depend on the invoice setting.
                var includesTax = !isPurchaseInvoice || invoice.CostPriceIncludesTax;
                var divisor = includesTax ? 1m + (rate / 100m) : 1m;
                line.LineSubTotal = line.TaxExempt || divisor <= 0m
                    ? lineTotal
                    : Math.Round(lineTotal / divisor, 3);
                line.TaxAmount = includesTax ? Math.Round(lineTotal - line.LineSubTotal, 3) : 0m;

                var costTotal = line.Quantity * (line.PurchaseAcquisitionCost.HasValue && !isPurchaseInvoice
                    ? line.UnitCost : CalculateEffectivePurchaseCost(line.UnitCost, line.TaxRate, line.TaxExempt, includesTax));
                line.ProfitBeforeTax = line.LineSubTotal - costTotal;
                line.Profit = line.ProfitBeforeTax;
            }

            if (invoice.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn)
                ApplyPurchaseDiscountAndTax(invoice.InvoiceLines, invoice.DiscountAmount, invoice.CostPriceIncludesTax);

            // 2) Invoice totals
            var grossSales = invoice.InvoiceLines.Sum(l =>
                GetGrossLineTotal(invoice.InvoiceType, l.Quantity, l.UnitPrice) - l.LineDiscountAmount);
            invoice.SubTotal = invoice.InvoiceLines.Sum(l => l.LineSubTotal);
            invoice.TotalTax = invoice.InvoiceLines.Sum(l => l.TaxAmount);

            var discount = invoice.DiscountAmount ?? 0m;

            invoice.TotalCOGS = invoice.InvoiceLines.Sum(l => l.Quantity * CalculateEffectivePurchaseCost(l.UnitCost, l.TaxRate, l.TaxExempt, (invoice.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn) && invoice.CostPriceIncludesTax));

            invoice.NetSales = invoice.SubTotal - discount;             // قبل الضريبة
            invoice.GrossProfit = invoice.NetSales - invoice.TotalCOGS; // الربح

            invoice.TotalAmount = invoice.InvoiceType == InvoiceType.Return
                ? -(grossSales - discount)
                : invoice.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn
                ? invoice.SubTotal - discount + invoice.TotalTax
                : grossSales - discount;  // النهائي شامل الضريبة
        }
        private static void ApplyPurchaseDiscountAndTax(ICollection<InvoiceLineWriteDto> lines, decimal? discountAmount, bool costPriceIncludesTax)
        {
            var subtotal = Math.Round(lines.Sum(line => line.LineSubTotal), 3);
            var discount = Math.Clamp(discountAmount ?? 0m, 0m, subtotal);
            var taxableRatio = subtotal > 0m ? (subtotal - discount) / subtotal : 0m;

            foreach (var line in lines)
            {
                var taxBeforeInvoiceDiscount = costPriceIncludesTax
                    ? line.TaxAmount
                    : line.LineSubTotal * line.TaxRate / 100m;
                line.TaxAmount = line.TaxExempt || line.TaxRate <= 0m
                    ? 0m
                    : Math.Round(taxBeforeInvoiceDiscount * taxableRatio, 3);
            }
        }

        private static void ApplyPurchaseDiscountAndTax(ICollection<InvoiceLine> lines, decimal? discountAmount, bool costPriceIncludesTax)
        {
            var subtotal = Math.Round(lines.Sum(line => line.LineSubTotal), 3);
            var discount = Math.Clamp(discountAmount ?? 0m, 0m, subtotal);
            var taxableRatio = subtotal > 0m ? (subtotal - discount) / subtotal : 0m;

            foreach (var line in lines)
            {
                var taxBeforeInvoiceDiscount = costPriceIncludesTax
                    ? line.TaxAmount
                    : line.LineSubTotal * line.TaxRate / 100m;
                line.TaxAmount = line.TaxExempt || line.TaxRate <= 0m
                    ? 0m
                    : Math.Round(taxBeforeInvoiceDiscount * taxableRatio, 3);
            }
        }

        private static decimal GetGrossLineTotal(InvoiceType invoiceType, decimal quantity, decimal unitPrice)
        {
            var gross = quantity * unitPrice;
            return invoiceType is InvoiceType.Return or InvoiceType.PurchaseReturn
                ? Math.Abs(gross)
                : Math.Max(0m, gross);
        }

        private static decimal CalculateEffectivePurchaseCost(decimal unitCost, decimal taxRate, bool taxExempt, bool costPriceIncludesTax)
        {
            if (!costPriceIncludesTax || taxExempt || taxRate <= 0m)
                return unitCost;

            return Math.Round(unitCost / (1m + taxRate / 100m), 3);
        }

        public async Task<InvoiceReadDto?> GetFullInvoiceByIdAsync(int id)
        {
            var query = _uow.Invoices.GetAllAsQueryable()
                .Where(i => i.Id == id)
                .Include(i => i.InvoiceLines)
                    .ThenInclude(l => l.Product)
                .Include(i => i.InvoiceLines)
                    .ThenInclude(l => l.ProductUnit)
                        .ThenInclude(u => u.Unit)
                .Include(i => i.Checks)
                .Include(i => i.Payments)
                .Include(i => i.User)          // customer
                .Include(i => i.Delegate)
                .Include(i => i.Voucher)       // voucher (optional)
                .AsNoTracking();

            var entity = await query.FirstOrDefaultAsync();
            var mapped = _mapper.Map<InvoiceReadDto>(entity);
            if (mapped != null)
                await AttachPartiesAsync(new[] { mapped });
            return mapped;
        }

        private async Task AttachPartiesAsync(IEnumerable<InvoiceReadDto> invoices)
        {
            var invoiceList = invoices.ToList();
            var customerIds = invoiceList
                .Where(invoice => invoice.CustomerId.HasValue)
                .Select(invoice => invoice.CustomerId!.Value)
                .Distinct()
                .ToList();
            var supplierIds = invoiceList
                .Where(invoice => invoice.SupplierId.HasValue)
                .Select(invoice => invoice.SupplierId!.Value)
                .Distinct()
                .ToList();

            var partyIds = customerIds.Concat(supplierIds).Distinct().ToList();
            if (partyIds.Count == 0)
                return;

            var parties = await _context.Set<User>()
                .AsNoTracking()
                .Where(user => partyIds.Contains(user.Id))
                .ToDictionaryAsync(user => user.Id);

            foreach (var invoice in invoiceList)
            {
                if (invoice.CustomerId is int customerId && parties.TryGetValue(customerId, out var customer))
                    invoice.Customer = _mapper.Map<UserReadDto>(customer);
                if (invoice.SupplierId is int supplierId && parties.TryGetValue(supplierId, out var supplier))
                    invoice.Supplier = _mapper.Map<UserReadDto>(supplier);
            }
        }

        public async Task<Result<List<InvoiceReadDto>>> SearchSalesInvoicesAsync(
                    string? invoiceNumber,string? customerName,
                    DateTime? dateFrom,DateTime? dateTo,bool? isSal=null, bool? isPOS = null,
                          InvoiceStatus? status = null, string? productBarcode = null)
        {
            try
            {


                var query = _uow.Invoices.GetAllAsQueryable()
                       .Include(i => i.InvoiceLines)
                       .Include(i => i.Payments)
                       .Include(i => i.User)
                       .Include(i => i.Delegate)
                       .AsNoTracking();
                if (isSal==true)
                {
                   
                       query= query.Where(i => i.InvoiceType == InvoiceType.Sale);

                }
                else if (isSal != null)
                {
                    query =query.Where(i => i.InvoiceType == InvoiceType.Purchase);
                }
                // ✅ POS filter (optional)
                if (isPOS.HasValue)
                {
                    query = query.Where(i => i.IsPOS == isPOS.Value);
                }

                // ✅ Status filter (optional)
                if (status.HasValue)
                {
                    query = query.Where(i => i.Status == status.Value);
                }



                if (!string.IsNullOrWhiteSpace(invoiceNumber))
                {
                    var identifier = invoiceNumber.Trim();
                    query = query.Where(i =>
                        i.InvoiceNumber == identifier ||
                        i.FalconInvoiceNumber == identifier);
                }

                if (!string.IsNullOrWhiteSpace(customerName))
                {
                    var accountSearch = customerName.Trim();
                    query = query.Where(i =>
                        (i.CustomerId.HasValue && _context.Set<User>().Any(u => u.Id == i.CustomerId.Value && u.Name.Contains(accountSearch))) ||
                        (i.User != null && i.User.Name.Contains(accountSearch)) ||
                        (i.SupplierId.HasValue && _context.Set<User>().Any(u => u.Id == i.SupplierId.Value && u.Name.Contains(accountSearch))) ||
                        (i.CreatedBy.HasValue && _context.Set<User>().Any(u => u.Id == i.CreatedBy.Value && u.Name.Contains(accountSearch))));
                }

                if (!string.IsNullOrWhiteSpace(productBarcode))
                {
                    var barcode = BarcodeLookup.Normalize(productBarcode);
                    var hasNumericItemCode = long.TryParse(barcode, out var numericItemCode);
                    query = query.Where(i => i.InvoiceLines.Any(line =>
                        line.Product != null &&
                        ((hasNumericItemCode && line.Product.ITEMCODE == numericItemCode) ||
                         line.Product.ProductUnits!.Any(unit => unit.AlternateBarcode == barcode))));
                }

                if (dateFrom.HasValue)
                {
                    query = query.Where(i => i.CreatedDate >= dateFrom.Value);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(i => i.CreatedDate <= dateTo.Value);
                }

                var data = await query.ToListAsync();
                var mapped = _mapper.Map<List<InvoiceReadDto>>(data);
                await AttachPartiesAsync(mapped);

                return Result<List<InvoiceReadDto>>.Ok(mapped);
            }
            catch (Exception ex)
            {
                return Result<List<InvoiceReadDto>>.Fail("خطأ أثناء البحث عن الفواتير: " + ex.Message);
            }
        }

        public async Task<Result<List<ReturnInvoiceLookupDto>>> SearchReturnInvoiceHeadersAsync(
            DateTime? dateFrom,
            DateTime? dateTo,
            string? invoiceNumber = null,
            string? customerName = null,
            string? productName = null,
            string? barcode = null)
        {
            try
            {
                var query = _uow.Invoices
                    .GetAllAsQueryable()
                    .AsNoTracking()
                    .Where(invoice => (invoice.InvoiceType == InvoiceType.Sale && invoice.TotalAmount > 0m)
                        || (invoice.InvoiceType == InvoiceType.Purchase
                            && invoice.InvoiceLines.Any(line => line.PurchaseAcquisitionCost.HasValue)));

                if (dateFrom.HasValue)
                    query = query.Where(invoice => invoice.CreatedDate >= dateFrom.Value.Date);

                if (dateTo.HasValue)
                {
                    var exclusiveDateTo = dateTo.Value.Date.AddDays(1);
                    query = query.Where(invoice => invoice.CreatedDate < exclusiveDateTo);
                }

                if (!string.IsNullOrWhiteSpace(invoiceNumber))
                {
                    var number = invoiceNumber.Trim();
                    query = query.Where(invoice => invoice.InvoiceNumber == number);
                }

                if (!string.IsNullOrWhiteSpace(productName))
                {
                    var product = productName.Trim();
                    query = query.Where(invoice => _context.Set<InvoiceLine>().Any(line =>
                        line.InvoiceId == invoice.Id &&
                        line.ProductId > 0 &&
                        line.Product != null &&
                        line.Product.Name != null &&
                        line.Product.Name.Contains(product)));
                }

                if (!string.IsNullOrWhiteSpace(barcode))
                {
                    var barcodeText = BarcodeLookup.Normalize(barcode);
                    var hasItemCode = long.TryParse(barcodeText, out var itemCode);
                    query = query.Where(invoice => _context.Set<InvoiceLine>().Any(line =>
                        line.InvoiceId == invoice.Id &&
                        line.ProductId > 0 &&
                        ((hasItemCode && line.Product != null && line.Product.ITEMCODE == itemCode) ||
                         (line.ProductUnit != null &&
                          line.ProductUnit.AlternateBarcode != null &&
                          line.ProductUnit.AlternateBarcode.Contains(barcodeText)))));
                }

                if (!string.IsNullOrWhiteSpace(customerName))
                {
                    var customer = customerName.Trim();
                    query = query.Where(invoice => invoice.CustomerId.HasValue
                        && _context.Set<User>().Any(user => user.Id == invoice.CustomerId.Value && user.Name.Contains(customer)));
                }

                var headers = await query
                    .OrderByDescending(invoice => invoice.CreatedDate)
                    .Select(invoice => new ReturnInvoiceLookupDto
                    {
                        Id = invoice.Id,
                        InvoiceNumber = invoice.InvoiceNumber,
                        CreatedDate = invoice.CreatedDate,
                        CustomerName = invoice.CustomerId.HasValue
                            ? _context.Set<User>()
                                .Where(user => user.Id == invoice.CustomerId.Value)
                                .Select(user => user.Name)
                                .FirstOrDefault()
                            : null,
                        TotalAmount = invoice.TotalAmount,
                        PaymentTypes = invoice.Payments!
                            .Where(payment => payment.Amount > 0m)
                            .Select(payment => payment.PaymentType)
                            .Distinct()
                            .ToList()
                    })
                    .Take(500)
                    .ToListAsync();

                var invoiceIds = headers.Select(header => header.Id).ToList();
                var invoiceNumbers = headers.Select(header => header.InvoiceNumber).ToList();
                var originalLines = await _context.Set<InvoiceLine>()
                    .AsNoTracking()
                    .Where(line => invoiceIds.Contains(line.InvoiceId)
                        && line.ProductId > 0
                        && line.ProductUnitId > 0
                        && (line.Quantity > 0m || (line.PurchaseAcquisitionCost.HasValue && line.Invoice!.InvoiceType == InvoiceType.Purchase && line.FreeQuantity > 0m)))
                    .Select(line => new
                    {
                        line.InvoiceId,
                        line.ProductId,
                        line.ProductUnitId,
                        Quantity = line.Quantity + (line.PurchaseAcquisitionCost.HasValue && line.Invoice!.InvoiceType == InvoiceType.Purchase ? line.FreeQuantity : 0m)
                    })
                    .ToListAsync();

                var previousReturnLines = await _context.Set<InvoiceLine>()
                    .AsNoTracking()
                    .Where(line => line.Invoice != null
                        && (line.Invoice.InvoiceType == InvoiceType.Return
                            || line.Invoice.InvoiceType == InvoiceType.PurchaseReturn)
                        && line.Invoice.OriginalInvoiceId != null
                        && invoiceNumbers.Contains(line.Invoice.OriginalInvoiceId)
                        && line.ProductId > 0
                        && line.ProductUnitId > 0
                        && (line.Quantity < 0m || (line.SourcePurchaseLineId.HasValue && line.FreeQuantity > 0m))
                        && (!line.SourcePurchaseLineId.HasValue || (line.Invoice.Status != InvoiceStatus.Cancelled
                            && line.Invoice.Status != InvoiceStatus.Draft && line.Invoice.Status != InvoiceStatus.OnHold)))
                    .Select(line => new
                    {
                        OriginalInvoiceId = line.Invoice!.OriginalInvoiceId!,
                        line.ProductId,
                        line.ProductUnitId,
                        Quantity = Math.Abs(line.Quantity) + (line.SourcePurchaseLineId.HasValue ? line.FreeQuantity : 0m)
                    })
                    .ToListAsync();

                var originalByInvoice = originalLines
                    .GroupBy(line => line.InvoiceId)
                    .ToDictionary(
                        group => group.Key,
                        group => group.GroupBy(line => (line.ProductId, line.ProductUnitId))
                            .ToDictionary(lines => lines.Key, lines => lines.Sum(line => line.Quantity)));
                var returnedByInvoice = previousReturnLines
                    .GroupBy(line => line.OriginalInvoiceId)
                    .ToDictionary(
                        group => group.Key,
                        group => group.GroupBy(line => (line.ProductId, line.ProductUnitId))
                            .ToDictionary(lines => lines.Key, lines => lines.Sum(line => line.Quantity)));

                foreach (var header in headers)
                {
                    var original = originalByInvoice.TryGetValue(header.Id, out var originalQuantities)
                        ? originalQuantities
                        : new Dictionary<(int ProductId, int ProductUnitId), decimal>();
                    returnedByInvoice.TryGetValue(header.InvoiceNumber, out var returnedQuantities);

                    var hasPreviousReturn = false;
                    var hasReturnableQuantity = false;
                    foreach (var line in original)
                    {
                        var returnedQuantity = returnedQuantities?.GetValueOrDefault(line.Key) ?? 0m;
                        hasPreviousReturn |= returnedQuantity > 0.0005m;
                        hasReturnableQuantity |= line.Value - returnedQuantity > 0.0005m;
                    }

                    header.IsFullyReturned = original.Count == 0 || !hasReturnableQuantity;
                    header.ReturnStatus = header.IsFullyReturned
                        ? "Fully returned / مرتجع بالكامل"
                        : hasPreviousReturn
                            ? "Partially returned / مرتجع جزئياً"
                            : "Available / متاح للإرجاع";
                }

                foreach (var header in headers)
                {
                    if (header.PaymentTypes.Count == 0)
                    {
                        var fallback = await _uow.Invoices.GetAllAsQueryable()
                            .Where(invoice => invoice.Id == header.Id)
                            .Select(invoice => invoice.PaymentType)
                            .FirstOrDefaultAsync();

                        if (fallback.HasValue)
                            header.PaymentTypes.Add(fallback.Value);
                    }
                }

                return Result<List<ReturnInvoiceLookupDto>>.Ok(headers);
            }
            catch (Exception ex)
            {
                return Result<List<ReturnInvoiceLookupDto>>.Fail(
                    $"Failed to load return invoice headers: {ex.Message}");
            }
        }



        public async Task<Result<List<InvoiceReadDto>>> GetHeldPOSInvoicesAsync()
        {
            try
            {
                var data = await _uow.Invoices
                    .GetAllAsQueryable()
                    .Include(i => i.InvoiceLines)
                        .ThenInclude(l => l.Product)
                    .Include(i => i.InvoiceLines)
                        .ThenInclude(l => l.ProductUnit)
                            .ThenInclude(pu => pu.Unit)
                    .Include(i => i.User)
                    .Where(i =>
                        i.IsPOS == true &&
                        i.Status == InvoiceStatus.OnHold)
                    .OrderBy(i => i.OpenedAt)
                    .AsNoTracking()
                    .ToListAsync();

                var mapped = _mapper.Map<List<InvoiceReadDto>>(data);
                return Result<List<InvoiceReadDto>>.Ok(mapped);
            }
            catch (Exception ex)
            {
                return Result<List<InvoiceReadDto>>
                    .Fail("خطأ أثناء تحميل الفواتير المعلقة");
            }
        }




        public async Task<Result<(FinancialSummaryDto summary, List<SalesReportRowDto> rows)>>
     GetSalesReportAsync(FinancialSummaryFilterDto filter, InvoiceType? type = null, bool? isPOS = null, bool includePurchases = false)
        {
            if (filter.From > filter.To)
                return Result<(FinancialSummaryDto, List<SalesReportRowDto>)>.Fail("Invalid date range.");

            var invoiceRepo = _uow.GetRepository<Invoice>();
            var lineRepo = _uow.GetRepository<InvoiceLine>();

            var invoicesQ = invoiceRepo.GetAllAsQueryable()
                .AsNoTracking()
                .Where(x => x.CreatedDate >= filter.From && x.CreatedDate <= filter.To);

            if (filter.CustomerId.HasValue)
                invoicesQ = invoicesQ.Where(x => x.CustomerId == filter.CustomerId.Value);

            if (filter.SupplierId.HasValue)
                invoicesQ = invoicesQ.Where(x => x.SupplierId == filter.SupplierId.Value);

            if (filter.CashierId.HasValue)
                invoicesQ = invoicesQ.Where(x => x.CasherId == filter.CashierId.Value);

            if (filter.PaymentType.HasValue)
                invoicesQ = invoicesQ.Where(x =>
                    x.PaymentType == filter.PaymentType.Value ||
                    x.Payments!.Any(payment => payment.PaymentType == filter.PaymentType.Value && payment.Amount > 0m));

            if (type.HasValue)
                invoicesQ = invoicesQ.Where(x => x.InvoiceType == type.Value);
            else if (includePurchases)
                invoicesQ = invoicesQ.Where(x =>
                    x.InvoiceType == InvoiceType.Purchase ||
                    x.InvoiceType == InvoiceType.PurchaseReturn);
            else if (filter.IncludeReturns)
                invoicesQ = invoicesQ.Where(x =>
                    x.InvoiceType == InvoiceType.Sale ||
                    x.InvoiceType == InvoiceType.Return ||
                    x.InvoiceType == InvoiceType.EndpointOrder);
            else
                invoicesQ = invoicesQ.Where(x =>
                    x.InvoiceType == InvoiceType.Sale ||
                    x.InvoiceType == InvoiceType.EndpointOrder);

            if (isPOS.HasValue)
                invoicesQ = invoicesQ.Where(x => x.IsPOS == isPOS.Value);

            // Sales reports must contain only finalized invoices.
            invoicesQ = invoicesQ.Where(x =>
                x.Status == InvoiceStatus.Completed ||
                x.Status == InvoiceStatus.Posted);

            // ✅ rows
            var invoiceIds = await invoicesQ.Select(x => x.Id).ToListAsync();

            var lines = await lineRepo.GetAllAsQueryable()
                .AsNoTracking()
                .Where(l => invoiceIds.Contains(l.InvoiceId))
                .ToListAsync();

                var linesByInvoice = lines.GroupBy(l => l.InvoiceId)
                .ToDictionary(g => g.Key, g => new
                {
                    Cogs = g.Sum(x => x.Quantity * x.UnitCost),
                    SubTotal = g.Sum(x => x.LineSubTotal),
                    LineDiscount = g.Sum(x => x.LineDiscountAmount),
                    Tax = g.Sum(x => x.TaxAmount > 0m
                        ? x.TaxAmount
                        : !x.TaxExempt && x.TaxRate > 0m
                            ? Math.Round(
                                Math.Max(0m, x.Quantity * x.UnitPrice - x.LineDiscountAmount)
                                * x.TaxRate / (100m + x.TaxRate),
                                3)
                            : 0m),
                    SaleSettlementTotal = g.Where(x => x.Quantity > 0m)
                        .Sum(x => x.Quantity * x.UnitPrice),
                    ReturnSettlementTotal = Math.Abs(g.Where(x => x.Quantity < 0m)
                        .Sum(x => x.Quantity * x.UnitPrice)),
                    ReturnAmount = g.Sum(x => x.Quantity < 0m
                        ? Math.Abs(x.Quantity * x.UnitPrice)
                        : 0m)
                });

            var invoices = await invoicesQ
                .Include(x => x.User) // customer
                .Include(x => x.Payments)
                .ToListAsync();

            var cashierIds = invoices
                .Where(x => x.CasherId.HasValue)
                .Select(x => x.CasherId!.Value)
                .Distinct()
                .ToList();

            var cashierNames = cashierIds.Count == 0
                ? new Dictionary<int, string>()
                : await _uow.GetRepository<User>().GetAllAsQueryable()
                    .Where(x => cashierIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.Name);

            var supplierIds = invoices
                .Where(x => x.SupplierId.HasValue)
                .Select(x => x.SupplierId!.Value)
                .Distinct()
                .ToList();
            var supplierNames = supplierIds.Count == 0
                ? new Dictionary<int, string>()
                : await _uow.GetRepository<User>().GetAllAsQueryable()
                    .Where(x => supplierIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.Name);

            var rows = invoices.Select(inv =>
            {
                var invoiceDiscount = inv.DiscountAmount ?? 0m;

                linesByInvoice.TryGetValue(inv.Id, out var agg);
                var subTotal = agg?.SubTotal ?? inv.SubTotal;     // prefer lines, fallback invoice
                var tax = agg?.Tax ?? inv.TotalTax;
                var cogs = agg?.Cogs ?? inv.TotalCOGS;
                var discount = (agg?.LineDiscount ?? 0m) + invoiceDiscount;
                var returnAmount = inv.InvoiceType == InvoiceType.Return
                    ? agg?.ReturnAmount ?? Math.Abs(Math.Min(subTotal, 0m))
                    : 0m;

                // LineSubTotal already includes line-level discounts. Only the
                // invoice-level discount is subtracted again from the net total.
                var total = subTotal - invoiceDiscount + tax;
                var profit = (subTotal - invoiceDiscount) - cogs;
                var invoiceSign = inv.TotalAmount < 0m ? -1m : 1m;
                var paymentAmounts = (inv.Payments ?? new List<InvoicePayment>())
                    .Where(payment => payment.Amount > 0m)
                    .GroupBy(payment => payment.PaymentType)
                    .ToDictionary(group => group.Key, group => invoiceSign * group.Sum(payment => payment.Amount));
                if (paymentAmounts.Count == 0 && inv.PaymentType.HasValue)
                    paymentAmounts[inv.PaymentType.Value] = invoiceSign * Math.Abs(inv.TotalAmount);

                return new SalesReportRowDto
                {
                    InvoiceId = inv.Id,
                    InvoiceNumber = inv.InvoiceNumber,
                    Date = inv.CreatedDate,
                    CustomerName = inv.User?.Name
                        ?? (inv.SupplierId.HasValue && supplierNames.TryGetValue(inv.SupplierId.Value, out var supplierName)
                            ? supplierName
                            : "—"),
                    CashierName = inv.CasherId.HasValue && cashierNames.TryGetValue(inv.CasherId.Value, out var cashierName)
                        ? cashierName
                        : "—",

                    SubTotal = subTotal,
                    ReturnAmount = returnAmount,
                    TotalTax = tax,
                    Discount = discount,
                    Total = total,

                    Cogs = cogs,
                    Profit = profit,

                    InvoiceType = inv.InvoiceType.ToString(),
                    PaymentMethod = paymentAmounts.Count > 1 ? "Mixed" : paymentAmounts.Keys.FirstOrDefault().ToString(),
                    PaymentAmounts = paymentAmounts,
                    Status = inv.Status?.ToString() ?? "—"
                };
            }).OrderByDescending(r => r.Date).ToList();

            // ✅ summary
            static bool IsCountedSale(SalesReportRowDto row)
            {
                if (row.InvoiceType == InvoiceType.Sale.ToString())
                    return true;

                return row.InvoiceType == InvoiceType.EndpointOrder.ToString() &&
                       (row.Status == InvoiceStatus.Completed.ToString() ||
                        row.Status == InvoiceStatus.Posted.ToString());
            }

            var countedSales = rows.Where(IsCountedSale).ToList();
            var totalSales = countedSales.Sum(r => r.SubTotal);
            var totalTax = countedSales.Sum(r => r.TotalTax);
            var totalDiscounts = countedSales.Sum(r => r.Discount);

            var totalReturns = filter.IncludeReturns
                ? rows.Where(r => r.InvoiceType == InvoiceType.Return.ToString()).Sum(r => r.ReturnAmount)
                : 0m;

            var netSales = countedSales.Sum(row => row.Total - row.TotalTax) - totalReturns; // قبل الضريبة
            var totalCogs = countedSales.Sum(r => r.Cogs);
            var grossProfit = netSales - totalCogs;
            var margin = netSales == 0 ? 0 : Math.Round((grossProfit / netSales) * 100m, 2);

            var countInvoices = countedSales.Count;
            var avg = countInvoices == 0 ? 0 : Math.Round(netSales / countInvoices, 2);

            var summary = new FinancialSummaryDto
            {
                TotalSales = totalSales,
                TotalTax = totalTax,
                TotalDiscounts = totalDiscounts,
                TotalReturns = totalReturns,
                NetSales = netSales,

                TotalCOGS = totalCogs,
                GrossProfit = grossProfit,
                GrossProfitMargin = margin,

                NumberOfInvoices = countInvoices,
                AverageInvoiceValue = avg
            };

            return Result<(FinancialSummaryDto, List<SalesReportRowDto>)>.Ok((summary, rows));
        }

        public async Task<InvoiceReadDto?> FindPOSInvoiceByFalconNumberAsync(
            string falconInvoiceNumber,
            int? excludeInvoiceId = null)
        {
            var identifier = falconInvoiceNumber?.Trim();
            if (string.IsNullOrWhiteSpace(identifier))
                return null;

            var query = _uow.Invoices.GetAllAsQueryable()
                .AsNoTracking()
                .Where(invoice =>
                    invoice.IsPOS == true &&
                    invoice.FalconInvoiceNumber == identifier);

            if (excludeInvoiceId.HasValue)
                query = query.Where(invoice => invoice.Id != excludeInvoiceId.Value);

            var invoice = await query.FirstOrDefaultAsync();
            return invoice == null ? null : _mapper.Map<InvoiceReadDto>(invoice);
        }

        public async Task<Result<(DateTime? from, DateTime? to)>> GetSalesReportDateRangeAsync(bool includePurchases = false)
        {
            try
            {
                var invoices = _uow.GetRepository<Invoice>()
                    .GetAllAsQueryable();

                invoices = includePurchases
                    ? invoices.Where(x => x.InvoiceType == InvoiceType.Purchase || x.InvoiceType == InvoiceType.PurchaseReturn)
                    : invoices.Where(x => x.InvoiceType == InvoiceType.Sale || x.InvoiceType == InvoiceType.Return || x.InvoiceType == InvoiceType.EndpointOrder);

                var from = await invoices
                    .OrderBy(x => x.CreatedDate)
                    .Select(x => (DateTime?)x.CreatedDate)
                    .FirstOrDefaultAsync();

                var to = await invoices
                    .OrderByDescending(x => x.CreatedDate)
                    .Select(x => (DateTime?)x.CreatedDate)
                    .FirstOrDefaultAsync();

                return Result<(DateTime?, DateTime?)>.Ok((from, to));
            }
            catch (Exception ex)
            {
                return Result<(DateTime?, DateTime?)>.Fail(
                    $"Failed to determine the sales report date range: {ex.Message}");
            }
        }
    }
    public interface IInvoiceService : IGenericService<Invoice, InvoiceWriteDto, InvoiceReadDto>
    {

        Task<Result<List<InvoiceReadDto>>> SearchSalesInvoicesAsync(
          string? invoiceNumber,
          string? customerName,
          DateTime? dateFrom,
          DateTime? dateTo,
          bool? isSal,
          bool? isPOS = null,
          InvoiceStatus? status = null,
          string? productBarcode = null);

        Task<Result<List<ReturnInvoiceLookupDto>>> SearchReturnInvoiceHeadersAsync(
            DateTime? dateFrom,
            DateTime? dateTo,
            string? invoiceNumber = null,
            string? customerName = null,
            string? productName = null,
            string? barcode = null);

        Task<InvoiceReadDto?> FindPOSInvoiceByFalconNumberAsync(
            string falconInvoiceNumber,
            int? excludeInvoiceId = null);


        Task<InvoiceReadDto?> GetFullInvoiceByIdAsync(int id);

        Task<Result<bool>> SaveSupplierJoFotaraReferenceAsync(int invoiceId, string? number, string? uuid, string? qr);

        Task<Result<List<InvoiceReadDto>>> GetHeldPOSInvoicesAsync();
        Task<Result<(FinancialSummaryDto summary, List<SalesReportRowDto> rows)>>
                GetSalesReportAsync(FinancialSummaryFilterDto filter, InvoiceType? type = null, bool? isPOS = null, bool includePurchases = false);
        Task<Result<(DateTime? from, DateTime? to)>> GetSalesReportDateRangeAsync(bool includePurchases = false);
    }
}
