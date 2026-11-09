using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.PurchaseInvoiceMatches;
using RaccoonWarehouse.Domain.PurchaseInvoiceMatches.DTOs;

namespace RaccoonWarehouse.Application.Service.PurchaseInvoiceMatches;

public interface IPurchaseInvoiceMatchService
{
    Task<Result<PurchaseInvoiceMatchReadDto>> MatchAsync(PurchaseInvoiceMatchWriteDto dto);
}

public sealed class PurchaseInvoiceMatchService : IPurchaseInvoiceMatchService
{
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;
    private readonly IUserSession _session;

    public PurchaseInvoiceMatchService(ApplicationDbContext db, IMapper mapper, IUserSession session)
    {
        _db = db;
        _mapper = mapper;
        _session = session;
    }

    public async Task<Result<PurchaseInvoiceMatchReadDto>> MatchAsync(PurchaseInvoiceMatchWriteDto dto)
    {
if (dto.Lines == null || dto.Lines.Count == 0)
            return Result<PurchaseInvoiceMatchReadDto>.Fail("At least one invoice-to-order line mapping is required.");

        var invoice = await _db.Set<Invoice>().Include(x => x.InvoiceLines).FirstOrDefaultAsync(x => x.Id == dto.InvoiceId);
        if (invoice == null || invoice.InvoiceType != InvoiceType.Purchase)
            return Result<PurchaseInvoiceMatchReadDto>.Fail("A posted purchase invoice is required for matching.");
        if (!invoice.SupplierId.HasValue)
            return Result<PurchaseInvoiceMatchReadDto>.Fail("The purchase invoice must have a supplier.");
        var supplierInvoiceNumber = dto.SupplierInvoiceNumber?.Trim();
        if (!string.IsNullOrWhiteSpace(supplierInvoiceNumber))
        {
            var duplicate = await _db.PurchaseInvoiceMatches
                .AnyAsync(x => x.SupplierId == invoice.SupplierId &&
                               x.SupplierInvoiceNumber == supplierInvoiceNumber &&
                               x.InvoiceId != invoice.Id);
            if (duplicate)
                return Result<PurchaseInvoiceMatchReadDto>.Fail("This supplier invoice number is already matched for the supplier.");
        }
        if (dto.Lines.Select(x => x.InvoiceLineId).Distinct().Count() != dto.Lines.Count)
            return Result<PurchaseInvoiceMatchReadDto>.Fail("An invoice line cannot be mapped more than once.");

        var invoiceLineIds = dto.Lines.Select(x => x.InvoiceLineId).ToList();
        var poLineIds = dto.Lines.Select(x => x.PurchaseOrderLineId).ToList();
        var invoiceLines = invoice.InvoiceLines?.Where(x => invoiceLineIds.Contains(x.Id)).ToDictionary(x => x.Id) ?? new();
        var poLines = await _db.PurchaseOrderLines.Where(x => poLineIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        if (invoiceLines.Count != invoiceLineIds.Count || poLines.Count != poLineIds.Count)
            return Result<PurchaseInvoiceMatchReadDto>.Fail("Every mapping must reference existing invoice and purchase-order lines.");

        var acceptedByPoLine = await _db.GoodsReceiptLines
            .Where(x => poLineIds.Contains(x.PurchaseOrderLineId) && x.GoodsReceipt.Status == GoodsReceiptStatus.Accepted)
            .GroupBy(x => x.PurchaseOrderLineId)
            .Select(x => new { Id = x.Key, Quantity = x.Sum(y => y.QuantityAccepted + y.FreeQuantity) })
            .ToDictionaryAsync(x => x.Id, x => x.Quantity);

        var match = await _db.PurchaseInvoiceMatches.Include(x => x.Lines).FirstOrDefaultAsync(x => x.InvoiceId == dto.InvoiceId);
        if (match == null)
        {
            match = new PurchaseInvoiceMatch { InvoiceId = invoice.Id, SupplierId = invoice.SupplierId, CreatedDate = DateTime.Now };
            await _db.PurchaseInvoiceMatches.AddAsync(match);
        }
        else if (match.Status == PurchaseInvoiceMatchingStatus.Posted)
            return Result<PurchaseInvoiceMatchReadDto>.Fail("A posted supplier invoice match cannot be modified.");

        match.PurchaseOrderId = dto.PurchaseOrderId;
        match.SupplierInvoiceNumber = supplierInvoiceNumber;
        match.PriceTolerance = dto.PriceTolerance < 0 ? 0 : dto.PriceTolerance;
        match.QuantityTolerance = dto.QuantityTolerance < 0 ? 0 : dto.QuantityTolerance;
        match.Status = PurchaseInvoiceMatchingStatus.Matching;
        match.Lines.Clear();

        var discrepancies = new List<string>();
        foreach (var mapping in dto.Lines)
        {
            var invoiceLine = invoiceLines[mapping.InvoiceLineId];
            var poLine = poLines[mapping.PurchaseOrderLineId];
            acceptedByPoLine.TryGetValue(poLine.Id, out var accepted);
            var invoiced = invoiceLine.Quantity + invoiceLine.FreeQuantity;
            var priceVariance = invoiceLine.UnitPrice - poLine.UnitPrice;
            var quantityVariance = invoiced - accepted;
            match.Lines.Add(new PurchaseInvoiceMatchLine
            {
                InvoiceLineId = invoiceLine.Id,
                PurchaseOrderLineId = poLine.Id,
                InvoicedQuantity = invoiced,
                AcceptedQuantity = accepted,
                InvoicedUnitPrice = invoiceLine.UnitPrice,
                OrderedUnitPrice = poLine.UnitPrice,
                PriceVariance = priceVariance,
                QuantityVariance = quantityVariance,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            });
            if (Math.Abs(priceVariance) > match.PriceTolerance)
                discrepancies.Add($"Price variance on PO line {poLine.Id}.");
            if (invoiced > accepted + match.QuantityTolerance)
                discrepancies.Add($"Invoiced quantity exceeds accepted quantity on PO line {poLine.Id}.");
        }

        match.DiscrepancyReason = discrepancies.Count == 0 ? null : string.Join(" ", discrepancies.Distinct());
        match.Status = discrepancies.Count == 0 ? PurchaseInvoiceMatchingStatus.Matched : PurchaseInvoiceMatchingStatus.Disputed;
        match.MatchedBy = _session.CurrentUser?.Id;
        match.MatchedDate = discrepancies.Count == 0 ? DateTime.Now : null;
        match.UpdatedDate = DateTime.Now;
        await _db.SaveChangesAsync();
        var result = _mapper.Map<PurchaseInvoiceMatchReadDto>(match);
        return discrepancies.Count == 0
            ? Result<PurchaseInvoiceMatchReadDto>.Ok(result, "Supplier invoice matched successfully.")
            : Result<PurchaseInvoiceMatchReadDto>.Fail($"Supplier invoice matching is disputed: {match.DiscrepancyReason}");
    }
}
