using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.PurchaseReturns;
using RaccoonWarehouse.Domain.PurchaseReturns.DTOs;

namespace RaccoonWarehouse.Application.Service.PurchaseReturns;

public interface IPurchaseReturnService
{
    Task<Result<PurchaseReturnWriteDto>> CreateAsync(PurchaseReturnWriteDto dto);
    Task<Result<PurchaseReturnWriteDto>> ConfirmAsync(int id);
}

public sealed class PurchaseReturnService : IPurchaseReturnService
{
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;
    private readonly IStockService _stock;
    private readonly IUserSession _session;

    public PurchaseReturnService(ApplicationDbContext db, IMapper mapper, IStockService stock, IUserSession session)
    {
        _db = db;
        _mapper = mapper;
        _stock = stock;
        _session = session;
    }

    public async Task<Result<PurchaseReturnWriteDto>> CreateAsync(PurchaseReturnWriteDto dto)
    {
if (dto.Lines == null || dto.Lines.Count == 0 || dto.Lines.Any(x => x.GoodsReceiptLineId <= 0 || x.Quantity <= 0))
            return Result<PurchaseReturnWriteDto>.Fail("A purchase return must contain positive quantities linked to receipt lines.");
        if (string.IsNullOrWhiteSpace(dto.Reason))
            return Result<PurchaseReturnWriteDto>.Fail("A purchase return reason is required.");
        var receiptLineIds = dto.Lines.Select(x => x.GoodsReceiptLineId).ToList();
        var receiptLines = await _db.GoodsReceiptLines.Include(x => x.GoodsReceipt).Where(x => receiptLineIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        if (receiptLines.Count != receiptLineIds.Count || receiptLines.Values.Any(x => x.GoodsReceipt.Status != GoodsReceiptStatus.Accepted))
            return Result<PurchaseReturnWriteDto>.Fail("Returns must reference accepted goods-receipt lines.");
        if (dto.Lines.Any(x => x.ProductId != receiptLines[x.GoodsReceiptLineId].ProductId || x.ProductUnitId != receiptLines[x.GoodsReceiptLineId].ProductUnitId))
            return Result<PurchaseReturnWriteDto>.Fail("Return product references do not match the original receipt.");

        var now = DateTime.Now;
        var entity = _mapper.Map<PurchaseReturn>(dto);
        entity.ReturnNumber = string.IsNullOrWhiteSpace(dto.ReturnNumber) ? $"PRT-{now:yyyyMMddHHmmssfff}" : dto.ReturnNumber.Trim();
        entity.ReturnDate = dto.ReturnDate == default ? now : dto.ReturnDate;
        entity.Status = PurchaseReturnStatus.Approved;
        entity.CreatedBy = _session.CurrentUser?.Id;
        entity.CashierSessionId = dto.CashierSessionId ?? _session.CurrentCashierSessionId;
        entity.CreatedDate = now;
        entity.UpdatedDate = now;
        entity.Lines = dto.Lines.Select(x => _mapper.Map<PurchaseReturnLine>(x)).ToList();
        await _db.PurchaseReturns.AddAsync(entity);
        await _db.SaveChangesAsync();
        return Result<PurchaseReturnWriteDto>.Ok(_mapper.Map<PurchaseReturnWriteDto>(entity), "Purchase return approved for confirmation.");
    }

    public async Task<Result<PurchaseReturnWriteDto>> ConfirmAsync(int id)
    {
var entity = await _db.PurchaseReturns.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null) return Result<PurchaseReturnWriteDto>.Fail("Purchase return was not found.");
        if (entity.Status == PurchaseReturnStatus.Confirmed)
            return Result<PurchaseReturnWriteDto>.Ok(_mapper.Map<PurchaseReturnWriteDto>(entity), "Purchase return was already confirmed; no duplicate stock deduction was created.");
        if (entity.Status != PurchaseReturnStatus.Approved)
            return Result<PurchaseReturnWriteDto>.Fail("Only approved purchase returns can be confirmed.");

        entity.CashierSessionId ??= _session.CurrentCashierSessionId;

        var receiptLines = await _db.GoodsReceiptLines.Where(x => entity.Lines.Select(y => y.GoodsReceiptLineId).Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        var movements = entity.Lines.Select(line =>
        {
            var source = receiptLines[line.GoodsReceiptLineId];
            return new StockMovementPostDto
            {
                ProductId = line.ProductId,
                ProductUnitId = line.ProductUnitId,
                Quantity = -line.Quantity,
                QuantityPerUnitSnapshot = 1m,
                BaseQuantity = -line.Quantity,
                UnitPrice = 0m,
                ExpiryDate = line.ExpiryDate ?? source.ExpiryDate,
                TransactionType = TransactionType.Purchase,
                TransactionDate = entity.ReturnDate,
                Notes = $"Purchase return {entity.ReturnNumber}",
                ReferenceNumber = entity.ReturnNumber,
                UpdateCatalogAverageCost = false
            };
        }).ToList();
        var result = await _stock.PostMovementsAsync(movements);
        if (!result.Success) return Result<PurchaseReturnWriteDto>.Fail(result.Message ?? "Purchase return stock reversal failed.");
        entity.Status = PurchaseReturnStatus.Confirmed;
        entity.ConfirmedBy = _session.CurrentUser?.Id;
        entity.ConfirmedDate = DateTime.Now;
        entity.UpdatedDate = DateTime.Now;
        await _db.SaveChangesAsync();
        return Result<PurchaseReturnWriteDto>.Ok(_mapper.Map<PurchaseReturnWriteDto>(entity), "Purchase return confirmed.");
    }
}
