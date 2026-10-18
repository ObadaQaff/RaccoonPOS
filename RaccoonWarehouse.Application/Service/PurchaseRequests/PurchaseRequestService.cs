using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.PurchaseRequests;
using RaccoonWarehouse.Domain.PurchaseRequests.DTOs;

namespace RaccoonWarehouse.Application.Service.PurchaseRequests;

public interface IPurchaseRequestService
{
    Task<Result<PurchaseRequestWriteDto>> CreateAsync(PurchaseRequestWriteDto dto);
    Task<Result<List<PurchaseRequestReadDto>>> GetAllAsync();
    Task<Result<PurchaseRequestWriteDto>> SubmitAsync(int id);
    Task<Result<PurchaseRequestWriteDto>> ApproveAsync(int id);
    Task<Result<PurchaseRequestWriteDto>> RejectAsync(int id, string reason);
}

public sealed class PurchaseRequestService : IPurchaseRequestService
{
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;
    private readonly IPermissionService _permissionService;
    private readonly IUserSession _userSession;

    public PurchaseRequestService(
        ApplicationDbContext db,
        IMapper mapper,
        IPermissionService permissionService,
        IUserSession userSession)
    {
        _db = db;
        _mapper = mapper;
        _permissionService = permissionService;
        _userSession = userSession;
    }

    public async Task<Result<PurchaseRequestWriteDto>> CreateAsync(PurchaseRequestWriteDto dto)
    {
        if (!await _permissionService.HasCurrentUserPermissionAsync("PurchaseRequest.Create"))
            return Result<PurchaseRequestWriteDto>.Fail("You do not have permission to create purchase requests.");
        if (dto.Lines == null || dto.Lines.Count == 0)
            return Result<PurchaseRequestWriteDto>.Fail("A purchase request must contain at least one line.");
        if (dto.Lines.Any(line => string.IsNullOrWhiteSpace(line.ItemName) || line.RequestedQuantity <= 0))
            return Result<PurchaseRequestWriteDto>.Fail("Every purchase request line must have an item name and positive quantity.");
        if (dto.Lines.GroupBy(line => new { ProductId = line.ProductId, ItemName = line.ProductId.HasValue ? null : line.ItemName.Trim().ToUpperInvariant(), line.ProductUnitId }).Any(group => group.Count() > 1))
            return Result<PurchaseRequestWriteDto>.Fail("Duplicate product lines are not allowed.");

        var now = DateTime.Now;
        var entity = _mapper.Map<PurchaseRequest>(dto);
        entity.RequestNumber = string.IsNullOrWhiteSpace(dto.RequestNumber)
            ? $"PR-{now:yyyyMMddHHmmssfff}"
            : dto.RequestNumber.Trim();
        entity.RequestDate = dto.RequestDate == default ? now : dto.RequestDate;
        entity.RequesterId ??= _userSession.CurrentUser?.Id;
        entity.Status = PurchaseRequestStatus.Draft;
        entity.CreatedDate = now;
        entity.UpdatedDate = now;
        entity.Lines = dto.Lines.Select(line =>
        {
            var mapped = _mapper.Map<PurchaseRequestLine>(line);
            mapped.ItemName = line.ItemName.Trim();
            mapped.ApprovedQuantity = line.ApprovedQuantity > 0 ? line.ApprovedQuantity : line.RequestedQuantity;
            mapped.CreatedDate = now;
            mapped.UpdatedDate = now;
            return mapped;
        }).ToList();

        await _db.PurchaseRequests.AddAsync(entity);
        await _db.SaveChangesAsync();
        return Result<PurchaseRequestWriteDto>.Ok(_mapper.Map<PurchaseRequestWriteDto>(entity), "Purchase request saved as draft.");
    }

