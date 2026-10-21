using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Application.Service.Sales;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Domain.StockAdjustments;
using RaccoonWarehouse.Domain.StockAdjustments.DTOs;
using RaccoonWarehouse.Domain.StockLots;
using RaccoonWarehouse.Domain.StockTransactions;
using RaccoonWarehouse.Domain.Units;
using RaccoonWarehouse.Domain.POS.DTOs;
using RaccoonWarehouse.Domain.POS;
using RaccoonWarehouse.Domain.SubCategories.DTOs;
using System.Diagnostics;

namespace RaccoonWarehouse.Application.Service.Stocks
{
    public class StockService : GenericService<Stock, StockWriteDto, StockReadDto>, IStockService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        private readonly IAccountingService? _accountingService;
        private readonly IInventoryCostingSettingsService? _inventoryCostingSettingsService;
        private readonly IPosProductCatalogService? _posProductCatalogService;
        private readonly IPosProductCatalogRefreshQueue? _posProductCatalogRefreshQueue;

        public StockService(ApplicationDbContext context, IUOW uow, IMapper mapper)
            : this(context, uow, mapper, null, null, null, null)
        {
        }

        public StockService(
            ApplicationDbContext context,
            IUOW uow,
            IMapper mapper,
            IAccountingService? accountingService,
            IInventoryCostingSettingsService? inventoryCostingSettingsService = null,
            IPosProductCatalogService? posProductCatalogService = null,
            IPosProductCatalogRefreshQueue? posProductCatalogRefreshQueue = null) : base(context, uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
            _accountingService = accountingService;
            _inventoryCostingSettingsService = inventoryCostingSettingsService;
            _posProductCatalogService = posProductCatalogService;
            _posProductCatalogRefreshQueue = posProductCatalogRefreshQueue;
        }

        public override Task<Result<StockWriteDto>> UpdateAsync(StockWriteDto dto)
        {
            return Task.FromResult(Result<StockWriteDto>.Fail(
                "Direct stock editing is blocked. Use the stock adjustment and batch replacement flow so invoice, costing, and reporting history remains unchanged."));
        }

        public async Task<Result<List<StockBatchLookupDto>>> GetBatchLookupAsync(int? productId = null)
        {
            var lotRepo = _uow.GetRepository<StockLot>();
            var transactionRepo = _uow.GetRepository<StockTransaction>();
            var adjustmentRepo = _uow.GetRepository<StockAdjustment>();

            var query = lotRepo.GetAllAsQueryable()
                .Include(x => x.Product)
                .Include(x => x.ProductUnit)
                    .ThenInclude(x => x.Unit)
                .AsQueryable();

            if (productId.HasValue)
                query = query.Where(x => x.ProductId == productId.Value);

            var lots = await query
                .OrderBy(x => x.Product!.Name)
                .ThenByDescending(x => x.CreatedDate)
                .ToListAsync();

            var lotIds = lots.Select(x => x.Id).ToList();
            var usedTransactionLotIds = await transactionRepo.GetAllAsQueryable()
                .Where(x => x.StockLotId.HasValue && lotIds.Contains(x.StockLotId.Value) && x.BaseQuantity < 0)
                .Select(x => x.StockLotId!.Value)
                .Distinct()
                .ToListAsync();

            var usedAdjustmentSourceLotIds = await adjustmentRepo.GetAllAsQueryable()
                .Where(x => lotIds.Contains(x.StockLotId))
                .Select(x => x.StockLotId)
                .Distinct()
                .ToListAsync();

            var usedAdjustmentNewLotIds = await adjustmentRepo.GetAllAsQueryable()
                .Where(x => x.NewStockLotId.HasValue && lotIds.Contains(x.NewStockLotId.Value))
                .Select(x => x.NewStockLotId!.Value)
                .Distinct()
                .ToListAsync();

            var usedLotIds = usedTransactionLotIds
                .Concat(usedAdjustmentSourceLotIds)
                .Concat(usedAdjustmentNewLotIds)
                .ToHashSet();

            var result = new List<StockBatchLookupDto>();
            foreach (var lot in lots)
            {
                result.Add(new StockBatchLookupDto
                {
                    StockLotId = lot.Id,
                    ProductId = lot.ProductId,
                    ProductName = lot.Product?.Name ?? $"#{lot.ProductId}",
                    ProductUnitId = lot.ProductUnitId,
                    UnitName = lot.ProductUnit?.Unit?.Name ?? $"#{lot.ProductUnitId}",
                    OriginalQuantity = lot.Quantity,
                    RemainingQuantity = lot.RemainingQuantity,
                    PurchasePrice = lot.PurchasePrice,
                    SalePrice = lot.SalePrice,
                    CreatedDate = lot.CreatedDate,
                    ExpiryDate = lot.ExpiryDate,
                    Status = lot.Status,
                    IsUsed = lot.RemainingQuantity != lot.Quantity ||
                             lot.Status != BatchStatus.Active ||
                             usedLotIds.Contains(lot.Id)
                });
            }

            return Result<List<StockBatchLookupDto>>.Ok(result);
        }

        public async Task<Result<decimal>> GetAvailableQuantityInUnitAsync(int productId, int productUnitId)
        {
            if (productId <= 0)
                return Result<decimal>.Fail("ProductId is required.");

            if (productUnitId <= 0)
                return Result<decimal>.Fail("ProductUnitId is required.");

            var unitRepo = _uow.GetRepository<ProductUnit>();
            var lotRepo = _uow.GetRepository<StockLot>();
            var today = DateTime.Today;

            var selectedUnit = await unitRepo.GetAllAsQueryable()
                .FirstOrDefaultAsync(unit => unit.Id == productUnitId && unit.ProductId == productId);

            if (selectedUnit == null)
                return Result<decimal>.Fail("Product unit not found.");

            var factor = selectedUnit.QuantityPerUnit > 0 ? selectedUnit.QuantityPerUnit : 1m;
            var totalBaseQuantity = await lotRepo.GetAllAsQueryable()
                .Where(lot =>
                    lot.ProductId == productId &&
                    lot.Status == BatchStatus.Active &&
                    (!lot.ExpiryDate.HasValue || lot.ExpiryDate.Value >= today) &&
                    (lot.RemainingBaseQuantity > 0 || lot.RemainingQuantity > 0)
                    )
                .Select(lot => lot.RemainingBaseQuantity > 0
                    ? lot.RemainingBaseQuantity
                    : lot.RemainingQuantity * (lot.QuantityPerUnitSnapshot > 0 ? lot.QuantityPerUnitSnapshot : 1m))
                .SumAsync();

            var availableQuantity = factor > 0 ? totalBaseQuantity / factor : totalBaseQuantity;
            return Result<decimal>.Ok(Math.Max(availableQuantity, 0m));
        }

        public async Task<Result<List<StockAvailabilityDto>>> GetAvailableQuantitiesInUnitsAsync(
            IEnumerable<StockAllocationRequestDto> requests)
        {
            var keys = requests?
                .Where(x => x != null && x.ProductId > 0 && x.ProductUnitId > 0)
                .Select(x => (x.ProductId, x.ProductUnitId))
                .Distinct()
                .ToList() ?? new List<(int ProductId, int ProductUnitId)>();

            if (keys.Count == 0)
                return Result<List<StockAvailabilityDto>>.Ok(new List<StockAvailabilityDto>());

            var unitIds = keys.Select(x => x.ProductUnitId).ToList();
            var productIds = keys.Select(x => x.ProductId).Distinct().ToList();
            var today = DateTime.Today;
            var unitRepo = _uow.GetRepository<ProductUnit>();
            var lotRepo = _uow.GetRepository<StockLot>();

            var factors = await unitRepo.GetAllAsQueryable()
                .Where(unit => unitIds.Contains(unit.Id))
                .Select(unit => new { unit.Id, unit.ProductId, unit.QuantityPerUnit })
                .ToListAsync();

            var totalsByProduct = await lotRepo.GetAllAsQueryable()
                .Where(lot => productIds.Contains(lot.ProductId) &&
                              lot.Status == BatchStatus.Active &&
                              (!lot.ExpiryDate.HasValue || lot.ExpiryDate.Value >= today) &&
                              (lot.RemainingBaseQuantity > 0 || lot.RemainingQuantity > 0)
                              )
                .GroupBy(lot => lot.ProductId)
                .Select(group => new
                {
                    ProductId = group.Key,
                    TotalBaseQuantity = group.Sum(lot => lot.RemainingBaseQuantity > 0
                        ? lot.RemainingBaseQuantity
                        : lot.RemainingQuantity * (lot.QuantityPerUnitSnapshot > 0 ? lot.QuantityPerUnitSnapshot : 1m))
                })
                .ToDictionaryAsync(x => x.ProductId, x => x.TotalBaseQuantity);

            var result = keys.Select(key =>
            {
                var factor = factors.FirstOrDefault(x => x.Id == key.ProductUnitId && x.ProductId == key.ProductId)?.QuantityPerUnit ?? 1m;
                var totalBaseQuantity = totalsByProduct.TryGetValue(key.ProductId, out var total) ? total : 0m;
                return new StockAvailabilityDto
                {
                    ProductId = key.ProductId,
                    ProductUnitId = key.ProductUnitId,
                    AvailableQuantity = Math.Max(totalBaseQuantity / (factor > 0 ? factor : 1m), 0m)
                };
            }).ToList();

            return Result<List<StockAvailabilityDto>>.Ok(result);
        }

