using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.PurchaseRequests;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.CustomerRequests;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.PurchaseRequests;
using RaccoonWarehouse.Domain.PurchaseRequests.DTOs;
using RaccoonWarehouse.Domain.Users;

namespace RaccoonWarehouse.Application.Service.CustomerRequests;

public interface ICustomerRequestService
{
    Task<Result<CustomerRequestDto>> CreateAsync(CustomerRequestWriteDto dto);
    Task<Result<List<CustomerRequestDto>>> GetAllAsync();
    Task<Result<CustomerRequestDto>> GetByIdAsync(int id);
    Task<Result<CustomerRequestDto>> ApproveAsync(int id);
    Task<Result<CustomerRequestDto>> CancelAsync(int id);
    Task<Result<CustomerRequestDto>> UpdateReceivingAsync(int id, CustomerRequestReceivingStatus status, IReadOnlyDictionary<int, decimal> receivedQuantities);
    Task<Result<PurchaseRequestWriteDto>> CreatePurchaseRequestAsync(int id);
    Task<Result<CustomerRequestDto>> MarkSoldAsync(int id, IReadOnlyDictionary<int, decimal> quantities);
}

public sealed class CustomerRequestService : ICustomerRequestService
{
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;
    private readonly IUserSession _session;
    private readonly IPurchaseRequestService _purchaseRequests;

    public CustomerRequestService(ApplicationDbContext db, IMapper mapper, IUserSession session, IPurchaseRequestService purchaseRequests)
    { _db = db; _mapper = mapper; _session = session; _purchaseRequests = purchaseRequests; }

