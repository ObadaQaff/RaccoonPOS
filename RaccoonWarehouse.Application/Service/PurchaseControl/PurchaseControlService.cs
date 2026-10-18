using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.PurchaseControl;
using RaccoonWarehouse.Domain.PurchaseControl.DTOs;

namespace RaccoonWarehouse.Application.Service.PurchaseControl;

public interface IPurchaseControlService
{
    Task<Result<List<PurchaseControlEntryDto>>> GetAllAsync();
    Task<Result<PurchaseControlEntryDto>> CreateAsync(PurchaseControlEntryDto dto);
}

public sealed class PurchaseControlService : IPurchaseControlService
{
    private readonly ApplicationDbContext _db;
    public PurchaseControlService(ApplicationDbContext db) => _db = db;

    public async Task<Result<List<PurchaseControlEntryDto>>> GetAllAsync()
    {
        var rows = await _db.PurchaseControlEntries.AsNoTracking().OrderByDescending(x => x.PurchaseOrderDate).ThenByDescending(x => x.Id).ToListAsync();
        return Result<List<PurchaseControlEntryDto>>.Ok(rows.Select(Map).ToList());
    }

    public async Task<Result<PurchaseControlEntryDto>> CreateAsync(PurchaseControlEntryDto dto)
    {
        var entity = new PurchaseControlEntry
        {
            PaymentMethod = dto.PaymentMethod, PurchaseOrderNumber = dto.PurchaseOrderNumber, PurchaseOrderDate = dto.PurchaseOrderDate,
            PurchaseRequestNumber = dto.PurchaseRequestNumber, SupplierName = dto.SupplierName, PurchaseOrderAmount = dto.PurchaseOrderAmount,
            InvoiceNumber = dto.InvoiceNumber, InvoiceDate = dto.InvoiceDate, DeliveryDate = dto.DeliveryDate, Description = dto.Description,
            Category = dto.Category, PaidAmount = dto.PaidAmount, DueAmount = dto.DueAmount, ReceiptAmount = dto.ReceiptAmount,
            PaymentStatus = dto.PaymentStatus, DueDate = dto.DueDate, LinkCheck = dto.LinkCheck, Notes = dto.Notes,
            OverdueDays = dto.OverdueDays, ReceivedValue = dto.ReceivedValue, CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now
        };
        await _db.PurchaseControlEntries.AddAsync(entity);
        await _db.SaveChangesAsync();
        return Result<PurchaseControlEntryDto>.Ok(Map(entity), "Purchase control line saved.");
    }

    private static PurchaseControlEntryDto Map(PurchaseControlEntry x) => new()
    {
        Id = x.Id, PaymentMethod = x.PaymentMethod, PurchaseOrderNumber = x.PurchaseOrderNumber, PurchaseOrderDate = x.PurchaseOrderDate,
        PurchaseRequestNumber = x.PurchaseRequestNumber, SupplierName = x.SupplierName, PurchaseOrderAmount = x.PurchaseOrderAmount,
        InvoiceNumber = x.InvoiceNumber, InvoiceDate = x.InvoiceDate, DeliveryDate = x.DeliveryDate, Description = x.Description,
        Category = x.Category, PaidAmount = x.PaidAmount, DueAmount = x.DueAmount, ReceiptAmount = x.ReceiptAmount,
        PaymentStatus = x.PaymentStatus, DueDate = x.DueDate, LinkCheck = x.LinkCheck, Notes = x.Notes,
        OverdueDays = x.OverdueDays, ReceivedValue = x.ReceivedValue
    };
}