        public async Task<Result<StockLotUpdateDto>> UpdateBatchMetadataAsync(StockLotUpdateDto dto)
        {
            var lotRepo = _uow.GetRepository<StockLot>();
            var lot = await lotRepo.GetAllAsQueryable()
                .FirstOrDefaultAsync(x => x.Id == dto.StockLotId);

            if (lot == null)
                return Result<StockLotUpdateDto>.Fail("Batch not found.");

            if (await IsLotUsedAsync(lot))
            {
                return Result<StockLotUpdateDto>.Fail(
                    "This batch has already been used in stock history. Direct updates are blocked. Use replacement or adjustment instead.");
            }

            if (dto.PurchasePrice.HasValue)
                lot.PurchasePrice = dto.PurchasePrice.Value;

            if (dto.SalePrice.HasValue)
                lot.SalePrice = dto.SalePrice.Value;

            lot.ExpiryDate = dto.ExpiryDate;

            if (!string.IsNullOrWhiteSpace(dto.Notes))
                lot.Notes = dto.Notes;

            if (dto.Status.HasValue)
                lot.Status = dto.Status.Value;

            lot.UpdatedDate = NowInJordan();

            await lotRepo.UpdateAsync(lot);
            await _uow.CommitAsync();
            await SyncStockSummaryAsync(_uow.GetRepository<Stock>(), lotRepo, lot.ProductId, lot.ProductUnitId, lot.UpdatedDate);
            await _uow.CommitAsync();

            return Result<StockLotUpdateDto>.Ok(dto, "Batch metadata updated safely.");
        }

        public async Task<Result<StockAdjustmentWriteDto>> CreateAdjustmentAsync(StockAdjustmentWriteDto dto)
        {
            var lotRepo = _uow.GetRepository<StockLot>();
            var transactionRepo = _uow.GetRepository<StockTransaction>();
            var adjustmentRepo = _uow.GetRepository<StockAdjustment>();
            var stockRepo = _uow.GetRepository<Stock>();
            var now = NowInJordan();

            var lot = await lotRepo.GetAllAsQueryable()
                .Include(x => x.Product)
                .Include(x => x.ProductUnit)
                    .ThenInclude(x => x.Unit)
                .FirstOrDefaultAsync(x => x.Id == dto.StockLotId);

            if (lot == null)
                return Result<StockAdjustmentWriteDto>.Fail("Batch not found.");

            if (lot.Status != BatchStatus.Active && dto.AdjustmentType is StockAdjustmentType.Increase or StockAdjustmentType.Decrease)
                return Result<StockAdjustmentWriteDto>.Fail("Only active batches can receive quantity adjustments.");

            var quantityPerUnit = lot.QuantityPerUnitSnapshot > 0 ? lot.QuantityPerUnitSnapshot : 1m;
            var absoluteQty = Math.Abs(dto.QuantityDelta);
            var effectiveQty = absoluteQty > 0 ? absoluteQty : lot.RemainingQuantity;
            var effectiveBaseQty = effectiveQty * quantityPerUnit;
            var adjustmentDate = dto.AdjustmentDate == default ? now : dto.AdjustmentDate;

            if (string.IsNullOrWhiteSpace(dto.Reason))
                return Result<StockAdjustmentWriteDto>.Fail("Adjustment reason is required.");

            var adjustment = new StockAdjustment
            {
                ProductId = lot.ProductId,
                ProductUnitId = lot.ProductUnitId,
                StockLotId = lot.Id,
                AdjustmentType = dto.AdjustmentType,
                QuantityDelta = dto.AdjustmentType == StockAdjustmentType.Decrease ? -effectiveQty : effectiveQty,
                QuantityPerUnitSnapshot = quantityPerUnit,
                BaseQuantityDelta = dto.AdjustmentType == StockAdjustmentType.Decrease ? -effectiveBaseQty : effectiveBaseQty,
                PurchasePrice = dto.PurchasePrice ?? lot.PurchasePrice,
                SalePrice = dto.SalePrice ?? lot.SalePrice,
                ExpiryDate = dto.ExpiryDate ?? lot.ExpiryDate,
                Reason = dto.Reason.Trim(),
                Reference = dto.Reference?.Trim(),
                AdjustmentDate = adjustmentDate,
                UserId = dto.UserId,
                CreatedDate = now,
                UpdatedDate = now
            };

            await adjustmentRepo.AddAsync(adjustment);

            StockLot? newLot = null;
            switch (dto.AdjustmentType)
            {
                case StockAdjustmentType.Increase:
                    if (effectiveQty <= 0)
                        return Result<StockAdjustmentWriteDto>.Fail("Increase quantity must be greater than zero.");

                    newLot = CreateLinkedLot(
                        lot,
                        effectiveQty,
                        quantityPerUnit,
                        dto.PurchasePrice ?? lot.PurchasePrice,
                        dto.SalePrice ?? lot.SalePrice,
                        dto.ExpiryDate ?? lot.ExpiryDate,
                        $"Adjustment increase from batch #{lot.Id}. {dto.Reason}",
                        now);

                    await lotRepo.AddAsync(newLot);
                    await transactionRepo.AddAsync(CreateTransaction(
                        lot.ProductId,
                        lot.ProductUnitId,
                        newLot,
                        adjustment,
                        effectiveQty,
                        effectiveBaseQty,
                        dto.PurchasePrice ?? lot.PurchasePrice,
                        dto.ExpiryDate ?? lot.ExpiryDate,
                        TransactionType.Adjustment,
                        adjustmentDate,
                        dto.Reference,
                        dto.Reason,
                        dto.UserId,
                        positiveMovement: true));
                    break;

                case StockAdjustmentType.Decrease:
                    if (effectiveQty <= 0)
                        return Result<StockAdjustmentWriteDto>.Fail("Decrease quantity must be greater than zero.");
                    if (effectiveQty > lot.RemainingQuantity)
                        return Result<StockAdjustmentWriteDto>.Fail("Decrease quantity exceeds remaining batch quantity.");

                    lot.RemainingQuantity -= effectiveQty;
                    lot.RemainingBaseQuantity -= effectiveBaseQty;
                    if (lot.RemainingQuantity <= 0)
                    {
                        lot.RemainingQuantity = 0;
                        lot.RemainingBaseQuantity = 0;
                        lot.Status = BatchStatus.Closed;
                        lot.ClosedDate = now;
                        lot.ClosedReason = dto.Reason;
                        lot.ClosedByUserId = dto.UserId;
                    }
                    lot.UpdatedDate = now;
                    await lotRepo.UpdateAsync(lot);

                    await transactionRepo.AddAsync(CreateTransaction(
                        lot.ProductId,
                        lot.ProductUnitId,
                        lot,
                        adjustment,
                        -effectiveQty,
                        -effectiveBaseQty,
                        dto.SalePrice ?? lot.SalePrice,
                        lot.ExpiryDate,
                        TransactionType.Adjustment,
                        adjustmentDate,
                        dto.Reference,
                        dto.Reason,
                        dto.UserId,
                        positiveMovement: false));
                    break;

                case StockAdjustmentType.Replace:
                case StockAdjustmentType.CloseAndRecreate:
                    var replacementQty = lot.RemainingQuantity > 0 ? lot.RemainingQuantity : effectiveQty;
                    if (replacementQty <= 0)
                        return Result<StockAdjustmentWriteDto>.Fail("Replacement quantity must be greater than zero.");

                    if (lot.RemainingQuantity > 0)
                    {
                        var oldRemainingBase = replacementQty * quantityPerUnit;
                        lot.RemainingQuantity -= replacementQty;
                        lot.RemainingBaseQuantity -= oldRemainingBase;

                        await transactionRepo.AddAsync(CreateTransaction(
                            lot.ProductId,
                            lot.ProductUnitId,
                            lot,
                            adjustment,
                            -replacementQty,
                            -oldRemainingBase,
                            lot.SalePrice,
                            lot.ExpiryDate,
                            TransactionType.Adjustment,
                            adjustmentDate,
                            dto.Reference,
                            $"Close batch #{lot.Id} before replacement. {dto.Reason}",
                            dto.UserId,
                            positiveMovement: false));
                    }

                    newLot = CreateLinkedLot(
                        lot,
                        replacementQty,
                        quantityPerUnit,
                        dto.PurchasePrice ?? lot.PurchasePrice,
                        dto.SalePrice ?? lot.SalePrice,
                        dto.ExpiryDate ?? lot.ExpiryDate,
                        $"Replacement for batch #{lot.Id}. {dto.Reason}",
                        now);

                    newLot.ReplacesStockLotId = lot.Id;
                    await lotRepo.AddAsync(newLot);

                    lot.Status = dto.AdjustmentType == StockAdjustmentType.Replace ? BatchStatus.Replaced : BatchStatus.Closed;
                    lot.ReplacedByStockLot = newLot;
                    lot.ReplacedByStockLotId = newLot.Id;
                    lot.ClosedDate = now;
                    lot.ClosedReason = dto.Reason;
                    lot.ClosedByUserId = dto.UserId;
                    lot.UpdatedDate = now;
                    lot.RemainingQuantity = Math.Max(lot.RemainingQuantity, 0);
                    lot.RemainingBaseQuantity = Math.Max(lot.RemainingBaseQuantity, 0);
                    await lotRepo.UpdateAsync(lot);

                    await transactionRepo.AddAsync(CreateTransaction(
                        lot.ProductId,
                        lot.ProductUnitId,
                        newLot,
                        adjustment,
                        replacementQty,
                        replacementQty * quantityPerUnit,
                        dto.SalePrice ?? lot.SalePrice,
                        dto.ExpiryDate ?? lot.ExpiryDate,
                        TransactionType.Adjustment,
                        adjustmentDate,
                        dto.Reference,
                        $"New replacement batch for #{lot.Id}. {dto.Reason}",
                        dto.UserId,
                        positiveMovement: true));
                    break;

                default:
                    return Result<StockAdjustmentWriteDto>.Fail("Unsupported adjustment type.");
            }

            await _uow.CommitAsync();

            if (newLot != null && lot.ReplacedByStockLotId == null)
            {
                lot.ReplacedByStockLotId = newLot.Id;
                adjustment.NewStockLotId = newLot.Id;
                adjustment.UpdatedDate = now;
                await lotRepo.UpdateAsync(lot);
                await adjustmentRepo.UpdateAsync(adjustment);
                await _uow.CommitAsync();
            }

            await SyncStockSummaryAsync(stockRepo, lotRepo, lot.ProductId, lot.ProductUnitId, now);
            await _uow.CommitAsync();

            dto.Id = adjustment.Id;
            dto.NewStockLotId = adjustment.NewStockLotId;
            dto.AdjustmentDate = adjustment.AdjustmentDate;
            dto.QuantityPerUnitSnapshot = quantityPerUnit;
            dto.BaseQuantityDelta = adjustment.BaseQuantityDelta;
            dto.CreatedDate = adjustment.CreatedDate;
            dto.UpdatedDate = adjustment.UpdatedDate;

            if (_accountingService != null)
            {
                var journalResult = await _accountingService.PostStockAdjustmentEntryAsync(dto);
                if (!journalResult.Success)
                    return Result<StockAdjustmentWriteDto>.Fail($"Stock adjustment saved but accounting posting failed: {journalResult.Message}");
            }

            return Result<StockAdjustmentWriteDto>.Ok(dto, "Stock adjustment saved successfully.");
        }

