using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.PurchaseOrders;
using RaccoonWarehouse.Domain.PurchaseOrders.DTOs;
using RaccoonWarehouse.Domain.PurchaseRequests;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.StockTransactions;
using RaccoonWarehouse.Domain.Users;

namespace RaccoonWarehouse.Application.Service.PurchaseOrders
{
    public interface IPurchaseOrderService
    {
        Task<Result<PurchaseOrderWriteDto>> CreateAsync(PurchaseOrderWriteDto dto);
        Task<Result<PurchaseOrderWriteDto>> CreateFromPurchaseRequestAsync(int purchaseRequestId);
        Task<Result<List<PurchaseOrderReadDto>>> GetAllAsync();
        Task<Result<PurchaseOrderWriteDto>> GetByIdAsync(int id);
        Task<Result<PurchaseOrderWriteDto>> ApproveAsync(int id);
        Task<Result<PurchaseOrderWriteDto>> RejectAsync(int id, string reason);
    }

    public sealed class PurchaseOrderService : IPurchaseOrderService
    {
        private const string ApprovePermission = "PurchaseOrder.Approve";
        private readonly ApplicationDbContext _db;
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        private readonly IInvoiceService _invoiceService;
        private readonly IStockService _stockService;
        private readonly IPermissionService _permissionService;
        private readonly IUserSession _userSession;

        public PurchaseOrderService(
            ApplicationDbContext db,
            IUOW uow,
            IMapper mapper,
            IInvoiceService invoiceService,
            IStockService stockService,
            IPermissionService permissionService,
            IUserSession userSession)
        {
            _db = db;
            _uow = uow;
            _mapper = mapper;
            _invoiceService = invoiceService;
            _stockService = stockService;
            _permissionService = permissionService;
            _userSession = userSession;
        }

        public async Task<Result<PurchaseOrderWriteDto>> CreateAsync(PurchaseOrderWriteDto dto)
        {
            if (!await _permissionService.HasCurrentUserPermissionAsync("PurchaseOrder.Create"))
                return Result<PurchaseOrderWriteDto>.Fail("You do not have permission to create purchase orders.");
            if (dto.Lines == null || dto.Lines.Count == 0)
                return Result<PurchaseOrderWriteDto>.Fail("A purchase order must contain at least one line.");
            if (dto.Lines.Any(x => x.ProductId <= 0 || x.ProductUnitId <= 0 || x.Quantity <= 0))
                return Result<PurchaseOrderWriteDto>.Fail("Every purchase order line must have a product, unit, and positive quantity.");

            var requestLineIds = dto.Lines.Where(x => x.PurchaseRequestLineId.HasValue)
                .Select(x => x.PurchaseRequestLineId!.Value).Distinct().ToList();
            if (requestLineIds.Count > 0)
            {
                var requestLines = await _db.PurchaseRequestLines
                    .Include(x => x.PurchaseRequest)
                    .Where(x => requestLineIds.Contains(x.Id))
                    .ToListAsync();
                if (requestLines.Count != requestLineIds.Count)
                    return Result<PurchaseOrderWriteDto>.Fail("One or more purchase request lines were not found.");
                if (requestLines.Any(x => x.PurchaseRequest.Status != PurchaseRequestStatus.Approved))
                    return Result<PurchaseOrderWriteDto>.Fail("Only approved purchase request lines can generate a purchase order.");
                if (dto.Lines.Any(line => line.PurchaseRequestLineId.HasValue &&
                    requestLines.First(x => x.Id == line.PurchaseRequestLineId!.Value).ProductId != line.ProductId))
                    return Result<PurchaseOrderWriteDto>.Fail("Purchase order lines must match their purchase request products.");
            }

            var now = DateTime.Now;
            var entity = _mapper.Map<PurchaseOrder>(dto);
            entity.OrderNumber = string.IsNullOrWhiteSpace(dto.OrderNumber)
                ? $"PO-{now:yyyyMMddHHmmssfff}"
                : dto.OrderNumber.Trim();
            entity.Status = PurchaseOrderStatus.Submitted;
            entity.CreatedBy = _userSession.CurrentUser?.Id;
            entity.SubmittedBy = _userSession.CurrentUser?.Id;
            entity.CreatedDate = now;
            entity.UpdatedDate = now;
            entity.Lines = dto.Lines.Select(line =>
            {
                var mapped = _mapper.Map<PurchaseOrderLine>(line);
                mapped.CreatedDate = now;
                mapped.UpdatedDate = now;
                return mapped;
            }).ToList();

            await _db.PurchaseOrders.AddAsync(entity);
            await _db.SaveChangesAsync();
            return Result<PurchaseOrderWriteDto>.Ok(_mapper.Map<PurchaseOrderWriteDto>(entity), "Purchase order submitted for approval.");
        }

