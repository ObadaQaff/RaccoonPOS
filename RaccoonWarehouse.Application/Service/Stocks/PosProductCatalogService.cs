using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.POS;
using RaccoonWarehouse.Domain.StockLots;

namespace RaccoonWarehouse.Application.Service.Stocks
{
    public interface IPosProductCatalogService
    {
        Task<IReadOnlyList<PosProductCatalogEntry>> GetCatalogAsync(CancellationToken cancellationToken = default);
        Task RefreshAllAsync(CancellationToken cancellationToken = default);
        Task RefreshProductsAsync(IEnumerable<int> productIds, CancellationToken cancellationToken = default);
    }

    public sealed class PosProductCatalogService : IPosProductCatalogService
    {
        private readonly ApplicationDbContext _context;

        private const string RebuildCatalogSql = """
            SET NOCOUNT ON;

            ;WITH ActiveUnits AS
            (
                SELECT
                    pu.ProductId,
                    pu.Id AS ProductUnitId,
                    p.SubCategoryId,
                    p.Name AS ProductName,
                    p.ITEMCODE AS ItemCode,
                    p.TaxExempt,
                    p.TaxRate,
                    pu.AlternateBarcode,
                    COALESCE(u.Name, N'') AS UnitName,
                    pu.SalePrice AS UnitSalePrice,
                    pu.PurchasePrice AS UnitPurchasePrice,
                    pu.QuantityPerUnit,
                    pu.AllowFractionalSale,
                    pu.FractionalPartsPerUnit,
                    pu.FractionalPartName,
                    pu.IsBaseUnit,
                    pu.IsDefaultSaleUnit,
                    pu.IsDefaultPurchaseUnit,
                    p.SupplierId,
                    supplier.Name AS SupplierName
                FROM dbo.ProductUnit pu
                INNER JOIN dbo.Product p
                    ON p.Id = pu.ProductId
                   AND p.IsDeleted = 0
                LEFT JOIN dbo.[Unit] u
                    ON u.Id = pu.UnitId
                LEFT JOIN dbo.[User] supplier
                    ON supplier.Id = p.SupplierId
            ),
            LotTotals AS
            (
                SELECT
                    lot.ProductId,
                    SUM(
                        CASE
                            WHEN lot.RemainingBaseQuantity > 0
                                THEN lot.RemainingBaseQuantity
                            ELSE lot.RemainingQuantity *
                                CASE
                                    WHEN lot.QuantityPerUnitSnapshot > 0
                                        THEN lot.QuantityPerUnitSnapshot
                                    ELSE 1
                                END
                        END) AS TotalBaseQuantity
                FROM dbo.StockLot lot
                WHERE lot.Status = 1
                  AND (lot.RemainingQuantity > 0 OR lot.RemainingBaseQuantity > 0)
                GROUP BY lot.ProductId
            ),
            RankedLots AS
            (
                SELECT
                    lot.ProductId,
                    lot.ProductUnitId,
                    lot.PurchasePrice,
                    lot.SalePrice,
                    lot.ExpiryDate,
                    ROW_NUMBER() OVER
                    (
                        PARTITION BY lot.ProductId, lot.ProductUnitId
                        ORDER BY
                            CASE WHEN lot.ExpiryDate IS NULL THEN 1 ELSE 0 END,
                            lot.ExpiryDate,
                            lot.CreatedDate
                    ) AS LotRank
                FROM dbo.StockLot lot
                WHERE lot.Status = 1
                  AND (lot.RemainingQuantity > 0 OR lot.RemainingBaseQuantity > 0)
            ),
            SourceRows AS
            (
                SELECT
                    unit.ProductId,
                    unit.ProductUnitId,
                    unit.SubCategoryId,
                    unit.ProductName,
                    unit.ItemCode,
                    unit.TaxExempt,
                    unit.TaxRate,
                    unit.AlternateBarcode,
                    unit.UnitName,
                    unit.UnitSalePrice,
                    unit.UnitPurchasePrice,
                    unit.QuantityPerUnit,
                    unit.AllowFractionalSale,
                    unit.FractionalPartsPerUnit,
                    unit.FractionalPartName,
                    unit.IsBaseUnit,
                    unit.IsDefaultSaleUnit,
                    unit.IsDefaultPurchaseUnit,
                    unit.SupplierId,
                    unit.SupplierName,
                    CONVERT(decimal(18,6),
                        CASE
                            WHEN COALESCE(total.TotalBaseQuantity, 0) > 0
                                THEN COALESCE(total.TotalBaseQuantity, 0) /
                                    CASE WHEN unit.QuantityPerUnit > 0 THEN unit.QuantityPerUnit ELSE 1 END
                            ELSE 0
                        END) AS StockQuantity,
                    COALESCE(preferred.PurchasePrice, unit.UnitPurchasePrice) AS StockPurchasePrice,
                    COALESCE(preferred.SalePrice, unit.UnitSalePrice) AS StockSalePrice,
                    preferred.ExpiryDate,
                    CONVERT(bit, CASE WHEN COALESCE(total.TotalBaseQuantity, 0) > 0 THEN 1 ELSE 0 END) AS AvailableToSale,
                    CONVERT(nvarchar(max),
                        CASE
                            WHEN COALESCE(total.TotalBaseQuantity, 0) <= 0 THEN N'OutOfStock'
                            WHEN preferred.ExpiryDate IS NOT NULL
                             AND CONVERT(date, preferred.ExpiryDate) < CONVERT(date, GETDATE()) THEN N'Expired'
                            ELSE N'Available'
                        END) AS StockStatus
                FROM ActiveUnits unit
                LEFT JOIN LotTotals total
                    ON total.ProductId = unit.ProductId
                LEFT JOIN RankedLots preferred
                    ON preferred.ProductId = unit.ProductId
                   AND preferred.ProductUnitId = unit.ProductUnitId
                   AND preferred.LotRank = 1
            )
            MERGE dbo.PosProductCatalogEntry AS target
            USING SourceRows AS source
                ON target.ProductId = source.ProductId
               AND target.ProductUnitId = source.ProductUnitId
            WHEN MATCHED THEN
                UPDATE SET
                    target.SubCategoryId = source.SubCategoryId,
                    target.ProductName = source.ProductName,
                    target.ItemCode = source.ItemCode,
                    target.TaxExempt = source.TaxExempt,
                    target.TaxRate = source.TaxRate,
                    target.AlternateBarcode = source.AlternateBarcode,
                    target.UnitName = source.UnitName,
                    target.UnitSalePrice = source.UnitSalePrice,
                    target.UnitPurchasePrice = source.UnitPurchasePrice,
                    target.QuantityPerUnit = source.QuantityPerUnit,
                    target.AllowFractionalSale = source.AllowFractionalSale,
                    target.FractionalPartsPerUnit = source.FractionalPartsPerUnit,
                    target.FractionalPartName = source.FractionalPartName,
                    target.IsBaseUnit = source.IsBaseUnit,
                    target.IsDefaultSaleUnit = source.IsDefaultSaleUnit,
                    target.IsDefaultPurchaseUnit = source.IsDefaultPurchaseUnit,
                    target.SupplierId = source.SupplierId,
                    target.SupplierName = source.SupplierName,
                    target.StockQuantity = source.StockQuantity,
                    target.StockPurchasePrice = source.StockPurchasePrice,
                    target.StockSalePrice = source.StockSalePrice,
                    target.ExpiryDate = source.ExpiryDate,
                    target.AvailableToSale = source.AvailableToSale,
                    target.StockStatus = source.StockStatus,
                    target.IsActive = 1,
                    target.UpdatedDate = SYSUTCDATETIME()
            WHEN NOT MATCHED BY TARGET THEN
                INSERT
                (
                    ProductId,
                    ProductUnitId,
                    SubCategoryId,
                    ProductName,
                    ItemCode,
                    TaxExempt,
                    TaxRate,
                    AlternateBarcode,
                    UnitName,
                    UnitSalePrice,
                    UnitPurchasePrice,
                    QuantityPerUnit,
                    AllowFractionalSale,
                    FractionalPartsPerUnit,
                    FractionalPartName,
                    IsBaseUnit,
                    IsDefaultSaleUnit,
                    IsDefaultPurchaseUnit,
                    SupplierId,
                    SupplierName,
                    StockQuantity,
                    StockPurchasePrice,
                    StockSalePrice,
                    ExpiryDate,
                    AvailableToSale,
                    StockStatus,
                    IsActive,
                    CreatedDate,
                    UpdatedDate
                )
                VALUES
                (
                    source.ProductId,
                    source.ProductUnitId,
                    source.SubCategoryId,
                    source.ProductName,
                    source.ItemCode,
                    source.TaxExempt,
                    source.TaxRate,
                    source.AlternateBarcode,
                    source.UnitName,
                    source.UnitSalePrice,
                    source.UnitPurchasePrice,
                    source.QuantityPerUnit,
                    source.AllowFractionalSale,
                    source.FractionalPartsPerUnit,
                    source.FractionalPartName,
                    source.IsBaseUnit,
                    source.IsDefaultSaleUnit,
                    source.IsDefaultPurchaseUnit,
                    source.SupplierId,
                    source.SupplierName,
                    source.StockQuantity,
                    source.StockPurchasePrice,
                    source.StockSalePrice,
                    source.ExpiryDate,
                    source.AvailableToSale,
                    source.StockStatus,
                    1,
                    SYSUTCDATETIME(),
                    SYSUTCDATETIME()
                )
            WHEN NOT MATCHED BY SOURCE THEN
                UPDATE SET
                    target.IsActive = 0,
                    target.UpdatedDate = SYSUTCDATETIME();
            """;

