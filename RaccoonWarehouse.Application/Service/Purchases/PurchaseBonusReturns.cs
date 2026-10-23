using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.Domain.Invoices.DTOs;

namespace RaccoonWarehouse.Application.Service.Purchases;

public static class PurchaseBonusReturns
{
    public static async Task PrepareAsync(ApplicationDbContext db, InvoiceWriteDto invoice)
    {
        if (invoice.InvoiceType != InvoiceType.PurchaseReturn || invoice.InvoiceLines == null) return;
        var lines = invoice.InvoiceLines.ToList();
        var sourceIds = lines.Where(x => x.SourcePurchaseLineId.HasValue).Select(x => x.SourcePurchaseLineId!.Value).Distinct().ToList();
        if (sourceIds.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(invoice.OriginalInvoiceId)
                && await db.Set<InvoiceLine>().AnyAsync(x => x.Invoice!.InvoiceNumber == invoice.OriginalInvoiceId
                    && x.PurchaseAcquisitionCost.HasValue))
                throw new ArgumentException("Return paid and free quantities separately for this purchase / أرجع المدفوع والمجاني بشكل منفصل لهذه الفاتورة.");
            return;
        }
        if (lines.Any(x => !x.SourcePurchaseLineId.HasValue) || lines.GroupBy(x => x.SourcePurchaseLineId).Any(g => g.Count() > 1))
            throw new ArgumentException("Invalid purchase-return line references / مراجع سطور مرتجع الشراء غير صالحة.");
        var sources = await db.Set<InvoiceLine>().AsNoTracking().Include(x => x.Invoice)
            .Where(x => sourceIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        var prior = await db.Set<InvoiceLine>().AsNoTracking()
            .Where(x => x.SourcePurchaseLineId.HasValue && sourceIds.Contains(x.SourcePurchaseLineId.Value)
                && x.InvoiceId != invoice.Id && x.Invoice!.InvoiceType == InvoiceType.PurchaseReturn
                && x.Invoice.Status != InvoiceStatus.Cancelled && x.Invoice.Status != InvoiceStatus.OnHold
                && x.Invoice.Status != InvoiceStatus.Draft).ToListAsync();
        foreach (var line in lines)
        {
            if (!sources.TryGetValue(line.SourcePurchaseLineId!.Value, out var source)
                || source.Invoice!.InvoiceType != InvoiceType.Purchase || !source.PurchaseAcquisitionCost.HasValue
                || source.Invoice.InvoiceNumber != invoice.OriginalInvoiceId || source.Invoice.SupplierId != invoice.SupplierId
                || source.ProductId != line.ProductId || source.ProductUnitId != line.ProductUnitId
                || line.Quantity > 0m || line.FreeQuantity < 0m || -line.Quantity + line.FreeQuantity <= 0m)
                throw new ArgumentException("Invalid original purchase or return quantity / فاتورة الشراء الأصلية أو كمية الإرجاع غير صالحة.");
            var earlier = prior.Where(x => x.SourcePurchaseLineId == source.Id).ToList();
            if (earlier.Sum(x => Math.Abs(x.Quantity)) + Math.Abs(line.Quantity) > source.Quantity
                || earlier.Sum(x => x.FreeQuantity) + line.FreeQuantity > source.FreeQuantity)
                throw new ArgumentException("Returned paid/free quantity exceeds the original receipt / كمية الإرجاع المدفوعة أو المجانية تتجاوز الاستلام الأصلي.");
            var received = source.Quantity + source.FreeQuantity;
            var paid = Math.Abs(line.Quantity);
            // Supplier credit uses paid units only, after original discounts and input VAT.
            var priorPaid = earlier.Sum(x => Math.Abs(x.Quantity));
            var beforeReceived = priorPaid + earlier.Sum(x => x.FreeQuantity);
            var netRefund = source.Quantity > 0m
                ? Math.Round(source.PurchaseAcquisitionCost.Value * (priorPaid + paid) / source.Quantity, 3)
                    - Math.Round(source.PurchaseAcquisitionCost.Value * priorPaid / source.Quantity, 3) : 0m;
            var taxRefund = source.Quantity > 0m
                ? Math.Round(source.TaxAmount * (priorPaid + paid) / source.Quantity, 3)
                    - Math.Round(source.TaxAmount * priorPaid / source.Quantity, 3) : 0m;
            line.UnitPrice = paid > 0m ? Math.Round((netRefund + taxRefund) / paid, 6) : 0m;
            line.LineSubTotal = netRefund;
            line.TaxAmount = taxRefund;
            line.LineDiscountAmount = 0m;
            line.BonusRatio = null;
            line.TaxRate = source.TaxRate;
            line.TaxExempt = source.TaxExempt;
            line.ExpiryDate = source.ExpiryDate;
            line.QuantityPerUnitSnapshot = source.QuantityPerUnitSnapshot;
            line.BaseQuantity = line.Quantity * source.QuantityPerUnitSnapshot;
            line.UnitCost = PurchaseBonusCosting.EffectiveUnitCost(source.PurchaseAcquisitionCost.Value, received);
            line.PurchaseAcquisitionCost = Math.Round(source.PurchaseAcquisitionCost.Value
                * (beforeReceived + paid + line.FreeQuantity) / received, 6)
                - Math.Round(source.PurchaseAcquisitionCost.Value * beforeReceived / received, 6);
        }
        invoice.CostPriceIncludesTax = true;
        invoice.DiscountAmount = 0m;
    }
}