        public async Task<Result> PostMovementAsync(StockMovementPostDto dto)
        {
            return await PostMovementsAsync(new[] { dto });
        }

        public async Task<Result<List<StockLotAllocationDto>>> AllocateOutgoingAsync(IEnumerable<StockAllocationRequestDto> requests)
        {
            var items = requests?.Where(x => x != null && x.Quantity > 0).ToList() ?? new List<StockAllocationRequestDto>();
            if (items.Count == 0)
                return Result<List<StockLotAllocationDto>>.Ok(new List<StockLotAllocationDto>());

            var lotRepo = _uow.GetRepository<StockLot>();
            var unitRepo = _uow.GetRepository<ProductUnit>();
            var productRepo = _uow.GetRepository<Product>();
            var simulatedRemaining = new Dictionary<int, decimal>();
            var allocations = new List<StockLotAllocationDto>();

            var unitIds = items.Select(x => x.ProductUnitId).Distinct().ToList();
            var productIds = items.Select(x => x.ProductId).Distinct().ToList();
            var selectedUnits = await unitRepo.GetAllAsQueryable()
                .AsNoTracking()
                .Where(unit => unitIds.Contains(unit.Id))
                .ToDictionaryAsync(unit => unit.Id);

            var availableLots = await lotRepo.GetAllAsQueryable()
                .AsNoTracking()
                .Where(lot => productIds.Contains(lot.ProductId) &&
                              lot.Status == BatchStatus.Active &&
                               (lot.RemainingBaseQuantity > 0 || lot.RemainingQuantity > 0)
                              )
                .OrderBy(lot => lot.ProductId)
                .ThenBy(lot => lot.ExpiryDate == null ? 1 : 0)
                .ThenBy(lot => lot.ExpiryDate)
                .ThenBy(lot => lot.CreatedDate)
                .ToListAsync();
            var lotsByProduct = availableLots
                .GroupBy(lot => lot.ProductId)
                .ToDictionary(group => group.Key, group => group.ToList());
            var costingMethod = await GetCurrentCostingMethodAsync();
            // A single invoice may contain multiple lines for the same product.
            // Order the immutable query snapshot once per product instead of
            // sorting the same lot collection for every line.
            var orderedLotsByProduct = new Dictionary<int, IReadOnlyList<StockLot>>();

            foreach (var request in items)
            {
                selectedUnits.TryGetValue(request.ProductUnitId, out var selectedUnit);

                if (selectedUnit == null || selectedUnit.ProductId != request.ProductId)
                {
                    return Result<List<StockLotAllocationDto>>.Fail(
                        $"Product unit #{request.ProductUnitId} was not found for product #{request.ProductId}.");
                }

                var requestedFactor = selectedUnit.QuantityPerUnit > 0 ? selectedUnit.QuantityPerUnit : 1m;
                if (!orderedLotsByProduct.TryGetValue(request.ProductId, out var lots))
                {
                    var productLots = lotsByProduct.TryGetValue(request.ProductId, out var availableProductLots)
                        ? availableProductLots
                        : new List<StockLot>();
                    lots = OrderAvailableLots(productLots, costingMethod).ToList();
                    orderedLotsByProduct[request.ProductId] = lots;
                }
                var totalAvailableBaseQuantity = lots.Sum(GetEffectiveRemainingBaseQuantity);
                var availableQuantityAfterAllocation = Math.Max(
                    totalAvailableBaseQuantity - (request.Quantity * requestedFactor),
                    0m) / requestedFactor;
                var averagePurchasePrice = CalculateWeightedAverageUnitCost(lots, requestedFactor);

                var remainingRequiredBase = request.Quantity * requestedFactor;
                foreach (var lot in lots)
                {
                    var availableBase = simulatedRemaining.TryGetValue(lot.Id, out var cachedRemaining)
                        ? cachedRemaining
                        : GetEffectiveRemainingBaseQuantity(lot);

                    if (availableBase <= 0)
                        continue;

                    var allocatedBaseQuantity = Math.Min(availableBase, remainingRequiredBase);
                    if (allocatedBaseQuantity <= 0)
                        continue;

                    var allocatedQuantity = allocatedBaseQuantity / requestedFactor;
                    var purchasePrice = costingMethod == InventoryCostingMethod.WeightedAverage
                        ? averagePurchasePrice
                        : ConvertLotCostToRequestedUnit(lot, requestedFactor);
                    if (purchasePrice <= 0m)
                        purchasePrice = selectedUnit.PurchasePrice;
                    var salePrice = lot.SalePrice > 0m
                        ? lot.SalePrice
                        : selectedUnit.SalePrice;

                    allocations.Add(new StockLotAllocationDto
                    {
                        RequestIndex = request.RequestIndex,
                        StockLotId = lot.Id,
                        ProductId = request.ProductId,
                        ProductUnitId = request.ProductUnitId,
                        Quantity = allocatedQuantity,
                        QuantityPerUnitSnapshot = requestedFactor,
                        BaseQuantity = allocatedBaseQuantity,
                        AvailableQuantityAfterAllocation = availableQuantityAfterAllocation,
                        PurchasePrice = purchasePrice,
                        SalePrice = salePrice,
                        ExpiryDate = lot.ExpiryDate
                    });

                    simulatedRemaining[lot.Id] = availableBase - allocatedBaseQuantity;
                    remainingRequiredBase -= allocatedBaseQuantity;
                    if (remainingRequiredBase <= 0)
                        break;
                }

                if (remainingRequiredBase > 0)
                {
                    var product = await productRepo.GetByIdAsync(request.ProductId);
                    var unit = selectedUnit.UnitId > 0
                        ? await _uow.GetRepository<Unit>().GetByIdAsync(selectedUnit.UnitId)
                        : null;
                    var availableQuantity = request.Quantity - (remainingRequiredBase / requestedFactor);
                    var missingQuantity = remainingRequiredBase / requestedFactor;
                    var productName = !string.IsNullOrWhiteSpace(product?.Name)
                        ? product.Name
                        : $"Product #{request.ProductId}";
                    var barcode = product?.ITEMCODE?.ToString() ?? "Not available";
                    var unitName = !string.IsNullOrWhiteSpace(unit?.Name)
                        ? unit.Name
                        : $"Unit #{request.ProductUnitId}";

                    return Result<List<StockLotAllocationDto>>.Fail(
                        $"Insufficient stock.{Environment.NewLine}" +
                        $"Product: {productName}{Environment.NewLine}" +
                        $"Barcode: {barcode}{Environment.NewLine}" +
                        $"Unit: {unitName}{Environment.NewLine}" +
                        $"Requested quantity: {request.Quantity:0.###}{Environment.NewLine}" +
                        $"Available quantity: {availableQuantity:0.###}{Environment.NewLine}" +
                        $"Missing quantity: {missingQuantity:0.###}{Environment.NewLine}" +
                        "Increase the product stock or reduce the Box order quantity.");
                }
            }

            return Result<List<StockLotAllocationDto>>.Ok(allocations);
        }

        private static decimal GetEffectiveRemainingBaseQuantity(StockLot lot)
        {
            if (lot.RemainingBaseQuantity > 0)
                return lot.RemainingBaseQuantity;

            if (lot.RemainingQuantity <= 0)
                return 0m;

            var lotFactor = lot.QuantityPerUnitSnapshot > 0 ? lot.QuantityPerUnitSnapshot : 1m;
            return lot.RemainingQuantity * lotFactor;
        }

        private static decimal CalculateWeightedAverageUnitCost(
            IEnumerable<StockLot> lots,
            decimal requestedUnitFactor)
        {
            var availableLots = lots.Where(lot => GetEffectiveRemainingBaseQuantity(lot) > 0).ToList();
            var totalBaseQuantity = availableLots.Sum(GetEffectiveRemainingBaseQuantity);
            if (totalBaseQuantity <= 0)
                return 0m;

            var totalBaseValue = availableLots.Sum(lot =>
            {
                var lotFactor = lot.QuantityPerUnitSnapshot > 0 ? lot.QuantityPerUnitSnapshot : 1m;
                return GetEffectiveRemainingBaseQuantity(lot) * (lot.PurchasePrice / lotFactor);
            });
            var factor = requestedUnitFactor > 0 ? requestedUnitFactor : 1m;
            return Math.Round((totalBaseValue / totalBaseQuantity) * factor, 3);
        }

        public async Task<Result<PagedResult<PosBrowseItemDto>>> GetPosBrowsePageAsync(
            int pageNumber,
            int pageSize,
            string? searchText,
            int? subCategoryId)
        {
            if (pageNumber <= 0)
                pageNumber = 1;

            if (pageSize <= 0)
                pageSize = 60;

            var stockRepo = _uow.GetRepository<Stock>();
            var baseQuery = stockRepo.GetAllAsQueryable()
                .AsNoTracking()
                .Where(s => s.Quantity > 0 && s.Product != null);

            if (subCategoryId.HasValue)
                baseQuery = baseQuery.Where(s => s.Product!.SubCategoryId == subCategoryId.Value);

            var trimmed = searchText?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                baseQuery = baseQuery.Where(s =>
                    (s.Product!.Name != null && EF.Functions.Like(s.Product.Name, $"%{trimmed}%")) ||
                    s.Product.ITEMCODE.ToString().Contains(trimmed));
            }

