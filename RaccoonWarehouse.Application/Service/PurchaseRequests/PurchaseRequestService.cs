using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.PurchaseRequests;
using RaccoonWarehouse.Domain.PurchaseRequests.DTOs;
using RaccoonWarehouse.Domain.Users;

namespace RaccoonWarehouse.Application.Service.PurchaseRequests;

public interface IPurchaseRequestService
{
    Task<Result<PurchaseRequestWriteDto>> CreateAsync(PurchaseRequestWriteDto dto);
    Task<Result<PurchaseRequestWriteDto>> UpdateAsync(PurchaseRequestWriteDto dto);
    Task<Result<List<PurchaseRequestReadDto>>> GetAllAsync();
    Task<Result<PurchaseRequestReadDto>> GetByIdAsync(int id);
    Task<Result<bool>> DeleteAsync(int id);
    Task<Result<PurchaseRequestWriteDto>> SubmitAsync(int id);
    Task<Result<PurchaseRequestWriteDto>> ApproveAsync(int id);
    Task<Result<PurchaseRequestWriteDto>> ReviewAsync(PurchaseRequestApprovalDto dto);
    Task<Result<PurchaseRequestWriteDto>> RejectAsync(int id, string reason);
}

public sealed class PurchaseRequestService : IPurchaseRequestService
{
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;
    private readonly IUserSession _userSession;

    public PurchaseRequestService(
        ApplicationDbContext db,
        IMapper mapper,
        IUserSession userSession)
    {
        _db = db;
        _mapper = mapper;
        _userSession = userSession;
    }

    public async Task<Result<PurchaseRequestWriteDto>> CreateAsync(PurchaseRequestWriteDto dto)
    {
if (dto.Lines == null || dto.Lines.Count == 0)
            return Result<PurchaseRequestWriteDto>.Fail("A purchase request must contain at least one line.");
        if (dto.Lines.Any(line => string.IsNullOrWhiteSpace(line.ItemName) || line.RequestedQuantity <= 0 || line.UnitPrice < 0))
            return Result<PurchaseRequestWriteDto>.Fail("Every purchase request line must have an item name and positive quantity.");
        if (dto.Category == PurchaseRequestCategory.Drugs && dto.Lines.Any(line => !line.ProductId.HasValue || !line.ProductUnitId.HasValue))
            return Result<PurchaseRequestWriteDto>.Fail("Drug requests must use products and purchase units from the catalog.");
        if (dto.Category == PurchaseRequestCategory.Other && dto.Lines.Any(line => line.ProductId.HasValue || line.ProductUnitId.HasValue))
            return Result<PurchaseRequestWriteDto>.Fail("Other requests must contain free-text lines only.");
        if (dto.Lines.GroupBy(line => new { ProductId = line.ProductId, ItemName = line.ProductId.HasValue ? null : line.ItemName.Trim().ToUpperInvariant(), line.ProductUnitId }).Any(group => group.Count() > 1))
            return Result<PurchaseRequestWriteDto>.Fail("Duplicate product lines are not allowed.");

        var catalogLines = dto.Lines.Where(line => line.ProductId.HasValue && line.ProductUnitId.HasValue).ToList();
        var productIds = catalogLines.Select(line => line.ProductId!.Value).Distinct().ToList();
        var unitIds = catalogLines.Select(line => line.ProductUnitId!.Value).Distinct().ToList();
        var products = await _db.Set<Product>()
            .AsNoTracking()
            .Include(product => product.ProductUnits!)
                .ThenInclude(unit => unit.Unit)
            .Include(product => product.Supplier)
            .Where(product => !product.IsDeleted && productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id);
        var units = await _db.Set<ProductUnit>()
            .AsNoTracking()
            .Include(unit => unit.Unit)
            .Where(unit => unitIds.Contains(unit.Id))
            .ToDictionaryAsync(unit => unit.Id);
        foreach (var line in catalogLines)
        {
            if (!products.TryGetValue(line.ProductId!.Value, out var product))
                return Result<PurchaseRequestWriteDto>.Fail($"Product {line.ProductId} was not found.");
            if (!units.TryGetValue(line.ProductUnitId!.Value, out var unit) || unit.ProductId != product.Id)
                return Result<PurchaseRequestWriteDto>.Fail($"The selected purchase unit is not valid for {product.Name}.");
        }

        var now = DateTime.Now;
        var entity = _mapper.Map<PurchaseRequest>(dto);
        entity.RequestNumber = string.IsNullOrWhiteSpace(dto.RequestNumber)
            ? $"PR-TEMP-{Guid.NewGuid():N}"
            : dto.RequestNumber.Trim();
        entity.RequestDate = dto.RequestDate == default ? now : dto.RequestDate;
        entity.RequesterId ??= _userSession.CurrentUser?.Id;
        entity.RequesterName ??= _userSession.CurrentUser?.Name;
        entity.Category = dto.Category;
        entity.Status = PurchaseRequestStatus.Draft;
        entity.CreatedDate = now;
        entity.UpdatedDate = now;
        entity.Lines = dto.Lines.Select(line =>
        {
            var mapped = _mapper.Map<PurchaseRequestLine>(line);
            mapped.ItemName = line.ItemName.Trim();
            if (line.ProductId.HasValue && products.TryGetValue(line.ProductId.Value, out var product))
            {
                var unit = units[line.ProductUnitId!.Value];
                mapped.ItemName = product.Name?.Trim() ?? mapped.ItemName;
                mapped.Barcode = product.ITEMCODE?.ToString() ?? line.Barcode?.Trim();
                mapped.UnitName = unit.Unit?.Name ?? line.UnitName?.Trim();
                mapped.SupplierId = product.SupplierId;
                mapped.SupplierName = product.Supplier?.Name ?? line.SupplierName?.Trim();
                mapped.UnitPrice = line.UnitPrice > 0 ? line.UnitPrice : unit.PurchasePrice;
            }
            else
            {
                mapped.Barcode = line.Barcode?.Trim();
                mapped.UnitName = line.UnitName?.Trim();
                mapped.SupplierName = line.SupplierName?.Trim();
            }
            mapped.ApprovedQuantity = line.ApprovedQuantity > 0 ? line.ApprovedQuantity : line.RequestedQuantity;
            mapped.ApprovedUnitPrice = line.ApprovedUnitPrice > 0 ? line.ApprovedUnitPrice : mapped.UnitPrice;
            mapped.Decision = PurchaseRequestLineDecision.Pending;
            mapped.CreatedDate = now;
            mapped.UpdatedDate = now;
            return mapped;
        }).ToList();

        await _db.PurchaseRequests.AddAsync(entity);
        await _db.SaveChangesAsync();
        if (entity.RequestNumber.StartsWith("PR-TEMP-", StringComparison.Ordinal))
        {
            entity.RequestNumber = $"PR-{entity.Id:D4}";
            await _db.SaveChangesAsync();
        }
        return Result<PurchaseRequestWriteDto>.Ok(_mapper.Map<PurchaseRequestWriteDto>(entity), "Purchase request saved as draft.");
    }