        public async Task<Result<PurchaseOrderWriteDto>> CreateFromPurchaseRequestAsync(int purchaseRequestId)
        {
            if (!await _permissionService.HasCurrentUserPermissionAsync("PurchaseOrder.Create"))
                return Result<PurchaseOrderWriteDto>.Fail("You do not have permission to create purchase orders.");

            var request = await _db.PurchaseRequests
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == purchaseRequestId);
            if (request == null)
                return Result<PurchaseOrderWriteDto>.Fail("Purchase request was not found.");
            if (request.Status != PurchaseRequestStatus.Approved)
                return Result<PurchaseOrderWriteDto>.Fail("Only approved purchase requests can generate purchase orders.");
            if (request.Lines.Count == 0)
                return Result<PurchaseOrderWriteDto>.Fail("The approved purchase request has no lines.");
            if (request.Lines.Any(x => !x.ProductId.HasValue))
                return Result<PurchaseOrderWriteDto>.Fail("This PR contains free-text items. Link each item to a catalog product before creating a PO.");

            var productIds = request.Lines.Select(x => x.ProductId!.Value).Distinct().ToList();
            var units = await _db.Set<ProductUnit>()
                .Where(x => productIds.Contains(x.ProductId))
                .GroupBy(x => x.ProductId)
                .Select(group => group.OrderByDescending(x => x.IsDefaultPurchaseUnit).ThenByDescending(x => x.IsBaseUnit).First())
                .ToDictionaryAsync(x => x.ProductId);

            var lines = new List<PurchaseOrderLineWriteDto>();
            foreach (var requestLine in request.Lines)
            {
                if (!units.TryGetValue(requestLine.ProductId!.Value, out var unit))
                    return Result<PurchaseOrderWriteDto>.Fail($"No purchase unit is configured for product {requestLine.ProductId}.");
                lines.Add(new PurchaseOrderLineWriteDto
                {
                    PurchaseRequestLineId = requestLine.Id,
                    ProductId = requestLine.ProductId.Value,
                    ProductUnitId = unit.Id,
                    Quantity = requestLine.ApprovedQuantity > 0 ? requestLine.ApprovedQuantity : requestLine.RequestedQuantity,
                    QuantityPerUnitSnapshot = unit.QuantityPerUnit > 0 ? unit.QuantityPerUnit : 1m,
                    UnitPrice = unit.PurchasePrice,
                    UnitCost = unit.PurchasePrice,
                    TaxExempt = true
                });
            }

            return await CreateAsync(new PurchaseOrderWriteDto
            {
                Notes = $"Generated from approved PR {request.RequestNumber}.",
                DocumentDate = DateTime.Now,
                Lines = lines
            });
        }

        public async Task<Result<List<PurchaseOrderReadDto>>> GetAllAsync()
        {
            if (!await _permissionService.HasCurrentUserPermissionAsync("PurchaseOrder.View"))
                return Result<List<PurchaseOrderReadDto>>.Fail("You do not have permission to view purchase orders.");

            var orders = await _db.PurchaseOrders
                .AsNoTracking()
                .Include(x => x.Supplier)
                .Include(x => x.Lines)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();
            return Result<List<PurchaseOrderReadDto>>.Ok(_mapper.Map<List<PurchaseOrderReadDto>>(orders));
        }

