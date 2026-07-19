using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Domain.StockAdjustments;
using RaccoonWarehouse.Domain.StockAdjustments.DTOs;
using RaccoonWarehouse.Domain.StockLots;
using RaccoonWarehouse.Domain.StockTransactions;
using RaccoonWarehouse.Domain.Units;
using RaccoonWarehouse.Domain.POS.DTOs;
using RaccoonWarehouse.Domain.SubCategories.DTOs;

namespace RaccoonWarehouse.Application.Service.Stocks
{
    public class StockService : GenericService<Stock, StockWriteDto, StockReadDto>, IStockService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        private readonly IAccountingService? _accountingService;

        public StockService(ApplicationDbContext context, IUOW uow, IMapper mapper) : this(context, uow, mapper, null)
        {
        }

        public StockService(ApplicationDbContext context, IUOW uow, IMapper mapper, IAccountingService? accountingService) : base(context, uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
            _accountingService = accountingService;
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
                    ExpiryDate = lot.ExpiryDate,
                    Status = lot.Status,
                    IsUsed = lot.RemainingQuantity != lot.Quantity ||
                             lot.Status != BatchStatus.Active ||
                             usedLotIds.Contains(lot.Id)
                });
            }

            return Result<List<StockBatchLookupDto>>.Ok(result);
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
            var simulatedRemaining = new Dictionary<int, decimal>();
            var allocations = new List<StockLotAllocationDto>();

            foreach (var request in items)
            {
                var lots = await GetAvailableLotsQuery(lotRepo, request.ProductId, request.ProductUnitId)
                    .ToListAsync();

                var remainingRequired = request.Quantity;
                foreach (var lot in lots)
                {
                    var available = simulatedRemaining.TryGetValue(lot.Id, out var cachedRemaining)
                        ? cachedRemaining
                        : lot.RemainingQuantity;

                    if (available <= 0)
                        continue;

                    var allocatedQuantity = Math.Min(available, remainingRequired);
                    if (allocatedQuantity <= 0)
                        continue;

                    var factor = lot.QuantityPerUnitSnapshot > 0 ? lot.QuantityPerUnitSnapshot : 1m;
                    allocations.Add(new StockLotAllocationDto
                    {
                        StockLotId = lot.Id,
                        ProductId = request.ProductId,
                        ProductUnitId = request.ProductUnitId,
                        Quantity = allocatedQuantity,
                        QuantityPerUnitSnapshot = factor,
                        BaseQuantity = allocatedQuantity * factor,
                        PurchasePrice = lot.PurchasePrice,
                        SalePrice = lot.SalePrice,
                        ExpiryDate = lot.ExpiryDate
                    });

                    simulatedRemaining[lot.Id] = available - allocatedQuantity;
                    remainingRequired -= allocatedQuantity;
                    if (remainingRequired <= 0)
                        break;
                }

                if (remainingRequired > 0)
                {
                    return Result<List<StockLotAllocationDto>>.Fail(
                        $"Insufficient stock for product #{request.ProductId} and unit #{request.ProductUnitId}. Uncovered quantity: {remainingRequired:0.###}.");
                }
            }

            return Result<List<StockLotAllocationDto>>.Ok(allocations);
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
                    IsDefaultSaleUnit = s.ProductUnit != null && s.ProductUnit.IsDefaultSaleUnit,
                    s.Quantity,
                    s.ProductUnitId
                })
                .ToListAsync();

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
                            CurrentSalePrice = preferred.SalePrice
                        };
                    });

            // Preserve page order.
            var items = productIdPage
                .Where(itemsByProduct.ContainsKey)
                .Select(id => itemsByProduct[id])
                .ToList();

            return Result<PagedResult<PosBrowseItemDto>>.Ok(new PagedResult<PosBrowseItemDto>(items, totalCount, pageNumber, pageSize));
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

        public async Task<Result> PostMovementsAsync(IEnumerable<StockMovementPostDto> dtos)
        {
            var items = dtos?.ToList() ?? new List<StockMovementPostDto>();
            if (items.Count == 0)
                return Result.Ok("No stock movements to post.");

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
            }

            if (errors.Count > 0)
                return Result.Fail("Invalid stock movement.", errors.Distinct().ToList());

            var stockRepo = _uow.GetRepository<Stock>();
            var transactionRepo = _uow.GetRepository<StockTransaction>();
            var lotRepo = _uow.GetRepository<StockLot>();
            var productRepo = _uow.GetRepository<Product>();
            var productUnitRepo = _uow.GetRepository<ProductUnit>();
            var unitRepo = _uow.GetRepository<Unit>();
            var now = NowInJordan();
            var touchedKeys = new HashSet<(int ProductId, int ProductUnitId)>();

            foreach (var dto in items)
            {
                touchedKeys.Add((dto.ProductId, dto.ProductUnitId));

                if (dto.Quantity > 0)
                {
                    var quantityPerUnit = dto.QuantityPerUnitSnapshot > 0 ? dto.QuantityPerUnitSnapshot : 1m;
                    var baseQuantity = dto.BaseQuantity != 0 ? dto.BaseQuantity : dto.Quantity * quantityPerUnit;

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
                        ExpiryDate = dto.ExpiryDate,
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
                        ExpiryDate = dto.ExpiryDate,
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
                    var candidates = await GetAvailableLotsQuery(lotRepo, dto.ProductId, dto.ProductUnitId, dto)
                        .ToListAsync();
                    var totalAvailableBeforeConsumption = candidates.Sum(x => x.RemainingQuantity);

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

                        var available = lot.RemainingQuantity;
                        if (available <= 0)
                            continue;

                        var consumedQuantity = Math.Min(available, remainingToConsume);
                        var factor = lot.QuantityPerUnitSnapshot > 0 ? lot.QuantityPerUnitSnapshot : 1m;

                        lot.RemainingQuantity -= consumedQuantity;
                        lot.RemainingBaseQuantity -= consumedQuantity * factor;
                        if (lot.RemainingQuantity <= 0)
                        {
                            lot.RemainingQuantity = 0;
                            lot.RemainingBaseQuantity = 0;
                            if (lot.Status == BatchStatus.Active)
                            {
                                lot.Status = BatchStatus.Closed;
                                lot.ClosedDate = now;
                                lot.ClosedReason = dto.Notes ?? "Batch fully consumed.";
                            }
                        }

                        lot.UpdatedDate = now;
                        await lotRepo.UpdateAsync(lot);

                        await transactionRepo.AddAsync(new StockTransaction
                        {
                            ProductId = dto.ProductId,
                            ProductUnitId = dto.ProductUnitId,
                            StockLotId = lot.Id,
                            Quantity = -consumedQuantity,
                            QuantityPerUnitSnapshot = factor,
                            BaseQuantity = -(consumedQuantity * factor),
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

            await _uow.CommitAsync();

            foreach (var key in touchedKeys)
                await SyncStockSummaryAsync(stockRepo, lotRepo, key.ProductId, key.ProductUnitId, now);

            await _uow.CommitAsync();
            return Result.Ok("Stock movement posted successfully.");
        }

        private static IQueryable<StockLot> GetAvailableLotsQuery(
            IGenericRepository<StockLot> lotRepo,
            int productId,
            int productUnitId,
            StockMovementPostDto? hint = null)
        {
            var today = DateTime.Today;
            IQueryable<StockLot> query = lotRepo.GetAllAsQueryable()
                .Where(l => l.ProductId == productId &&
                            l.ProductUnitId == productUnitId &&
                            l.Status == BatchStatus.Active &&
                            l.RemainingQuantity > 0 &&
                            (!l.ExpiryDate.HasValue || l.ExpiryDate.Value >= today));

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

        private static async Task SyncStockSummaryAsync(
            IGenericRepository<Stock> stockRepo,
            IGenericRepository<StockLot> lotRepo,
            int productId,
            int productUnitId,
            DateTime now)
        {
            var today = now.Date;
            var lots = await lotRepo.GetAllAsQueryable()
                .Where(l => l.ProductId == productId &&
                            l.ProductUnitId == productUnitId &&
                            l.Status == BatchStatus.Active &&
                            l.RemainingQuantity > 0 &&
                            (!l.ExpiryDate.HasValue || l.ExpiryDate.Value >= today))
                .OrderBy(l => l.ExpiryDate == null ? 1 : 0)
                .ThenBy(l => l.ExpiryDate)
                .ThenBy(l => l.CreatedDate)
                .ToListAsync();

            var stock = stockRepo.GetAllAsQueryable()
                .FirstOrDefault(s => s.ProductId == productId && s.ProductUnitId == productUnitId);

            var currentLot = lots.OrderByDescending(l => l.CreatedDate).FirstOrDefault();
            if (stock == null)
            {
                stock = new Stock
                {
                    ProductId = productId,
                    ProductUnitId = productUnitId,
                    Quantity = lots.Sum(l => l.RemainingQuantity),
                    PurchasePrice = currentLot?.PurchasePrice ?? 0m,
                    SalePrice = currentLot?.SalePrice ?? 0m,
                    CreatedDate = now,
                    UpdatedDate = now
                };
                await stockRepo.AddAsync(stock);
                return;
            }

            stock.Quantity = lots.Sum(l => l.RemainingQuantity);
            stock.PurchasePrice = currentLot?.PurchasePrice ?? 0m;
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
        Task<Result> PostMovementsAsync(IEnumerable<StockMovementPostDto> dtos);
        Task<Result<List<StockLotAllocationDto>>> AllocateOutgoingAsync(IEnumerable<StockAllocationRequestDto> requests);
        Task<Result<PagedResult<PosBrowseItemDto>>> GetPosBrowsePageAsync(int pageNumber, int pageSize, string? searchText, int? subCategoryId);
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
    }

    public class StockAllocationRequestDto
    {
        public int ProductId { get; set; }
        public int ProductUnitId { get; set; }
        public decimal Quantity { get; set; }
    }

    public class StockLotAllocationDto
    {
        public int StockLotId { get; set; }
        public int ProductId { get; set; }
        public int ProductUnitId { get; set; }
        public decimal Quantity { get; set; }
        public decimal QuantityPerUnitSnapshot { get; set; }
        public decimal BaseQuantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