    public async Task<Result<PurchaseRequestWriteDto>> UpdateAsync(PurchaseRequestWriteDto dto)
    {
if (dto.Id <= 0)
            return Result<PurchaseRequestWriteDto>.Fail("A valid purchase request is required.");
        if (dto.Lines == null || dto.Lines.Count == 0)
            return Result<PurchaseRequestWriteDto>.Fail("A purchase request must contain at least one line.");
        if (dto.Lines.Any(line => string.IsNullOrWhiteSpace(line.ItemName) || line.RequestedQuantity <= 0 || line.UnitPrice < 0))
            return Result<PurchaseRequestWriteDto>.Fail("Every purchase request line must have an item name and positive quantity.");
        if (dto.Category == PurchaseRequestCategory.Drugs && dto.Lines.Any(line => !line.ProductId.HasValue || !line.ProductUnitId.HasValue))
            return Result<PurchaseRequestWriteDto>.Fail("Drug requests must use products and purchase units from the catalog.");
        if (dto.Category == PurchaseRequestCategory.Other && dto.Lines.Any(line => line.ProductId.HasValue || line.ProductUnitId.HasValue))
            return Result<PurchaseRequestWriteDto>.Fail("Other requests must contain free-text lines only.");
        if (dto.Lines.GroupBy(line => new { ProductId = line.ProductId, ItemName = line.ProductId.HasValue ? null : line.ItemName.Trim().ToUpperInvariant(), line.ProductUnitId }).Any(group => group.Count() > 1))
            return Result<PurchaseRequestWriteDto>.Fail("Duplicate product lines are not allowed.");

        var entity = await _db.PurchaseRequests
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == dto.Id);
        if (entity == null)
            return Result<PurchaseRequestWriteDto>.Fail("Purchase request was not found.");
        if (entity.Status != PurchaseRequestStatus.Draft && entity.Status != PurchaseRequestStatus.Rejected)
            return Result<PurchaseRequestWriteDto>.Fail("Only draft or rejected purchase requests can be edited.");

