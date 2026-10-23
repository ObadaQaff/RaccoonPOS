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
        Task<Result<List<PurchaseOrderWriteDto>>> CreateFromPurchaseRequestAsync(int purchaseRequestId);
        Task<Result<List<PurchaseOrderReadDto>>> GetAllAsync();
        Task<Result<PurchaseOrderWriteDto>> GetByIdAsync(int id);
        Task<Result<PurchaseOrderWriteDto>> ApproveAsync(int id);
        Task<Result<PurchaseOrderWriteDto>> RejectAsync(int id, string reason);
        Task<Result<PurchaseOrderWriteDto>> MarkPaidAsync(int id, string note);
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

            var unitIds = dto.Lines.Select(line => line.ProductUnitId).Distinct().ToList();
            var units = await _db.Set<ProductUnit>().Include(unit => unit.Product)
                .Where(unit => unitIds.Contains(unit.Id)).ToDictionaryAsync(unit => unit.Id);
            foreach (var line in dto.Lines)
            {
                if (!units.TryGetValue(line.ProductUnitId, out var unit) || unit.Product == null
                    || unit.ProductId != line.ProductId)
                    return Result<PurchaseOrderWriteDto>.Fail("Every purchase order line must have a product, unit, and positive quantity.");
                line.TaxExempt = unit.Product.TaxExempt ?? false;
                line.TaxRate = line.TaxExempt ? 0m : Math.Max(0m, unit.Product.TaxRate ?? 0m);
                line.QuantityPerUnitSnapshot = unit.QuantityPerUnit > 0m ? unit.QuantityPerUnit : 1m;
                line.BaseQuantity = line.Quantity * line.QuantityPerUnitSnapshot;
                if (line.UnitCost == 0m && line.UnitPrice > 0m) line.UnitCost = line.UnitPrice;
            }
            CalculateTotals(dto);

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

        public async Task<Result<List<PurchaseOrderWriteDto>>> CreateFromPurchaseRequestAsync(int purchaseRequestId)
        {
            if (!await _permissionService.HasCurrentUserPermissionAsync("PurchaseOrder.Create"))
                return Result<List<PurchaseOrderWriteDto>>.Fail("You do not have permission to create purchase orders.");

            var request = await _db.PurchaseRequests
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == purchaseRequestId);
            if (request == null)
                return Result<List<PurchaseOrderWriteDto>>.Fail("Purchase request was not found.");
            if (request.Status != PurchaseRequestStatus.Approved)
                return Result<List<PurchaseOrderWriteDto>>.Fail("Only approved purchase requests can generate purchase orders.");

            var approvedRequestLines = request.Lines
                .Where(x => x.Decision == PurchaseRequestLineDecision.Approved && x.ApprovedQuantity > 0)
                .ToList();
            if (approvedRequestLines.Count == 0)
                return Result<List<PurchaseOrderWriteDto>>.Fail("The approved purchase request has no approved lines.");
            if (approvedRequestLines.Any(x => !x.ProductId.HasValue || !x.ProductUnitId.HasValue))
                return Result<List<PurchaseOrderWriteDto>>.Fail("Approved free-text lines cannot create a purchase order. Link each approved item to a catalog product first.");
            await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var approvedLineIds = approvedRequestLines.Select(x => x.Id).ToList();
            var alreadyOrderedLineIds = await _db.Set<PurchaseOrderLine>()
                .Where(x => x.PurchaseRequestLineId.HasValue && approvedLineIds.Contains(x.PurchaseRequestLineId.Value))
                .Select(x => x.PurchaseRequestLineId!.Value)
                .Distinct()
                .ToListAsync();
            if (alreadyOrderedLineIds.Count > 0)
                return Result<List<PurchaseOrderWriteDto>>.Fail("One or more approved lines already belong to a purchase order. Duplicate purchase orders were not created.");

            var requestedUnitIds = approvedRequestLines
                .Select(x => x.ProductUnitId!.Value)
                .Distinct()
                .ToList();
            var requestedUnits = await _db.Set<ProductUnit>()
                .Include(unit => unit.Product)
                .Where(x => requestedUnitIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            foreach (var requestLine in approvedRequestLines)
            {
                if (!requestedUnits.TryGetValue(requestLine.ProductUnitId!.Value, out var unit) ||
                    unit.ProductId != requestLine.ProductId!.Value || unit.Product == null)
                    return Result<List<PurchaseOrderWriteDto>>.Fail($"The approved purchase unit is not valid for {requestLine.ItemName}.");
            }

            var now = DateTime.Now;
            var createdOrders = new List<PurchaseOrder>();
            foreach (var supplierGroup in approvedRequestLines.GroupBy(x => x.SupplierId))
            {
                var orderLines = supplierGroup.Select(requestLine =>
                {
                    var unit = requestedUnits[requestLine.ProductUnitId!.Value];
                    var quantityPerUnit = unit.QuantityPerUnit > 0 ? unit.QuantityPerUnit : 1m;
                    return new PurchaseOrderLine
                    {
                        PurchaseRequestLineId = requestLine.Id,
                        ProductId = requestLine.ProductId!.Value,
                        ProductUnitId = unit.Id,
                        Quantity = requestLine.ApprovedQuantity,
                        QuantityPerUnitSnapshot = quantityPerUnit,
                        BaseQuantity = requestLine.ApprovedQuantity * quantityPerUnit,
                        UnitPrice = requestLine.ApprovedUnitPrice,
                        UnitCost = requestLine.ApprovedUnitPrice,
                        TaxExempt = unit.Product!.TaxExempt ?? false,
                        TaxRate = unit.Product.TaxExempt == true ? 0m : Math.Max(0m, unit.Product.TaxRate ?? 0m),
                        CreatedDate = now,
                        UpdatedDate = now
                    };
                }).ToList();

                createdOrders.Add(new PurchaseOrder
                {
                    OrderNumber = $"PO-{now:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..8]}",
                    Status = PurchaseOrderStatus.Submitted,
                    SupplierId = supplierGroup.Key,
                    Notes = supplierGroup.Key.HasValue
                        ? $"Generated from approved PR {request.RequestNumber}."
                        : $"Generated from approved PR {request.RequestNumber}. Supplier not specified.",
                    DocumentDate = now,
                    CostPriceIncludesTax = true,
                    CreatedBy = _userSession.CurrentUser?.Id,
                    SubmittedBy = _userSession.CurrentUser?.Id,
                    CreatedDate = now,
                    UpdatedDate = now,
                    Lines = orderLines
                });
            }

            foreach (var order in createdOrders)
                CalculateEntityTotals(order);

            await _db.PurchaseOrders.AddRangeAsync(createdOrders);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            var result = createdOrders.Select(_mapper.Map<PurchaseOrderWriteDto>).ToList();
            return Result<List<PurchaseOrderWriteDto>>.Ok(
                result,
                $"Created {result.Count} purchase order(s) from approved PR {request.RequestNumber}.");
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
            var productIds = orders.SelectMany(order => order.Lines).Select(line => line.ProductId).Distinct().ToList();
            var products = productIds.Count == 0
                ? new List<Product>()
                : await _db.Set<Product>().IgnoreQueryFilters().AsNoTracking()
                    .Where(product => productIds.Contains(product.Id))
                    .Select(product => new Product { Id = product.Id, Name = product.Name, ITEMCODE = product.ITEMCODE })
                    .ToListAsync();
            var catalog = products.ToDictionary(product => product.Id);
            var rows = _mapper.Map<List<PurchaseOrderReadDto>>(orders);
            var sourceById = orders.ToDictionary(order => order.Id);
            foreach (var row in rows)
            {
                row.SetFilterLines(sourceById[row.Id].Lines.Select(line =>
                {
                    catalog.TryGetValue(line.ProductId, out var product);
                    return new PurchaseOrderFilterLineDto
                    {
                        ProductId = line.ProductId, ProductName = product?.Name,
                        Barcode = product?.ITEMCODE?.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    };
                }));
            }
            return Result<List<PurchaseOrderReadDto>>.Ok(rows);
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
                .ToDictionaryAsync(x => x.Id);
            var units = await _db.Set<ProductUnit>()
                .AsNoTracking()
                .Include(x => x.Unit)
                .Where(x => unitIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Unit != null ? x.Unit.Name : null);

            foreach (var line in dto.Lines)
            {
                if (products.TryGetValue(line.ProductId, out var product))
                    line.ProductName = product.Name;
                if (units.TryGetValue(line.ProductUnitId, out var unitName))
                    line.UnitName = unitName;
            }

            // Repair only the returned view for the old PR-generation zero-tax bug.
            // No tracked entities or saved historical orders are modified on a read.
            if (ShouldRefreshLegacyTax(dto))
            {
                foreach (var line in dto.Lines)
                {
                    if (!products.TryGetValue(line.ProductId, out var product)) continue;
                    line.TaxExempt = product.TaxExempt ?? false;
                    line.TaxRate = line.TaxExempt ? 0m : Math.Max(0m, product.TaxRate ?? 0m);
                }
                CalculateTotals(dto);
            }

            return Result<PurchaseOrderWriteDto>.Ok(dto);
        }

        private static bool ShouldRefreshLegacyTax(PurchaseOrderWriteDto order) =>
            order.PaymentStatus == PurchaseOrderPaymentStatus.Unpaid && !order.ConvertedInvoiceId.HasValue
            && order.Status is PurchaseOrderStatus.Submitted or PurchaseOrderStatus.Approved
            && order.TotalTax == 0m && order.Lines.Count > 0
            && order.Lines.All(line => line.PurchaseRequestLineId.HasValue
                && line.TaxExempt && line.TaxRate == 0m && line.TaxAmount == 0m);

        private static void CalculateTotals(PurchaseOrderWriteDto order)
        {
            var taxes = new List<(PurchaseOrderLineWriteDto Line, decimal Tax)>();
            foreach (var line in order.Lines)
            {
                var gross = Math.Round(Math.Max(0m, line.Quantity * line.UnitCost), 3);
                line.LineDiscountAmount = Math.Round(Math.Clamp(line.LineDiscountAmount, 0m, gross), 3);
                var amount = gross - line.LineDiscountAmount;
                var rate = line.TaxExempt ? 0m : Math.Max(0m, line.TaxRate);
                line.LineSubTotal = order.CostPriceIncludesTax && rate > 0m
                    ? Math.Round(amount / (1m + rate / 100m), 3) : amount;
                var tax = order.CostPriceIncludesTax ? amount - line.LineSubTotal : line.LineSubTotal * rate / 100m;
                taxes.Add((line, rate > 0m ? tax : 0m));
            }
            order.SubTotal = Math.Round(order.Lines.Sum(line => line.LineSubTotal), 3);
            order.DiscountAmount = Math.Round(Math.Clamp(order.DiscountAmount ?? 0m, 0m, order.SubTotal), 3);
            var ratio = order.SubTotal > 0m ? (order.SubTotal - order.DiscountAmount.Value) / order.SubTotal : 0m;
            foreach (var (line, tax) in taxes)
                line.TaxAmount = Math.Round(tax * ratio, 3);
            order.TotalTax = order.Lines.Sum(line => line.TaxAmount);
            order.TotalAmount = Math.Round(order.SubTotal - order.DiscountAmount.Value + order.TotalTax, 3);
        }

        private void CalculateEntityTotals(PurchaseOrder order)
        {
            var dto = _mapper.Map<PurchaseOrderWriteDto>(order);
            CalculateTotals(dto);
            order.SubTotal = dto.SubTotal;
            order.TotalTax = dto.TotalTax;
            order.DiscountAmount = dto.DiscountAmount;
            order.TotalAmount = dto.TotalAmount;
            foreach (var (entityLine, dtoLine) in order.Lines.Zip(dto.Lines))
            {
                entityLine.LineDiscountAmount = dtoLine.LineDiscountAmount;
                entityLine.LineSubTotal = dtoLine.LineSubTotal;
                entityLine.TaxAmount = dtoLine.TaxAmount;
            }
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

        public async Task<Result<PurchaseOrderWriteDto>> MarkPaidAsync(int id, string note)
        {
            if (!await _permissionService.HasCurrentUserPermissionAsync("PurchaseOrder.MarkPaid"))
                return Result<PurchaseOrderWriteDto>.Fail("You do not have permission to mark purchase orders as paid.");
            if (string.IsNullOrWhiteSpace(note))
                return Result<PurchaseOrderWriteDto>.Fail("A payment note is required.");

            var order = await _db.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == id);
            if (order == null)
                return Result<PurchaseOrderWriteDto>.Fail("Purchase order was not found.");
            if (order.Status is PurchaseOrderStatus.Rejected or PurchaseOrderStatus.Cancelled)
                return Result<PurchaseOrderWriteDto>.Fail("Rejected or cancelled purchase orders cannot be marked as paid.");
            if (order.PaymentStatus == PurchaseOrderPaymentStatus.Paid)
                return Result<PurchaseOrderWriteDto>.Fail("This purchase order is already marked as paid.");

            var now = DateTime.Now;
            order.PaymentStatus = PurchaseOrderPaymentStatus.Paid;
            order.PaidBy = _userSession.CurrentUser?.Id;
            order.PaidDate = now;
            order.PaymentNote = note.Trim();
            order.UpdatedDate = now;
            await _db.SaveChangesAsync();

            return Result<PurchaseOrderWriteDto>.Ok(
                _mapper.Map<PurchaseOrderWriteDto>(order),
                "Purchase order payment status updated to paid.");
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
