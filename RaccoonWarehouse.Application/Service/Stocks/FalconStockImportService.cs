using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.StockDocuments;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Domain.StockDocuments.DTOs;
using RaccoonWarehouse.Domain.StockItems.DTOs;
using RaccoonWarehouse.Domain.StockLots;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RaccoonWarehouse.Application.Service.Stocks
{
    #region Temporary Falcon API Integration

    public interface IFalconStockImportService
    {
        Task<Result<FalconStockImportResultDto>> ImportAsync(
            FalconStockImportRequestDto request,
            CancellationToken cancellationToken = default);
    }

    public sealed class FalconStockImportService : IFalconStockImportService
    {
        private const string FalconItemsUrl =
            "http://94.249.61.219:8085/Falcons/van.dll/itemsinfo?cono=290";
        private const string ImportNotes = "from valcon api";

        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        private readonly IUOW _uow;
        private readonly IStockDocumentService _stockDocumentService;
        private readonly IStockService _stockService;

        public FalconStockImportService(
            IUOW uow,
            IStockDocumentService stockDocumentService,
            IStockService stockService)
        {
            _uow = uow;
            _stockDocumentService = stockDocumentService;
            _stockService = stockService;
        }

        public async Task<Result<FalconStockImportResultDto>> ImportAsync(
            FalconStockImportRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.WarehouseId <= 0)
                return Result<FalconStockImportResultDto>.Fail("Warehouse is required.");

            List<FalconApiItem> apiItems;
            try
            {
                apiItems = await FetchItemsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                return Result<FalconStockImportResultDto>.Fail(
                    $"Failed to load Falcon stock: {ex.Message}");
            }

            var result = new FalconStockImportResultDto
            {
                ApiItemCount = apiItems.Count
            };

            var positiveApiItems = apiItems
                .Select(item => new
                {
                    Item = item,
                    Barcode = NormalizeBarcode(item.ItemCode),
                    IsValidQuantity = TryParsePositiveQuantity(item.Quantity, out var quantity),
                    Quantity = quantity
                })
                .Where(x => x.IsValidQuantity && !string.IsNullOrWhiteSpace(x.Barcode))
                .GroupBy(x => x.Barcode, StringComparer.Ordinal)
                .Select(group => group.Last())
                .ToDictionary(x => x.Barcode, x => x.Quantity, StringComparer.Ordinal);

            result.PositiveApiItemCount = positiveApiItems.Count;
            result.IgnoredItemCount = apiItems.Count - positiveApiItems.Count;

            if (positiveApiItems.Count == 0)
                return Result<FalconStockImportResultDto>.Ok(result, "No positive quantities were returned.");

            var productRepo = _uow.GetRepository<Product>();
            var products = await productRepo.GetAllAsQueryable()
                .AsNoTracking()
                .Include(product => product.ProductUnits)
                    .ThenInclude(unit => unit.Unit)
                .Where(product => !product.IsDeleted && product.ITEMCODE.HasValue)
                .ToListAsync(cancellationToken);

            var matchedProducts = products
                .Select(product => new
                {
                    Product = product,
                    Barcode = NormalizeBarcode(product.ITEMCODE?.ToString(CultureInfo.InvariantCulture))
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Barcode) && positiveApiItems.ContainsKey(x.Barcode))
                .GroupBy(x => x.Barcode, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList();

            result.MatchedProductCount = matchedProducts.Count;
            result.UnmatchedProductCount = positiveApiItems.Count - matchedProducts.Count;

            var matchedProductIds = matchedProducts.Select(x => x.Product.Id).ToList();
            var today = DateTime.Today;
            var lotRepo = _uow.GetRepository<StockLot>();
            var activeLots = await lotRepo.GetAllAsQueryable()
                .AsNoTracking()
                .Where(lot =>
                    matchedProductIds.Contains(lot.ProductId) &&
                    lot.Status == BatchStatus.Active &&
                    lot.RemainingBaseQuantity > 0 &&
                    (!lot.ExpiryDate.HasValue || lot.ExpiryDate.Value >= today))
                .ToListAsync(cancellationToken);

            var currentBaseQuantityByProduct = activeLots
                .GroupBy(lot => lot.ProductId)
                .ToDictionary(group => group.Key, group => group.Sum(lot => lot.RemainingBaseQuantity));

            var positiveMovements = new List<StockMovementPostDto>();
            var negativeMovements = new List<StockMovementPostDto>();
            var stockDocumentItems = new List<StockItemWriteDto>();
            var now = GetJordanNow();

            foreach (var match in matchedProducts)
            {
                var product = match.Product;
                var unit = SelectImportUnit(product.ProductUnits);
                if (unit == null)
                {
                    result.UnmatchedProductCount++;
                    result.MatchedProductCount--;
                    continue;
                }

                var factor = unit.QuantityPerUnit > 0 ? unit.QuantityPerUnit : 1m;
                var currentBaseQuantity = currentBaseQuantityByProduct.GetValueOrDefault(product.Id);
                var currentQuantity = currentBaseQuantity / factor;
                var targetQuantity = positiveApiItems[match.Barcode];
                var difference = targetQuantity - currentQuantity;

                if (Math.Abs(difference) < 0.0001m)
                {
                    result.UnchangedProductCount++;
                    continue;
                }

                var movement = new StockMovementPostDto
                {
                    ProductId = product.Id,
                    ProductUnitId = unit.Id,
                    Quantity = difference,
                    QuantityPerUnitSnapshot = factor,
                    BaseQuantity = difference * factor,
                    UnitPrice = difference > 0 ? 0m : unit.SalePrice,
                    PurchasePrice = difference > 0 ? 0m : unit.PurchasePrice,
                    SalePrice = unit.SalePrice,
                    TransactionType = TransactionType.Adjustment,
                    CasherId = request.UserId,
                    TransactionDate = now,
                    Notes = ImportNotes,
                    ReferenceNumber = "FALCON-API"
                };

                if (difference > 0)
                {
                    positiveMovements.Add(movement);
                    stockDocumentItems.Add(new StockItemWriteDto
                    {
                        ProductId = product.Id,
                        ProductUnitId = unit.Id,
                        ProductName = product.Name ?? string.Empty,
                        UnitName = unit.Unit?.Name ?? string.Empty,
                        Quantity = difference,
                        QuantityPerUnitSnapshot = factor,
                        BaseQuantity = difference * factor,
                        PurchasePrice = 0m,
                        SalePrice = unit.SalePrice,
                        CreatedDate = now,
                        UpdatedDate = now
                    });
                    result.IncreasedProductCount++;
                }
                else
                {
                    negativeMovements.Add(movement);
                    result.DecreasedProductCount++;
                }
            }

            if (stockDocumentItems.Count > 0)
            {
                var documentNumber = $"FALCON-{now:yyyyMMddHHmmssfff}";
                var documentResult = await _stockDocumentService.CreateAsync(new StockDocumentWriteDto
                {
                    DocumentNumber = documentNumber,
                    Type = StockVoucherType.In,
                    WarehouseId = request.WarehouseId,
                    Notes = ImportNotes,
                    Items = stockDocumentItems,
                    CreatedDate = now,
                    UpdatedDate = now
                });

                if (!documentResult.Success)
                {
                    return Result<FalconStockImportResultDto>.Fail(
                        documentResult.Message ?? "Failed to create Falcon stock-in voucher.");
                }

                result.StockDocumentId = documentResult.Data?.Id;
                result.StockDocumentNumber = documentNumber;
            }

            if (positiveMovements.Count > 0)
            {
                var positiveResult = await _stockService.PostMovementsAsync(positiveMovements);
                if (!positiveResult.Success)
                    return Result<FalconStockImportResultDto>.Fail(positiveResult.Message);
            }

            if (negativeMovements.Count > 0)
            {
                var negativeResult = await _stockService.PostMovementsAsync(negativeMovements);
                if (!negativeResult.Success)
                    return Result<FalconStockImportResultDto>.Fail(negativeResult.Message);
            }

            return Result<FalconStockImportResultDto>.Ok(result, "Falcon stock imported successfully.");
        }

        public static string NormalizeBarcode(string? barcode)
        {
            var normalized = barcode?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return string.Empty;

            if (normalized.All(char.IsDigit))
            {
                normalized = normalized.TrimStart('0');
                return normalized.Length == 0 ? "0" : normalized;
            }

            return normalized;
        }

        public static bool TryParsePositiveQuantity(string? value, out decimal quantity)
        {
            return decimal.TryParse(
                       value?.Trim(),
                       NumberStyles.Number,
                       CultureInfo.InvariantCulture,
                       out quantity) &&
                   quantity > 0;
        }

        private static ProductUnit? SelectImportUnit(IEnumerable<ProductUnit>? units)
        {
            var availableUnits = units?.ToList() ?? new List<ProductUnit>();
            return availableUnits.FirstOrDefault(unit => unit.IsDefaultSaleUnit)
                   ?? availableUnits.FirstOrDefault(unit => unit.IsBaseUnit)
                   ?? availableUnits.FirstOrDefault();
        }

        private static async Task<List<FalconApiItem>> FetchItemsAsync(CancellationToken cancellationToken)
        {
            using var response = await HttpClient.GetAsync(FalconItemsUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var itemsElement = document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement
                : document.RootElement.TryGetProperty("value", out var valueElement)
                    ? valueElement
                    : default;

            if (itemsElement.ValueKind != JsonValueKind.Array)
                throw new JsonException("Falcon API response does not contain an item array.");

            return JsonSerializer.Deserialize<List<FalconApiItem>>(
                       itemsElement.GetRawText(),
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? new List<FalconApiItem>();
        }

        private static DateTime GetJordanNow()
        {
            var jordanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, jordanTimeZone);
        }

        private sealed class FalconApiItem
        {
            [JsonPropertyName("ITEMCODE")]
            public string? ItemCode { get; set; }

            [JsonPropertyName("ITEMNAME")]
            public string? ItemName { get; set; }

            [JsonPropertyName("QTY")]
            public string? Quantity { get; set; }
        }
    }

    #endregion
}
