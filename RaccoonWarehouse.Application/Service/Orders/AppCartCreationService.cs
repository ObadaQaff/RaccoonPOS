using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Users;
using System.Globalization;

namespace RaccoonWarehouse.Application.Service.Orders;

public sealed record AppCartLineRequest(
    int CartItemId,
    string ItemCode,
    string ProductName,
    string? UnitName,
    decimal Quantity,
    decimal UnitPrice);

public sealed record AppCartCreateRequest(
    int CartId,
    string? OrderNumber,
    int CustomerId,
    string? CustomerName,
    string? CustomerPhone,
    string? ShopName,
    DateTime CreatedDate,
    decimal TotalPrice,
    IReadOnlyList<AppCartLineRequest> Lines);

public sealed record AppCartCreateResult(int InvoiceId, bool AlreadyExists);

public interface IAppCartCreationService
{
    Task<Result<AppCartCreateResult>> CreatePendingAsync(
        AppCartCreateRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class AppCartCreationService : IAppCartCreationService
{
    private readonly ApplicationDbContext _context;
    private readonly IInvoiceService _invoiceService;
    private readonly IEndpointOrderStatusService _statusService;

    public AppCartCreationService(
        ApplicationDbContext context,
        IInvoiceService invoiceService,
        IEndpointOrderStatusService statusService)
    {
        _context = context;
        _invoiceService = invoiceService;
        _statusService = statusService;
    }

    public async Task<Result<AppCartCreateResult>> CreatePendingAsync(
        AppCartCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.CartId <= 0)
            return Result<AppCartCreateResult>.Fail("A valid Panda cart id is required.");
        if (request.Lines.Count == 0)
            return Result<AppCartCreateResult>.Fail("The Panda cart contains no lines.");

        var reference = $"BOX-CART-{request.CartId}";
        var existingInvoiceId = await _context.Set<Invoice>()
            .AsNoTracking()
            .Where(invoice => invoice.OriginalInvoiceId == reference)
            .Select(invoice => (int?)invoice.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (existingInvoiceId.HasValue)
            return Result<AppCartCreateResult>.Ok(new(existingInvoiceId.Value, true), "The app cart already exists in Raccoon.");

        var requestedCodes = request.Lines
            .Select(line => NormalizeItemCode(line.ItemCode))
            .Where(code => code.Length > 0)
            .Distinct()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var products = await _context.Set<Product>()
            .AsNoTracking()
            .Include(product => product.ProductUnits)
                .ThenInclude(unit => unit.Unit)
            .Where(product => !product.IsDeleted && product.ITEMCODE.HasValue)
            .ToListAsync(cancellationToken);
        var productsByCode = products
            .Where(product => requestedCodes.Contains(NormalizeItemCode(
                product.ITEMCODE?.ToString(CultureInfo.InvariantCulture))))
            .GroupBy(product => NormalizeItemCode(product.ITEMCODE?.ToString(CultureInfo.InvariantCulture)))
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var invoiceLines = new List<InvoiceLineWriteDto>();
        foreach (var sourceLine in request.Lines)
        {
            var itemCode = NormalizeItemCode(sourceLine.ItemCode);
            if (sourceLine.Quantity <= 0 || sourceLine.UnitPrice < 0 ||
                !productsByCode.TryGetValue(itemCode, out var product))
            {
                return Result<AppCartCreateResult>.Fail(
                    $"Raccoon product mapping failed for Panda cart item {sourceLine.CartItemId} ({sourceLine.ItemCode}).");
            }

            var productUnit = product.ProductUnits.FirstOrDefault(unit =>
                                  string.Equals(unit.Unit?.Name?.Trim(), sourceLine.UnitName?.Trim(),
                                      StringComparison.OrdinalIgnoreCase))
                              ?? product.ProductUnits.FirstOrDefault(unit => unit.IsDefaultSaleUnit)
                              ?? product.ProductUnits.FirstOrDefault(unit => unit.IsBaseUnit);
            if (productUnit == null)
            {
                return Result<AppCartCreateResult>.Fail(
                    $"Raccoon unit mapping failed for Panda cart item {sourceLine.CartItemId} ({sourceLine.UnitName}).");
            }

            var factor = productUnit.QuantityPerUnit > 0 ? productUnit.QuantityPerUnit : 1m;
            invoiceLines.Add(new InvoiceLineWriteDto
            {
                OriginalInvoiceId = $"BOX-CART-ITEM-{sourceLine.CartItemId}",
                ProductId = product.Id,
                ProductUnitId = productUnit.Id,
                ProductName = product.Name,
                UnitName = productUnit.Unit?.Name,
                Quantity = sourceLine.Quantity,
                QuantityPerUnitSnapshot = factor,
                BaseQuantity = sourceLine.Quantity * factor,
                UnitPrice = sourceLine.UnitPrice,
                UnitCost = productUnit.PurchasePrice,
                TaxExempt = product.TaxExempt ?? false,
                TaxRate = product.TaxRate ?? 0m,
                ExpiryDate = DateTime.Today.AddYears(10),
                CreatedDate = request.CreatedDate,
                UpdatedDate = DateTime.Now
            });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var customerId = await ResolveCustomerAsync(request, cancellationToken);
            var invoiceResult = await _invoiceService.CreateAsync(new InvoiceWriteDto
            {
                InvoiceNumber = string.IsNullOrWhiteSpace(request.OrderNumber)
                    ? $"PANDA-ORDER-{request.CartId}"
                    : request.OrderNumber,
                OriginalInvoiceId = reference,
                InvoiceType = InvoiceType.appCart,
                PaymentType = PaymentType.Credit,
                CustomerId = customerId,
                Status = InvoiceStatus.Unknown,
                IsPOS = false,
                OpenedAt = request.CreatedDate,
                InvoiceLines = invoiceLines,
                DiscountAmount = 0m,
                CreatedDate = request.CreatedDate,
                UpdatedDate = DateTime.Now
            });
            if (!invoiceResult.Success || invoiceResult.Data == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<AppCartCreateResult>.Fail(invoiceResult.Message ?? "Raccoon invoice creation failed.");
            }

            var stockResult = await _statusService.ApplyStatusAsync(
                invoiceResult.Data.Id,
                InvoiceStatus.Unknown,
                cancellationToken,
                synchronizeBox: false);
            if (!stockResult.Success)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<AppCartCreateResult>.Fail(stockResult.Message ?? "Raccoon stock reservation failed.", stockResult.Errors);
            }

            await transaction.CommitAsync(cancellationToken);
            return Result<AppCartCreateResult>.Ok(new(invoiceResult.Data.Id, false), "Raccoon app cart created and stock reserved.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<AppCartCreateResult>.Fail($"Raccoon app-cart creation failed: {ex.Message}");
        }
    }

    private async Task<int?> ResolveCustomerAsync(AppCartCreateRequest request, CancellationToken cancellationToken)
    {
        var phone = request.CustomerPhone?.Trim();
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var existingCustomerId = await _context.Set<User>()
                .Where(user => user.PhoneNumber == phone)
                .Select(user => (int?)user.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (existingCustomerId.HasValue)
                return existingCustomerId;
        }

        var customer = new User
        {
            Name = request.ShopName ?? request.CustomerName ?? $"Panda customer {request.CustomerId}",
            PhoneNumber = phone,
            Password = "PANDA-EXTERNAL",
            Role = UserRole.Customer,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        await _context.Set<User>().AddAsync(customer, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return customer.Id;
    }

    private static string NormalizeItemCode(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return normalized.All(char.IsDigit)
            ? normalized.TrimStart('0') is { Length: > 0 } digits ? digits : "0"
            : normalized.ToUpperInvariant();
    }
}