    public async Task<Result<CustomerRequestDto>> CreateAsync(CustomerRequestWriteDto dto)
    {
        if (dto.CustomerId <= 0 || dto.Lines.Count == 0)
            return Result<CustomerRequestDto>.Fail("يجب اختيار العميل وإضافة مادة واحدة على الأقل.");
        if (dto.Lines.Any(x => string.IsNullOrWhiteSpace(x.ItemName) || x.Quantity <= 0 || x.UnitPrice < 0))
            return Result<CustomerRequestDto>.Fail("تحقق من اسم المادة والكمية والسعر.");
        var customer = await _db.Set<User>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == dto.CustomerId && x.Role == UserRole.Customer);
        if (customer == null) return Result<CustomerRequestDto>.Fail("العميل غير موجود.");
        var now = DateTime.Now;
        var entity = new CustomerRequest
        {
            RequestNumber = "CR-TEMP-" + Guid.NewGuid().ToString("N")[..24], CustomerId = customer.Id, CustomerName = customer.Name, CustomerPhone = customer.PhoneNumber?.Trim(),
            TotalAmount = dto.Lines.Sum(x => x.Quantity * x.UnitPrice), PaidAmount = Math.Clamp(dto.PaidAmount, 0m, dto.Lines.Sum(x => x.Quantity * x.UnitPrice)),
            PaymentNote = dto.PaymentNote?.Trim(), Notes = dto.Notes?.Trim(), CreatedDate = now, UpdatedDate = now,
            Lines = dto.Lines.Select(x => new CustomerRequestLine { ProductId = x.ProductId, ProductUnitId = x.ProductUnitId, ItemName = x.ItemName.Trim(), UnitName = x.UnitName?.Trim(), Quantity = x.Quantity, UnitPrice = x.UnitPrice, Notes = x.Notes?.Trim(), CreatedDate = now, UpdatedDate = now }).ToList()
        };
        _db.CustomerRequests.Add(entity);
        await _db.SaveChangesAsync();
        entity.RequestNumber = $"CR-{entity.Id:D5}";
        await _db.SaveChangesAsync();
        return Result<CustomerRequestDto>.Ok(_mapper.Map<CustomerRequestDto>(entity));
    }

    public async Task<Result<List<CustomerRequestDto>>> GetAllAsync()
    {
        var rows = await _db.CustomerRequests.AsNoTracking().Include(x => x.Lines).OrderByDescending(x => x.CreatedDate).ToListAsync();
        return Result<List<CustomerRequestDto>>.Ok(_mapper.Map<List<CustomerRequestDto>>(rows));
    }

    public async Task<Result<CustomerRequestDto>> GetByIdAsync(int id)
    {
        var row = await _db.CustomerRequests.AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        return row == null ? Result<CustomerRequestDto>.Fail("طلب العميل غير موجود.") : Result<CustomerRequestDto>.Ok(_mapper.Map<CustomerRequestDto>(row));
    }

    public async Task<Result<CustomerRequestDto>> ApproveAsync(int id)
    {
        var row = await _db.CustomerRequests.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (row == null || row.Status != CustomerRequestStatus.Submitted) return Result<CustomerRequestDto>.Fail("لا يمكن اعتماد هذا الطلب.");
        row.Status = CustomerRequestStatus.Approved; row.ApprovedBy = _session.CurrentUser?.Id; row.ApprovedDate = DateTime.Now; row.UpdatedDate = DateTime.Now;
        await _db.SaveChangesAsync(); return Result<CustomerRequestDto>.Ok(_mapper.Map<CustomerRequestDto>(row));
    }

    public async Task<Result<CustomerRequestDto>> CancelAsync(int id)
    {
        var row = await _db.CustomerRequests.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (row == null || row.Status == CustomerRequestStatus.Cancelled || row.SalesStatus == CustomerRequestSalesStatus.Sold) return Result<CustomerRequestDto>.Fail("لا يمكن إلغاء هذا الطلب.");
        row.Status = CustomerRequestStatus.Cancelled; row.CancelledBy = _session.CurrentUser?.Id; row.CancelledDate = DateTime.Now; row.UpdatedDate = DateTime.Now;
        await _db.SaveChangesAsync(); return Result<CustomerRequestDto>.Ok(_mapper.Map<CustomerRequestDto>(row));
    }

    public async Task<Result<CustomerRequestDto>> UpdateReceivingAsync(int id, CustomerRequestReceivingStatus status, IReadOnlyDictionary<int, decimal> receivedQuantities)
    {
        var row = await _db.CustomerRequests.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (row == null) return Result<CustomerRequestDto>.Fail("طلب العميل غير موجود.");
        foreach (var line in row.Lines)
            if (receivedQuantities.TryGetValue(line.Id, out var value)) line.ReceivedQuantity = Math.Clamp(value, 0m, line.Quantity);
        row.ReceivingStatus = status; row.UpdatedDate = DateTime.Now;
        await _db.SaveChangesAsync(); return Result<CustomerRequestDto>.Ok(_mapper.Map<CustomerRequestDto>(row));
    }

    public async Task<Result<PurchaseRequestWriteDto>> CreatePurchaseRequestAsync(int id)
    {
        var row = await _db.CustomerRequests.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (row == null) return Result<PurchaseRequestWriteDto>.Fail("طلب العميل غير موجود.");
        if (row.PurchaseRequestId.HasValue) return Result<PurchaseRequestWriteDto>.Fail("تم إنشاء طلب شراء لهذا الطلب مسبقاً.");
        var pendingLines = row.Lines.Where(x => x.Quantity > x.ReceivedQuantity).ToList();
        if (pendingLines.Any(x => !x.ProductId.HasValue || !x.ProductUnitId.HasValue))
            return Result<PurchaseRequestWriteDto>.Fail("لا يمكن إنشاء طلب شراء لمادة يدوية. أضف المادة إلى المنتجات أولاً.");
        var dto = new PurchaseRequestWriteDto { Category = PurchaseRequestCategory.Other, RequestDate = DateTime.Now, RequesterName = row.CustomerName, Notes = $"طلب شراء مرتبط بطلب العميل {row.RequestNumber}" };
        dto.Lines = pendingLines.Select(x => new PurchaseRequestLineWriteDto { ProductId = x.ProductId, ProductUnitId = x.ProductUnitId, ItemName = x.ItemName, UnitName = x.UnitName, RequestedQuantity = Math.Max(0m, x.Quantity - x.ReceivedQuantity), UnitPrice = x.UnitPrice }).Where(x => x.RequestedQuantity > 0).ToList();
        if (dto.Lines.Count == 0) return Result<PurchaseRequestWriteDto>.Fail("لا توجد كميات متبقية للشراء.");
        var result = await _purchaseRequests.CreateAsync(dto);
        if (!result.Success || result.Data == null) return result;
        row.PurchaseRequestId = result.Data.Id; row.UpdatedDate = DateTime.Now; await _db.SaveChangesAsync();
        return result;
    }

    public async Task<Result<CustomerRequestDto>> MarkSoldAsync(int id, IReadOnlyDictionary<int, decimal> quantities)
    {
        var row = await _db.CustomerRequests.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (row == null || row.Status != CustomerRequestStatus.Approved) return Result<CustomerRequestDto>.Fail("يجب اعتماد الطلب قبل تسجيل البيع.");
        foreach (var line in row.Lines)
            if (quantities.TryGetValue(line.Id, out var value)) line.SoldQuantity = Math.Clamp(line.SoldQuantity + value, 0m, line.Quantity);
        var total = row.Lines.Sum(x => x.Quantity); var sold = row.Lines.Sum(x => x.SoldQuantity);
        row.SalesStatus = sold <= 0 ? CustomerRequestSalesStatus.NotSold : sold >= total ? CustomerRequestSalesStatus.Sold : CustomerRequestSalesStatus.PartiallySold;
        row.UpdatedDate = DateTime.Now; await _db.SaveChangesAsync(); return Result<CustomerRequestDto>.Ok(_mapper.Map<CustomerRequestDto>(row));
    }
}
