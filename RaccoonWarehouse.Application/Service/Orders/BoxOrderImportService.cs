using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Orders.DTOs;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.Users;
using System.Globalization;

namespace RaccoonWarehouse.Application.Service.Orders
{
    #region Temporary Box API Integration

    public interface IBoxOrderImportService
    {
        Task<Result<BoxOrderImportResultDto>> ImportPendingAsync(
            CancellationToken cancellationToken = default);
    }

    public sealed class BoxOrderImportService : IBoxOrderImportService
    {
        private static readonly SemaphoreSlim ImportLock = new(1, 1);

        private readonly IUOW _uow;
        private readonly IInvoiceService _invoiceService;
        private readonly IEndpointOrderStatusService _endpointOrderStatusService;
        private readonly IBoxCartApiService _boxCartApiService;

        public BoxOrderImportService(
            IUOW uow,
            IInvoiceService invoiceService,
            IEndpointOrderStatusService endpointOrderStatusService,
            IBoxCartApiService boxCartApiService)
        {
            _uow = uow;
            _invoiceService = invoiceService;
            _endpointOrderStatusService = endpointOrderStatusService;
            _boxCartApiService = boxCartApiService;
        }

        public async Task<Result<BoxOrderImportResultDto>> ImportPendingAsync(
            CancellationToken cancellationToken = default)
        {
            await ImportLock.WaitAsync(cancellationToken);
            try
            {
                var pendingResult = await _boxCartApiService.GetPendingOrdersAsync(cancellationToken);
                if (!pendingResult.Success || pendingResult.Data == null)
                {
                    return Result<BoxOrderImportResultDto>.Fail(
                        pendingResult.Message ?? "Pending Box orders could not be loaded.",
                        pendingResult.Errors);
                }

                var orders = pendingResult.Data.Orders;
                var result = new BoxOrderImportResultDto { ReceivedCount = orders.Count };
                if (orders.Count == 0)
                    return Result<BoxOrderImportResultDto>.Ok(result, "No pending Box orders were returned.");

                var invoiceRepo = _uow.GetRepository<Invoice>();
                var existingReferences = await invoiceRepo.GetAllAsQueryable()
                    .AsNoTracking()
                    .Where(invoice =>
                        invoice.InvoiceType == InvoiceType.EndpointOrder &&
                        invoice.OriginalInvoiceId != null &&
                        invoice.OriginalInvoiceId.StartsWith("BOX-CART-"))
                    .Select(invoice => invoice.OriginalInvoiceId!)
                    .ToHashSetAsync(cancellationToken);

                var products = await _uow.GetRepository<Product>()
                    .GetAllAsQueryable()
                    .AsNoTracking()
                    .Include(product => product.ProductUnits)
                        .ThenInclude(productUnit => productUnit.Unit)
                    .Where(product => !product.IsDeleted && product.ITEMCODE.HasValue)
                    .ToListAsync(cancellationToken);

                var productsByBarcode = products
                    .Select(product => new
                    {
                        Product = product,
                        Barcode = NormalizeBarcode(product.ITEMCODE?.ToString(CultureInfo.InvariantCulture))
                    })
                    .Where(item => !string.IsNullOrWhiteSpace(item.Barcode))
                    .GroupBy(item => item.Barcode, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.First().Product, StringComparer.Ordinal);

                foreach (var order in orders.OrderBy(order => order.CartId))
                {
                    var sourceReference = $"BOX-CART-{order.CartId}";
                    if (existingReferences.Contains(sourceReference))
                    {
                        result.ExistingCount++;
                        continue;
                    }

                    var lines = new List<InvoiceLineWriteDto>();
                    var orderErrors = new List<string>();

                    foreach (var item in order.Items)
                    {
                        var barcode = NormalizeBarcode(item.Barcode);
                        if (item.Quantity <= 0 || string.IsNullOrWhiteSpace(barcode) ||
                            !productsByBarcode.TryGetValue(barcode, out var product))
                        {
                            orderErrors.Add($"Cart {order.CartId}: product barcode {item.Barcode} was not matched.");
                            continue;
                        }

                        var unit = SelectUnit(product.ProductUnits, item.UnitName);
                        if (unit == null)
                        {
                            orderErrors.Add($"Cart {order.CartId}: product {barcode} has no usable unit.");
                            continue;
                        }

                        var factor = unit.QuantityPerUnit > 0 ? unit.QuantityPerUnit : 1m;
                        lines.Add(new InvoiceLineWriteDto
                        {
                            OriginalInvoiceId = $"BOX-CART-ITEM-{item.CartItemId}",
                            ProductId = product.Id,
                            ProductUnitId = unit.Id,
                            ProductName = product.Name,
                            UnitName = unit.Unit?.Name,
                            Quantity = item.Quantity,
                            QuantityPerUnitSnapshot = factor,
                            BaseQuantity = item.Quantity * factor,
                            UnitPrice = item.UnitPrice,
                            UnitCost = unit.PurchasePrice,
                            TaxExempt = product.TaxExempt ?? false,
                            TaxRate = product.TaxRate ?? 0m,
                            ExpiryDate = DateTime.Today.AddYears(10),
                            CreatedDate = order.CreatedDate == default ? DateTime.Now : order.CreatedDate,
                            UpdatedDate = DateTime.Now
                        });
                    }

                    if (orderErrors.Count > 0 || lines.Count == 0 || lines.Count != order.Items.Count)
                    {
                        result.SkippedCount++;
                        result.Errors.AddRange(orderErrors);
                        continue;
                    }

                    var customerId = await ResolveCustomerAsync(order, cancellationToken);
                    var createdDate = order.CreatedDate == default ? DateTime.Now : order.CreatedDate;
                    var createResult = await _invoiceService.CreateAsync(new InvoiceWriteDto
                    {
                        InvoiceNumber = sourceReference,
                        OriginalInvoiceId = sourceReference,
                        InvoiceType = InvoiceType.EndpointOrder,
                        PaymentType = PaymentType.Credit,
                        CustomerId = customerId,
                        Status = InvoiceStatus.Unknown,
                        IsPOS = false,
                        OpenedAt = createdDate,
                        InvoiceLines = lines,
                        CreatedDate = createdDate,
                        UpdatedDate = DateTime.Now
                    });

                    

                    var reserveResult = await _endpointOrderStatusService.ApplyStatusAsync(
                        createResult.Data!.Id,
                        InvoiceStatus.Unknown,
                        cancellationToken,
                        synchronizeBox: false);
                    
                    if (!reserveResult.Success)
                    {
                        var createdInvoiceId = createResult.Data.Id;
                        var lineRepo = _uow.GetRepository<Domain.InvoiceLines.InvoiceLine>();
                        var createdLineIds = await lineRepo.GetAllAsQueryable()
                            .Where(line => line.InvoiceId == createdInvoiceId)
                            .Select(line => line.Id)
                            .ToListAsync(cancellationToken);
                        foreach (var lineId in createdLineIds)
                            await lineRepo.DeleteAsync(lineId);

                        await invoiceRepo.DeleteAsync(createdInvoiceId);
                        await _uow.CommitAsync();

                        result.SkippedCount++;
                        result.Errors.Add(
                            $"Cart {order.CartId} could not be imported.{Environment.NewLine}" +
                            reserveResult.Message);
                        continue;
                    }

                    existingReferences.Add(sourceReference);
                    result.ImportedCount++;
                }

                return Result<BoxOrderImportResultDto>.Ok(result, "Box order synchronization completed.");
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return Result<BoxOrderImportResultDto>.Fail("Box order synchronization timed out.");
            }
            catch (Exception ex)
            {
                return Result<BoxOrderImportResultDto>.Fail($"Box order synchronization failed: {ex.Message}");
            }
            finally
            {
                ImportLock.Release();
            }
        }

