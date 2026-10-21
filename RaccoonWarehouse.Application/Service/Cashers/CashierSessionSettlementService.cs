using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.PurchaseReturns;
using RaccoonWarehouse.Domain.Vouchers;

namespace RaccoonWarehouse.Application.Service.Cashers;

public interface ICashierSessionSettlementService
{
    Task<CashierSessionSettlementReadDto> CalculateAsync(int sessionId, decimal countedCash, decimal countedVisa, CancellationToken cancellationToken = default);
    Task<CashierSessionSettlementReadDto?> LoadSavedAsync(int sessionId, CancellationToken cancellationToken = default);
}

public sealed class CashierSessionSettlementService : ICashierSessionSettlementService
{
    private readonly IUOW _uow;

    public CashierSessionSettlementService(IUOW uow) => _uow = uow;

    public async Task<CashierSessionSettlementReadDto> CalculateAsync(
        int sessionId,
        decimal countedCash,
        decimal countedVisa,
        CancellationToken cancellationToken = default)
    {
        var session = await _uow.CashierSessions.GetByIdAsync(sessionId)
            ?? throw new InvalidOperationException("Cashier session not found.");

        if (session.SettlementLocked)
            return await LoadSavedAsync(sessionId, cancellationToken)
                ?? throw new InvalidOperationException("The settlement snapshot is incomplete.");

        var activity = await LoadActivityAsync(sessionId, cancellationToken);
        var invoiceCash = SumInvoicePayment(activity.Invoices, PaymentType.Cash);
        var invoiceVisa = SumInvoicePayment(activity.Invoices, PaymentType.Visa);
        var voucherCash = SumVoucher(activity.Vouchers, activity.LinkedVoucherIds, PaymentType.Cash);
        var voucherVisa = SumVoucher(activity.Vouchers, activity.LinkedVoucherIds, PaymentType.Visa);

        var result = new CashierSessionSettlementReadDto
        {
            SessionId = sessionId,
            OpeningCash = session.StatrBalance,
            ExpectedCash = session.StatrBalance + invoiceCash + voucherCash,
            ExpectedVisa = invoiceVisa + voucherVisa,
            CountedCash = countedCash,
            CountedVisa = countedVisa,
            Actions = BuildActions(activity.Invoices, activity.Vouchers, activity.PurchaseReturns, activity.LinkedVoucherIds)
        };

        session.SettlementCalculatedAt = DateTime.Now;
        session.SettlementCountedCash = countedCash;
        session.SettlementCountedVisa = countedVisa;
        session.SettlementExpectedCash = result.ExpectedCash;
        session.SettlementExpectedVisa = result.ExpectedVisa;
        session.SettlementCashDifference = result.CashDifference;
        session.SettlementVisaDifference = result.VisaDifference;
        session.SettlementLocked = true;
        session.Status = SessionStatus.Closing;
        await _uow.CommitAsync();

        return result;
    }

    public async Task<CashierSessionSettlementReadDto?> LoadSavedAsync(
        int sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _uow.CashierSessions.GetByIdAsync(sessionId);
        if (session == null || !session.SettlementLocked
            || !session.SettlementCountedCash.HasValue
            || !session.SettlementCountedVisa.HasValue
            || !session.SettlementExpectedCash.HasValue
            || !session.SettlementExpectedVisa.HasValue)
            return null;

        var activity = await LoadActivityAsync(sessionId, cancellationToken);

        return new CashierSessionSettlementReadDto
        {
            SessionId = sessionId,
            OpeningCash = session.StatrBalance,
            ExpectedCash = session.SettlementExpectedCash.Value,
            ExpectedVisa = session.SettlementExpectedVisa.Value,
            CountedCash = session.SettlementCountedCash.Value,
            CountedVisa = session.SettlementCountedVisa.Value,
            Actions = BuildActions(activity.Invoices, activity.Vouchers, activity.PurchaseReturns, activity.LinkedVoucherIds)
        };
    }

    private async Task<SettlementActivity> LoadActivityAsync(int sessionId, CancellationToken cancellationToken)
    {
        var invoiceQuery = _uow.Invoices.GetAllAsQueryable()
            .Where(invoice => invoice.CashierSessionId == sessionId
                && (invoice.InvoiceType == InvoiceType.Sale
                    || invoice.InvoiceType == InvoiceType.Return
                    || invoice.InvoiceType == InvoiceType.Purchase
                    || invoice.InvoiceType == InvoiceType.PurchaseReturn)
                && (invoice.Status == InvoiceStatus.Completed || invoice.Status == InvoiceStatus.Posted));

        var invoices = await invoiceQuery
            .Include(invoice => invoice.Payments)
            .ToListAsync(cancellationToken);

        var linkedVoucherIds = invoices
            .Where(invoice => invoice.VoucherId.HasValue)
            .Select(invoice => invoice.VoucherId!.Value)
            .ToHashSet();

        var vouchers = await _uow.Vouchers.GetAllAsQueryable()
            .Where(voucher => voucher.CashierSessionId == sessionId
                && voucher.PostingStatus == RaccoonWarehouse.Domain.Accounting.Enums.AccountingPostingStatus.Posted)
            .ToListAsync(cancellationToken);

        var purchaseReturns = await _uow.GetRepository<PurchaseReturn>().GetAllAsQueryable()
            .Where(returnDocument => returnDocument.CashierSessionId == sessionId
                && returnDocument.Status == PurchaseReturnStatus.Confirmed)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new SettlementActivity(invoices, vouchers, purchaseReturns, linkedVoucherIds);
    }

