using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Helper;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Branches;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Warehouses;

const string defaultPath = @"\\Mac\Home\Downloads\purchase_invoices.xlsx";
var workbookPath = args.Length > 0 ? args[0] : defaultPath;

if (args.Any(x => string.Equals(x, "--cleanup", StringComparison.OrdinalIgnoreCase)))
{
    var cleanupOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer(DatabaseConnectionStringProvider.GetConnectionString())
        .Options;
    await using var cleanupDb = new ApplicationDbContext(cleanupOptions);
    var cleanupSqlPath = Path.Combine(AppContext.BaseDirectory, "PurchaseInvoiceCleanup.sql");
    if (!File.Exists(cleanupSqlPath))
        cleanupSqlPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "tools", "PurchaseInvoiceCleanup.sql"));
    await cleanupDb.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(cleanupSqlPath));
    Console.WriteLine("Invoice cleanup completed.");
    return;
}

if (!File.Exists(workbookPath))
    throw new FileNotFoundException("Purchase invoice workbook was not found.", workbookPath);

if (args.Any(x => string.Equals(x, "--all", StringComparison.OrdinalIgnoreCase)))
{
    await FullImporter.RunAsync(workbookPath, firstOnly: false);
    return;
}

if (args.Any(x => string.Equals(x, "--first", StringComparison.OrdinalIgnoreCase)))
{
    await FullImporter.RunAsync(workbookPath, firstOnly: true);
    return;
}

const string pilotInvoiceId = "A1000001";
var rows = (Path.GetExtension(workbookPath).Equals(".csv", StringComparison.OrdinalIgnoreCase)
        ? CsvReader.Read(workbookPath)
        : WorkbookReader.Read(workbookPath))
    .Where(x => x.InvoiceId == pilotInvoiceId)
    .ToList();

if (rows.Count == 0)
    throw new InvalidOperationException($"Source invoice {pilotInvoiceId} was not found in the workbook.");

var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer(DatabaseConnectionStringProvider.GetConnectionString())
    .Options;

await using var db = new ApplicationDbContext(options);
var mapper = new MapperConfiguration(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly)).CreateMapper();
using var uow = new UOW(db, mapper);
var currency = new CurrencyService(db);
var accounting = new AccountingService(db, uow, mapper, currency);
var invoiceService = new InvoiceService(db, uow, mapper, accounting);
var stockService = new StockService(db, uow, mapper, accounting);

var source = rows[0];
var supplierCandidates = await db.Set<User>().AsNoTracking()
    .Select(x => new { x.Id, x.Name, x.Role })
    .ToListAsync();
var normalizedSupplierName = TextNormalization.NormalizeName(source.Supplier);
var supplierMatch = supplierCandidates
    .Where(x => x.Role == UserRole.Supplier)
    .SingleOrDefault(x => TextNormalization.NormalizeName(x.Name) == normalizedSupplierName);
var warehouseCandidates = await db.Set<Warehouse>().AsNoTracking()
    .Select(x => new { x.Id, x.Code, x.Name, x.BranchId })
    .ToListAsync();
var branch = await db.Set<Branch>().AsNoTracking()
    .SingleOrDefaultAsync(x => x.Code == source.Branch);
var warehouse = warehouseCandidates.SingleOrDefault(x => x.Code == source.Branch);
if (warehouse == null && branch != null)
{
    var branchWarehouses = warehouseCandidates.Where(x => x.BranchId == branch.Id).ToList();
    if (branchWarehouses.Count == 1)
        warehouse = branchWarehouses[0];
}
if (warehouse == null && warehouseCandidates.Count == 1)
{
    warehouse = warehouseCandidates[0];
    Console.WriteLine($"Mapped source branch '{source.Branch}' to the only database warehouse: {warehouse.Code} ({warehouse.Name})");
}

int supplierId;
string supplierName;
if (supplierMatch == null)
{
    var candidates = supplierCandidates
        .Where(x => TextNormalization.NormalizeName(x.Name).Contains("ابنخلدون", StringComparison.OrdinalIgnoreCase)
            || TextNormalization.NormalizeName(source.Supplier).Contains(TextNormalization.NormalizeName(x.Name), StringComparison.OrdinalIgnoreCase)
            || TextNormalization.NormalizeName(x.Name).Contains(TextNormalization.NormalizeName(source.Supplier), StringComparison.OrdinalIgnoreCase))
        .Select(x => $"{x.Id}: {x.Name} (role={x.Role})")
        .ToList();
    var newSupplier = new User
    {
        Name = source.Supplier,
        Password = Guid.NewGuid().ToString("N"),
        Role = UserRole.Supplier,
        CreditStatus = CreditStatus.Active,
        CreatedDate = DateTime.Now,
        UpdatedDate = DateTime.Now
    };
    db.Set<User>().Add(newSupplier);
    await db.SaveChangesAsync();
    supplierId = newSupplier.Id;
    supplierName = newSupplier.Name;
    Console.WriteLine($"Created missing supplier for pilot: {supplierName} (Id={supplierId})");
}
else
{
    supplierId = supplierMatch.Id;
    supplierName = supplierMatch.Name;
}
if (warehouse == null)
{
    var warehouseSummary = string.Join(" | ", warehouseCandidates.Select(x => $"{x.Id}:{x.Code}:{x.Name}:branch={x.BranchId}"));
    throw new InvalidOperationException($"Warehouse was not matched by code or branch: '{source.Branch}'. Available: {warehouseSummary}");
}