        private async Task<int?> ResolveCustomerAsync(
            BoxOrderExportDto order,
            CancellationToken cancellationToken)
        {
            var userRepo = _uow.GetRepository<User>();
            var phone = order.CustomerPhone?.Trim();

            if (!string.IsNullOrWhiteSpace(phone))
            {
                var existingId = await userRepo.GetAllAsQueryable()
                    .AsNoTracking()
                    .Where(user => user.PhoneNumber == phone)
                    .Select(user => (int?)user.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingId.HasValue)
                    return existingId;
            }

            var name = FirstNotEmpty(order.ShopName, order.CustomerName, $"Box customer {order.UserId}");
            var customer = new User
            {
                Name = name,
                PhoneNumber = phone,
                Password = "BOX-EXTERNAL",
                Role = UserRole.Customer,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            await userRepo.AddAsync(customer);
            await _uow.CommitAsync();
            return customer.Id;
        }

        private static ProductUnit? SelectUnit(
            IEnumerable<ProductUnit>? productUnits,
            string? requestedUnitName)
        {
            var units = productUnits?.ToList() ?? new List<ProductUnit>();
            if (!string.IsNullOrWhiteSpace(requestedUnitName))
            {
                var requested = requestedUnitName.Trim();
                var matched = units.FirstOrDefault(unit =>
                    string.Equals(unit.Unit?.Name?.Trim(), requested, StringComparison.OrdinalIgnoreCase));
                if (matched != null)
                    return matched;
            }

            return units.FirstOrDefault(unit => unit.IsDefaultSaleUnit)
                   ?? units.FirstOrDefault(unit => unit.IsBaseUnit)
                   ?? units.FirstOrDefault();
        }

        public static string NormalizeBarcode(string? barcode)
        {
            var normalized = barcode?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return string.Empty;

            if (!normalized.All(char.IsDigit))
                return normalized;

            normalized = normalized.TrimStart('0');
            return normalized.Length == 0 ? "0" : normalized;
        }

        private static string FirstNotEmpty(params string?[] values)
        {
            return values.First(value => !string.IsNullOrWhiteSpace(value))!.Trim();
        }
    }

    #endregion
}