        var catalogLines = dto.Lines.Where(line => line.ProductId.HasValue && line.ProductUnitId.HasValue).ToList();
        var productIds = catalogLines.Select(line => line.ProductId!.Value).Distinct().ToList();
        var unitIds = catalogLines.Select(line => line.ProductUnitId!.Value).Distinct().ToList();
        var products = await _db.Set<Product>()
            .AsNoTracking()
            .Include(product => product.ProductUnits!).ThenInclude(unit => unit.Unit)
            .Include(product => product.Supplier)
            .Where(product => !product.IsDeleted && productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id);
        var units = await _db.Set<ProductUnit>()
            .AsNoTracking()
            .Include(unit => unit.Unit)
            .Where(unit => unitIds.Contains(unit.Id))
            .ToDictionaryAsync(unit => unit.Id);

        foreach (var line in catalogLines)
        {
            if (!products.TryGetValue(line.ProductId!.Value, out var product))
                return Result<PurchaseRequestWriteDto>.Fail($"Product {line.ProductId} was not found.");
            if (!units.TryGetValue(line.ProductUnitId!.Value, out var unit) || unit.ProductId != product.Id)
                return Result<PurchaseRequestWriteDto>.Fail($"The selected purchase unit is not valid for {product.Name}.");
        }

        var now = DateTime.Now;
        entity.Category = dto.Category;
        entity.RequestDate = dto.RequestDate == default ? entity.RequestDate : dto.RequestDate;
        entity.RequesterId = dto.RequesterId ?? entity.RequesterId ?? _userSession.CurrentUser?.Id;
        entity.RequesterName = dto.RequesterName ?? entity.RequesterName ?? _userSession.CurrentUser?.Name;
        entity.BranchId = dto.BranchId;
        entity.Department = dto.Department?.Trim();
        entity.Priority = dto.Priority?.Trim();
        entity.Justification = dto.Justification?.Trim();
        entity.Notes = dto.Notes?.Trim();
        entity.RejectionReason = null;
        entity.RejectedBy = null;
        entity.RejectedDate = null;
        entity.UpdatedDate = now;

        _db.Set<PurchaseRequestLine>().RemoveRange(entity.Lines);
        entity.Lines = dto.Lines.Select(line =>
        {
            var mapped = _mapper.Map<PurchaseRequestLine>(line);
            mapped.ItemName = line.ItemName.Trim();
            if (line.ProductId.HasValue && products.TryGetValue(line.ProductId.Value, out var product))
            {
                var unit = units[line.ProductUnitId!.Value];
                mapped.ItemName = product.Name?.Trim() ?? mapped.ItemName;
                mapped.Barcode = product.ITEMCODE?.ToString() ?? line.Barcode?.Trim();
                mapped.UnitName = unit.Unit?.Name ?? line.UnitName?.Trim();
                mapped.SupplierId = product.SupplierId;
                mapped.SupplierName = product.Supplier?.Name ?? line.SupplierName?.Trim();
                mapped.UnitPrice = line.UnitPrice > 0 ? line.UnitPrice : unit.PurchasePrice;
            }
            else
            {
                mapped.Barcode = line.Barcode?.Trim();
                mapped.UnitName = line.UnitName?.Trim();
                mapped.SupplierName = line.SupplierName?.Trim();
            }
            mapped.ApprovedQuantity = line.ApprovedQuantity > 0 ? line.ApprovedQuantity : line.RequestedQuantity;
            mapped.ApprovedUnitPrice = line.ApprovedUnitPrice > 0 ? line.ApprovedUnitPrice : mapped.UnitPrice;
            mapped.Decision = PurchaseRequestLineDecision.Pending;
            mapped.CreatedDate = now;
            mapped.UpdatedDate = now;
            return mapped;
        }).ToList();