    public async Task<Result<List<PurchaseRequestReadDto>>> GetAllAsync()
    {
        if (!await _permissionService.HasCurrentUserPermissionAsync("PurchaseRequest.View"))
            return Result<List<PurchaseRequestReadDto>>.Fail("You do not have permission to view purchase requests.");

        var requests = await _db.PurchaseRequests
            .AsNoTracking()
            .Include(x => x.Lines)
            .OrderByDescending(x => x.RequestDate)
            .ToListAsync();
        return Result<List<PurchaseRequestReadDto>>.Ok(_mapper.Map<List<PurchaseRequestReadDto>>(requests));
    }

    public async Task<Result<PurchaseRequestWriteDto>> SubmitAsync(int id)
    {
        if (!await _permissionService.HasCurrentUserPermissionAsync("PurchaseRequest.Submit"))
            return Result<PurchaseRequestWriteDto>.Fail("You do not have permission to submit purchase requests.");
        var request = await _db.PurchaseRequests.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (request == null) return Result<PurchaseRequestWriteDto>.Fail("Purchase request was not found.");
        if (request.Status != PurchaseRequestStatus.Draft)
            return Result<PurchaseRequestWriteDto>.Fail("Only draft purchase requests can be submitted.");
        request.Status = PurchaseRequestStatus.Submitted;
        request.SubmittedBy = _userSession.CurrentUser?.Id;
        request.SubmittedDate = DateTime.Now;
        request.UpdatedDate = DateTime.Now;
        await _db.SaveChangesAsync();
        return Result<PurchaseRequestWriteDto>.Ok(_mapper.Map<PurchaseRequestWriteDto>(request), "Purchase request submitted for approval.");
    }

    public async Task<Result<PurchaseRequestWriteDto>> ApproveAsync(int id)
    {
        if (!await _permissionService.HasCurrentUserPermissionAsync("PurchaseRequest.Approve"))
            return Result<PurchaseRequestWriteDto>.Fail("You do not have permission to approve purchase requests.");
        var request = await _db.PurchaseRequests.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (request == null) return Result<PurchaseRequestWriteDto>.Fail("Purchase request was not found.");
        if (request.Status != PurchaseRequestStatus.Submitted)
            return Result<PurchaseRequestWriteDto>.Fail("Only submitted purchase requests can be approved.");
        if (request.Lines.Any(x => x.ApprovedQuantity <= 0))
            return Result<PurchaseRequestWriteDto>.Fail("Approved quantities must be positive.");
        request.Status = PurchaseRequestStatus.Approved;
        request.ApprovedBy = _userSession.CurrentUser?.Id;
        request.ApprovedDate = DateTime.Now;
        request.UpdatedDate = DateTime.Now;
        await _db.SaveChangesAsync();
        return Result<PurchaseRequestWriteDto>.Ok(_mapper.Map<PurchaseRequestWriteDto>(request), "Purchase request approved. No inventory or accounting entry was posted.");
    }

    public async Task<Result<PurchaseRequestWriteDto>> RejectAsync(int id, string reason)
    {
        if (!await _permissionService.HasCurrentUserPermissionAsync("PurchaseRequest.Reject"))
            return Result<PurchaseRequestWriteDto>.Fail("You do not have permission to reject purchase requests.");
        if (string.IsNullOrWhiteSpace(reason))
            return Result<PurchaseRequestWriteDto>.Fail("A rejection reason is required.");
        var request = await _db.PurchaseRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (request == null) return Result<PurchaseRequestWriteDto>.Fail("Purchase request was not found.");
        if (request.Status != PurchaseRequestStatus.Submitted)
            return Result<PurchaseRequestWriteDto>.Fail("Only submitted purchase requests can be rejected.");
        request.Status = PurchaseRequestStatus.Rejected;
        request.RejectedBy = _userSession.CurrentUser?.Id;
        request.RejectedDate = DateTime.Now;
        request.RejectionReason = reason.Trim();
        request.UpdatedDate = DateTime.Now;
        await _db.SaveChangesAsync();
        return Result<PurchaseRequestWriteDto>.Ok(_mapper.Map<PurchaseRequestWriteDto>(request), "Purchase request rejected.");
    }
}
