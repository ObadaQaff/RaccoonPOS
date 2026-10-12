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
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.StockTransactions;
using RaccoonWarehouse.Domain.Users;

namespace RaccoonWarehouse.Application.Service.PurchaseOrders
{
    public interface IPurchaseOrderService
    {
        Task<Result<PurchaseOrderWriteDto>> CreateAsync(PurchaseOrderWriteDto dto);
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
            if (dto.Lines.Any(x => !x.ExpiryDate.HasValue))
                return Result<PurchaseOrderWriteDto>.Fail("Expiry date is required for every purchase order line.");

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

            var order = await _db.PurchaseOrders
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (order == null)
                return Result<PurchaseOrderWriteDto>.Fail("Purchase order was not found.");
            if (order.Status != PurchaseOrderStatus.Submitted)
                return Result<PurchaseOrderWriteDto>.Fail("Only submitted purchase orders can be approved.");
            if (order.ConvertedInvoiceId.HasValue)
                return Result<PurchaseOrderWriteDto>.Fail("This purchase order has already been converted.");

            await using var transaction = await _uow.BeginTransactionAsync();
            try
            {
                var userId = _userSession.CurrentUser?.Id;
                var invoiceNumber = await GeneratePurchaseInvoiceNumberAsync(order.OrderNumber);
                order.Status = PurchaseOrderStatus.Approved;
                order.ApprovedBy = userId;
                order.ApprovedDate = DateTime.Now;
                order.UpdatedDate = DateTime.Now;
                await _db.SaveChangesAsync();

                var invoice = new InvoiceWriteDto
                {
                    InvoiceNumber = invoiceNumber,
                    InvoiceType = InvoiceType.Purchase,
                    PaymentType = order.PaymentType ?? PaymentType.Credit,
                    SupplierId = order.SupplierId,
                    WarehouseId = order.WarehouseId,
                    CurrencyId = order.CurrencyId,
                    DocumentDate = order.DocumentDate,
                    DueDate = order.DueDate,
                    Notes = $"Created from purchase order {order.OrderNumber}. {order.Notes}".Trim(),
                    DiscountAmount = order.DiscountAmount,
                    SubTotal = order.SubTotal,
                    TotalTax = order.TotalTax,
                    TotalAmount = order.TotalAmount,
                    CostPriceIncludesTax = order.CostPriceIncludesTax,
                    Status = InvoiceStatus.Posted,
                    CreatedBy = userId,
                    InvoiceLines = order.Lines.Select(line => new InvoiceLineWriteDto
                    {
                        ProductId = line.ProductId,
                        ProductUnitId = line.ProductUnitId,
                        Quantity = line.Quantity,
                        QuantityPerUnitSnapshot = line.QuantityPerUnitSnapshot,
                        BaseQuantity = line.BaseQuantity,
                        UnitPrice = line.UnitPrice,
                        UnitCost = line.UnitCost,
                        LineDiscountAmount = line.LineDiscountAmount,
                        FreeQuantity = line.FreeQuantity,
                        ExpiryDate = line.ExpiryDate ?? DateTime.MinValue,
                        TaxExempt = line.TaxExempt,
                        TaxRate = line.TaxRate,
                        TaxAmount = line.TaxAmount,
                        LineSubTotal = line.LineSubTotal
                    }).ToList()
                };

                var invoiceResult = await _invoiceService.CreateAsync(invoice);
                if (!invoiceResult.Success || invoiceResult.Data == null)
                    return Result<PurchaseOrderWriteDto>.Fail(invoiceResult.Message ?? "Purchase invoice conversion failed.");

                var movements = order.Lines.Select(line => new StockMovementPostDto
                {
                    ProductId = line.ProductId,
                    ProductUnitId = line.ProductUnitId,
                    Quantity = line.Quantity + line.FreeQuantity,
                    QuantityPerUnitSnapshot = line.QuantityPerUnitSnapshot > 0 ? line.QuantityPerUnitSnapshot : 1m,
                    BaseQuantity = line.BaseQuantity > 0 ? line.BaseQuantity : line.Quantity * line.QuantityPerUnitSnapshot,
                    UnitPrice = line.UnitPrice,
                    PurchasePrice = line.UnitCost,
                    ExpiryDate = line.ExpiryDate,
                    TransactionType = TransactionType.Purchase,
                    InvoiceId = invoiceResult.Data.Id,
                    TransactionDate = order.DocumentDate ?? DateTime.Now,
                    Notes = $"Purchase order {order.OrderNumber}",
                    ReferenceNumber = order.OrderNumber,
                    UpdateCatalogAverageCost = true
                }).ToList();

                var stockResult = await _stockService.PostMovementsAsync(movements);
                if (!stockResult.Success)
                    return Result<PurchaseOrderWriteDto>.Fail(stockResult.Message ?? "Purchase order stock posting failed.");

                order.ConvertedInvoiceId = invoiceResult.Data.Id;
                order.Status = PurchaseOrderStatus.Converted;
                order.UpdatedDate = DateTime.Now;
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return Result<PurchaseOrderWriteDto>.Ok(_mapper.Map<PurchaseOrderWriteDto>(order), "Purchase order approved and converted to a purchase invoice.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Result<PurchaseOrderWriteDto>.Fail($"Purchase order approval failed: {ex.Message}");
            }
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
            order.Status = PurchaseOrderStatus.Rejected;
            order.RejectedBy = _userSession.CurrentUser?.Id;
            order.RejectedDate = DateTime.Now;
            order.RejectionReason = string.IsNullOrWhiteSpace(reason) ? "Rejected by approver." : reason.Trim();
            order.UpdatedDate = DateTime.Now;
            await _db.SaveChangesAsync();
            return Result<PurchaseOrderWriteDto>.Ok(_mapper.Map<PurchaseOrderWriteDto>(order), "Purchase order rejected.");
        }
    }
}