        public async Task<Result<PurchaseOrderWriteDto>> GetByIdAsync(int id)
        {
            if (!await _permissionService.HasCurrentUserPermissionAsync("PurchaseOrder.View"))
                return Result<PurchaseOrderWriteDto>.Fail("You do not have permission to view purchase orders.");

            var entity = await _db.PurchaseOrders
                .AsNoTracking()
                .Include(x => x.Supplier)
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                return Result<PurchaseOrderWriteDto>.Fail("Purchase order was not found.");

            var dto = _mapper.Map<PurchaseOrderWriteDto>(entity);
            dto.SupplierName = entity.Supplier?.Name;

            var productIds = entity.Lines.Select(x => x.ProductId).Distinct().ToList();
            var unitIds = entity.Lines.Select(x => x.ProductUnitId).Distinct().ToList();
            var products = await _db.Set<Product>()
                .AsNoTracking()
                .Where(x => productIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name);
            var units = await _db.Set<ProductUnit>()
                .AsNoTracking()
                .Include(x => x.Unit)
                .Where(x => unitIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Unit != null ? x.Unit.Name : null);

            foreach (var line in dto.Lines)
            {
                if (products.TryGetValue(line.ProductId, out var productName))
                    line.ProductName = productName;
                if (units.TryGetValue(line.ProductUnitId, out var unitName))
                    line.UnitName = unitName;
            }

            return Result<PurchaseOrderWriteDto>.Ok(dto);
        }

        public async Task<Result<PurchaseOrderWriteDto>> ApproveAsync(int id)
        {
            if (!await _permissionService.HasCurrentUserPermissionAsync(ApprovePermission))
                return Result<PurchaseOrderWriteDto>.Fail("You do not have permission to approve purchase orders.");

            var order = await _db.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == id);
            if (order == null)
                return Result<PurchaseOrderWriteDto>.Fail("Purchase order was not found.");
            if (order.Status != PurchaseOrderStatus.Submitted)
                return Result<PurchaseOrderWriteDto>.Fail("Only submitted purchase orders can be approved.");

            // Purchase-order approval is non-posting. Receiving and supplier-invoice
            // stages are responsible for inventory, payable, and accounting effects.
            order.Status = PurchaseOrderStatus.Approved;
            order.ApprovedBy = _userSession.CurrentUser?.Id;
            order.ApprovedDate = DateTime.Now;
            order.UpdatedDate = DateTime.Now;
            await _db.SaveChangesAsync();

            return Result<PurchaseOrderWriteDto>.Ok(
                _mapper.Map<PurchaseOrderWriteDto>(order),
                "Purchase order approved. No inventory or accounting entry was posted.");
        }

        private async Task<string> GeneratePurchaseInvoiceNumberAsync(string orderNumber)
        {
            var usedNumbers = await _uow.GetRepository<Invoice>().GetAllAsQueryable()
                .AsNoTracking()
                .Select(invoice => invoice.InvoiceNumber)
                .ToHashSetAsync();

            if (int.TryParse(orderNumber, out var purchaseOrderNumber) &&
                purchaseOrderNumber is >= 100 and <= 99999)
            {
                var candidate = purchaseOrderNumber.ToString();
                if (!usedNumbers.Contains(candidate))
                    return candidate;
            }

            var startingNumber = (int)(DateTime.Now.Ticks % 90000) + 10000;
            for (var offset = 0; offset < 90000; offset++)
            {
                var candidateNumber = ((startingNumber - 10000 + offset) % 90000) + 10000;
                var candidate = candidateNumber.ToString();
                if (!usedNumbers.Contains(candidate))
                    return candidate;
            }

            throw new InvalidOperationException("No available numeric purchase invoice number exists.");
        }

        public async Task<Result<PurchaseOrderWriteDto>> RejectAsync(int id, string reason)
        {
            if (!await _permissionService.HasCurrentUserPermissionAsync("PurchaseOrder.Reject"))
                return Result<PurchaseOrderWriteDto>.Fail("You do not have permission to reject purchase orders.");
            var order = await _db.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == id);
            if (order == null)
                return Result<PurchaseOrderWriteDto>.Fail("Purchase order was not found.");
            if (order.Status != PurchaseOrderStatus.Submitted)
                return Result<PurchaseOrderWriteDto>.Fail("Only submitted purchase orders can be rejected.");
            if (string.IsNullOrWhiteSpace(reason))
                return Result<PurchaseOrderWriteDto>.Fail("A rejection reason is required.");
            order.Status = PurchaseOrderStatus.Rejected;
            order.RejectedBy = _userSession.CurrentUser?.Id;
            order.RejectedDate = DateTime.Now;
            order.RejectionReason = reason.Trim();
            order.UpdatedDate = DateTime.Now;
            await _db.SaveChangesAsync();
            return Result<PurchaseOrderWriteDto>.Ok(_mapper.Map<PurchaseOrderWriteDto>(order), "Purchase order rejected.");
        }
    }
}