        public PosProductCatalogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<PosProductCatalogEntry>> GetCatalogAsync(
            CancellationToken cancellationToken = default)
        {
            var activeUnitCount = await _context.Set<RaccoonWarehouse.Domain.ProductUnits.ProductUnit>()
                .AsNoTracking()
                .CountAsync(unit => unit.Product != null && !unit.Product.IsDeleted, cancellationToken);
            var catalogCount = await _context.Set<PosProductCatalogEntry>()
                .AsNoTracking()
                .CountAsync(cancellationToken);

            if (catalogCount == 0 || catalogCount != activeUnitCount)
                await RefreshAllAsync(cancellationToken);

            return await _context.Set<PosProductCatalogEntry>()
                .AsNoTracking()
                .Where(entry => entry.IsActive)
                .OrderBy(entry => entry.ProductName)
                .ThenBy(entry => entry.UnitName)
                .ToListAsync(cancellationToken);
        }

        public async Task RefreshAllAsync(CancellationToken cancellationToken = default)
        {
            await _context.Database.ExecuteSqlRawAsync(RebuildCatalogSql, cancellationToken);
        }

        public async Task RefreshProductsAsync(
            IEnumerable<int> productIds,
            CancellationToken cancellationToken = default)
        {
            var ids = productIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();
            if (ids.Count == 0)
                return;

            var units = await _context.Set<RaccoonWarehouse.Domain.ProductUnits.ProductUnit>()
                .AsNoTracking()
                .Where(unit => ids.Contains(unit.ProductId) &&
                              unit.Product != null &&
                              !unit.Product.IsDeleted)
                .Select(unit => new UnitSnapshot
                {
                    ProductId = unit.ProductId,
                    ProductUnitId = unit.Id,
                    SubCategoryId = unit.Product!.SubCategoryId,
                    ProductName = unit.Product.Name,
                    ItemCode = unit.Product.ITEMCODE,
                    TaxExempt = unit.Product.TaxExempt,
                    TaxRate = unit.Product.TaxRate,
                    AlternateBarcode = unit.AlternateBarcode,
                    UnitName = unit.Unit != null ? unit.Unit.Name : string.Empty,
                    UnitSalePrice = unit.SalePrice,
                    UnitPurchasePrice = unit.PurchasePrice,
                    QuantityPerUnit = unit.QuantityPerUnit,
                    AllowFractionalSale = unit.AllowFractionalSale,
                    FractionalPartsPerUnit = unit.FractionalPartsPerUnit,
                    FractionalPartName = unit.FractionalPartName,
                    IsBaseUnit = unit.IsBaseUnit,
                    IsDefaultSaleUnit = unit.IsDefaultSaleUnit,
                    IsDefaultPurchaseUnit = unit.IsDefaultPurchaseUnit,
                    SupplierId = unit.Product.SupplierId,
                    SupplierName = unit.Product.Supplier != null ? unit.Product.Supplier.Name : null
                })
                .ToListAsync(cancellationToken);

            var lots = await _context.Set<StockLot>()
                .AsNoTracking()
                .Where(lot => ids.Contains(lot.ProductId) &&
                              lot.Status == RaccoonWarehouse.Domain.Enums.BatchStatus.Active &&
                              (lot.RemainingQuantity > 0 || lot.RemainingBaseQuantity > 0))
                .Select(lot => new LotSnapshot
                {
                    ProductId = lot.ProductId,
                    ProductUnitId = lot.ProductUnitId,
                    RemainingQuantity = lot.RemainingQuantity,
                    RemainingBaseQuantity = lot.RemainingBaseQuantity,
                    QuantityPerUnitSnapshot = lot.QuantityPerUnitSnapshot,
                    PurchasePrice = lot.PurchasePrice,
                    SalePrice = lot.SalePrice,
                    ExpiryDate = lot.ExpiryDate,
                    CreatedDate = lot.CreatedDate
                })
                .ToListAsync(cancellationToken);

            var lotsByProduct = lots
                .GroupBy(lot => lot.ProductId)
                .ToDictionary(group => group.Key, group => group.ToList());
            var lotsByUnit = lots
                .GroupBy(lot => (lot.ProductId, lot.ProductUnitId))
                .ToDictionary(group => group.Key, group => group.ToList());

            var existing = await _context.Set<PosProductCatalogEntry>()
                .Where(entry => ids.Contains(entry.ProductId))
                .ToListAsync(cancellationToken);
            var existingByKey = existing.ToDictionary(entry => (entry.ProductId, entry.ProductUnitId));
            var validKeys = units
                .Select(unit => (unit.ProductId, unit.ProductUnitId))
                .ToHashSet();

            foreach (var staleEntry in existing.Where(entry => !validKeys.Contains((entry.ProductId, entry.ProductUnitId))))
                _context.Set<PosProductCatalogEntry>().Remove(staleEntry);

            foreach (var unit in units)
            {
                lotsByProduct.TryGetValue(unit.ProductId, out var productLots);
                lotsByUnit.TryGetValue((unit.ProductId, unit.ProductUnitId), out var unitLots);
                productLots ??= new List<LotSnapshot>();
                unitLots ??= new List<LotSnapshot>();

                var totalBaseQuantity = productLots.Sum(GetRemainingBaseQuantity);
                var quantityPerUnit = unit.QuantityPerUnit > 0 ? unit.QuantityPerUnit : 1m;
                var stockQuantity = totalBaseQuantity / quantityPerUnit;
                var preferredLot = unitLots
                    .OrderBy(lot => lot.ExpiryDate.HasValue ? 0 : 1)
                    .ThenBy(lot => lot.ExpiryDate)
                    .ThenBy(lot => lot.CreatedDate)
                    .FirstOrDefault();
                var available = totalBaseQuantity > 0m;

                if (!existingByKey.TryGetValue((unit.ProductId, unit.ProductUnitId), out var entry))
                {
                    entry = new PosProductCatalogEntry
                    {
                        ProductId = unit.ProductId,
                        ProductUnitId = unit.ProductUnitId,
                        CreatedDate = DateTime.Now
                    };
                    _context.Set<PosProductCatalogEntry>().Add(entry);
                }

                entry.SubCategoryId = unit.SubCategoryId;
                entry.ProductName = unit.ProductName;
                entry.ItemCode = unit.ItemCode;
                entry.TaxExempt = unit.TaxExempt;
                entry.TaxRate = unit.TaxRate;
                entry.AlternateBarcode = unit.AlternateBarcode;
                entry.UnitName = unit.UnitName;
                entry.UnitSalePrice = unit.UnitSalePrice;
                entry.UnitPurchasePrice = unit.UnitPurchasePrice;
                entry.QuantityPerUnit = unit.QuantityPerUnit;
                entry.AllowFractionalSale = unit.AllowFractionalSale;
                entry.FractionalPartsPerUnit = unit.FractionalPartsPerUnit;
                entry.FractionalPartName = unit.FractionalPartName;
                entry.IsBaseUnit = unit.IsBaseUnit;
                entry.IsDefaultSaleUnit = unit.IsDefaultSaleUnit;
                entry.IsDefaultPurchaseUnit = unit.IsDefaultPurchaseUnit;
                entry.SupplierId = unit.SupplierId;
                entry.SupplierName = unit.SupplierName;
                entry.StockQuantity = Math.Max(0m, stockQuantity);
                entry.StockPurchasePrice = preferredLot?.PurchasePrice ?? unit.UnitPurchasePrice;
                entry.StockSalePrice = preferredLot?.SalePrice ?? unit.UnitSalePrice;
                entry.ExpiryDate = preferredLot?.ExpiryDate;
                entry.AvailableToSale = available;
                entry.StockStatus = !available
                    ? "OutOfStock"
                    : preferredLot?.ExpiryDate?.Date < DateTime.Today
                        ? "Expired"
                        : "Available";
                entry.IsActive = true;
                entry.UpdatedDate = DateTime.Now;
            }

            foreach (var staleEntry in existing.Where(entry => validKeys.Contains((entry.ProductId, entry.ProductUnitId))))
                staleEntry.IsActive = true;
        }