            // Count distinct products for paging.
            var totalCount = await baseQuery
                .Select(s => s.ProductId)
                .Distinct()
                .CountAsync();

            // Get the page of product ids (stable order by product name + id).
            var productIdPage = await baseQuery
                .Select(s => new { s.ProductId, ProductName = s.Product!.Name })
                .Distinct()
                .OrderBy(x => x.ProductName)
                .ThenBy(x => x.ProductId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.ProductId)
                .ToListAsync();

            if (productIdPage.Count == 0)
            {
                return Result<PagedResult<PosBrowseItemDto>>.Ok(
                    new PagedResult<PosBrowseItemDto>(new List<PosBrowseItemDto>(), totalCount, pageNumber, pageSize));
            }

            // Fetch one "preferred stock" per product for display price, keeping the query lightweight.
            var preferredRows = await baseQuery
                .Where(s => productIdPage.Contains(s.ProductId))
                .Select(s => new
                {
                    s.ProductId,
                    ProductName = s.Product!.Name,
                    s.Product.ITEMCODE,
                    s.Product.SubCategoryId,
                    s.Product.TaxExempt,
                    s.Product.TaxRate,
                    s.SalePrice,
                    AverageCost = s.ProductUnit != null ? s.ProductUnit.PurchasePrice : 0m,
                    IsDefaultSaleUnit = s.ProductUnit != null && s.ProductUnit.IsDefaultSaleUnit,
                    QuantityPerUnit = s.ProductUnit != null && s.ProductUnit.QuantityPerUnit > 0
                        ? s.ProductUnit.QuantityPerUnit
                        : 1m,
                    s.Quantity,
                    s.ProductUnitId
                })
                .ToListAsync();

            // Display availability from active lots, not summary rows. Summary rows are
            // useful for fast stock screens, but they can be stale or contain more than
            // one selling unit. Lots are the source of truth for available base quantity.
            var availableBaseByProduct = await _uow.GetRepository<StockLot>().GetAllAsQueryable()
                .AsNoTracking()
                .Where(lot => productIdPage.Contains(lot.ProductId) &&
                             lot.Status == BatchStatus.Active &&
                             lot.RemainingBaseQuantity > 0
                             )
                .GroupBy(lot => lot.ProductId)
                .Select(group => new
                {
                    ProductId = group.Key,
                    TotalBaseQuantity = group.Sum(lot => lot.RemainingBaseQuantity)
                })
                .ToDictionaryAsync(x => x.ProductId, x => x.TotalBaseQuantity);

            var latestCostRows = await _uow.GetRepository<StockLot>().GetAllAsQueryable()
                .AsNoTracking()
                .Where(lot => productIdPage.Contains(lot.ProductId) && lot.PurchasePrice > 0)
                .Select(lot => new
                {
                    lot.ProductId,
                    lot.ProductUnitId,
                    lot.PurchasePrice,
                    lot.CreatedDate
                })
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            var latestCostByProductUnit = latestCostRows
                .GroupBy(x => (x.ProductId, x.ProductUnitId))
                .ToDictionary(g => g.Key, g => g.First().PurchasePrice);