var products = await db.Set<Product>()
    .Include(x => x.ProductUnits)
    .ToListAsync();
var barcodeProductGroups = products
    .SelectMany(product =>
    {
        var barcodes = new List<string>();
        if (product.ITEMCODE.HasValue)
            barcodes.Add(product.ITEMCODE.Value.ToString(CultureInfo.InvariantCulture));
        if (product.ProductUnits != null)
            barcodes.AddRange(product.ProductUnits
                .Select(unit => unit.AlternateBarcode?.Trim())
                .Where(barcode => !string.IsNullOrWhiteSpace(barcode))
                .Select(barcode => barcode!));
        return barcodes.Select(barcode => new { Barcode = barcode!, Product = product });
    })
    .GroupBy(x => x.Barcode, StringComparer.OrdinalIgnoreCase)
    .ToDictionary(x => x.Key, x => x.Select(y => y.Product).DistinctBy(y => y.Id).ToList(), StringComparer.OrdinalIgnoreCase);

IEnumerable<string> BarcodeCandidates(PurchaseRow row) => new[] { row.Barcode }
    .Concat(row.AllBarcodes.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    .Concat(new[] { row.ProductCode })
    .Select(x => x.Trim())
    .Where(x => x.Length > 0)
    .Distinct(StringComparer.OrdinalIgnoreCase);

Product? FindProduct(PurchaseRow row)
{
    foreach (var barcode in BarcodeCandidates(row))
    {
        if (barcodeProductGroups.TryGetValue(barcode, out var matches) && matches.Count == 1)
            return matches[0];
    }
    return null;
}

var missingProducts = rows
    .Where(x => FindProduct(x) == null)
    .Select(x => $"sourceId={x.ProductId}, barcodes={x.AllBarcodes}, name={x.ProductName}")
    .Distinct()
    .ToList();
if (missingProducts.Count > 0)
    throw new InvalidOperationException($"Products not matched by PRODUCT_CODE/ITEMCODE: {string.Join(" | ", missingProducts)}. No invoice was inserted.");

var mapped = new List<(PurchaseRow Row, Product Product, ProductUnit Unit)>();
var missingUnits = new List<int>();
foreach (var row in rows)
{
    var product = FindProduct(row)!;
    var unit = product.ProductUnits?
        .Where(x => x.IsDefaultPurchaseUnit)
        .OrderByDescending(x => x.IsBaseUnit)
        .FirstOrDefault()
        ?? product.ProductUnits?.OrderByDescending(x => x.IsBaseUnit).FirstOrDefault();

    if (unit == null)
        missingUnits.Add(row.ProductId);
    else
        mapped.Add((row, product, unit));
}

if (missingUnits.Count > 0)
    throw new InvalidOperationException($"Products without a purchase unit: {string.Join(", ", missingUnits.Distinct())}. No data was inserted.");

foreach (var item in mapped)
{
    var inferredFactor = InferQuantityPerUnit(item.Row);
    if (inferredFactor > 0m && item.Unit.QuantityPerUnit <= 1m)
        item.Unit.QuantityPerUnit = inferredFactor;

    if (!item.Product.ITEMCODE.HasValue && WorkbookReader.TryParseItemCode(item.Row.ProductCode, out var itemCode))
        item.Product.ITEMCODE = itemCode;

    if (string.IsNullOrWhiteSpace(item.Unit.AlternateBarcode))
    {
        var unitBarcode = string.IsNullOrWhiteSpace(item.Row.Barcode)
            ? BarcodeCandidates(item.Row).FirstOrDefault(code => !string.Equals(code, item.Row.ProductCode, StringComparison.OrdinalIgnoreCase))
            : item.Row.Barcode;
        if (!string.IsNullOrWhiteSpace(unitBarcode))
            item.Unit.AlternateBarcode = unitBarcode;
    }
}
await db.SaveChangesAsync();

foreach (var item in mapped.Where(x => x.Row.Quantity > 1m).Take(12))
{
    var units = string.Join(", ", item.Product.ProductUnits!.Select(unit =>
        $"unit={unit.Id},barcode={unit.AlternateBarcode},factor={unit.QuantityPerUnit},purchase={unit.IsDefaultPurchaseUnit},base={unit.IsBaseUnit}"));
    Console.WriteLine($"Database units for product {item.Product.Id} ({item.Product.Name}): {units}");
}

var duplicate = await db.Set<RaccoonWarehouse.Domain.Invoices.Invoice>()
    .AsNoTracking()
    .AnyAsync(x => x.InvoiceNumber == source.InvoiceNo);
if (duplicate)
    throw new InvalidOperationException($"Invoice number '{source.InvoiceNo}' already exists. No data was inserted.");

var taxRates = mapped
    .Select(x => x.Row.LineTotal == 0m ? 0m : Math.Round(x.Row.LineTax / x.Row.LineTotal * 100m, 3))
    .ToList();
var createdDate = DateTime.FromOADate(source.InvoiceDate).Date;
var invoiceLines = mapped.Select(x =>
{
    var taxRate = x.Row.LineTotal == 0m ? 0m : Math.Round(x.Row.LineTax / x.Row.LineTotal * 100m, 3);
    var factor = x.Unit.QuantityPerUnit > 0m ? x.Unit.QuantityPerUnit : 1m;
    return new InvoiceLineWriteDto
    {
        ProductId = x.Product.Id,
        ProductUnitId = x.Unit.Id,
        ProductName = x.Product.Name,
        UnitName = x.Unit.Unit?.Name,
        Quantity = x.Row.Quantity / factor,
        FreeQuantity = x.Row.Bonus / factor,
        QuantityPerUnitSnapshot = factor,
        BaseQuantity = x.Row.Quantity + x.Row.Bonus,
        UnitPrice = x.Row.CostPrice,
        UnitCost = x.Row.CostPrice,
        LineDiscountAmount = x.Row.LineDiscount,
        ExpiryDate = DateTime.FromOADate(x.Row.ExpiryDate).Date,
        SalePrice = x.Row.SalePrice,
        TaxExempt = x.Row.LineTax <= 0m,
        TaxRate = taxRate
    };
}).ToList();

var calculatedImportTotal = invoiceLines.Sum(line =>
{
    var lineTotal = Math.Max(0m, line.Quantity * line.UnitPrice - line.LineDiscountAmount);
    var taxRate = line.TaxExempt ? 0m : line.TaxRate;
    var tax = Math.Round(lineTotal * taxRate / 100m, 3);
    return Math.Round(lineTotal, 3) + tax;
});

var dto = new InvoiceWriteDto
{
    InvoiceNumber = source.InvoiceNo,
    FalconInvoiceNumber = source.InvoiceId,
    InvoiceType = InvoiceType.Purchase,
    PaymentType = PaymentType.Credit,
    SupplierId = supplierId,
    WarehouseId = warehouse.Id,
    DocumentDate = createdDate,
    CreatedDate = createdDate,
    UpdatedDate = DateTime.Now,
    CostPriceIncludesTax = false,
    Notes = $"Imported from purchase_invoices.xlsx; source invoice ID {source.InvoiceId}; source branch {source.Branch}",
    InvoiceLines = invoiceLines,
    Payments = new List<InvoicePaymentWriteDto>
    {
        new() { PaymentType = PaymentType.Credit, Amount = calculatedImportTotal }
    }
};

Console.WriteLine($"Pilot source: {source.InvoiceId}; supplier: {supplierName}; lines: {invoiceLines.Count}; warehouse: {warehouse.Code} ({warehouse.Id})");
Console.WriteLine($"Source total: {source.InvoiceNetTotal:0.000}; source tax rates: {string.Join(", ", taxRates.Distinct())}");
Console.WriteLine($"Application-calculated total: {calculatedImportTotal:0.000}");
Console.WriteLine($"Estimated pre-tax line total: {invoiceLines.Sum(x => x.Quantity * x.UnitPrice - x.LineDiscountAmount):0.000}");
foreach (var line in invoiceLines.Where(x => x.Quantity > 1m).Take(12))
    Console.WriteLine($"Unit conversion: product={line.ProductId}; quantity={line.Quantity}; factor={line.QuantityPerUnitSnapshot}; price={line.UnitPrice}; discount={line.LineDiscountAmount}");

var createResult = await invoiceService.CreateAsync(dto);
if (!createResult.Success || createResult.Data == null)
    throw new InvalidOperationException($"Invoice creation failed: {createResult.Message}");

var saved = createResult.Data;
var movements = invoiceLines.Select(line => new StockMovementPostDto
{
    ProductId = line.ProductId,
    ProductUnitId = line.ProductUnitId,
    Quantity = line.Quantity + line.FreeQuantity,
    QuantityPerUnitSnapshot = line.QuantityPerUnitSnapshot,
    BaseQuantity = (line.Quantity + line.FreeQuantity) * line.QuantityPerUnitSnapshot,
    UnitPrice = line.UnitPrice,
    PurchasePrice = line.UnitCost,
    SalePrice = line.SalePrice,
    ExpiryDate = line.ExpiryDate,
    TransactionType = TransactionType.Purchase,
    InvoiceId = saved.Id,
    TransactionDate = createdDate,
    ReferenceNumber = saved.InvoiceNumber,
    Notes = $"Imported purchase invoice #{saved.InvoiceNumber}",
                    UpdateCatalogAverageCost = false
}).ToList();

var stockResult = await stockService.PostMovementsAsync(movements);
if (!stockResult.Success)
    throw new InvalidOperationException($"Invoice {saved.Id} was created, but stock posting failed: {stockResult.Message}");

Console.WriteLine($"PILOT IMPORT COMPLETE: InvoiceId={saved.Id}; InvoiceNumber={saved.InvoiceNumber}; Lines={invoiceLines.Count}; Stock movements={movements.Count}");

static decimal InferQuantityPerUnit(PurchaseRow row)
{
    var denominator = row.LineTotal + row.LineDiscount;
    if (row.Quantity <= 0m || row.CostPrice <= 0m || denominator <= 0m)
        return 1m;

    var inferred = row.Quantity * row.CostPrice / denominator;
    var rounded = Math.Round(inferred, 0, MidpointRounding.AwayFromZero);
    return Math.Abs(inferred - rounded) <= 0.02m && rounded >= 1m
        ? rounded
        : Math.Max(1m, Math.Round(inferred, 6, MidpointRounding.AwayFromZero));
}

internal sealed record PurchaseRow(
    string InvoiceId,
    string InvoiceNo,
    double InvoiceDate,
    string Supplier,
    string Branch,
    decimal InvoiceNetTotal,
    int ProductId,
    string ProductCode,
    string ProductName,
    string Barcode,
    string AllBarcodes,
    decimal Quantity,
    decimal Bonus,
    decimal CostPrice,
    decimal LineDiscount,
    decimal LineTax,
    double ExpiryDate,
    decimal LineTotal,
    decimal SalePrice,
    string PaymentType);

internal static class TextNormalization
{
    public static string NormalizeName(string value)
    {
        return value.Trim()
            .Replace("أ", "ا", StringComparison.Ordinal)
            .Replace("إ", "ا", StringComparison.Ordinal)
            .Replace("آ", "ا", StringComparison.Ordinal)
            .Replace("ى", "ي", StringComparison.Ordinal)
            .Replace("ة", "ه", StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("/", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }
}

internal static class FullImporter
{
    public static async Task RunAsync(string workbookPath, bool firstOnly)
    {
        var rows = CsvReader.Read(workbookPath).ToList();
        var groups = rows.GroupBy(x => x.InvoiceId, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (firstOnly)
            groups = groups.Take(1).ToList();
        var rowsToImport = groups.SelectMany(x => x).ToList();
        if (groups.Count == 0)
            throw new InvalidOperationException("The CSV contains no invoices.");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(DatabaseConnectionStringProvider.GetConnectionString())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var mapper = new MapperConfiguration(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly)).CreateMapper();
        using var uow = new UOW(db, mapper);
        var accounting = new AccountingService(db, uow, mapper, new CurrencyService(db));
        var invoiceService = new InvoiceService(db, uow, mapper, accounting);
        var stockService = new StockService(db, uow, mapper, accounting);

        var users = await db.Set<User>().ToListAsync();
        var warehouseCandidates = await db.Set<Warehouse>().AsNoTracking()
            .Select(x => new { x.Id, x.Code, x.Name, x.BranchId })
            .ToListAsync();
        if (warehouseCandidates.Count == 0)
            throw new InvalidOperationException("No warehouse exists in the database.");

        var sourceBranch = groups.First().First().Branch;
        var warehouse = warehouseCandidates.SingleOrDefault(x => x.Code == sourceBranch);
        if (warehouse == null && warehouseCandidates.Count == 1)
            warehouse = warehouseCandidates[0];
        if (warehouse == null)
            throw new InvalidOperationException($"Source branch '{sourceBranch}' could not be mapped to one warehouse.");

        var products = await db.Set<Product>().Include(x => x.ProductUnits).ToListAsync();
        var barcodeProductGroups = products
            .SelectMany(product => GetProductBarcodes(product).Select(barcode => new { barcode, product }))
            .GroupBy(x => x.barcode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Select(y => y.product).DistinctBy(y => y.Id).ToList(), StringComparer.OrdinalIgnoreCase);
        var nameProductGroups = products
            .GroupBy(product => ProductNameKey(product.Name ?? string.Empty), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);
        var placeholderProducts = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);

        string SourceProductKey(PurchaseRow row) =>
            $"{row.ProductId}|{row.ProductCode}|{row.Barcode}|{row.ProductName}";

        Product? FindProduct(PurchaseRow row)
        {
            if (placeholderProducts.TryGetValue(SourceProductKey(row), out var placeholder))
                return placeholder;

            foreach (var barcode in GetRowBarcodes(row))
            {
                if (barcodeProductGroups.TryGetValue(barcode, out var matches) && matches.Count == 1)
                    return matches[0];
            }
            var nameKey = ProductNameKey(row.ProductName);
            if (nameProductGroups.TryGetValue(nameKey, out var nameMatches) && nameMatches.Count == 1)
            {
                Console.WriteLine($"Matched by exact normalized name: {row.ProductName} -> {nameMatches[0].Id}");
                return nameMatches[0];
            }
            return null;
        }

        var missingRows = rowsToImport
            .Where(row => FindProduct(row) == null)
            .ToList();
        foreach (var missingGroup in missingRows.GroupBy(SourceProductKey, StringComparer.OrdinalIgnoreCase))
        {
            var source = missingGroup.First();
            var primaryBarcode = GetRowBarcodes(source).FirstOrDefault() ?? string.Empty;
            Product? placeholder = null;
            if (WorkbookReader.TryParseItemCode(source.ProductCode, out var placeholderItemCode))
            {
                placeholder = products.FirstOrDefault(product => product.ITEMCODE == placeholderItemCode);
                if (placeholder != null)
                    throw new InvalidOperationException($"Unmatched product code {source.ProductCode} already belongs to product {placeholder.Id} ({placeholder.Name}).");
            }

            placeholder = new Product
            {
                Name = $"[IMPORT PLACEHOLDER] {source.ProductName}",
                ITEMCODE = WorkbookReader.TryParseItemCode(source.ProductCode, out var itemCode) ? itemCode : null,
                SubCategoryId = 3,
                Status = ProductStatus.InStock,
                TaxExempt = false,
                TaxRate = 0m,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };
            placeholder.ProductUnits!.Add(new ProductUnit
            {
                AlternateBarcode = primaryBarcode,
                SalePrice = source.SalePrice,
                PurchasePrice = source.CostPrice,
                UnTaxedPrice = source.SalePrice,
                QuantityPerUnit = 1m,
                UnitId = 4,
                IsBaseUnit = true,
                IsDefaultSaleUnit = true,
                IsDefaultPurchaseUnit = true
            });
            db.Set<Product>().Add(placeholder);
            await db.SaveChangesAsync();
            products.Add(placeholder);
            placeholderProducts[missingGroup.Key] = placeholder;
            Console.WriteLine($"Created import placeholder: {placeholder.Id}; source={source.ProductId}; name={source.ProductName}");
        }

        var suppliers = new Dictionary<string, User?>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups)
        {
            var supplierName = group.First().Supplier.Trim();
            if (string.IsNullOrWhiteSpace(supplierName))
            {
                suppliers[group.Key] = null;
                continue;
            }
            var supplier = users.FirstOrDefault(x => x.Role == UserRole.Supplier && string.Equals(x.Name?.Trim(), supplierName, StringComparison.OrdinalIgnoreCase));
            if (supplier == null)
            {
                supplier = new User
                {
                    Name = supplierName,
                    Password = Guid.NewGuid().ToString("N"),
                    Role = UserRole.Supplier,
                    CreditStatus = CreditStatus.Active,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };
                db.Set<User>().Add(supplier);
                users.Add(supplier);
                await db.SaveChangesAsync();
                Console.WriteLine($"Created supplier: {supplier.Name} (Id={supplier.Id})");
            }
            suppliers[group.Key] = supplier;
        }

        foreach (var row in rowsToImport)
        {
            var product = FindProduct(row)!;
            var unit = product.ProductUnits?
                .Where(x => x.IsDefaultPurchaseUnit)
                .OrderByDescending(x => x.IsBaseUnit)
                .FirstOrDefault()
                ?? product.ProductUnits?.OrderByDescending(x => x.IsBaseUnit).FirstOrDefault();
            if (unit == null)
                throw new InvalidOperationException($"Product {product.Id} has no purchase unit.");

            var factor = InferQuantityPerUnit(row);
            if (unit.QuantityPerUnit <= 1m)
                unit.QuantityPerUnit = factor;
            if (!product.ITEMCODE.HasValue && WorkbookReader.TryParseItemCode(row.ProductCode, out var itemCode))
                product.ITEMCODE = itemCode;
            if (string.IsNullOrWhiteSpace(unit.AlternateBarcode))
                unit.AlternateBarcode = row.Barcode;
        }
        await db.SaveChangesAsync();

        var existingSourceIds = await db.Set<RaccoonWarehouse.Domain.Invoices.Invoice>()
            .Where(x => x.FalconInvoiceNumber != null)
            .Select(x => x.FalconInvoiceNumber!)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase);
        var usedInvoiceNumbers = (await db.Set<RaccoonWarehouse.Domain.Invoices.Invoice>()
            .Select(x => x.InvoiceNumber)
            .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var imported = 0;
        var skipped = 0;
        foreach (var group in groups)
        {
            if (existingSourceIds.Contains(group.Key))
            {
                skipped++;
                Console.WriteLine($"SKIP existing source invoice {group.Key}");
                continue;
            }

            var first = group.First();
            var invoiceNumber = first.InvoiceNo.Trim();
            if (usedInvoiceNumbers.Contains(invoiceNumber))
            {
                invoiceNumber = $"{invoiceNumber}-{group.Key}";
                var suffix = 2;
                while (usedInvoiceNumbers.Contains(invoiceNumber))
                    invoiceNumber = $"{first.InvoiceNo.Trim()}-{group.Key}-{suffix++}";
            }
            usedInvoiceNumbers.Add(invoiceNumber);

            var lines = group.Select(row =>
            {
                var product = FindProduct(row)!;
                var unit = product.ProductUnits!
                    .Where(x => x.IsDefaultPurchaseUnit)
                    .OrderByDescending(x => x.IsBaseUnit)
                    .First();
                var factor = unit.QuantityPerUnit > 0m ? unit.QuantityPerUnit : 1m;
                return new InvoiceLineWriteDto
                {
                    ProductId = product.Id,
                    ProductUnitId = unit.Id,
                    ProductName = product.Name,
                    UnitName = unit.Unit?.Name,
                    Quantity = row.Quantity / factor,
                    FreeQuantity = row.Bonus / factor,
                    QuantityPerUnitSnapshot = factor,
                    BaseQuantity = row.Quantity + row.Bonus,
                    UnitPrice = row.CostPrice,
                    UnitCost = row.CostPrice,
                    LineDiscountAmount = row.LineDiscount,
                    ExpiryDate = row.ExpiryDate == 0d ? default : DateTime.FromOADate(row.ExpiryDate).Date,
                    SalePrice = row.SalePrice,
                    TaxExempt = row.LineTax <= 0m,
                    TaxRate = row.LineTotal == 0m ? 0m : Math.Round(row.LineTax / row.LineTotal * 100m, 3)
                };
            }).ToList();

            var calculatedSubtotal = lines.Sum(line =>
                Math.Max(0m, line.Quantity * line.UnitPrice - line.LineDiscountAmount));
            var calculatedTax = lines.Sum(line =>
            {
                var lineTotal = Math.Max(0m, line.Quantity * line.UnitPrice - line.LineDiscountAmount);
                return line.TaxExempt ? 0m : Math.Round(lineTotal * line.TaxRate / 100m, 3);
            });
            var calculatedTotal = Math.Round(calculatedSubtotal + calculatedTax, 3);
            var dto = new InvoiceWriteDto
            {
                InvoiceNumber = invoiceNumber,
                FalconInvoiceNumber = group.Key,
                InvoiceType = InvoiceType.Purchase,
                PaymentType = ParsePaymentType(first.PaymentType),
                SupplierId = suppliers.TryGetValue(group.Key, out var invoiceSupplier) ? invoiceSupplier?.Id : null,
                WarehouseId = warehouse.Id,
                DocumentDate = DateTime.FromOADate(first.InvoiceDate).Date,
                CreatedDate = DateTime.FromOADate(first.InvoiceDate).Date,
                UpdatedDate = DateTime.Now,
                CostPriceIncludesTax = false,
                Notes = $"Imported from purchase_invoices_all_barcodes_payment.csv; source invoice ID {group.Key}; original invoice number {first.InvoiceNo}; source branch {first.Branch}",
                AllowMissingPurchaseExpiry = true,
                DeferAccountingPosting = calculatedTotal <= 0m,
                InvoiceLines = lines
            };

            var createResult = await invoiceService.CreateAsync(dto);
            if (!createResult.Success || createResult.Data == null)
                throw new InvalidOperationException($"Invoice {group.Key} ({invoiceNumber}) failed: {createResult.Message}");

            var saved = createResult.Data;
            var savedLines = (saved.InvoiceLines ?? throw new InvalidOperationException($"Invoice {group.Key} has no saved lines.")).OrderBy(line => line.Id).ToList();
            if (savedLines.Count != lines.Count)
                throw new InvalidOperationException($"Invoice {group.Key} line count mismatch after save.");

            var movements = lines.Select((line, index) => new StockMovementPostDto
            {
                ProductId = line.ProductId,
                ProductUnitId = line.ProductUnitId,
                Quantity = line.Quantity + line.FreeQuantity,
                QuantityPerUnitSnapshot = line.QuantityPerUnitSnapshot,
                BaseQuantity = (line.Quantity + line.FreeQuantity) * line.QuantityPerUnitSnapshot,
                UnitPrice = line.UnitPrice,
                PurchasePrice = line.UnitCost,
                SalePrice = line.SalePrice,
                ExpiryDate = line.ExpiryDate == default ? DateTime.MinValue : line.ExpiryDate,
                TransactionType = TransactionType.Purchase,
                InvoiceId = saved.Id,
                SourcePurchaseLineId = savedLines[index].Id,
                TransactionDate = dto.DocumentDate ?? DateTime.Now,
                ReferenceNumber = saved.InvoiceNumber,
                Notes = $"Imported purchase invoice #{saved.InvoiceNumber}",
                UpdateCatalogAverageCost = false
            }).ToList();
            var stockResult = await stockService.PostMovementsAsync(movements);
            if (!stockResult.Success)
                throw new InvalidOperationException($"Invoice {group.Key} ({saved.InvoiceNumber}) created, but stock posting failed: {stockResult.Message}");

            imported++;
            Console.WriteLine($"IMPORTED {imported}/{groups.Count - skipped}: source={group.Key}; number={saved.InvoiceNumber}; lines={lines.Count}; total={calculatedTotal:0.000}");
        }

        Console.WriteLine($"FULL IMPORT COMPLETE: imported={imported}; skippedExisting={skipped}; totalSourceInvoices={groups.Count}");
    }

    private static PaymentType ParsePaymentType(string value) =>
        value.Trim().ToUpperInvariant() switch
        {
            "CASH" => PaymentType.Cash,
            "CREDIT" => PaymentType.Credit,
            _ => throw new InvalidOperationException($"Unsupported PAYMENT_TYPE '{value}'.")
        };

    private static IEnumerable<string> GetProductBarcodes(Product product)
    {
        if (product.ITEMCODE.HasValue)
            yield return product.ITEMCODE.Value.ToString(CultureInfo.InvariantCulture);
        foreach (var barcode in product.ProductUnits?.Select(x => x.AlternateBarcode?.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)) ?? Enumerable.Empty<string?>())
            yield return barcode!;
    }

    private static IEnumerable<string> GetRowBarcodes(PurchaseRow row) => new[] { row.Barcode }
        .Concat(row.AllBarcodes.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Concat(new[] { row.ProductCode })
        .Select(x => x.Trim())
        .Where(x => x.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase);

    private static string ProductNameKey(string value) => new string(value
        .Where(char.IsLetterOrDigit)
        .ToArray())
        .ToUpperInvariant();

    private static decimal InferQuantityPerUnit(PurchaseRow row)
    {
        var denominator = row.LineTotal + row.LineDiscount;
        if (row.Quantity <= 0m || row.CostPrice <= 0m || denominator <= 0m)
            return 1m;
        var inferred = row.Quantity * row.CostPrice / denominator;
        var rounded = Math.Round(inferred, 0, MidpointRounding.AwayFromZero);
        return Math.Abs(inferred - rounded) <= 0.02m && rounded >= 1m
            ? rounded
            : Math.Max(1m, Math.Round(inferred, 6, MidpointRounding.AwayFromZero));
    }
}

internal static class WorkbookReader
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public static IEnumerable<PurchaseRow> Read(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var strings = XElement.Load(archive.GetEntry("xl/sharedStrings.xml")!.Open())
            .Elements(Main + "si")
            .Select(x => x.Value)
            .ToArray();
        var workbook = XElement.Load(archive.GetEntry("xl/workbook.xml")!.Open());
        var rels = XElement.Load(archive.GetEntry("xl/_rels/workbook.xml.rels")!.Open());
        var firstSheet = workbook.Descendants(Main + "sheet").First();
        var relationId = (string?)firstSheet.Attribute(Relationships + "id")
            ?? throw new InvalidDataException("Workbook sheet relationship is missing.");
        var target = rels.Descendants().First(x => (string?)x.Attribute("Id") == relationId).Attribute("Target")!.Value;
        var sheetPath = target.StartsWith("xl/", StringComparison.Ordinal) ? target : "xl/" + target;
        var worksheet = XElement.Load(archive.GetEntry(sheetPath)!.Open());
        var rows = worksheet.Descendants(Main + "row").ToList();
        var headers = ReadCells(rows[0], strings).ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        var columns = headers.ToDictionary(x => x.Value, x => x.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows.Skip(1))
        {
            var cells = ReadCells(row, strings).ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
            string Get(string name) => cells.TryGetValue(columns[name], out var value) ? value : string.Empty;
            decimal Decimal(string name) => string.IsNullOrWhiteSpace(Get(name))
                ? 0m
                : decimal.Parse(Get(name), NumberStyles.Float, CultureInfo.InvariantCulture);
            double Double(string name) => string.IsNullOrWhiteSpace(Get(name))
                ? 0d
                : double.Parse(Get(name), NumberStyles.Float, CultureInfo.InvariantCulture);

            yield return new PurchaseRow(
                Get("INVOICE_ID"), Get("INVOICE_NO"), Double("INVOICE_DATE"), Get("SUPPLIER"), Get("BRANCH"),
                Decimal("INVOICE_NET_TOTAL"), int.Parse(Get("PRODUCT_ID"), CultureInfo.InvariantCulture), Get("PRODUCT_CODE"), Get("PRODUCT_NAME"), Get("BARCODE"), Get("ALL_BARCODES"), Decimal("QTY"),
                Decimal("BONUS"), Decimal("COST_PRICE"), Decimal("LINE_DISCOUNT"), Decimal("LINE_TAX"),
                Double("EXPIRY_DATE"), Decimal("LINE_TOTAL"), Decimal("SALE_PRICE"), Get("PAYMENT_TYPE"));
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> ReadCells(XElement row, string[] strings)
    {
        foreach (var cell in row.Elements(Main + "c"))
        {
            var reference = (string?)cell.Attribute("r") ?? string.Empty;
            var type = (string?)cell.Attribute("t");
            var value = (string?)cell.Element(Main + "v") ?? string.Empty;
            if (type == "s" && value.Length > 0)
                value = strings[int.Parse(value, CultureInfo.InvariantCulture)];
            yield return new KeyValuePair<string, string>(new string(reference.TakeWhile(char.IsLetter).ToArray()), value);
        }
    }

    public static bool TryParseItemCode(string value, out long itemCode)
    {
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out itemCode))
            return true;

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var numeric)
            && numeric >= long.MinValue && numeric <= long.MaxValue)
        {
            itemCode = Convert.ToInt64(numeric);
            return true;
        }

        itemCode = 0;
        return false;
    }
}