        private static decimal GetRemainingBaseQuantity(LotSnapshot lot)
        {
            if (lot.RemainingBaseQuantity > 0m)
                return lot.RemainingBaseQuantity;

            var factor = lot.QuantityPerUnitSnapshot > 0m ? lot.QuantityPerUnitSnapshot : 1m;
            return Math.Max(0m, lot.RemainingQuantity * factor);
        }

        private sealed class UnitSnapshot
        {
            public int ProductId { get; set; }
            public int ProductUnitId { get; set; }
            public int SubCategoryId { get; set; }
            public string? ProductName { get; set; }
            public long? ItemCode { get; set; }
            public bool? TaxExempt { get; set; }
            public decimal? TaxRate { get; set; }
            public string? AlternateBarcode { get; set; }
            public string? UnitName { get; set; }
            public decimal UnitSalePrice { get; set; }
            public decimal UnitPurchasePrice { get; set; }
            public decimal QuantityPerUnit { get; set; }
            public bool AllowFractionalSale { get; set; }
            public decimal FractionalPartsPerUnit { get; set; }
            public string? FractionalPartName { get; set; }
            public bool IsBaseUnit { get; set; }
            public bool IsDefaultSaleUnit { get; set; }
            public bool IsDefaultPurchaseUnit { get; set; }
            public int? SupplierId { get; set; }
            public string? SupplierName { get; set; }
        }

        private sealed class LotSnapshot
        {
            public int ProductId { get; set; }
            public int ProductUnitId { get; set; }
            public decimal RemainingQuantity { get; set; }
            public decimal RemainingBaseQuantity { get; set; }
            public decimal QuantityPerUnitSnapshot { get; set; }
            public decimal PurchasePrice { get; set; }
            public decimal SalePrice { get; set; }
            public DateTime? ExpiryDate { get; set; }
            public DateTime CreatedDate { get; set; }
        }
    }
}