            var itemsByProduct = preferredRows
                .GroupBy(x => x.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var preferred = g
                            .OrderByDescending(x => x.IsDefaultSaleUnit)
                            .ThenByDescending(x => x.Quantity)
                            .ThenBy(x => x.ProductUnitId)
                            .First();

                        return new PosBrowseItemDto
                        {
                            ProductId = preferred.ProductId,
                            Name = preferred.ProductName,
                            ItemCode = preferred.ITEMCODE,
                            SubCategoryId = preferred.SubCategoryId,
                            TaxExempt = preferred.TaxExempt,
                            TaxRate = preferred.TaxRate,
                            CurrentSalePrice = preferred.SalePrice,
                            LastCostIncludingTax = ToTaxInclusiveCost(
                                latestCostByProductUnit.TryGetValue(
                                    (preferred.ProductId, preferred.ProductUnitId), out var lastCost) ? lastCost : 0m,
                                preferred.TaxExempt,
                                preferred.TaxRate),
                            AverageCostIncludingTax = ToTaxInclusiveCost(
                                preferred.AverageCost,
                                preferred.TaxExempt,
                                preferred.TaxRate),
                            AvailableQuantity = availableBaseByProduct.TryGetValue(
                                preferred.ProductId,
                                out var totalBaseQuantity)
                                ? Math.Max(totalBaseQuantity / preferred.QuantityPerUnit, 0m)
                                : 0m
                        };
                    });

            // Preserve page order.
            var items = productIdPage
                .Where(itemsByProduct.ContainsKey)
                .Select(id => itemsByProduct[id])
                .ToList();

            return Result<PagedResult<PosBrowseItemDto>>.Ok(new PagedResult<PosBrowseItemDto>(items, totalCount, pageNumber, pageSize));
        }

        public Task<Result<List<PosProductSearchResultDto>>> SearchPosProductsAsync(
            string searchText,
            int maxResults = 100,
            int? supplierId = null)
            => SearchPosProductsAsync(searchText, null, maxResults, supplierId, CancellationToken.None);

        public async Task<Result<List<PosProductSearchResultDto>>> GetPosProductCatalogAsync(
            CancellationToken cancellationToken = default)
        {
            if (_posProductCatalogService == null)
                return Result<List<PosProductSearchResultDto>>.Fail("POS product catalog service is not configured.");

            var catalog = (await _posProductCatalogService.GetCatalogAsync(cancellationToken))
                .Select(entry => new PosProductSearchResultDto
                {
                    ProductId = entry.ProductId,
                    SubCategoryId = entry.SubCategoryId,
                    ProductName = entry.ProductName,
                    ItemCode = entry.ItemCode,
                    TaxExempt = entry.TaxExempt,
                    TaxRate = entry.TaxRate,
                    ProductUnitId = entry.ProductUnitId,
                    AlternateBarcode = entry.AlternateBarcode,
                    UnitName = entry.UnitName,
                    UnitSalePrice = entry.UnitSalePrice,
                    UnitPurchasePrice = entry.UnitPurchasePrice,
                    QuantityPerUnit = entry.QuantityPerUnit,
                    AllowFractionalSale = entry.AllowFractionalSale,
                    FractionalPartsPerUnit = entry.FractionalPartsPerUnit,
                    FractionalPartName = entry.FractionalPartName,
                    IsBaseUnit = entry.IsBaseUnit,
                    IsDefaultSaleUnit = entry.IsDefaultSaleUnit,
                    IsDefaultPurchaseUnit = entry.IsDefaultPurchaseUnit,
                    SupplierId = entry.SupplierId,
                    SupplierName = entry.SupplierName,
                    StockQuantity = entry.StockQuantity,
                    StockPurchasePrice = entry.StockPurchasePrice,
                    StockSalePrice = entry.StockSalePrice,
                    ExpiryDate = entry.ExpiryDate,
                    AvailableToSale = entry.AvailableToSale,
                    StockStatus = entry.StockStatus
                })
                .ToList();

            return Result<List<PosProductSearchResultDto>>.Ok(catalog);
        }

        public async Task<Result<List<PosProductSearchResultDto>>> GetPosProductStockAsync(
            IEnumerable<int> productIds,
            CancellationToken cancellationToken = default)
        {
            var ids = productIds.Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0)
                return Result<List<PosProductSearchResultDto>>.Ok(new List<PosProductSearchResultDto>());

            var stock = await _uow.GetRepository<StockLot>()
                    .GetAllAsQueryable()
                    .AsNoTracking()
                    .Where(lot => ids.Contains(lot.ProductId) &&
                                  lot.Status == BatchStatus.Active &&
                                  lot.RemainingQuantity > 0)
                    .GroupBy(lot => new { lot.ProductId, lot.ProductUnitId })
                    .Select(group => new PosProductSearchResultDto
                    {
                        ProductId = group.Key.ProductId,
                        ProductUnitId = group.Key.ProductUnitId,
                        StockQuantity = group.Sum(lot => lot.RemainingQuantity),
                        StockPurchasePrice = group.OrderBy(lot => lot.ExpiryDate == null ? 1 : 0)
                            .ThenBy(lot => lot.ExpiryDate)
                            .ThenBy(lot => lot.CreatedDate)
                            .Select(lot => lot.PurchasePrice)
                            .FirstOrDefault(),
                        StockSalePrice = group.OrderBy(lot => lot.ExpiryDate == null ? 1 : 0)
                            .ThenBy(lot => lot.ExpiryDate)
                            .ThenBy(lot => lot.CreatedDate)
                            .Select(lot => lot.SalePrice)
                            .FirstOrDefault(),
                        ExpiryDate = group.OrderBy(lot => lot.ExpiryDate == null ? 1 : 0)
                            .ThenBy(lot => lot.ExpiryDate)
                            .ThenBy(lot => lot.CreatedDate)
                            .Select(lot => lot.ExpiryDate)
                            .FirstOrDefault()
                    })
                .ToListAsync(cancellationToken);

            return Result<List<PosProductSearchResultDto>>.Ok(stock);
        }

        private static string EscapeLikeFragment(string fragment)
            => fragment
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_")
                .Replace("[", "\\[")
                .Replace("]", "\\]");

        public async Task<Result<List<PosProductSearchResultDto>>> SearchPosProductsAsync(
            string? productName,
            string? barcode,
            int maxResults = 100,
            int? supplierId = null,
            CancellationToken cancellationToken = default)
        {
            var terms = (productName ?? string.Empty)
                .Trim()
                .Split(new[] { ' ', '\t', '\r', '\n', '%' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var normalizedBarcode = barcode?.Trim();

            if (maxResults <= 0 || (terms.Length == 0 && string.IsNullOrWhiteSpace(normalizedBarcode) && (!supplierId.HasValue || supplierId.Value <= 0)))
                return Result<List<PosProductSearchResultDto>>.Ok(new List<PosProductSearchResultDto>());

            // Search the product catalog first, not the stock table. A product with
            // zero stock (or no stock record yet) must still be visible in POS so
            // the cashier can identify it and see that it is unavailable.
            var productQuery = _uow.GetRepository<Product>()
                .GetAllAsQueryable()
                .AsNoTracking()
                .Include(product => product.Supplier)
                .Include(product => product.ProductUnits!)
                    .ThenInclude(unit => unit.Unit)
                .Where(product => !product.IsDeleted && product.ProductUnits != null);

            if (supplierId.HasValue && supplierId.Value > 0)
                productQuery = productQuery.Where(product => product.SupplierId == supplierId.Value);

            if (!string.IsNullOrWhiteSpace(normalizedBarcode))
            {
                if (long.TryParse(normalizedBarcode, out var numericBarcode))
                {
                    productQuery = productQuery.Where(product =>
                        product.ITEMCODE == numericBarcode ||
                        product.ProductUnits!.Any(unit => unit.AlternateBarcode == normalizedBarcode));
                }
                else
                {
                    productQuery = productQuery.Where(product =>
                        product.ProductUnits!.Any(unit => unit.AlternateBarcode == normalizedBarcode));
                }
            }
            else
            {
                var orderedPattern = "%" + string.Join("%", terms.Select(EscapeLikeFragment)) + "%";
                productQuery = productQuery.Where(product =>
                    product.Name != null &&
                    EF.Functions.Like(product.Name, orderedPattern, "\\"));
            }

            var products = await productQuery
                .OrderBy(product => product.Name)
                .Take(maxResults)
                .ToListAsync(cancellationToken);

            if (products.Count == 0)
                return Result<List<PosProductSearchResultDto>>.Ok(new List<PosProductSearchResultDto>());

            var productIds = products.Select(product => product.Id).ToList();
            // Use active stock lots as the source of truth for POS search. The
            // summary Stock table does not carry expiry information and can be
            // stale after purchase/return operations. Expired lots remain
            // visible here intentionally; POS handles the confirmation before
            // completing a sale.
            var stockLotQuery = _uow.GetRepository<StockLot>()
                .GetAllAsQueryable()
                .AsNoTracking()
                .Where(lot => productIds.Contains(lot.ProductId) &&
                              lot.Status == BatchStatus.Active &&
                              lot.RemainingQuantity > 0);

            var stockByUnit = (await stockLotQuery
                .GroupBy(lot => new { lot.ProductId, lot.ProductUnitId })
                .Select(group => new
                {
                    group.Key.ProductId,
                    group.Key.ProductUnitId,
                    Quantity = group.Sum(lot => lot.RemainingQuantity),
                    PurchasePrice = group.OrderBy(lot => lot.ExpiryDate == null ? 1 : 0)
                        .ThenBy(lot => lot.ExpiryDate)
                        .ThenBy(lot => lot.CreatedDate)
                        .Select(lot => lot.PurchasePrice)
                        .FirstOrDefault(),
                    SalePrice = group.OrderBy(lot => lot.ExpiryDate == null ? 1 : 0)
                        .ThenBy(lot => lot.ExpiryDate)
                        .ThenBy(lot => lot.CreatedDate)
                        .Select(lot => lot.SalePrice)
                        .FirstOrDefault(),
                    ExpiryDate = group.OrderBy(lot => lot.ExpiryDate == null ? 1 : 0)
                        .ThenBy(lot => lot.ExpiryDate)
                        .ThenBy(lot => lot.CreatedDate)
                        .Select(lot => lot.ExpiryDate)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken))
                .ToDictionary(row => (row.ProductId, row.ProductUnitId));

            var baseQuantityByProduct = await stockLotQuery
                .GroupBy(lot => lot.ProductId)
                .Select(group => new
                {
                    ProductId = group.Key,
                    TotalBaseQuantity = group.Sum(lot => lot.RemainingBaseQuantity > 0
                        ? lot.RemainingBaseQuantity
                        : lot.RemainingQuantity * (lot.QuantityPerUnitSnapshot > 0 ? lot.QuantityPerUnitSnapshot : 1m))
                })
                .ToDictionaryAsync(row => row.ProductId, row => row.TotalBaseQuantity, cancellationToken);

            var rows = products
                .SelectMany(product => (product.ProductUnits ?? new List<ProductUnit>())
                    .Where(unit => unit != null)
                    .Select(unit =>
                    {
                        stockByUnit.TryGetValue((product.Id, unit.Id), out var stock);
                        return new PosProductSearchResultDto
                        {
                            ProductId = product.Id,
                    SubCategoryId = product.SubCategoryId,
                            ProductName = product.Name,
                            ItemCode = product.ITEMCODE,
                            TaxExempt = product.TaxExempt,
                            TaxRate = product.TaxRate,
                            ProductUnitId = unit.Id,
                            AlternateBarcode = unit.AlternateBarcode,
                            UnitName = unit.Unit?.Name,
                            UnitSalePrice = unit.SalePrice,
                            UnitPurchasePrice = unit.PurchasePrice,
                            QuantityPerUnit = unit.QuantityPerUnit,
                            IsBaseUnit = unit.IsBaseUnit,
                            IsDefaultSaleUnit = unit.IsDefaultSaleUnit,
                            IsDefaultPurchaseUnit = unit.IsDefaultPurchaseUnit,
                            StockQuantity = baseQuantityByProduct.TryGetValue(product.Id, out var totalBaseQuantity)
                                ? totalBaseQuantity / (unit.QuantityPerUnit > 0 ? unit.QuantityPerUnit : 1m)
                                : 0m,
                            StockPurchasePrice = stock?.PurchasePrice ?? unit.PurchasePrice,
                            StockSalePrice = stock?.SalePrice ?? unit.SalePrice,
                            AllowFractionalSale = unit.AllowFractionalSale,
                            FractionalPartsPerUnit = unit.FractionalPartsPerUnit,
                            FractionalPartName = unit.FractionalPartName,
                            ExpiryDate = stock?.ExpiryDate,
                            AvailableToSale = baseQuantityByProduct.TryGetValue(product.Id, out var availableBaseQuantity)
                                && availableBaseQuantity > 0m,
                            StockStatus = baseQuantityByProduct.TryGetValue(product.Id, out var statusBaseQuantity)
                                && statusBaseQuantity > 0m
                                ? (stock?.ExpiryDate?.Date < DateTime.Today ? "Expired" : "Available")
                                : "OutOfStock",
                            SupplierName = product.Supplier?.Name
                        };
                    }))
                .ToList();

            return Result<List<PosProductSearchResultDto>>.Ok(
                rows.GroupBy(row => row.ProductId)
                    .OrderBy(group => group.First().ProductName)
                    .SelectMany(group => group)
                    .ToList());
        }

        private static decimal ToTaxInclusiveCost(decimal cost, bool? taxExempt, decimal? taxRate)
        {
            if (cost <= 0 || taxExempt == true)
                return Math.Round(Math.Max(cost, 0m), 3);

            var rate = taxRate.GetValueOrDefault();
            return Math.Round(Math.Max(cost, 0m) * (1m + rate / 100m), 3);
        }

        public async Task<Result<List<SubCategoryReadDto>>> GetPosBrowseSubCategoriesAsync()
        {
            var stockRepo = _uow.GetRepository<Stock>();
            var query = stockRepo.GetAllAsQueryable()
                .AsNoTracking()
                .Where(s => s.Quantity > 0 && s.Product != null && s.Product.SubCategory != null)
                .Select(s => new
                {
                    s.Product!.SubCategory!.Id,
                    s.Product.SubCategory.Name
                })
                .Distinct()
                .OrderBy(s => s.Name);

            var subCategories = await query
                .Select(s => new SubCategoryReadDto { Id = s.Id, Name = s.Name })
                .ToListAsync();

            return Result<List<SubCategoryReadDto>>.Ok(subCategories);
        }

        public async Task<Result> PostMovementsAsync(
            IEnumerable<StockMovementPostDto> dtos,
            bool refreshCatalog = true)
        {
            var totalTiming = Stopwatch.StartNew();
            var stepTiming = Stopwatch.StartNew();
            var items = dtos?.ToList() ?? new List<StockMovementPostDto>();
            if (items.Count == 0)
                return Result.Ok("No stock movements to post.");

            var productIds = items
                .Select(item => item.ProductId)
                .Where(productId => productId > 0)
                .Distinct()
                .ToList();

            var errors = new List<string>();
            foreach (var dto in items)
            {
                if (dto.ProductId <= 0)
                    errors.Add("ProductId is required.");
                if (dto.ProductUnitId <= 0)
                    errors.Add("ProductUnitId is required.");
                if (dto.Quantity == 0)
                    errors.Add("Quantity cannot be zero.");
                if (dto.QuantityPerUnitSnapshot <= 0)
                    errors.Add("QuantityPerUnitSnapshot must be greater than zero.");
                if (dto.BaseQuantity == 0)
                    errors.Add("BaseQuantity cannot be zero.");
                if (dto.Quantity > 0 &&
                    dto.TransactionType == TransactionType.Purchase &&
                    !dto.ExpiryDate.HasValue)
                    errors.Add("ExpiryDate is required for purchase stock movements.");
            }

            if (errors.Count > 0)
                return Result.Fail("Invalid stock movement.", errors.Distinct().ToList());

            var costingMethod = await GetCurrentCostingMethodAsync();

            LogStockTiming("stock movement validation", totalTiming, stepTiming);

            var stockRepo = _uow.GetRepository<Stock>();
            var transactionRepo = _uow.GetRepository<StockTransaction>();
            var lotRepo = _uow.GetRepository<StockLot>();
            var productRepo = _uow.GetRepository<Product>();
            var productUnitRepo = _uow.GetRepository<ProductUnit>();
            var unitRepo = _uow.GetRepository<Unit>();
            var now = NowInJordan();
            var touchedKeys = new HashSet<(int ProductId, int ProductUnitId)>();
            var outgoingProductIds = items
                .Where(item => item.Quantity < 0)
                .Select(item => item.ProductId)
                .Distinct()
                .ToList();
            var outgoingLotQueryTiming = Stopwatch.StartNew();
            var outgoingLots = outgoingProductIds.Count == 0
                ? new List<StockLot>()
                : await lotRepo.GetAllAsQueryable()
                    .Where(lot => outgoingProductIds.Contains(lot.ProductId) &&
                                  lot.Status == BatchStatus.Active &&
                                  (!lot.ExpiryDate.HasValue || lot.ExpiryDate.Value >= now.Date) &&
                                  (lot.RemainingBaseQuantity > 0 || lot.RemainingQuantity > 0)
                                  )
                    .ToListAsync();
            LogStockTiming("outgoing stock lots batch query", totalTiming, outgoingLotQueryTiming);
            var outgoingLotsByProduct = outgoingLots
                .GroupBy(lot => lot.ProductId)
                .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var dto in items)
            {
                touchedKeys.Add((dto.ProductId, dto.ProductUnitId));

                if (dto.Quantity > 0)
                {
                    var quantityPerUnit = dto.QuantityPerUnitSnapshot > 0 ? dto.QuantityPerUnitSnapshot : 1m;
                    var baseQuantity = dto.BaseQuantity != 0 ? dto.BaseQuantity : dto.Quantity * quantityPerUnit;
                    var expiryDate = dto.ExpiryDate.HasValue && dto.ExpiryDate.Value > DateTime.MinValue.AddDays(1)
                        ? dto.ExpiryDate
                        : null;

                    var lot = new StockLot
                    {
                        ProductId = dto.ProductId,
                        ProductUnitId = dto.ProductUnitId,
                        Quantity = dto.Quantity,
                        RemainingQuantity = dto.Quantity,
                        QuantityPerUnitSnapshot = quantityPerUnit,
                        BaseQuantity = baseQuantity,
                        RemainingBaseQuantity = baseQuantity,
                        PurchasePrice = dto.PurchasePrice ?? 0m,
                        SalePrice = dto.SalePrice ?? dto.UnitPrice,
                        ExpiryDate = expiryDate,
                        Notes = dto.Notes,
                        Status = BatchStatus.Active,
                        CreatedDate = now,
                        UpdatedDate = now
                    };

                    await lotRepo.AddAsync(lot);

                    await transactionRepo.AddAsync(new StockTransaction
                    {
                        ProductId = dto.ProductId,
                        ProductUnitId = dto.ProductUnitId,
                        StockLot = lot,
                        Quantity = dto.Quantity,
                        QuantityPerUnitSnapshot = quantityPerUnit,
                        BaseQuantity = baseQuantity,
                        UnitPrice = dto.UnitPrice,
                        ExpiryDate = expiryDate,
                        TransactionType = dto.TransactionType,
                        InvoiceId = dto.InvoiceId,
                        VoucherId = dto.VoucherId,
                        CasherId = dto.CasherId,
                        CashierSessionId = dto.CashierSessionId,
                        CustomerId = dto.CustomerId,
                        TransactionDate = dto.TransactionDate == default ? now : dto.TransactionDate,
                        Notes = dto.Notes,
                        ReferenceNumber = dto.ReferenceNumber,
                        CreatedDate = now,
                        UpdatedDate = now
                    });
                }
                else
                {
                    var remainingToConsume = Math.Abs(dto.Quantity);
                    var requestedFactor = dto.QuantityPerUnitSnapshot > 0 ? dto.QuantityPerUnitSnapshot : 1m;
                    outgoingLotsByProduct.TryGetValue(dto.ProductId, out var availableLots);
                    var candidates = OrderAvailableLots(
                        GetAvailableLots(availableLots ?? Enumerable.Empty<StockLot>(), dto),
                        costingMethod,
                        dto).ToList();
                    var totalAvailableBeforeConsumption = candidates.Sum(GetEffectiveRemainingBaseQuantity) / requestedFactor;

                    if (!candidates.Any())
                    {
                        return Result.Fail(await BuildInsufficientStockMessageAsync(
                            productRepo,
                            productUnitRepo,
                            unitRepo,
                            dto.ProductId,
                            dto.ProductUnitId,
                            0m));
                    }

                    foreach (var lot in candidates)
                    {
                        if (remainingToConsume <= 0)
                            break;

                        var availableBase = GetEffectiveRemainingBaseQuantity(lot);
                        if (availableBase <= 0)
                            continue;

                        var consumedBaseQuantity = Math.Min(availableBase, remainingToConsume * requestedFactor);
                        if (consumedBaseQuantity <= 0)
                            continue;

                        var consumedQuantity = consumedBaseQuantity / requestedFactor;

                        lot.RemainingBaseQuantity = availableBase - consumedBaseQuantity;
                        lot.RemainingQuantity = lot.RemainingBaseQuantity / (lot.QuantityPerUnitSnapshot > 0 ? lot.QuantityPerUnitSnapshot : 1m);
                        if (lot.RemainingBaseQuantity <= 0)
                        {
                            lot.RemainingBaseQuantity = 0;
                            lot.RemainingQuantity = 0;
                            if (lot.Status == BatchStatus.Active)
                            {
                                lot.Status = BatchStatus.Closed;
                                lot.ClosedDate = now;
                                lot.ClosedReason = dto.Notes ?? "Batch fully consumed.";
                            }
                        }

                        lot.UpdatedDate = now;

                        await transactionRepo.AddAsync(new StockTransaction
                        {
                            ProductId = dto.ProductId,
                            ProductUnitId = dto.ProductUnitId,
                            StockLotId = lot.Id,
                            Quantity = -consumedQuantity,
                            QuantityPerUnitSnapshot = requestedFactor,
                            BaseQuantity = -consumedBaseQuantity,
                            UnitPrice = dto.UnitPrice != 0 ? dto.UnitPrice : lot.SalePrice,
                            ExpiryDate = lot.ExpiryDate,
                            TransactionType = dto.TransactionType,
                            InvoiceId = dto.InvoiceId,
                            VoucherId = dto.VoucherId,
                            CasherId = dto.CasherId,
                            CashierSessionId = dto.CashierSessionId,
                            CustomerId = dto.CustomerId,
                            TransactionDate = dto.TransactionDate == default ? now : dto.TransactionDate,
                            Notes = dto.Notes,
                            ReferenceNumber = dto.ReferenceNumber,
                            CreatedDate = now,
                            UpdatedDate = now
                        });

                        remainingToConsume -= consumedQuantity;
                    }

                    if (remainingToConsume > 0)
                    {
                        return Result.Fail(await BuildInsufficientStockMessageAsync(
                            productRepo,
                            productUnitRepo,
                            unitRepo,
                            dto.ProductId,
                            dto.ProductUnitId,
                            totalAvailableBeforeConsumption));
                    }
                }
            }

            LogStockTiming("stock lot and transaction preparation", totalTiming, stepTiming);

            var requiresInitialCommit = items.Any(item => item.Quantity > 0);
            if (requiresInitialCommit)
            {
                await _uow.CommitAsync();
                LogStockTiming("stock movement database save", totalTiming, stepTiming);
            }
            else
            {
                PosPerformanceLogger.Write(
                    "stock movement initial database save skipped",
                    0,
                    totalTiming.ElapsedMilliseconds);
            }

            await SyncStockSummariesAsync(stockRepo, lotRepo, touchedKeys, now);
            LogStockTiming("stock summary synchronization", totalTiming, stepTiming);

            var averageCostKeys = items
                .Where(item => item.Quantity > 0 &&
                               item.TransactionType == TransactionType.Purchase &&
                               item.UpdateCatalogAverageCost)
                .Select(item => (item.ProductId, item.ProductUnitId))
                .Distinct()
                .ToList();
            foreach (var key in averageCostKeys)
                await UpdateProductUnitAverageCostAsync(productUnitRepo, lotRepo, key.ProductId, key.ProductUnitId, now);
            LogStockTiming("product average cost synchronization", totalTiming, stepTiming);

            await _uow.CommitAsync();
            LogStockTiming("stock final database save", totalTiming, stepTiming);
            if (refreshCatalog)
                _posProductCatalogRefreshQueue?.Enqueue(productIds);
            PosPerformanceLogger.Write("stock posting total", totalTiming.ElapsedMilliseconds, totalTiming.ElapsedMilliseconds);
            return Result.Ok("Stock movement posted successfully.");
        }

        private static void LogStockTiming(string step, Stopwatch totalTiming, Stopwatch stepTiming)
        {
            var stepMilliseconds = stepTiming.ElapsedMilliseconds;
            var totalMilliseconds = totalTiming.ElapsedMilliseconds;
            PosPerformanceLogger.Write(step, stepMilliseconds, totalMilliseconds);
            Debug.WriteLine($"[POS timing] {step}: {stepMilliseconds} ms (total {totalMilliseconds} ms)");
            stepTiming.Restart();
        }

        private static async Task UpdateProductUnitAverageCostAsync(
            IGenericRepository<ProductUnit> productUnitRepo,
            IGenericRepository<StockLot> lotRepo,
            int productId,
            int productUnitId,
            DateTime now)
        {
            var unit = await productUnitRepo.GetAllAsQueryable()
                .FirstOrDefaultAsync(item => item.Id == productUnitId && item.ProductId == productId);
            if (unit == null)
                return;

            var lots = await lotRepo.GetAllAsQueryable()
                .Where(lot => lot.ProductId == productId &&
                              lot.Status == BatchStatus.Active &&
                              lot.RemainingBaseQuantity > 0)
                .ToListAsync();
            var totalBaseQuantity = lots.Sum(lot => lot.RemainingBaseQuantity);
            if (totalBaseQuantity <= 0)
                return;

            var totalBaseValue = lots.Sum(lot =>
            {
                var factor = lot.QuantityPerUnitSnapshot > 0 ? lot.QuantityPerUnitSnapshot : 1m;
                return lot.RemainingBaseQuantity * (lot.PurchasePrice / factor);
            });
            var selectedFactor = unit.QuantityPerUnit > 0 ? unit.QuantityPerUnit : 1m;
            unit.PurchasePrice = Math.Round((totalBaseValue / totalBaseQuantity) * selectedFactor, 3);
            unit.UpdatedDate = now;
            await productUnitRepo.UpdateAsync(unit);
        }

        private static IQueryable<StockLot> GetAvailableLotsQuery(
            IGenericRepository<StockLot> lotRepo,
            int productId,
            StockMovementPostDto? hint = null)
        {
            IQueryable<StockLot> query = lotRepo.GetAllAsQueryable()
                .Where(l => l.ProductId == productId &&
                            l.Status == BatchStatus.Active &&
                            (!l.ExpiryDate.HasValue || l.ExpiryDate.Value >= DateTime.Today) &&
                            (l.RemainingBaseQuantity > 0 || l.RemainingQuantity > 0)
                            );

            if (hint?.ProductUnitId > 0)
                query = query.Where(l => l.ProductUnitId == hint.ProductUnitId);

            if (hint?.StockLotId is > 0)
            {
                query = query
                    .OrderByDescending(l => l.Id == hint.StockLotId)
                    .ThenBy(l => l.ExpiryDate == null ? 1 : 0)
                    .ThenBy(l => l.ExpiryDate)
                    .ThenBy(l => l.CreatedDate);
                return query;
            }

            var hasHint = hint?.ExpiryDate.HasValue == true ||
                          hint?.PurchasePrice.HasValue == true ||
                          hint?.SalePrice.HasValue == true;

            if (hasHint)
            {
                query = query.OrderByDescending(l =>
                        l.ExpiryDate == hint!.ExpiryDate &&
                        (!hint.PurchasePrice.HasValue || l.PurchasePrice == hint.PurchasePrice.Value) &&
                        (!hint.SalePrice.HasValue || l.SalePrice == hint.SalePrice.Value))
                    .ThenBy(l => l.ExpiryDate == null ? 1 : 0)
                    .ThenBy(l => l.ExpiryDate)
                    .ThenBy(l => l.CreatedDate);
            }
            else
            {
                query = query
                    .OrderBy(l => l.ExpiryDate == null ? 1 : 0)
                    .ThenBy(l => l.ExpiryDate)
                    .ThenBy(l => l.CreatedDate);
            }

            return query;
        }

        private static IEnumerable<StockLot> GetAvailableLots(
            IEnumerable<StockLot> lots,
            StockMovementPostDto? hint = null)
        {
            var availableLots = lots.Where(lot =>
                lot.Status == BatchStatus.Active &&
                (!lot.ExpiryDate.HasValue || lot.ExpiryDate.Value.Date >= DateTime.Today) &&
                GetEffectiveRemainingBaseQuantity(lot) > 0
                );

            if (hint?.StockLotId is > 0)
            {
                return availableLots
                    .OrderByDescending(lot => lot.Id == hint.StockLotId)
                    .ThenBy(lot => lot.ExpiryDate == null ? 1 : 0)
                    .ThenBy(lot => lot.ExpiryDate)
                    .ThenBy(lot => lot.CreatedDate);
            }

            var hasHint = hint?.ExpiryDate.HasValue == true ||
                          hint?.PurchasePrice.HasValue == true ||
                          hint?.SalePrice.HasValue == true;

            if (hasHint)
            {
                return availableLots
                    .OrderByDescending(lot =>
                        lot.ExpiryDate == hint!.ExpiryDate &&
                        (!hint.PurchasePrice.HasValue || lot.PurchasePrice == hint.PurchasePrice.Value) &&
                        (!hint.SalePrice.HasValue || lot.SalePrice == hint.SalePrice.Value))
                    .ThenBy(lot => lot.ExpiryDate == null ? 1 : 0)
                    .ThenBy(lot => lot.ExpiryDate)
                    .ThenBy(lot => lot.CreatedDate);
            }

            return availableLots
                .OrderBy(lot => lot.ExpiryDate == null ? 1 : 0)
                .ThenBy(lot => lot.ExpiryDate)
                .ThenBy(lot => lot.CreatedDate);
        }

        private static IEnumerable<StockLot> OrderAvailableLots(
            IEnumerable<StockLot> lots,
            InventoryCostingMethod costingMethod,
            StockMovementPostDto? hint = null)
        {
            var ordered = lots.Where(lot =>
                lot.Status == BatchStatus.Active &&
                (!lot.ExpiryDate.HasValue || lot.ExpiryDate.Value.Date >= DateTime.Today) &&
                GetEffectiveRemainingBaseQuantity(lot) > 0
                );

            if (hint?.StockLotId is > 0)
            {
                return costingMethod switch
                {
                    InventoryCostingMethod.FIFO => ordered
                        .OrderByDescending(lot => lot.Id == hint.StockLotId)
                        .ThenBy(lot => lot.CreatedDate)
                        .ThenBy(lot => lot.Id),
                    InventoryCostingMethod.FEFO => ordered
                        .OrderByDescending(lot => lot.Id == hint.StockLotId)
                        .ThenBy(lot => lot.ExpiryDate == null ? 1 : 0)
                        .ThenBy(lot => lot.ExpiryDate)
                        .ThenBy(lot => lot.CreatedDate)
                        .ThenBy(lot => lot.Id),
                    _ => ordered
                        .OrderByDescending(lot => lot.Id == hint.StockLotId)
                        .ThenBy(lot => lot.ExpiryDate == null ? 1 : 0)
                        .ThenBy(lot => lot.ExpiryDate)
                        .ThenBy(lot => lot.CreatedDate)
                        .ThenBy(lot => lot.Id)
                };
            }

            return costingMethod switch
            {
                InventoryCostingMethod.FIFO => ordered
                    .OrderBy(lot => lot.CreatedDate)
                    .ThenBy(lot => lot.Id),
                InventoryCostingMethod.FEFO => ordered
                    .OrderBy(lot => lot.ExpiryDate == null ? 1 : 0)
                    .ThenBy(lot => lot.ExpiryDate)
                    .ThenBy(lot => lot.CreatedDate)
                    .ThenBy(lot => lot.Id),
                _ => ordered
                    .OrderBy(lot => lot.ExpiryDate == null ? 1 : 0)
                    .ThenBy(lot => lot.ExpiryDate)
                    .ThenBy(lot => lot.CreatedDate)
                    .ThenBy(lot => lot.Id)
            };
        }

        private async Task<InventoryCostingMethod> GetCurrentCostingMethodAsync()
        {
            if (_inventoryCostingSettingsService == null)
                return InventoryCostingMethod.WeightedAverage;

            return await _inventoryCostingSettingsService.GetCurrentMethodAsync();
        }

        private static decimal ConvertLotCostToRequestedUnit(StockLot lot, decimal requestedFactor)
        {
            var lotFactor = lot.QuantityPerUnitSnapshot > 0 ? lot.QuantityPerUnitSnapshot : 1m;
            var targetFactor = requestedFactor > 0 ? requestedFactor : 1m;
            return Math.Round((lot.PurchasePrice / lotFactor) * targetFactor, 3);
        }

        private async Task SyncStockSummariesAsync(
            IGenericRepository<Stock> stockRepo,
            IGenericRepository<StockLot> lotRepo,
            IEnumerable<(int ProductId, int ProductUnitId)> touchedKeys,
            DateTime now)
        {
            var keys = touchedKeys
                .Distinct()
                .ToList();
            if (keys.Count == 0)
                return;

            var productIds = keys.Select(key => key.ProductId).Distinct().ToList();
            var productUnitIds = keys.Select(key => key.ProductUnitId).Distinct().ToList();
            var lotsQueryTiming = Stopwatch.StartNew();
            var lots = await lotRepo.GetAllAsQueryable()
                .Where(l => productIds.Contains(l.ProductId) &&
                            productUnitIds.Contains(l.ProductUnitId) &&
                            l.Status == BatchStatus.Active &&
                            (!l.ExpiryDate.HasValue || l.ExpiryDate.Value >= now.Date) &&
                            l.RemainingQuantity > 0
                            )
                .ToListAsync();
            LogStockTiming("stock summary lots query", lotsQueryTiming, lotsQueryTiming);

            var stocksQueryTiming = Stopwatch.StartNew();
            var stocks = await stockRepo.GetAllAsQueryable()
                .Where(s => productIds.Contains(s.ProductId) &&
                            productUnitIds.Contains(s.ProductUnitId))
                .ToListAsync();
            LogStockTiming("stock summary records query", stocksQueryTiming, stocksQueryTiming);

            var recalculationTiming = Stopwatch.StartNew();
            var lotsByKey = lots
                .GroupBy(lot => (lot.ProductId, lot.ProductUnitId))
                .ToDictionary(group => group.Key, group => group.ToList());
            // Older data can contain multiple summary rows for the same product/unit,
            // especially when rows were created under different warehouses. The summary
            // synchronization key does not include warehouse, so choose the latest row
            // deterministically instead of crashing with a duplicate dictionary key.
            var stocksByKey = stocks
                .GroupBy(stock => (stock.ProductId, stock.ProductUnitId))
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(stock => stock.UpdatedDate)
                        .ThenByDescending(stock => stock.Id)
                        .First());

            foreach (var key in keys)
            {
                lotsByKey.TryGetValue(key, out var keyLots);
                keyLots ??= new List<StockLot>();

                var currentLot = keyLots
                    .OrderByDescending(lot => lot.CreatedDate)
                    .FirstOrDefault();
                var summaryFactor = currentLot?.QuantityPerUnitSnapshot > 0
                    ? currentLot.QuantityPerUnitSnapshot
                    : 1m;
                var weightedPurchasePrice = CalculateWeightedAverageUnitCost(keyLots, summaryFactor);

                if (!stocksByKey.TryGetValue(key, out var stock))
                {
                    await stockRepo.AddAsync(new Stock
                    {
                        ProductId = key.ProductId,
                        ProductUnitId = key.ProductUnitId,
                        Quantity = keyLots.Sum(lot => lot.RemainingQuantity),
                        PurchasePrice = weightedPurchasePrice,
                        SalePrice = currentLot?.SalePrice ?? 0m,
                        CreatedDate = now,
                        UpdatedDate = now
                    });
                    continue;
                }

                stock.Quantity = keyLots.Sum(lot => lot.RemainingQuantity);
                stock.PurchasePrice = weightedPurchasePrice;
                stock.SalePrice = currentLot?.SalePrice ?? 0m;
                stock.UpdatedDate = now;
            }

            LogStockTiming("stock summary in-memory recalculation", recalculationTiming, recalculationTiming);
        }

        private async Task SyncStockSummaryAsync(
            IGenericRepository<Stock> stockRepo,
            IGenericRepository<StockLot> lotRepo,
            int productId,
            int productUnitId,
            DateTime now)
        {
            var lots = await lotRepo.GetAllAsQueryable()
                .Where(l => l.ProductId == productId &&
                            l.ProductUnitId == productUnitId &&
                            l.Status == BatchStatus.Active &&
                            (!l.ExpiryDate.HasValue || l.ExpiryDate.Value >= now.Date) &&
                            l.RemainingQuantity > 0
                            )
                .OrderBy(l => l.ExpiryDate == null ? 1 : 0)
                .ThenBy(l => l.ExpiryDate)
                .ThenBy(l => l.CreatedDate)
                .ToListAsync();

            var stock = stockRepo.GetAllAsQueryable()
                .FirstOrDefault(s => s.ProductId == productId && s.ProductUnitId == productUnitId);

            var currentLot = lots.OrderByDescending(l => l.CreatedDate).FirstOrDefault();
            var summaryFactor = currentLot?.QuantityPerUnitSnapshot > 0
                ? currentLot.QuantityPerUnitSnapshot
                : 1m;
            var weightedPurchasePrice = CalculateWeightedAverageUnitCost(lots, summaryFactor);
            if (stock == null)
            {
                stock = new Stock
                {
                    ProductId = productId,
                    ProductUnitId = productUnitId,
                    Quantity = lots.Sum(l => l.RemainingQuantity),
                    PurchasePrice = weightedPurchasePrice,
                    SalePrice = currentLot?.SalePrice ?? 0m,
                    CreatedDate = now,
                    UpdatedDate = now
                };
                await stockRepo.AddAsync(stock);
                return;
            }

            stock.Quantity = lots.Sum(l => l.RemainingQuantity);
            stock.PurchasePrice = weightedPurchasePrice;
            stock.SalePrice = currentLot?.SalePrice ?? 0m;
            stock.UpdatedDate = now;
            await stockRepo.UpdateAsync(stock);

        }

        private async Task<bool> IsLotUsedAsync(StockLot lot)
        {
            if (lot.RemainingQuantity != lot.Quantity)
                return true;

            if (lot.Status != BatchStatus.Active)
                return true;

            var transactionRepo = _uow.GetRepository<StockTransaction>();
            var adjustmentRepo = _uow.GetRepository<StockAdjustment>();

            var transactionQuery = transactionRepo.GetAllAsQueryable();
            var hasTransactions = await transactionQuery
                .AnyAsync(x => x.StockLotId == lot.Id && x.BaseQuantity < 0);
            if (hasTransactions)
                return true;

            var adjustmentQuery = adjustmentRepo.GetAllAsQueryable();
            return await adjustmentQuery
                .AnyAsync(x => x.StockLotId == lot.Id || x.NewStockLotId == lot.Id);
        }

        private static StockLot CreateLinkedLot(
            StockLot sourceLot,
            decimal quantity,
            decimal quantityPerUnit,
            decimal purchasePrice,
            decimal salePrice,
            DateTime? expiryDate,
            string notes,
            DateTime now)
        {
            var baseQuantity = quantity * quantityPerUnit;
            return new StockLot
            {
                ProductId = sourceLot.ProductId,
                ProductUnitId = sourceLot.ProductUnitId,
                Quantity = quantity,
                RemainingQuantity = quantity,
                QuantityPerUnitSnapshot = quantityPerUnit,
                BaseQuantity = baseQuantity,
                RemainingBaseQuantity = baseQuantity,
                PurchasePrice = purchasePrice,
                SalePrice = salePrice,
                ExpiryDate = expiryDate,
                Notes = notes,
                Status = BatchStatus.Active,
                CreatedDate = now,
                UpdatedDate = now
            };
        }

        private static StockTransaction CreateTransaction(
            int productId,
            int productUnitId,
            StockLot stockLot,
            StockAdjustment adjustment,
            decimal quantity,
            decimal baseQuantity,
            decimal unitPrice,
            DateTime? expiryDate,
            TransactionType transactionType,
            DateTime transactionDate,
            string? referenceNumber,
            string? notes,
            int? userId,
            bool positiveMovement)
        {
            return new StockTransaction
            {
                ProductId = productId,
                ProductUnitId = productUnitId,
                StockLot = stockLot,
                StockAdjustment = adjustment,
                Quantity = quantity,
                QuantityPerUnitSnapshot = stockLot.QuantityPerUnitSnapshot > 0 ? stockLot.QuantityPerUnitSnapshot : 1m,
                BaseQuantity = baseQuantity,
                UnitPrice = unitPrice,
                ExpiryDate = expiryDate,
                TransactionType = transactionType,
                CasherId = userId,
                TransactionDate = transactionDate,
                Notes = notes,
                ReferenceNumber = referenceNumber ?? (positiveMovement ? "ADJ-IN" : "ADJ-OUT"),
                CreatedDate = adjustment.CreatedDate,
                UpdatedDate = adjustment.UpdatedDate
            };
        }

        private static async Task<string> BuildInsufficientStockMessageAsync(
            IGenericRepository<Product> productRepo,
            IGenericRepository<ProductUnit> productUnitRepo,
            IGenericRepository<Unit> unitRepo,
            int productId,
            int productUnitId,
            decimal availableQuantity)
        {
            var product = await productRepo.GetByIdAsync(productId);
            var productUnit = await productUnitRepo.GetByIdAsync(productUnitId);
            Unit? unit = null;

            if (productUnit != null && productUnit.UnitId > 0)
                unit = await unitRepo.GetByIdAsync(productUnit.UnitId);

            var productName = !string.IsNullOrWhiteSpace(product?.Name)
                ? product.Name
                : $"#{productId}";
            var unitName = !string.IsNullOrWhiteSpace(unit?.Name)
                ? unit.Name
                : $"#{productUnitId}";

            return availableQuantity > 0
                ? $"Insufficient stock for \"{productName}\" in unit \"{unitName}\". Available quantity: {availableQuantity:0.###}."
                : $"Stock not available for \"{productName}\" in unit \"{unitName}\".";
        }

        private static DateTime NowInJordan()
        {
            var jordanTime = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, jordanTime);
        }
    }

    public interface IStockService : IGenericService<Stock, StockWriteDto, StockReadDto>
    {
        Task<Result> PostMovementAsync(StockMovementPostDto dto);
        Task<Result> PostMovementsAsync(IEnumerable<StockMovementPostDto> dtos, bool refreshCatalog = true);
        Task<Result<List<StockLotAllocationDto>>> AllocateOutgoingAsync(IEnumerable<StockAllocationRequestDto> requests);
        Task<Result<decimal>> GetAvailableQuantityInUnitAsync(int productId, int productUnitId);
        Task<Result<List<StockAvailabilityDto>>> GetAvailableQuantitiesInUnitsAsync(IEnumerable<StockAllocationRequestDto> requests);
        Task<Result<PagedResult<PosBrowseItemDto>>> GetPosBrowsePageAsync(int pageNumber, int pageSize, string? searchText, int? subCategoryId);
        Task<Result<List<PosProductSearchResultDto>>> SearchPosProductsAsync(string searchText, int maxResults = 100, int? supplierId = null);
        Task<Result<List<PosProductSearchResultDto>>> SearchPosProductsAsync(string? productName, string? barcode, int maxResults = 100, int? supplierId = null, CancellationToken cancellationToken = default);
        Task<Result<List<PosProductSearchResultDto>>> GetPosProductCatalogAsync(CancellationToken cancellationToken = default);
        Task<Result<List<PosProductSearchResultDto>>> GetPosProductStockAsync(IEnumerable<int> productIds, CancellationToken cancellationToken = default);
        Task<Result<List<SubCategoryReadDto>>> GetPosBrowseSubCategoriesAsync();
        Task<Result<List<StockBatchLookupDto>>> GetBatchLookupAsync(int? productId = null);
        Task<Result<StockLotUpdateDto>> UpdateBatchMetadataAsync(StockLotUpdateDto dto);
        Task<Result<StockAdjustmentWriteDto>> CreateAdjustmentAsync(StockAdjustmentWriteDto dto);
    }

    public class StockMovementPostDto
    {
        public int ProductId { get; set; }
        public int ProductUnitId { get; set; }
        public int? StockLotId { get; set; }
        public decimal Quantity { get; set; }
        public decimal QuantityPerUnitSnapshot { get; set; }
        public decimal BaseQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? PurchasePrice { get; set; }
        public decimal? SalePrice { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public TransactionType TransactionType { get; set; }
        public int? InvoiceId { get; set; }
        public int? VoucherId { get; set; }
        public int? CasherId { get; set; }
        public int? CashierSessionId { get; set; }
        public int? CustomerId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Notes { get; set; }
        public string? ReferenceNumber { get; set; }
        public bool UpdateCatalogAverageCost { get; set; }
    }

    public class StockAllocationRequestDto
    {
        public int RequestIndex { get; set; }
        public int ProductId { get; set; }
        public int ProductUnitId { get; set; }
        public decimal Quantity { get; set; }
    }

    public class StockAvailabilityDto
    {
        public int ProductId { get; set; }
        public int ProductUnitId { get; set; }
        public decimal AvailableQuantity { get; set; }
    }

    public class StockLotAllocationDto
    {
        public int RequestIndex { get; set; }
        public int StockLotId { get; set; }
        public int ProductId { get; set; }
        public int ProductUnitId { get; set; }
        public decimal Quantity { get; set; }
        public decimal QuantityPerUnitSnapshot { get; set; }
        public decimal BaseQuantity { get; set; }
        public decimal AvailableQuantityAfterAllocation { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
