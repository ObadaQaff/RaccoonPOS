using System.Data;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Sales;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Cashiers.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;

namespace RaccoonWarehouse.Application.Service.Purchases;

public sealed class PurchaseBonusReturnRow
{
    public int SourceLineId { get; set; }
    public int ProductId { get; set; }
    public int ProductUnitId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal PaidAvailable { get; set; }
    public decimal FreeAvailable { get; set; }
    public decimal PaidToReturn { get; set; }
    public decimal FreeToReturn { get; set; }
}

public sealed class PurchaseBonusReturnService
{
    private readonly ApplicationDbContext _db;
    private readonly ISaleCheckoutService _checkout;
    private readonly IPosProductCatalogRefreshQueue? _catalog;
    public PurchaseBonusReturnService(ApplicationDbContext db, ISaleCheckoutService checkout,
        IPosProductCatalogRefreshQueue? catalog = null)
        => (_db, _checkout, _catalog) = (db, checkout, catalog);

    public async Task<List<PurchaseBonusReturnRow>> GetRowsAsync(int invoiceId)
    {
var sources = await _db.Set<InvoiceLine>().AsNoTracking().Include(x => x.Product)
            .Where(x => x.InvoiceId == invoiceId).ToListAsync();
        var ids = sources.Select(x => x.Id).ToList();
        var prior = await _db.Set<InvoiceLine>().AsNoTracking()
            .Where(x => x.SourcePurchaseLineId.HasValue && ids.Contains(x.SourcePurchaseLineId.Value)
                && x.Invoice!.InvoiceType == InvoiceType.PurchaseReturn
                && x.Invoice.Status != InvoiceStatus.Cancelled && x.Invoice.Status != InvoiceStatus.Draft
                && x.Invoice.Status != InvoiceStatus.OnHold).ToListAsync();
        return sources.Where(x => x.PurchaseAcquisitionCost.HasValue).Select(x => new PurchaseBonusReturnRow
        {
            SourceLineId = x.Id, ProductId = x.ProductId, ProductUnitId = x.ProductUnitId,
            ProductName = x.Product?.Name ?? $"#{x.ProductId}",
            PaidAvailable = Math.Max(0m, x.Quantity - prior.Where(p => p.SourcePurchaseLineId == x.Id).Sum(p => Math.Abs(p.Quantity))),
            FreeAvailable = Math.Max(0m, x.FreeQuantity - prior.Where(p => p.SourcePurchaseLineId == x.Id).Sum(p => p.FreeQuantity))
        }).ToList();
    }

    public async Task<Result<SaleCheckoutResult>> CompleteAsync(int invoiceId, IEnumerable<PurchaseBonusReturnRow> requested,
        PaymentType paymentType, CashierSessionReadDto session)
    {
        // Serialize return-limit reads and original-lot deduction on SQL Server.
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        try
        {
            var original = await _db.Set<Invoice>().AsNoTracking().FirstAsync(x => x.Id == invoiceId);
            if (original.InvoiceType != InvoiceType.Purchase || original.Status is InvoiceStatus.Cancelled or InvoiceStatus.Draft or InvoiceStatus.OnHold)
                throw new ArgumentException("The original purchase is not finalized / فاتورة الشراء الأصلية غير مرحّلة.");
            var rows = requested.Where(x => x.PaidToReturn != 0m || x.FreeToReturn != 0m).ToList();
            if (rows.Count == 0 || rows.Any(x => x.PaidToReturn < 0m || x.FreeToReturn < 0m))
                throw new ArgumentException("Enter positive return quantities / أدخل كميات إرجاع موجبة.");
            if (paymentType is PaymentType.Check or PaymentType.InsuranceReceivable)
                throw new ArgumentException("Choose cash, card, transfer, or supplier credit / اختر النقد أو البطاقة أو التحويل أو رصيد المورد.");
            var invoice = new InvoiceWriteDto
            {
                InvoiceNumber = (DateTime.Now.Ticks % 90000 + 10000).ToString(),
                OriginalInvoiceId = original.InvoiceNumber, SupplierId = original.SupplierId,
                InvoiceType = InvoiceType.PurchaseReturn, Status = InvoiceStatus.Completed,
                CasherId = session.CashierId, CashierSessionId = session.Id,
                PaymentType = paymentType, CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now,
                DeferAccountingPosting = true,
                InvoiceLines = rows.Select(x => new InvoiceLineWriteDto
                {
                    SourcePurchaseLineId = x.SourceLineId, ProductId = x.ProductId, ProductUnitId = x.ProductUnitId,
                    Quantity = -x.PaidToReturn, FreeQuantity = x.FreeToReturn
                }).ToList()
            };
            await PurchaseBonusReturns.PrepareAsync(_db, invoice);
            invoice.TotalAmount = invoice.InvoiceLines.Sum(x => x.LineSubTotal + x.TaxAmount);
            if (invoice.TotalAmount > 0m)
                invoice.Payments = new List<InvoicePaymentWriteDto> { new() { PaymentType = paymentType, Amount = invoice.TotalAmount } };
            var result = await _checkout.CompleteAsync(new SaleCheckoutRequest
            {
                Invoice = invoice, Session = session,
                StockMovementsFactory = id => invoice.InvoiceLines.Select(line => new StockMovementPostDto
                {
                    ProductId = line.ProductId, ProductUnitId = line.ProductUnitId,
                    SourcePurchaseLineId = line.SourcePurchaseLineId,
                    Quantity = -(Math.Abs(line.Quantity) + line.FreeQuantity),
                    QuantityPerUnitSnapshot = line.QuantityPerUnitSnapshot,
                    BaseQuantity = -(Math.Abs(line.Quantity) + line.FreeQuantity) * line.QuantityPerUnitSnapshot,
                    PurchasePrice = line.UnitCost, UnitPrice = line.UnitPrice, ExpiryDate = line.ExpiryDate,
                    TransactionType = TransactionType.Return, InvoiceId = id, CasherId = session.CashierId,
                    CashierSessionId = session.Id, TransactionDate = DateTime.Now,
                    Notes = $"Purchase bonus return #{invoice.InvoiceNumber}"
                }).ToList()
            });
            if (!result.Success)
            {
                if (transaction != null) await transaction.RollbackAsync();
                _db.ChangeTracker.Clear();
                return result;
            }
            await _db.SaveChangesAsync();
            if (transaction != null) await transaction.CommitAsync();
            _catalog?.Enqueue(rows.Select(x => x.ProductId).Distinct());
            return result;
        }
        catch (Exception ex)
        {
            if (transaction != null) await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();
            return Result<SaleCheckoutResult>.Fail(ex.Message);
        }
    }
}