internal static class CsvReader
{
    public static IEnumerable<PurchaseRow> Read(string path)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var reader = new StreamReader(path, Encoding.GetEncoding(1256), detectEncodingFromByteOrderMarks: true);
        var headerLine = reader.ReadLine() ?? throw new InvalidDataException("CSV header is missing.");
        var headers = ParseLine(headerLine).ToArray();

        string Get(string[] values, string name)
        {
            var index = Array.FindIndex(headers, x => string.Equals(x.Trim(), name, StringComparison.OrdinalIgnoreCase));
            return index >= 0 && index < values.Length ? values[index].Trim() : string.Empty;
        }

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = ParseLine(line).ToArray();
            decimal Decimal(string name) => string.IsNullOrWhiteSpace(Get(values, name))
                ? 0m
                : decimal.Parse(Get(values, name), NumberStyles.Float, CultureInfo.InvariantCulture);
            double DateSerial(string name)
            {
                var value = Get(values, name);
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    return date.ToOADate();
                return string.IsNullOrWhiteSpace(value)
                    ? 0d
                    : double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            yield return new PurchaseRow(
                Get(values, "INVOICE_ID"), Get(values, "INVOICE_NO"), DateSerial("INVOICE_DATE"), Get(values, "SUPPLIER"), Get(values, "BRANCH"),
                Decimal("INVOICE_NET_TOTAL"), int.Parse(Get(values, "PRODUCT_ID"), CultureInfo.InvariantCulture), Get(values, "PRODUCT_CODE"), Get(values, "PRODUCT_NAME"), Get(values, "BARCODE"), Get(values, "ALL_BARCODES"), Decimal("QTY"),
                Decimal("BONUS"), Decimal("COST_PRICE"), Decimal("LINE_DISCOUNT"), Decimal("LINE_TAX"),
                DateSerial("EXPIRY_DATE"), Decimal("LINE_TOTAL"), Decimal("SALE_PRICE"), Get(values, "PAYMENT_TYPE"));
        }
    }

    private static IEnumerable<string> ParseLine(string line)
    {
        var value = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                {
                    value.Append('"');
                    i++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (ch == ',' && !quoted)
            {
                yield return value.ToString();
                value.Clear();
            }
            else
            {
                value.Append(ch);
            }
        }

        yield return value.ToString();
    }
}
