using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.Domain.Reports.Products.Dtos;
using RaccoonWarehouse.Domain.Reports.Products.Filters;

namespace RaccoonWarehouse.Application.Service.Sales;

public sealed class ProductSalesAverageService(IUOW uow)
{
    public async Task<List<ProductSalesAverageRowDto>> GetAsync(
        ProductSalesAverageFilterDto filter, DateTime endExclusive)
    {
        if (filter.Months is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(filter.Months));

        var from = endExclusive.AddMonths(-filter.Months);
        var query = uow.GetRepository<InvoiceLine>().GetAllAsQueryable().AsNoTracking()
            .Where(l => l.Invoice != null && l.Product != null
                && l.Invoice.CreatedDate >= from && l.Invoice.CreatedDate < endExclusive
                && (l.Invoice.Status == InvoiceStatus.Completed || l.Invoice.Status == InvoiceStatus.Posted)
                && (l.Invoice.InvoiceType == InvoiceType.Sale || l.Invoice.InvoiceType == InvoiceType.EndpointOrder));

        if (filter.SupplierId is > 0)
            query = query.Where(l => l.Product!.SupplierId == filter.SupplierId);

        var name = filter.ProductName?.Trim();
        var code = filter.ItemCode?.Trim();
        var search = filter.SearchText?.Trim();
        if (!string.IsNullOrEmpty(name))
            query = query.Where(l => l.Product!.Name != null && l.Product.Name.Contains(name));
        if (!string.IsNullOrEmpty(code))
            query = query.Where(l => l.Product!.ITEMCODE.HasValue && l.Product.ITEMCODE.Value.ToString().Contains(code));
        if (!string.IsNullOrEmpty(search))
            query = query.Where(l => (l.Product!.Name != null && l.Product.Name.Contains(search))
                || (l.Product.ITEMCODE.HasValue && l.Product.ITEMCODE.Value.ToString().Contains(search)));

        // Group in SQL. Keep selling units separate.
        return await query.GroupBy(l => new
            {
                l.ProductId, l.Product!.Name, l.Product.ITEMCODE, l.ProductUnitId,
                UnitName = l.ProductUnit != null && l.ProductUnit.Unit != null ? l.ProductUnit.Unit.Name : null
            })
            .Select(g => new ProductSalesAverageRowDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                ItemCode = g.Key.ITEMCODE,
                Barcode = g.Key.ITEMCODE.HasValue ? g.Key.ITEMCODE.Value.ToString() : null,
                UnitName = g.Key.UnitName,
                TotalSoldQuantity = g.Sum(l => l.Quantity),
                AverageMonthlyQuantity = g.Sum(l => l.Quantity) / filter.Months,
                TotalSalesAmount = g.Sum(l => l.Quantity * l.UnitPrice - l.LineDiscountAmount > 0m
                    ? l.Quantity * l.UnitPrice - l.LineDiscountAmount : 0m),
                LastSaleDate = g.Max(l => (DateTime?)l.Invoice!.CreatedDate)
            })
            .OrderBy(r => r.ProductName).ThenBy(r => r.ItemCode).ThenBy(r => r.UnitName)
            .ToListAsync();
    }
}