    private sealed record SettlementActivity(
        List<Invoice> Invoices,
        List<Voucher> Vouchers,
        List<PurchaseReturn> PurchaseReturns,
        HashSet<int> LinkedVoucherIds);

    private static decimal SumInvoicePayment(IEnumerable<Invoice> invoices, PaymentType paymentType)
    {
        return invoices.Sum(invoice =>
        {
            var allocated = (invoice.Payments ?? Array.Empty<InvoicePayment>())
                .Where(payment => payment.PaymentType == paymentType && payment.Amount > 0m)
                .Sum(payment => payment.Amount);

            if (allocated == 0m && invoice.PaymentType == paymentType)
                allocated = Math.Abs(invoice.TotalAmount);

            return invoice.InvoiceType is InvoiceType.Return or InvoiceType.Purchase
                ? -allocated
                : allocated;
        });
    }

    private static decimal SumVoucher(
        IEnumerable<Voucher> vouchers,
        ISet<int> linkedVoucherIds,
        PaymentType paymentType)
    {
        return vouchers
            .Where(voucher => voucher.PaymentType == paymentType && !linkedVoucherIds.Contains(voucher.Id))
            .Sum(voucher => IsIncomingVoucher(voucher.VoucherType)
                ? Math.Abs(voucher.Amount)
                : -Math.Abs(voucher.Amount));
    }

    private static List<CashierSessionActionReadDto> BuildActions(
        IEnumerable<Invoice> invoices,
        IEnumerable<Voucher> vouchers,
        IEnumerable<PurchaseReturn> purchaseReturns,
        ISet<int> linkedVoucherIds)
    {
        var actions = invoices.Select(invoice => new CashierSessionActionReadDto
        {
            Date = invoice.ClosedAt ?? invoice.CreatedDate,
            ActionType = invoice.InvoiceType.ToString(),
            DocumentNumber = invoice.InvoiceNumber,
            PaymentMethod = FormatInvoicePaymentMethod(invoice),
            Amount = Math.Abs(invoice.TotalAmount),
            Direction = invoice.InvoiceType is InvoiceType.Return or InvoiceType.Purchase ? "Outgoing" : "Incoming",
            Status = invoice.Status?.ToString() ?? "-",
            Notes = GetSettlementVisibleNotes(invoice.Notes),
            CountedInSettlement = true
        }).ToList();

        actions.AddRange(vouchers.Select(voucher => new CashierSessionActionReadDto
        {
            Date = voucher.CreatedDate,
            ActionType = $"Voucher: {voucher.VoucherType}",
            DocumentNumber = voucher.VoucherNumber ?? voucher.Id.ToString(),
            PaymentMethod = voucher.PaymentType.ToString(),
            Amount = Math.Abs(voucher.Amount),
            Direction = IsIncomingVoucher(voucher.VoucherType) ? "Incoming" : "Outgoing",
            Status = voucher.PostingStatus.ToString(),
            Notes = voucher.Notes,
            CountedInSettlement = !linkedVoucherIds.Contains(voucher.Id)
        }));

        actions.AddRange(purchaseReturns.Select(returnDocument => new CashierSessionActionReadDto
        {
            Date = returnDocument.ConfirmedDate ?? returnDocument.ReturnDate,
            ActionType = "PurchaseReturn",
            DocumentNumber = returnDocument.ReturnNumber,
            PaymentMethod = "-",
            Amount = 0m,
            Direction = "Stock movement",
            Status = returnDocument.Status.ToString(),
            Notes = returnDocument.Reason,
            CountedInSettlement = false
        }));

        return actions.OrderByDescending(x => x.Date).ToList();
    }

    private static string? GetSettlementVisibleNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return notes;

        const string marker = "Return refund payment method differs from original invoice.";
        var visibleNotes = notes
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(note => note.Trim())
            .Where(note => !note.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return visibleNotes.Count == 0 ? null : string.Join(" | ", visibleNotes);
    }

    private static string FormatInvoicePaymentMethod(Invoice invoice)
    {
        var methods = (invoice.Payments ?? Array.Empty<InvoicePayment>())
            .Where(payment => payment.Amount > 0m)
            .Select(payment => payment.PaymentType.ToString())
            .Distinct()
            .ToList();

        return methods.Count > 1 ? "Mixed" : methods.FirstOrDefault() ?? invoice.PaymentType?.ToString() ?? "-";
    }

    private static bool IsIncomingVoucher(VoucherType type) => type is
        VoucherType.Sales or VoucherType.Receipt or VoucherType.ReturnPurchase or VoucherType.Adjustment;
}
