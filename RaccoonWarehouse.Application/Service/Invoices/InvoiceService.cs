using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.Reports.Financial.Dtos;
using RaccoonWarehouse.Domain.Reports.Financial.Filters;
using RaccoonWarehouse.Domain.Reports.Sales.Dtos;
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

        //POS Operations
        #region POS Operations
        //POS Invoice Creation
        public async Task<Result<InvoiceWriteDto>> CreatePOSInvoice(InvoiceWriteDto Dto)
        {



            return Result<InvoiceWriteDto>.Ok(null, "Not implemented yet");
        }

        #endregion


        public InvoiceService(ApplicationDbContext context, IUOW uow, IMapper mapper) : base(context, uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }
        public override async Task<Result<InvoiceWriteDto>> CreateAsync(InvoiceWriteDto dto)
        {
            try
            {
                var invoiceRepo = _uow.GetRepository<Invoice>();
                var lineRepo = _uow.GetRepository<InvoiceLine>();

                // 1) احسب قيم السطور (Tax/Cost/Profit) قبل الحفظ
                foreach (var l in dto.InvoiceLines)
                {
                    var costTotal = l.Quantity * l.UnitCost;
                    var profitBeforeTax = l.LineSubTotal - costTotal;
                    var profit = profitBeforeTax - l.TaxAmount; // حسب منطقك (تخصم الضريبة)

                    l.ProfitBeforeTax = profitBeforeTax;
                    l.Profit = profit;
                }

                // 2) احسب قيم الفاتورة
                dto.TotalCOGS = dto.InvoiceLines.Sum(x => x.CostTotal);
                dto.NetSales = dto.SubTotal - (dto.DiscountAmount ?? 0m) - dto.TotalTax; // حسب طلبك
                dto.GrossProfit = dto.NetSales - dto.TotalCOGS;

                // 3) أنشئ invoice بدون lines (مهم)
                var invoice = _mapper.Map<Invoice>(dto);
                invoice.InvoiceLines = new List<InvoiceLine>();

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

                await _uow.CommitAsync();

                dto.Id = invoice.Id;
                return Result<InvoiceWriteDto>.Ok(dto, "Invoice created successfully.");
            }
            catch (Exception ex)
            {
                return Result<InvoiceWriteDto>.Fail($"Error creating invoice: {ex.Message}");
            }
        }
        private void RecalculateInvoice(Invoice invoice)
        {
            if (invoice.InvoiceLines == null)
                invoice.InvoiceLines = new List<InvoiceLine>();

            // 1) Per-line calculations
            foreach (var line in invoice.InvoiceLines)
            {
                // subtotal before tax
                line.LineSubTotal = line.Quantity * line.UnitPrice;

                // tax
                var rate = line.TaxExempt ? 0m : line.TaxRate;
                line.TaxAmount = line.TaxExempt ? 0m : (line.LineSubTotal * rate / 100m);

                // profit
                var costTotal = line.Quantity * line.UnitCost;
                line.ProfitBeforeTax = line.LineSubTotal - costTotal;

                // usually tax doesn't affect profit (it's a liability)
                line.Profit = line.ProfitBeforeTax;
            }

            // 2) Invoice totals
            invoice.SubTotal = invoice.InvoiceLines.Sum(l => l.LineSubTotal);
            invoice.TotalTax = invoice.InvoiceLines.Sum(l => l.TaxAmount);

            var discount = invoice.DiscountAmount ?? 0m;

            invoice.TotalCOGS = invoice.InvoiceLines.Sum(l => l.Quantity * l.UnitCost);

            invoice.NetSales = invoice.SubTotal - discount;             // قبل الضريبة
            invoice.GrossProfit = invoice.NetSales - invoice.TotalCOGS; // الربح

            invoice.TotalAmount = invoice.NetSales + invoice.TotalTax;  // النهائي
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
                .Include(i => i.User)          // customer
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
     GetSalesReportAsync(FinancialSummaryFilterDto filter, InvoiceType? type = null)
        {
            if (filter.From > filter.To)
                return Result<(FinancialSummaryDto, List<SalesReportRowDto>)>.Fail("Invalid date range.");

            var invoiceRepo = _uow.GetRepository<Invoice>();
            var lineRepo = _uow.GetRepository<InvoiceLine>();

            var invoicesQ = invoiceRepo.GetAllAsQueryable()
                .Where(x => x.CreatedDate >= filter.From && x.CreatedDate <= filter.To);

            if (filter.CustomerId.HasValue)
                invoicesQ = invoicesQ.Where(x => x.CustomerId == filter.CustomerId.Value);

            if (type.HasValue)
                invoicesQ = invoicesQ.Where(x => x.InvoiceType == type.Value);

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
            var totalSales = rows.Where(r => r.InvoiceType == InvoiceType.Sale.ToString()).Sum(r => r.SubTotal);
            var totalTax = rows.Where(r => r.InvoiceType == InvoiceType.Sale.ToString()).Sum(r => r.TotalTax);
            var totalDiscounts = rows.Where(r => r.InvoiceType == InvoiceType.Sale.ToString()).Sum(r => r.Discount);

            var totalReturns = filter.IncludeReturns
                ? rows.Where(r => r.InvoiceType == InvoiceType.Return.ToString()).Sum(r => r.SubTotal)
                : 0m;

            var netSales = (totalSales - totalReturns) - totalDiscounts; // قبل الضريبة
            var totalCogs = rows.Where(r => r.InvoiceType == InvoiceType.Sale.ToString()).Sum(r => r.Cogs);
            var grossProfit = netSales - totalCogs;
            var margin = netSales == 0 ? 0 : Math.Round((grossProfit / netSales) * 100m, 2);

            var countInvoices = rows.Count(r => r.InvoiceType == InvoiceType.Sale.ToString());
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
                GetSalesReportAsync(FinancialSummaryFilterDto filter, InvoiceType? type = null);
    }
}
