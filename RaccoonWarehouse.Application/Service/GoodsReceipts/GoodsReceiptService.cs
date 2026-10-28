using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.GoodsReceipts;
using RaccoonWarehouse.Domain.GoodsReceipts.DTOs;
using RaccoonWarehouse.Domain.PurchaseOrders;

namespace RaccoonWarehouse.Application.Service.GoodsReceipts;

public interface IGoodsReceiptService
{
    Task<Result<GoodsReceiptWriteDto>> CreateAsync(GoodsReceiptWriteDto dto);
    Task<Result<List<GoodsReceiptReadDto>>> GetAllAsync();
    Task<Result<GoodsReceiptWriteDto>> ConfirmAsync(int id);
}

public sealed class GoodsReceiptService : IGoodsReceiptService
{
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;
    private readonly IStockService _stockService;
    private readonly IUserSession _userSession;

    public GoodsReceiptService(ApplicationDbContext db, IMapper mapper, IStockService stockService, IUserSession userSession)
    {
        _db = db;
        _mapper = mapper;
        _stockService = stockService;
        _userSession = userSession;
    }

    public async Task<Result<GoodsReceiptWriteDto>> CreateAsync(GoodsReceiptWriteDto dto)
    {
if (dto.Lines == null || dto.Lines.Count == 0)
            return Result<GoodsReceiptWriteDto>.Fail("A goods receipt must contain at least one line.");
        if (dto.Lines.Any(x => x.PurchaseOrderLineId <= 0 || x.ProductId <= 0 || x.ProductUnitId <= 0 || x.QuantityReceived <= 0 || x.QuantityAccepted < 0))
            return Result<GoodsReceiptWriteDto>.Fail("Every receipt line must have a positive received quantity and valid references.");
        if (dto.Lines.Any(x => x.QuantityAccepted > x.QuantityReceived || x.QuantityRejected + x.QuantityDamaged > x.QuantityReceived))
            return Result<GoodsReceiptWriteDto>.Fail("Accepted, rejected, and damaged quantities cannot exceed the received quantity.");

        var order = await _db.PurchaseOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == dto.PurchaseOrderId);
        if (order == null || order.Status is not PurchaseOrderStatus.Approved and not PurchaseOrderStatus.Converted)
            return Result<GoodsReceiptWriteDto>.Fail("Goods receipts can only be created for an approved purchase order.");
        var orderLineIds = order.Lines.Select(x => x.Id).ToHashSet();
        if (dto.Lines.Any(x => !orderLineIds.Contains(x.PurchaseOrderLineId)))
            return Result<GoodsReceiptWriteDto>.Fail("Every receipt line must belong to the selected purchase order.");

