using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.LostCustomers;

namespace RaccoonWarehouse.Application.Service.LostCustomers;

public interface ILostCustomerService
{
    Task<Result<LostCustomerDto>> CreateAsync(LostCustomerWriteDto dto);
    Task<Result<List<LostCustomerDto>>> GetAllAsync();
    Task<Result<LostCustomerDto>> UpdateAsync(int id, LostCustomerWriteDto dto);
    Task<Result<bool>> DeleteAsync(int id);
}

public sealed class LostCustomerService : ILostCustomerService
{
    private readonly ApplicationDbContext _db;
    private readonly IUserSession _session;

    public LostCustomerService(ApplicationDbContext db, IUserSession session)
    { _db = db; _session = session; }

    public async Task<Result<LostCustomerDto>> CreateAsync(LostCustomerWriteDto dto)
    {
        var validation = Validate(dto);
        if (validation != null) return Result<LostCustomerDto>.Fail(validation);
        var now = DateTime.Now;
        var entity = new LostCustomer
        {
            Type = dto.Type, Reason = dto.Reason, ProductId = dto.ProductId, ProductUnitId = dto.ProductUnitId,
            ProductName = dto.ProductName?.Trim() ?? string.Empty, Quantity = dto.Type == LostCustomerType.MissingProduct ? dto.Quantity : null,
            Message = dto.Message?.Trim(), Notes = dto.Notes?.Trim(), CreatedBy = _session.CurrentUserId,
            CreatedByName = _session.CurrentUser?.Name ?? string.Empty, CreatedDate = now, UpdatedDate = now
        };
        _db.LostCustomers.Add(entity);
        await _db.SaveChangesAsync();
        return Result<LostCustomerDto>.Ok(ToDto(entity));
    }

    public async Task<Result<List<LostCustomerDto>>> GetAllAsync()
    {
        var rows = await _db.LostCustomers.AsNoTracking().OrderByDescending(x => x.CreatedDate).ToListAsync();
        return Result<List<LostCustomerDto>>.Ok(rows.Select(ToDto).ToList());
    }

    public async Task<Result<LostCustomerDto>> UpdateAsync(int id, LostCustomerWriteDto dto)
    {
        var validation = Validate(dto);
        if (validation != null) return Result<LostCustomerDto>.Fail(validation);
        var entity = await _db.LostCustomers.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null) return Result<LostCustomerDto>.Fail("سجل العميل المفقود غير موجود.");
        entity.Type = dto.Type; entity.Reason = dto.Reason; entity.ProductId = dto.ProductId; entity.ProductUnitId = dto.ProductUnitId;
        entity.ProductName = dto.ProductName?.Trim() ?? string.Empty; entity.Quantity = dto.Type == LostCustomerType.MissingProduct ? dto.Quantity : null;
        entity.Message = dto.Message?.Trim(); entity.Notes = dto.Notes?.Trim(); entity.UpdatedDate = DateTime.Now;
        await _db.SaveChangesAsync();
        return Result<LostCustomerDto>.Ok(ToDto(entity));
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        var entity = await _db.LostCustomers.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null) return Result<bool>.Fail("سجل العميل المفقود غير موجود.");
        _db.LostCustomers.Remove(entity); await _db.SaveChangesAsync(); return Result<bool>.Ok(true);
    }

    private static string? Validate(LostCustomerWriteDto dto)
    {
        if (dto.Type == LostCustomerType.MissingProduct)
        {
            if (string.IsNullOrWhiteSpace(dto.ProductName)) return "أدخل اسم المنتج أو اختر منتجاً.";
            if (!dto.Quantity.HasValue || dto.Quantity <= 0) return "أدخل كمية صحيحة.";
            if (dto.Reason == LostCustomerReason.Other) return "اختر سبب عدم توفر المنتج.";
        }
        else if (string.IsNullOrWhiteSpace(dto.Message)) return "أدخل سبب فقدان العميل.";
        return null;
    }

    private static LostCustomerDto ToDto(LostCustomer x) => new()
    {
        Id = x.Id, Type = x.Type, Reason = x.Reason, ProductId = x.ProductId, ProductUnitId = x.ProductUnitId,
        ProductName = x.ProductName, Quantity = x.Quantity, Message = x.Message, Notes = x.Notes,
        CreatedBy = x.CreatedBy, CreatedByName = x.CreatedByName, CreatedDate = x.CreatedDate
    };
}