        await _db.SaveChangesAsync();
        return Result<PurchaseRequestWriteDto>.Ok(_mapper.Map<PurchaseRequestWriteDto>(entity), "Purchase request updated as draft.");
    }

    public async Task<Result<List<PurchaseRequestReadDto>>> GetAllAsync()
    {
var requests = await _db.PurchaseRequests
            .AsNoTracking()
            .Include(x => x.Lines)
            .OrderByDescending(x => x.RequestDate)
            .ToListAsync();
        return Result<List<PurchaseRequestReadDto>>.Ok(_mapper.Map<List<PurchaseRequestReadDto>>(requests));
    }

    public async Task<Result<PurchaseRequestReadDto>> GetByIdAsync(int id)
    {
var request = await _db.PurchaseRequests
            .AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (request == null)
            return Result<PurchaseRequestReadDto>.Fail("Purchase request was not found.");

        var dto = _mapper.Map<PurchaseRequestReadDto>(request);
        var userIds = new[] { dto.SubmittedBy, dto.ApprovedBy, dto.RejectedBy }
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();
        if (userIds.Count > 0)
        {
            var userNames = await _db.Set<User>()
                .AsNoTracking()
                .Where(x => userIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name);
            dto.SubmittedByName = dto.SubmittedBy.HasValue && userNames.TryGetValue(dto.SubmittedBy.Value, out var submittedByName)
                ? submittedByName
                : null;
            dto.ApprovedByName = dto.ApprovedBy.HasValue && userNames.TryGetValue(dto.ApprovedBy.Value, out var approvedByName)
                ? approvedByName
                : null;
            dto.RejectedByName = dto.RejectedBy.HasValue && userNames.TryGetValue(dto.RejectedBy.Value, out var rejectedByName)
                ? rejectedByName
                : null;
        }

        return Result<PurchaseRequestReadDto>.Ok(dto);
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
var request = await _db.PurchaseRequests
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (request == null)
            return Result<bool>.Fail("Purchase request was not found.");
        if (request.Status != PurchaseRequestStatus.Draft)
            return Result<bool>.Fail("Only draft purchase requests can be deleted.");

        _db.PurchaseRequestLines.RemoveRange(request.Lines);
        _db.PurchaseRequests.Remove(request);
        await _db.SaveChangesAsync();
        return Result<bool>.Ok(true, "Purchase request deleted.");
    }

    public async Task<Result<PurchaseRequestWriteDto>> SubmitAsync(int id)
    {
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
var request = await _db.PurchaseRequests.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (request == null) return Result<PurchaseRequestWriteDto>.Fail("Purchase request was not found.");
        if (request.Status != PurchaseRequestStatus.Submitted)
            return Result<PurchaseRequestWriteDto>.Fail("Only submitted purchase requests can be approved.");
        return await ReviewAsync(new PurchaseRequestApprovalDto
        {
            RequestId = id,
            Lines = request.Lines.Select(line => new PurchaseRequestApprovalLineDto
            {
                LineId = line.Id,
                Decision = PurchaseRequestLineDecision.Approved,
                ApprovedQuantity = line.RequestedQuantity,
                ApprovedUnitPrice = line.UnitPrice
            }).ToList()
        });
    }

    public async Task<Result<PurchaseRequestWriteDto>> ReviewAsync(PurchaseRequestApprovalDto dto)
    {
if (dto.RequestId <= 0)
            return Result<PurchaseRequestWriteDto>.Fail("A valid purchase request is required.");
        if (dto.Lines == null || dto.Lines.Count == 0)
            return Result<PurchaseRequestWriteDto>.Fail("At least one request line is required.");

        var request = await _db.PurchaseRequests
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == dto.RequestId);
        if (request == null)
            return Result<PurchaseRequestWriteDto>.Fail("Purchase request was not found.");
        if (request.Status != PurchaseRequestStatus.Submitted)
            return Result<PurchaseRequestWriteDto>.Fail("Only submitted purchase requests can be reviewed.");

        var submittedLineIds = request.Lines.Select(x => x.Id).ToHashSet();
        if (dto.Lines.Select(x => x.LineId).Distinct().Count() != dto.Lines.Count)
            return Result<PurchaseRequestWriteDto>.Fail("The approval data contains duplicate request lines.");
        var approvalLines = dto.Lines.ToDictionary(x => x.LineId);
        if (approvalLines.Count != request.Lines.Count || approvalLines.Keys.Any(x => !submittedLineIds.Contains(x)))
            return Result<PurchaseRequestWriteDto>.Fail("The approval data does not match the submitted request lines.");

        foreach (var line in request.Lines)
        {
            var decision = approvalLines[line.Id];
            if (decision.Decision == PurchaseRequestLineDecision.Approved)
            {
                if (decision.ApprovedQuantity <= 0 || decision.ApprovedQuantity > line.RequestedQuantity)
                    return Result<PurchaseRequestWriteDto>.Fail($"Approved quantity for {line.ItemName} must be greater than zero and no more than the requested quantity.");
                if (decision.ApprovedUnitPrice < 0)
                    return Result<PurchaseRequestWriteDto>.Fail($"Approved price for {line.ItemName} cannot be negative.");
            }
            else if (decision.Decision == PurchaseRequestLineDecision.Rejected)
            {
                if (string.IsNullOrWhiteSpace(decision.ApprovalNotes))
                    return Result<PurchaseRequestWriteDto>.Fail($"Enter a rejection note for {line.ItemName}.");
            }
            else
            {
                return Result<PurchaseRequestWriteDto>.Fail($"Choose approve or reject for {line.ItemName}.");
            }
        }

        var approvedLines = request.Lines.Where(line => approvalLines[line.Id].Decision == PurchaseRequestLineDecision.Approved).ToList();
        var now = DateTime.Now;
        var reviewNotes = dto.ReviewNotes?.Trim();
        if (approvedLines.Count == 0 && string.IsNullOrWhiteSpace(reviewNotes))
            return Result<PurchaseRequestWriteDto>.Fail("Enter an overall review note when rejecting the purchase request.");

        foreach (var line in request.Lines)
        {
            var decision = approvalLines[line.Id];
            line.Decision = decision.Decision;
            line.ApprovedQuantity = decision.Decision == PurchaseRequestLineDecision.Approved ? decision.ApprovedQuantity : 0m;
            line.ApprovedUnitPrice = decision.Decision == PurchaseRequestLineDecision.Approved ? decision.ApprovedUnitPrice : 0m;
            line.ApprovalNotes = decision.ApprovalNotes?.Trim();
            line.UpdatedDate = now;
        }

        request.Status = approvedLines.Count == 0 ? PurchaseRequestStatus.Rejected : PurchaseRequestStatus.Approved;
        request.ApprovedBy = approvedLines.Count == 0 ? null : _userSession.CurrentUser?.Id;
        request.ApprovedDate = approvedLines.Count == 0 ? null : now;
        request.RejectedBy = approvedLines.Count == 0 ? _userSession.CurrentUser?.Id : null;
        request.RejectedDate = approvedLines.Count == 0 ? now : null;
        request.RejectionReason = approvedLines.Count == 0 ? "All request lines were rejected during approval." : null;
        request.ReviewNotes = reviewNotes;
        request.UpdatedDate = now;

        await _db.SaveChangesAsync();
        var message = approvedLines.Count == 0
            ? "Purchase request rejected."
            : "Purchase request approved. No inventory or accounting entry was posted.";
        return Result<PurchaseRequestWriteDto>.Ok(_mapper.Map<PurchaseRequestWriteDto>(request), message);
    }

    public async Task<Result<PurchaseRequestWriteDto>> RejectAsync(int id, string reason)
    {
        if (_userSession.CurrentRole != UserRole.Admin)
            return Result<PurchaseRequestWriteDto>.Fail("Only administrators can reject purchase requests.");
if (string.IsNullOrWhiteSpace(reason))
            return Result<PurchaseRequestWriteDto>.Fail("A rejection reason is required.");
        var request = await _db.PurchaseRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (request == null) return Result<PurchaseRequestWriteDto>.Fail("Purchase request was not found.");
        if (request.Status != PurchaseRequestStatus.Draft && request.Status != PurchaseRequestStatus.Submitted)
            return Result<PurchaseRequestWriteDto>.Fail("Only draft or submitted purchase requests can be rejected.");
        request.Status = PurchaseRequestStatus.Rejected;
        request.RejectedBy = _userSession.CurrentUser?.Id;
        request.RejectedDate = DateTime.Now;
        request.RejectionReason = reason.Trim();
        request.ReviewNotes = reason.Trim();
        request.UpdatedDate = DateTime.Now;
        await _db.SaveChangesAsync();
        return Result<PurchaseRequestWriteDto>.Ok(_mapper.Map<PurchaseRequestWriteDto>(request), "Purchase request rejected.");
    }
}
