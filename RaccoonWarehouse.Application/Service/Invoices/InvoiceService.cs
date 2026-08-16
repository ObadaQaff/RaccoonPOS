using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Generic;
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
using RaccoonWarehouse.Domain.Reports.Financial.Dtos;
using RaccoonWarehouse.Domain.Reports.Financial.Filters;
using RaccoonWarehouse.Domain.Reports.Sales.Dtos;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
using System;
using System.Collections.Generic;
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
            try
            {
                var invoiceRepo = _uow.GetRepository<Invoice>();
                var lineRepo = _uow.GetRepository<InvoiceLine>();
                var checkRepo = _uow.GetRepository<Check>();

                if (dto.InvoiceLines == null || !dto.InvoiceLines.Any())
                    return Result<InvoiceWriteDto>.Fail("Invoice must contain at least one line.");
                if (dto.InvoiceType == InvoiceType.Purchase && dto.InvoiceLines.Any(line => line.ExpiryDate == default))
                    return Result<InvoiceWriteDto>.Fail("Expiry date is required for every purchase invoice line.");
                var partyValidation = ValidateCreditParty(dto);
                if (partyValidation != null)
                    return Result<InvoiceWriteDto>.Fail(partyValidation);

                await NormalizeInvoiceLinesAsync(dto.InvoiceLines);

                // 1) احسب قيم السطور (Tax/Cost/Profit) قبل الحفظ
                foreach (var l in dto.InvoiceLines)
                {
                    var lineTotal = l.Quantity * l.UnitPrice;
                    var taxRate = l.TaxExempt ? 0m : l.TaxRate;
                    var divisor = 1m + (taxRate / 100m);

                    l.LineSubTotal = l.TaxExempt || divisor <= 0m
                        ? lineTotal
                        : Math.Round(lineTotal / divisor, 3);
                    l.TaxAmount = Math.Round(lineTotal - l.LineSubTotal, 3);

                    var costTotal = l.Quantity * l.UnitCost;
                    var profitBeforeTax = l.LineSubTotal - costTotal;

                    l.ProfitBeforeTax = profitBeforeTax;
                    l.Profit = profitBeforeTax;
                }

                // 2) احسب قيم الفاتورة
                var grossSales = dto.InvoiceLines.Sum(x => x.Quantity * x.UnitPrice);
                dto.SubTotal = dto.InvoiceLines.Sum(x => x.LineSubTotal);
                dto.TotalTax = dto.InvoiceLines.Sum(x => x.TaxAmount);
                dto.TotalAmount = grossSales - (dto.DiscountAmount ?? 0m);
                dto.TotalCOGS = dto.InvoiceLines.Sum(x => x.CostTotal);
                dto.NetSales = dto.SubTotal - (dto.DiscountAmount ?? 0m); // قبل الضريبة
                dto.GrossProfit = dto.NetSales - dto.TotalCOGS;
                var checkValidation = ValidateChecks(dto);
                if (checkValidation != null)
                    return Result<InvoiceWriteDto>.Fail(checkValidation);
                if (dto.CreatedDate == default)
                    dto.CreatedDate = DateTime.Now;
                dto.UpdatedDate = DateTime.Now;

                // 3) أنشئ invoice بدون lines (مهم)
                var invoice = _mapper.Map<Invoice>(dto);
                invoice.InvoiceLines = new List<InvoiceLine>();
                invoice.Checks = new List<Check>();

                await invoiceRepo.AddAsync(invoice);
                await _uow.CommitAsync(); // ✅ هسا صار عندك invoice.Id

                // 4) أضف السطور وربط InvoiceId يدوي
                foreach (var l in dto.InvoiceLines)
                {
                    var line = _mapper.Map<InvoiceLine>(l);

                    line.InvoiceId = invoice.Id;  // ✅ أهم سطر
                    line.Invoice = null;          // اختياري لتجنب tracking issues

                    await lineRepo.AddAsync(line);
                }

                foreach (var checkDto in dto.Checks ?? Enumerable.Empty<Domain.Checks.DTOs.CheckWriteDto>())
                {
                    var check = _mapper.Map<Check>(checkDto);
                    check.Id = 0;
                    check.InvoiceId = invoice.Id;
                    check.Invoice = null;
                    await checkRepo.AddAsync(check);
                }

                await _uow.CommitAsync();

                dto.Id = invoice.Id;
                if (_accountingService != null)
                {
                    var journalResult = await _accountingService.PostInvoiceEntryAsync(dto);
                    if (!journalResult.Success)
                    {
                        _context.Set<InvoiceLine>().RemoveRange(_context.Set<InvoiceLine>().Where(x => x.InvoiceId == invoice.Id));
                        _context.Set<Check>().RemoveRange(_context.Set<Check>().Where(x => x.InvoiceId == invoice.Id));
                        _context.Set<Invoice>().Remove(invoice);
                        await _uow.CommitAsync();
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

        public override async Task<Result<InvoiceWriteDto>> UpdateAsync(InvoiceWriteDto dto)
        {
            try
            {
                if (dto.Id <= 0)
                    return Result<InvoiceWriteDto>.Fail("Invoice id is required.");

                if (dto.InvoiceLines == null || !dto.InvoiceLines.Any())
                    return Result<InvoiceWriteDto>.Fail("Invoice must contain at least one line.");
                if (dto.InvoiceType == InvoiceType.Purchase && dto.InvoiceLines.Any(line => line.ExpiryDate == default))
                    return Result<InvoiceWriteDto>.Fail("Expiry date is required for every purchase invoice line.");
                var partyValidation = ValidateCreditParty(dto);
                if (partyValidation != null)
                    return Result<InvoiceWriteDto>.Fail(partyValidation);

                var invoiceRepo = _uow.GetRepository<Invoice>();
                var lineRepo = _uow.GetRepository<InvoiceLine>();
                var existingInvoice = await invoiceRepo.GetAllAsQueryable()
                    .Include(x => x.InvoiceLines)
                    .Include(x => x.Checks)
                    .FirstOrDefaultAsync(x => x.Id == dto.Id);

                if (existingInvoice == null)
                    return Result<InvoiceWriteDto>.Fail("Invoice not found.");

                await NormalizeInvoiceLinesAsync(dto.InvoiceLines);
                ApplyInvoiceTotals(dto, existingInvoice.CreatedDate);
                var checkValidation = ValidateChecks(dto);
                if (checkValidation != null)
                    return Result<InvoiceWriteDto>.Fail(checkValidation);

                var oldStatus = existingInvoice.Status;
                var hadPostedAccounting = oldStatus is not InvoiceStatus.OnHold
                    and not InvoiceStatus.Draft
                    and not InvoiceStatus.Cancelled
                    and not InvoiceStatus.Unknown
                    and not InvoiceStatus.InProcess;

                var existingLines = existingInvoice.InvoiceLines?.ToList() ?? new List<InvoiceLine>();
                var existingChecks = existingInvoice.Checks?.ToList() ?? new List<Check>();

                _mapper.Map(dto, existingInvoice);
                existingInvoice.CreatedDate = dto.CreatedDate;
                existingInvoice.UpdatedDate = dto.UpdatedDate;

                if (existingLines.Count > 0)
                    _context.Set<InvoiceLine>().RemoveRange(existingLines);
                if (existingChecks.Count > 0)
                    _context.Set<Check>().RemoveRange(existingChecks);

                existingInvoice.InvoiceLines = new List<InvoiceLine>();
                existingInvoice.Checks = new List<Check>();

                await _uow.CommitAsync();

                foreach (var lineDto in dto.InvoiceLines)
                {
                    var line = _mapper.Map<InvoiceLine>(lineDto);
                    line.InvoiceId = existingInvoice.Id;
                    line.Invoice = null;
                    line.CreatedDate = lineDto.CreatedDate == default ? dto.CreatedDate : lineDto.CreatedDate;
                    line.UpdatedDate = dto.UpdatedDate;
                    await lineRepo.AddAsync(line);
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

                if (_accountingService != null)
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

        private static string? ValidateCreditParty(InvoiceWriteDto dto)
        {
            if (dto.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn)
                return dto.SupplierId.HasValue ? null : "A supplier is required for purchase invoices.";

            if (dto.PaymentType == PaymentType.Credit && !dto.CustomerId.HasValue)
                return "A customer is required for credit sales invoices.";

            return null;
        }

        private static string? ValidateChecks(InvoiceWriteDto dto)
        {
            if (dto.PaymentType != PaymentType.Check)
                return null;

            var checks = dto.Checks?.ToList() ?? new List<Domain.Checks.DTOs.CheckWriteDto>();
            if (checks.Count == 0)
                return "At least one check is required for check payment.";
            if (checks.Any(x => string.IsNullOrWhiteSpace(x.CheckNumber) || x.Amount <= 0m || x.DueDate == default))
                return "Check number, positive amount, and due date are required.";
            if (checks.GroupBy(x => x.CheckNumber.Trim(), StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
                return "Check numbers cannot be duplicated within an invoice.";
            if (Math.Round(checks.Sum(x => x.Amount), 3) != Math.Round(dto.TotalAmount, 3))
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
                var factor = factors.TryGetValue(line.ProductUnitId, out var quantityPerUnit)
                    ? quantityPerUnit
                    : 1m;

                line.QuantityPerUnitSnapshot = factor;
                line.BaseQuantity = line.Quantity * factor;
            }
        }

        private static void ApplyInvoiceTotals(InvoiceWriteDto dto, DateTime originalCreatedDate)
        {
            foreach (var l in dto.InvoiceLines!)
            {
                var lineTotal = l.Quantity * l.UnitPrice;
                var taxRate = l.TaxExempt ? 0m : l.TaxRate;
                var divisor = 1m + (taxRate / 100m);

                l.LineSubTotal = l.TaxExempt || divisor <= 0m
                    ? lineTotal
                    : Math.Round(lineTotal / divisor, 3);
                l.TaxAmount = Math.Round(lineTotal - l.LineSubTotal, 3);

                var costTotal = l.Quantity * l.UnitCost;
                l.ProfitBeforeTax = l.LineSubTotal - costTotal;
                l.Profit = l.ProfitBeforeTax;
            }

            var grossSales = dto.InvoiceLines.Sum(x => x.Quantity * x.UnitPrice);
            dto.SubTotal = dto.InvoiceLines.Sum(x => x.LineSubTotal);
            dto.TotalTax = dto.InvoiceLines.Sum(x => x.TaxAmount);
            dto.TotalAmount = grossSales - (dto.DiscountAmount ?? 0m);
            dto.TotalCOGS = dto.InvoiceLines.Sum(x => x.CostTotal);
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
                var lineTotal = line.Quantity * line.UnitPrice;
                var rate = line.TaxExempt ? 0m : line.TaxRate;
                var divisor = 1m + (rate / 100m);
                line.LineSubTotal = line.TaxExempt || divisor <= 0m
                    ? lineTotal
                    : Math.Round(lineTotal / divisor, 3);
                line.TaxAmount = Math.Round(lineTotal - line.LineSubTotal, 3);

                var costTotal = line.Quantity * line.UnitCost;
                line.ProfitBeforeTax = line.LineSubTotal - costTotal;
                line.Profit = line.ProfitBeforeTax;
            }

            // 2) Invoice totals
            var grossSales = invoice.InvoiceLines.Sum(l => l.Quantity * l.UnitPrice);
            invoice.SubTotal = invoice.InvoiceLines.Sum(l => l.LineSubTotal);
            invoice.TotalTax = invoice.InvoiceLines.Sum(l => l.TaxAmount);

            var discount = invoice.DiscountAmount ?? 0m;

            invoice.TotalCOGS = invoice.InvoiceLines.Sum(l => l.Quantity * l.UnitCost);

            invoice.NetSales = invoice.SubTotal - discount;             // قبل الضريبة
            invoice.GrossProfit = invoice.NetSales - invoice.TotalCOGS; // الربح

            invoice.TotalAmount = grossSales - discount;  // النهائي شامل الضريبة
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
                .Include(i => i.User)          // customer
                .Include(i => i.Delegate)
                .Include(i => i.Voucher)       // voucher (optional)
                .AsNoTracking();

            var entity = await query.FirstOrDefaultAsync();
            return _mapper.Map<InvoiceReadDto>(entity);
        }

        public async Task<Result<List<InvoiceReadDto>>> SearchSalesInvoicesAsync(
                    string? invoiceNumber,string? customerName,
                    DateTime? dateFrom,DateTime? dateTo,bool? isSal=null, bool? isPOS = null,
                          InvoiceStatus? status = null)
        {
            try
            {


                var query = _uow.Invoices.GetAllAsQueryable()
                       .Include(i => i.InvoiceLines)
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
                    query = query.Where(i => i.InvoiceNumber == invoiceNumber);
                }

                if (!string.IsNullOrWhiteSpace(customerName))
                {
                    query = query.Where(i => i.User.Name.Contains(customerName));
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

                return Result<List<InvoiceReadDto>>.Ok(mapped);
            }
            catch (Exception ex)
            {
                return Result<List<InvoiceReadDto>>.Fail("خطأ أثناء البحث عن الفواتير: " + ex.Message);
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
     GetSalesReportAsync(FinancialSummaryFilterDto filter, InvoiceType? type = null, bool? isPOS = null)
        {
            if (filter.From > filter.To)
                return Result<(FinancialSummaryDto, List<SalesReportRowDto>)>.Fail("Invalid date range.");

            var invoiceRepo = _uow.GetRepository<Invoice>();
            var lineRepo = _uow.GetRepository<InvoiceLine>();

            var invoicesQ = invoiceRepo.GetAllAsQueryable()
                .Where(x => x.CreatedDate >= filter.From && x.CreatedDate <= filter.To);

            if (filter.CustomerId.HasValue)
                invoicesQ = invoicesQ.Where(x => x.CustomerId == filter.CustomerId.Value);

            if (filter.CashierId.HasValue)
                invoicesQ = invoicesQ.Where(x => x.CasherId == filter.CashierId.Value);

            if (type.HasValue)
                invoicesQ = invoicesQ.Where(x => x.InvoiceType == type.Value);
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

            // ✅ rows
            var invoiceIds = await invoicesQ.Select(x => x.Id).ToListAsync();

            var lines = await lineRepo.GetAllAsQueryable()
                .Where(l => invoiceIds.Contains(l.InvoiceId))
                .ToListAsync();

            var linesByInvoice = lines.GroupBy(l => l.InvoiceId)
                .ToDictionary(g => g.Key, g => new
                {
                    Cogs = g.Sum(x => x.Quantity * x.UnitCost),
                    SubTotal = g.Sum(x => x.LineSubTotal),
                    Tax = g.Sum(x => x.TaxAmount)
                });

            var invoices = await invoicesQ
                .Include(x => x.User) // customer
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

            var rows = invoices.Select(inv =>
            {
                var discount = inv.DiscountAmount ?? 0m;

                linesByInvoice.TryGetValue(inv.Id, out var agg);
                var subTotal = agg?.SubTotal ?? inv.SubTotal;     // prefer lines, fallback invoice
                var tax = agg?.Tax ?? inv.TotalTax;
                var cogs = agg?.Cogs ?? inv.TotalCOGS;

                var total = subTotal - discount + tax;
                var profit = (subTotal - discount) - cogs;

                return new SalesReportRowDto
                {
                    InvoiceId = inv.Id,
                    InvoiceNumber = inv.InvoiceNumber,
                    Date = inv.CreatedDate,
                    CustomerName = inv.User?.Name ?? "—",
                    CashierName = inv.CasherId.HasValue && cashierNames.TryGetValue(inv.CasherId.Value, out var cashierName)
                        ? cashierName
                        : "—",

                    SubTotal = subTotal,
                    TotalTax = tax,
                    Discount = discount,
                    Total = total,

                    Cogs = cogs,
                    Profit = profit,

                    InvoiceType = inv.InvoiceType.ToString(),
                    PaymentMethod = inv.PaymentType?.ToString() ?? "—",
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
                ? rows.Where(r => r.InvoiceType == InvoiceType.Return.ToString()).Sum(r => r.SubTotal)
                : 0m;

            var netSales = (totalSales - totalReturns) - totalDiscounts; // قبل الضريبة
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

        public async Task<Result<(DateTime? from, DateTime? to)>> GetSalesReportDateRangeAsync()
        {
            try
            {
                var invoices = _uow.GetRepository<Invoice>()
                    .GetAllAsQueryable()
                    .Where(x =>
                        x.InvoiceType == InvoiceType.Sale ||
                        x.InvoiceType == InvoiceType.Return ||
                        x.InvoiceType == InvoiceType.EndpointOrder);

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
          InvoiceStatus? status = null);


        Task<InvoiceReadDto?> GetFullInvoiceByIdAsync(int id);

        Task<Result<List<InvoiceReadDto>>> GetHeldPOSInvoicesAsync();
        Task<Result<(FinancialSummaryDto summary, List<SalesReportRowDto> rows)>>
                GetSalesReportAsync(FinancialSummaryFilterDto filter, InvoiceType? type = null, bool? isPOS = null);
        Task<Result<(DateTime? from, DateTime? to)>> GetSalesReportDateRangeAsync();
    }
}