        var now = DateTime.Now;
        var receipt = _mapper.Map<GoodsReceipt>(dto);
        receipt.ReceiptNumber = string.IsNullOrWhiteSpace(dto.ReceiptNumber) ? $"GRN-{now:yyyyMMddHHmmssfff}" : dto.ReceiptNumber.Trim();
        receipt.ReceiptDate = dto.ReceiptDate == default ? now : dto.ReceiptDate;
        receipt.ReceivedBy = _userSession.CurrentUser?.Id;
        receipt.Status = GoodsReceiptStatus.QualityCheck;
        receipt.CreatedDate = now;
        receipt.UpdatedDate = now;
        receipt.Lines = dto.Lines.Select(line =>
        {
            var mapped = _mapper.Map<GoodsReceiptLine>(line);
            mapped.CreatedDate = now;
            mapped.UpdatedDate = now;
            return mapped;
        }).ToList();
        await _db.GoodsReceipts.AddAsync(receipt);
        await _db.SaveChangesAsync();
        return Result<GoodsReceiptWriteDto>.Ok(_mapper.Map<GoodsReceiptWriteDto>(receipt), "Goods receipt saved for quality confirmation.");
    }

    public async Task<Result<List<GoodsReceiptReadDto>>> GetAllAsync()
    {
var receipts = await _db.GoodsReceipts.AsNoTracking().Include(x => x.Lines).OrderByDescending(x => x.ReceiptDate).ToListAsync();
        return Result<List<GoodsReceiptReadDto>>.Ok(_mapper.Map<List<GoodsReceiptReadDto>>(receipts));
    }

    public async Task<Result<GoodsReceiptWriteDto>> ConfirmAsync(int id)
    {
var receipt = await _db.GoodsReceipts.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (receipt == null) return Result<GoodsReceiptWriteDto>.Fail("Goods receipt was not found.");
        if (receipt.Status == GoodsReceiptStatus.Accepted)
            return Result<GoodsReceiptWriteDto>.Ok(_mapper.Map<GoodsReceiptWriteDto>(receipt), "Goods receipt was already confirmed; no duplicate stock movement was created.");
        if (receipt.Status != GoodsReceiptStatus.QualityCheck)
            return Result<GoodsReceiptWriteDto>.Fail("Only receipts waiting for quality confirmation can be confirmed.");
        if (receipt.Lines.Any(x => x.QuantityAccepted > 0 && !x.ExpiryDate.HasValue))
            return Result<GoodsReceiptWriteDto>.Fail("Expiry date is required for accepted purchase stock.");

        var lineIds = receipt.Lines.Select(x => x.PurchaseOrderLineId).Distinct().ToList();
        var orderLines = await _db.PurchaseOrderLines.Where(x => lineIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        var priorAccepted = await _db.GoodsReceiptLines
            .Where(x => lineIds.Contains(x.PurchaseOrderLineId) && x.GoodsReceipt.Status == GoodsReceiptStatus.Accepted && x.GoodsReceiptId != receipt.Id)
            .GroupBy(x => x.PurchaseOrderLineId)
            .Select(x => new { Id = x.Key, Quantity = x.Sum(y => y.QuantityAccepted + y.FreeQuantity) })
            .ToDictionaryAsync(x => x.Id, x => x.Quantity);

        foreach (var line in receipt.Lines)
        {
            var orderLine = orderLines[line.PurchaseOrderLineId];
            priorAccepted.TryGetValue(line.PurchaseOrderLineId, out var alreadyAccepted);
            var requested = line.QuantityAccepted + line.FreeQuantity;
            if (alreadyAccepted + requested > orderLine.Quantity)
                return Result<GoodsReceiptWriteDto>.Fail($"Receipt quantity exceeds the remaining quantity for purchase-order line {line.PurchaseOrderLineId}.");
        }

        var movements = receipt.Lines.Where(x => x.QuantityAccepted + x.FreeQuantity > 0).Select(line =>
        {
            var poLine = orderLines[line.PurchaseOrderLineId];
            var quantity = line.QuantityAccepted + line.FreeQuantity;
            var unitFactor = poLine.QuantityPerUnitSnapshot > 0 ? poLine.QuantityPerUnitSnapshot : 1m;
            return new StockMovementPostDto
            {
                ProductId = line.ProductId,
                ProductUnitId = line.ProductUnitId,
                Quantity = quantity,
                QuantityPerUnitSnapshot = unitFactor,
                BaseQuantity = quantity * unitFactor,
                UnitPrice = poLine.UnitPrice,
                PurchasePrice = poLine.UnitCost,
                ExpiryDate = line.ExpiryDate,
                TransactionType = TransactionType.Purchase,
                TransactionDate = receipt.ReceiptDate,
                Notes = $"Goods receipt {receipt.ReceiptNumber}",
                ReferenceNumber = receipt.ReceiptNumber,
                UpdateCatalogAverageCost = true
            };
        }).ToList();

        var result = await _stockService.PostMovementsAsync(movements);
        if (!result.Success)
            return Result<GoodsReceiptWriteDto>.Fail(result.Message ?? "Stock posting failed.");
        receipt.Status = GoodsReceiptStatus.Accepted;
        receipt.ConfirmedDate = DateTime.Now;
        receipt.UpdatedDate = DateTime.Now;
        await _db.SaveChangesAsync();
        return Result<GoodsReceiptWriteDto>.Ok(_mapper.Map<GoodsReceiptWriteDto>(receipt), "Goods receipt confirmed and accepted stock posted.");
    }
}
