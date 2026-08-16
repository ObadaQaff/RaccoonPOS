using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Integration;
using RaccoonWarehouse.Domain.InvoiceLines.DTOs;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Users;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace RaccoonWarehouse.Application.Service.Orders;

public sealed record PandaOrderProcessResult(int InvoiceId, bool AlreadyProcessed);

public interface IPandaOrderProcessor
{
    Task<Result<PandaOrderProcessResult>> ProcessAsync(OrderSubmittedV1 integrationEvent, string payloadJson,
        CancellationToken cancellationToken = default);
}

public sealed class PandaOrderProcessor : IPandaOrderProcessor
{
    private readonly ApplicationDbContext _context;
    private readonly IInvoiceService _invoiceService;
    private readonly IEndpointOrderStatusService _statusService;

    public PandaOrderProcessor(ApplicationDbContext context, IInvoiceService invoiceService,
        IEndpointOrderStatusService statusService)
    { _context = context; _invoiceService = invoiceService; _statusService = statusService; }

    public async Task<Result<PandaOrderProcessResult>> ProcessAsync(OrderSubmittedV1 integrationEvent,
        string payloadJson, CancellationToken cancellationToken = default)
    {
        if (integrationEvent.EventId == Guid.Empty || integrationEvent.EventVersion != 1 ||
            integrationEvent.EventType != "OrderSubmitted.v1" || integrationEvent.Order.OrderId <= 0)
            return Result<PandaOrderProcessResult>.Fail("Unsupported or invalid Panda order event.");
        if (integrationEvent.Order.Lines.Count == 0)
            return Result<PandaOrderProcessResult>.Fail("Panda order contains no lines.");

        var externalOrderId = integrationEvent.Order.OrderId.ToString(CultureInfo.InvariantCulture);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson)));
        var existing = await _context.IntegrationInbox.AsNoTracking()
            .FirstOrDefaultAsync(x => x.EventId == integrationEvent.EventId ||
                                      (x.SourceSystem == "Panda" && x.ExternalOrderId == externalOrderId), cancellationToken);
        if (existing != null)
        {
            if (!string.Equals(existing.PayloadHash, hash, StringComparison.Ordinal))
                return Result<PandaOrderProcessResult>.Fail("The event identity was reused with a different payload.");
            if (existing.Status == IntegrationInboxStatus.Completed && existing.RaccoonInvoiceId.HasValue)
                return Result<PandaOrderProcessResult>.Ok(new(existing.RaccoonInvoiceId.Value, true));
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var inbox = existing ?? new IntegrationInbox
            {
                EventId = integrationEvent.EventId, EventType = integrationEvent.EventType,
                EventVersion = integrationEvent.EventVersion, SourceSystem = "Panda",
                ExternalOrderId = externalOrderId, PayloadHash = hash,
                Status = IntegrationInboxStatus.Processing, ReceivedAtUtc = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow
            };
            if (existing == null) await _context.IntegrationInbox.AddAsync(inbox, cancellationToken);

            var codes = integrationEvent.Order.Lines.Select(x => BoxOrderImportService.NormalizeBarcode(x.ItemCode)).Distinct().ToList();
            var products = await _context.Set<Product>().Include(x => x.ProductUnits).ThenInclude(x => x.Unit)
                .Where(x => !x.IsDeleted && x.ITEMCODE.HasValue).ToListAsync(cancellationToken);
            var byCode = products.Where(x => codes.Contains(BoxOrderImportService.NormalizeBarcode(x.ITEMCODE?.ToString(CultureInfo.InvariantCulture))))
                .GroupBy(x => BoxOrderImportService.NormalizeBarcode(x.ITEMCODE?.ToString(CultureInfo.InvariantCulture)))
                .ToDictionary(x => x.Key, x => x.First());
            var lines = new List<InvoiceLineWriteDto>();
            foreach (var source in integrationEvent.Order.Lines)
            {
                var code = BoxOrderImportService.NormalizeBarcode(source.ItemCode);
                if (source.Quantity <= 0 || source.UnitPrice < 0 || !byCode.TryGetValue(code, out var product))
                    return await RollbackFailAsync(transaction, $"Product mapping failed for Panda item {source.SourceProductId} ({source.ItemCode}).", cancellationToken);
                var unit = product.ProductUnits.FirstOrDefault(x => string.Equals(x.Unit?.Name?.Trim(), source.UnitName?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (unit == null)
                    return await RollbackFailAsync(transaction, $"Unit mapping failed for Panda unit {source.SourceProductUnitId} ({source.UnitName}).", cancellationToken);
                var factor = unit.QuantityPerUnit > 0 ? unit.QuantityPerUnit : 1m;
                lines.Add(new InvoiceLineWriteDto
                {
                    OriginalInvoiceId = $"PANDA-LINE-{source.OrderLineId}", ProductId = product.Id,
                    ProductUnitId = unit.Id, ProductName = product.Name, UnitName = unit.Unit?.Name,
                    Quantity = source.Quantity, QuantityPerUnitSnapshot = factor, BaseQuantity = source.Quantity * factor,
                    UnitPrice = source.UnitPrice, UnitCost = unit.PurchasePrice, TaxExempt = source.TaxRate == 0,
                    TaxRate = source.TaxRate, ExpiryDate = DateTime.Today.AddYears(10),
                    CreatedDate = integrationEvent.OccurredAtUtc, UpdatedDate = DateTime.UtcNow
                });
            }

            var customerId = await ResolveCustomerAsync(integrationEvent.Order, cancellationToken);
            var reference = $"PANDA-ORDER-{externalOrderId}";
            var create = await _invoiceService.CreateAsync(new InvoiceWriteDto
            {
                InvoiceNumber = reference, OriginalInvoiceId = reference, InvoiceType = InvoiceType.EndpointOrder,
                PaymentType = PaymentType.Credit, CustomerId = customerId, Status = InvoiceStatus.Unknown,
                IsPOS = false, OpenedAt = integrationEvent.OccurredAtUtc, InvoiceLines = lines,
                DiscountAmount = integrationEvent.Order.DiscountTotal,
                CreatedDate = integrationEvent.OccurredAtUtc, UpdatedDate = DateTime.UtcNow
            });
            if (!create.Success || create.Data == null)
                return await RollbackFailAsync(transaction, create.Message ?? "Invoice creation failed.", cancellationToken);
            var reserve = await _statusService.ApplyStatusAsync(create.Data.Id, InvoiceStatus.Unknown, cancellationToken, synchronizeBox: false);
            if (!reserve.Success)
                return await RollbackFailAsync(transaction, reserve.Message ?? "Stock reservation failed.", cancellationToken);

            inbox.Status = IntegrationInboxStatus.Completed;
            inbox.ProcessedAtUtc = inbox.UpdatedDate = DateTime.UtcNow;
            inbox.RaccoonInvoiceId = create.Data.Id;
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result<PandaOrderProcessResult>.Ok(new(create.Data.Id, false));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<PandaOrderProcessResult>.Fail($"Panda order processing failed: {ex.Message}");
        }
    }

    private async Task<int?> ResolveCustomerAsync(PandaOrderSnapshot order, CancellationToken cancellationToken)
    {
        var phone = order.CustomerPhone?.Trim();
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var id = await _context.Set<User>().Where(x => x.PhoneNumber == phone).Select(x => (int?)x.Id).FirstOrDefaultAsync(cancellationToken);
            if (id.HasValue) return id;
        }
        var user = new User { Name = order.ShopName ?? order.CustomerName ?? $"Panda customer {order.CustomerId}",
            PhoneNumber = phone, Password = "PANDA-EXTERNAL", Role = UserRole.Customer,
            CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow };
        await _context.Set<User>().AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return user.Id;
    }

    private static async Task<Result<PandaOrderProcessResult>> RollbackFailAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction, string message, CancellationToken token)
    { await transaction.RollbackAsync(token); return Result<PandaOrderProcessResult>.Fail(message); }
}
