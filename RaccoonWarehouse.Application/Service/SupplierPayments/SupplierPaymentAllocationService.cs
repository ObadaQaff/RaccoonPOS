using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.SupplierPayments;
using RaccoonWarehouse.Domain.SupplierPayments.DTOs;
using RaccoonWarehouse.Domain.Vouchers;

namespace RaccoonWarehouse.Application.Service.SupplierPayments;

public interface ISupplierPaymentAllocationService
{
    Task<Result> AllocateAsync(SupplierPaymentAllocationWriteDto dto);
}

public sealed class SupplierPaymentAllocationService : ISupplierPaymentAllocationService
{
    private readonly ApplicationDbContext _db;
    private readonly IUserSession _session;

    public SupplierPaymentAllocationService(ApplicationDbContext db, IUserSession session)
    {
        _db = db;
        _session = session;
    }

    public async Task<Result> AllocateAsync(SupplierPaymentAllocationWriteDto dto)
    {
if (dto.VoucherId <= 0 || dto.SupplierId <= 0 || dto.Lines == null || dto.Lines.Count == 0)
            return Result.Fail("A confirmed voucher, supplier, and at least one invoice allocation are required.");
        if (dto.Lines.Any(x => x.InvoiceId <= 0 || x.Amount <= 0))
            return Result.Fail("Allocation amounts must be positive.");
        if (dto.Lines.GroupBy(x => x.InvoiceId).Any(x => x.Count() > 1))
            return Result.Fail("An invoice may appear only once in an allocation request.");

        var voucher = await _db.Set<Voucher>().FirstOrDefaultAsync(x => x.Id == dto.VoucherId);
        if (voucher == null || voucher.VoucherType != VoucherType.Payment || voucher.SupplierId != dto.SupplierId)
            return Result.Fail("The voucher must be an existing supplier payment voucher.");
        if (voucher.PostingStatus != RaccoonWarehouse.Domain.Accounting.Enums.AccountingPostingStatus.Posted)
            return Result.Fail("Only confirmed and posted payment vouchers can be allocated.");

        var invoiceIds = dto.Lines.Select(x => x.InvoiceId).ToList();
        var invoices = await _db.Set<RaccoonWarehouse.Domain.Invoices.Invoice>()
            .Where(x => invoiceIds.Contains(x.Id) && x.InvoiceType == InvoiceType.Purchase)
            .ToDictionaryAsync(x => x.Id);
        if (invoices.Count != invoiceIds.Count || invoices.Values.Any(x => x.SupplierId != dto.SupplierId))
            return Result.Fail("All allocated invoices must be purchase invoices for the selected supplier.");

        var existing = await _db.Set<SupplierPaymentAllocation>()
            .Where(x => invoiceIds.Contains(x.InvoiceId))
            .GroupBy(x => x.InvoiceId)
            .Select(x => new { InvoiceId = x.Key, Amount = x.Sum(y => y.Amount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => x.Amount);
        if (await _db.Set<SupplierPaymentAllocation>().AnyAsync(x => x.VoucherId == dto.VoucherId))
            return Result.Fail("This payment voucher has already been allocated.");

        var total = dto.Lines.Sum(x => x.Amount);
        if (total > voucher.Amount)
            return Result.Fail("Allocation total cannot exceed the payment voucher amount.");
        foreach (var line in dto.Lines)
        {
            existing.TryGetValue(line.InvoiceId, out var allocated);
            var outstanding = Math.Max(0m, invoices[line.InvoiceId].TotalAmount - allocated);
            if (line.Amount > outstanding)
                return Result.Fail($"Allocation exceeds the outstanding balance for invoice {invoices[line.InvoiceId].InvoiceNumber}.");
        }

        var now = DateTime.Now;
        await _db.Set<SupplierPaymentAllocation>().AddRangeAsync(dto.Lines.Select(line => new SupplierPaymentAllocation
        {
            VoucherId = dto.VoucherId,
            InvoiceId = line.InvoiceId,
            SupplierId = dto.SupplierId,
            Amount = line.Amount,
            AllocationDate = now,
            CreatedDate = now,
            UpdatedDate = now,
            CreatedBy = _session.CurrentUser?.Id
        }));
        await _db.SaveChangesAsync();
        return Result.Ok("Supplier payment allocation saved.");
    }
}
