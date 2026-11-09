using RaccoonWarehouse.Application.Service.Stocks;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Users.DTOs;
using RaccoonWarehouse.Domain.Vouchers;
using RaccoonWarehouse.Domain.StockDocuments;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Application.Service.Sales;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Permissions;
using System.Reflection;

namespace RaccoonWarehouse.Application.Service.Permissions;

/// <summary>Enforces the existing permission service at the DI application-service boundary.
/// Child calls of an authorized workflow retain its context; business validations still execute.</summary>
public class ApplicationPermissionBoundary : DispatchProxy
{
    private static readonly AsyncLocal<int> WorkflowDepth = new();
    private object _target = null!;
    private Func<string, Task<bool>> _hasPermission = null!;
    private string _service = "";
    private ApplicationDbContext? _context;

    public static IDisposable EnterSystemWorkflow()
    {
        WorkflowDepth.Value++;
        return new SystemWorkflow();
    }
    private sealed class SystemWorkflow : IDisposable
    {
        private bool _disposed;
        public void Dispose() { if (!_disposed) { _disposed = true; WorkflowDepth.Value--; } }
    }

    public static object Wrap(Type contract, object target, IPermissionService permissions, ApplicationDbContext? context = null)
    {
        var proxy = (ApplicationPermissionBoundary)Create(contract, typeof(ApplicationPermissionBoundary));
        proxy._target = target;
        proxy._hasPermission = permissions.HasCurrentUserPermissionAsync;
        proxy._context = context;
        proxy._service = target.GetType().Name;
        return proxy;
    }

    public static object WrapSingleton(Type contract, object target, IServiceScopeFactory scopes)
    {
        var proxy = (ApplicationPermissionBoundary)Create(contract, typeof(ApplicationPermissionBoundary));
        proxy._target = target; proxy._service = target.GetType().Name;
        proxy._hasPermission = async key =>
        {
            using var scope = scopes.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IPermissionService>().HasCurrentUserPermissionAsync(key).ConfigureAwait(false);
        };
        return proxy;
    }

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(method);
        args ??= Array.Empty<object?>();
        if (method.Name.StartsWith("add_", StringComparison.Ordinal) || method.Name.StartsWith("remove_", StringComparison.Ordinal)) return InvokeTarget(method, args);
        if (!PermissionCatalog.EnforcementEnabled) return InvokeTarget(method, args);
        var rule = ServicePermissionPolicy.Rule(_service, method.Name, args);
        if (WorkflowDepth.Value > 0 || rule == "internal") return InvokeTarget(method, args);
        if (method.ReturnType == typeof(Task)) return RunAsync(method, args, rule);
        if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            return typeof(ApplicationPermissionBoundary).GetMethod(nameof(RunResultAsync), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(method.ReturnType.GenericTypeArguments[0]).Invoke(this, new object[] { method, args, rule });
        // Synchronous service operations are exceptional and never treated as implicit allows.
        if (!Task.Run(() => AllowedAsync(rule)).GetAwaiter().GetResult())
            throw new UnauthorizedAccessException("ليس لديك صلاحية لهذه العملية. / Permission denied.");
        WorkflowDepth.Value++;
        try { return InvokeTarget(method, args); }
        finally { WorkflowDepth.Value--; }
    }

    private async Task<string> ResolveRuleAsync(MethodInfo method, object?[] args, string rule)
    {
        var deleting = method.Name is "DeleteAsync" or "SoftDeleteAsync";
        if (_service == "InvoiceService" && _context != null && args.OfType<InvoiceWriteDto>().FirstOrDefault() is { Id: > 0 } edited)
        {
            var prior = await _context.Set<Invoice>().AsNoTracking().Where(i => i.Id == edited.Id)
                .Select(i => new { i.Status, i.InvoiceType, i.IsPOS, i.PostingStatus }).SingleOrDefaultAsync().ConfigureAwait(false);
            if (prior != null)
            {
                var resource = ServicePermissionPolicy.InvoiceResource(new InvoiceWriteDto { InvoiceType = prior.InvoiceType, IsPOS = prior.IsPOS });
                rule += ";" + resource + ".Edit";
                if (prior.PostingStatus != AccountingPostingStatus.NotPosted || prior.Status is InvoiceStatus.Completed or InvoiceStatus.Posted) rule += ";" + resource + ".Post";
                if (prior.PostingStatus == AccountingPostingStatus.Posted) rule += ";Accounting.JournalEntry.Reverse";
                if (edited.Status == InvoiceStatus.Cancelled) rule += ";" + resource + ".Cancel";
                if (prior.Status is InvoiceStatus.Completed or InvoiceStatus.Posted && edited.Status is InvoiceStatus.Draft or InvoiceStatus.OnHold)
                    rule += ";" + resource + ".Reopen";
            }
        }
        if (_service == "InvoiceLineService" && deleting)
        {
            if (_context == null || args.FirstOrDefault() is not int lineId) return "Permissions.ProtectedRecord";
            var parentInvoiceId = await _context.Set<RaccoonWarehouse.Domain.InvoiceLines.InvoiceLine>().AsNoTracking()
                .Where(line => line.Id == lineId).Select(line => line.InvoiceId).SingleOrDefaultAsync().ConfigureAwait(false);
            if (await _context.Set<Invoice>().AnyAsync(i => i.Id == parentInvoiceId && (i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.OnHold || i.PostingStatus != AccountingPostingStatus.NotPosted)).ConfigureAwait(false))
                return "Permissions.ProtectedRecord";
        }
        if (_service == "SaleCheckoutService" && args.FirstOrDefault() is SaleCheckoutRequest sale)
        {
            var resource = ServicePermissionPolicy.InvoiceResource(sale.Invoice);
            rule = resource + ".Create;" + resource + ".Post";
            if (sale.Invoice.InvoiceType is InvoiceType.Return or InvoiceType.Exchange) rule += ";SalesInvoice.Return";
            if (sale.InsuranceDetails != null) rule += ";InsuranceClaims.Create";
        }
        if (_service == "StockService" && method.Name is "PostMovementAsync" or "PostMovementsAsync")
        {
            var movements = args.OfType<IEnumerable<StockMovementPostDto>>().FirstOrDefault()?.ToArray()
                ?? args.OfType<StockMovementPostDto>().ToArray();
            if (movements.Length == 0) return "Permissions.Unmapped";
            rule = string.Join(";", movements.Select(m => m.TransactionType switch
            {
                TransactionType.Purchase => m.Quantity < 0 ? "PurchaseReturn.Confirm" : "PurchaseInvoice.Post|StockInVoucher.Post",
                TransactionType.Return => "SalesInvoice.Return",
                TransactionType.Sale => "POS.Post|SalesInvoice.Post",
                TransactionType.Adjustment => "StockAdjustment.Post",
                TransactionType.Damage => "StockOutVoucher.Post", _ => "Permissions.Unmapped"
            }).Distinct());
        }
        if (_service == "VoucherService" && method.Name == "UpdateAsync" && _context != null &&
            args.OfType<RaccoonWarehouse.Domain.Vouchers.DTOs.VoucherWriteDto>().FirstOrDefault() is { } editedVoucher &&
            await _context.Set<Voucher>().AnyAsync(v => v.Id == editedVoucher.Id && v.PostingStatus == AccountingPostingStatus.Posted).ConfigureAwait(false))
            rule += ";Accounting.JournalEntry.Reverse";
        if (_service == "StockDocumentService" && args.OfType<RaccoonWarehouse.Domain.StockDocuments.DTOs.StockDocumentWriteDto>().FirstOrDefault() is { } document)
        {
            var resource = document.Type == StockVoucherType.Out ? "StockOutVoucher" : "StockInVoucher";
            rule = resource + (method.Name == "UpdateAsync" ? ".Edit" : ".Create") + ";" + resource + ".Post";
        }
        if (_service == "UserService")
        {
            if (method.Name is "GetAllAsync" or "GetAllWithFilteringAndIncludeAsync" or "GetAllWithIncludeAsync")
                rule = "Users.View|Customers.View|Suppliers.View|POS.View|PurchaseInvoice.View|PurchaseOrder.View|Vouchers.View";
            var dto = args.OfType<UserWriteDto>().FirstOrDefault();
            var id = dto?.Id ?? (args.FirstOrDefault() is int number ? number : 0);
            var oldRole = _context != null && id > 0 ? await _context.Set<User>().AsNoTracking().Where(u => u.Id == id)
                .Select(u => (UserRole?)u.Role).SingleOrDefaultAsync().ConfigureAwait(false) : null;
            var role = dto?.Role ?? oldRole;
            var isParty = (role is UserRole.Customer or UserRole.Supplier) && (oldRole == null || oldRole is UserRole.Customer or UserRole.Supplier);
            if (isParty)
            {
                var resource = role == UserRole.Supplier ? "Suppliers" : "Customers";
                var action = deleting ? "Delete" : method.Name == "CreateAsync" ? "Create" : method.Name == "UpdateAsync" ? "Edit" : "View";
                rule = resource + "." + action;
                if (dto != null && !await _hasPermission("Users.ManageUsers").ConfigureAwait(false))
                {
                    var password = _context != null && dto.Id > 0 ? await _context.Set<User>().AsNoTracking()
                        .Where(u => u.Id == dto.Id).Select(u => u.Password).SingleOrDefaultAsync().ConfigureAwait(false) : string.Empty;
                    if (!string.IsNullOrEmpty(dto.Password) && dto.Password != password) return "Users.ManageUsers";
                    dto.Password = password ?? string.Empty;
                }
            }
        }
        if (_service == "InvoiceService" && _context != null &&
            method.Name is "GetFullInvoiceByIdAsync" or "GetByIdAsync" or "GetWriteDtoByIdAsync" or "DeleteAsync" or "SoftDeleteAsync" &&
            args.FirstOrDefault() is int invoiceId)
        {
            var invoice = await _context.Set<Invoice>().AsNoTracking().Where(i => i.Id == invoiceId)
                .Select(i => new { i.InvoiceType, i.Status, i.IsPOS, i.PostingStatus }).SingleOrDefaultAsync().ConfigureAwait(false);
            if (invoice != null)
            {
                if (deleting && (invoice.Status is not (InvoiceStatus.Draft or InvoiceStatus.OnHold) || invoice.PostingStatus != AccountingPostingStatus.NotPosted)) return "Permissions.ProtectedRecord";
                if (deleting && (await _context.Set<RaccoonWarehouse.Domain.FinancialTransactions.FinancialTransaction>().AnyAsync(t => t.InvoiceId == invoiceId).ConfigureAwait(false)
                    || await _context.Set<RaccoonWarehouse.Domain.StockTransactions.StockTransaction>().AnyAsync(t => t.InvoiceId == invoiceId).ConfigureAwait(false))) return "Permissions.ProtectedRecord";
                var resource = ServicePermissionPolicy.InvoiceResource(new InvoiceWriteDto { InvoiceType = invoice.InvoiceType, IsPOS = invoice.IsPOS });
                rule = resource + (deleting ? ".Delete" : ".View");
            }
        }
        if (_service == "StockDocumentService" && _context != null && args.FirstOrDefault() is int documentId &&
            (deleting || method.Name is "GetFullDocumentByIdAsync" or "GetByIdAsync" or "GetWriteDtoByIdAsync"))
        {
            var type = await _context.Set<StockDocument>().Where(d => d.Id == documentId).Select(d => d.Type).FirstOrDefaultAsync().ConfigureAwait(false);
            rule = (type == StockVoucherType.Out ? "StockOutVoucher" : "StockInVoucher") + (deleting ? ".Delete" : ".View");
        }
        if (deleting && _service is "VoucherService" or "StockDocumentService")
        {
            if (_context == null || args.FirstOrDefault() is not int id) return "Permissions.ProtectedRecord";
            var posted = _service == "VoucherService"
                ? await _context.Set<Voucher>().AnyAsync(x => x.Id == id && x.PostingStatus != AccountingPostingStatus.NotPosted).ConfigureAwait(false)
                : await _context.Set<StockDocument>().AnyAsync(x => x.Id == id && x.PostingStatus != AccountingPostingStatus.NotPosted).ConfigureAwait(false);
            if (posted) return "Permissions.ProtectedRecord";
            if (_service == "VoucherService" && await _context.Set<RaccoonWarehouse.Domain.FinancialTransactions.FinancialTransaction>()
                .AnyAsync(t => t.VoucherId == id || (t.SourceId == id && (t.SourceType == FinancialSourceType.ReceiptVoucher || t.SourceType == FinancialSourceType.PaymentVoucher))).ConfigureAwait(false))
                return "Permissions.ProtectedRecord";
            if (_service == "StockDocumentService" && await _context.Set<RaccoonWarehouse.Domain.StockTransactions.StockTransaction>()
                .AnyAsync(t => t.StockDocumentId == id).ConfigureAwait(false)) return "Permissions.ProtectedRecord";
        }
        if (_context != null && _service is "ProductService" or "ProductUnitService")
        {
            var editedUnits = args.OfType<IEnumerable<RaccoonWarehouse.Domain.ProductUnits.DTOs.ProductUnitWriteDto>>().FirstOrDefault()?.Where(unit => unit.Id > 0).ToArray()
                ?? args.OfType<RaccoonWarehouse.Domain.ProductUnits.DTOs.ProductUnitWriteDto>().Where(unit => unit.Id > 0).ToArray();
            if (editedUnits.Length > 0)
            {
                var ids = editedUnits.Select(unit => unit.Id).ToArray();
                var prices = await _context.Set<RaccoonWarehouse.Domain.ProductUnits.ProductUnit>().AsNoTracking()
                    .Where(unit => ids.Contains(unit.Id)).Select(unit => new { unit.Id, unit.SalePrice }).ToListAsync().ConfigureAwait(false);
                if (editedUnits.Any(unit => prices.Any(price => price.Id == unit.Id && price.SalePrice != unit.SalePrice))) rule += ";Products.ChangePrice";
            }
        }
        var pricedInvoice = args.OfType<InvoiceWriteDto>().FirstOrDefault() ?? args.OfType<SaleCheckoutRequest>().FirstOrDefault()?.Invoice;
        if (pricedInvoice != null && pricedInvoice.InvoiceType == InvoiceType.Sale)
        {
            var resource = ServicePermissionPolicy.InvoiceResource(pricedInvoice);
            if (pricedInvoice.DiscountAmount > 0 || pricedInvoice.InvoiceLines?.Any(line => line.LineDiscountAmount > 0) == true)
                rule += ";" + resource + ".ApplyDiscount;SensitiveFinancial.ApplyDiscount";
            if (_context != null && pricedInvoice.InvoiceLines != null)
            {
                var lines = pricedInvoice.InvoiceLines.Where(line => line.ProductUnitId > 0 && line.Quantity > 0).ToArray();
                var ids = lines.Select(line => line.ProductUnitId).Distinct().ToArray();
                var prices = await _context.Set<RaccoonWarehouse.Domain.ProductUnits.ProductUnit>().AsNoTracking()
                    .Where(unit => ids.Contains(unit.Id)).Select(unit => new { unit.Id, unit.SalePrice }).ToListAsync().ConfigureAwait(false);
                var lotPrices = await _context.Set<RaccoonWarehouse.Domain.StockLots.StockLot>().AsNoTracking()
                    .Where(lot => ids.Contains(lot.ProductUnitId) && lot.Status == BatchStatus.Active && lot.RemainingQuantity > 0)
                    .Select(lot => new { Id = lot.ProductUnitId, lot.SalePrice }).Distinct().ToListAsync().ConfigureAwait(false);
                if (lines.Any(line => !prices.Any(price => price.Id == line.ProductUnitId && price.SalePrice == line.UnitPrice) &&
                    !lotPrices.Any(price => price.Id == line.ProductUnitId && price.SalePrice == line.UnitPrice)))
                    rule += ";" + resource + ".ChangePrice;SensitiveFinancial.ChangePrice";
            }
        }
        return rule;
    }

    private object? InvokeTarget(MethodInfo method, object?[] args)
    {
        try { return method.Invoke(_target, args); }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private async Task<bool> AllowedAsync(string rule)
    {
        foreach (var required in rule.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var allowed = false;
            foreach (var alternative in required.Split('|', StringSplitOptions.RemoveEmptyEntries))
                if (await _hasPermission(alternative).ConfigureAwait(false)) { allowed = true; break; }
            if (!allowed) return false;
        }
        return rule.Length > 0;
    }

    private async Task<IDisposable> ProtectSecurityEntitiesAsync()
    {
        if (_context == null) return new Cleanup(() => { });
        var canManageUsers = await _hasPermission("Users.ManageUsers").ConfigureAwait(false);
        EventHandler<Microsoft.EntityFrameworkCore.SavingChangesEventArgs> check = (_, _) =>
        {
            var changed = _context.ChangeTracker.Entries().Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
            var securityWrite = changed.Any(entry => entry.Entity is RaccoonWarehouse.Domain.Permissions.PermissionDefinition
                or RaccoonWarehouse.Domain.Permissions.RolePermission or RaccoonWarehouse.Domain.Permissions.UserPermission);
            var staffWrite = !canManageUsers && changed.Any(entry => entry.Entity is User user &&
                (user.Role is not (UserRole.Customer or UserRole.Supplier) ||
                 entry.State != EntityState.Added && entry.OriginalValues.GetValue<UserRole>(nameof(User.Role)) is not (UserRole.Customer or UserRole.Supplier)));
            if (securityWrite || staffWrite)
            {
                // A rejected graph must not be committed by a later operation on the same scope.
                _context.ChangeTracker.Clear();
                throw new UnauthorizedAccessException("لا يمكن تعديل الأمان من هذه العملية. / Security changes require their dedicated authorization boundary.");
            }
        };
        _context.SavingChanges += check;
        return new Cleanup(() => _context.SavingChanges -= check);
    }
    private sealed class Cleanup(Action action) : IDisposable
    {
        private Action? _action = action;
        public void Dispose() => Interlocked.Exchange(ref _action, null)?.Invoke();
    }

    private async Task RunAsync(MethodInfo method, object?[] args, string rule)
    {
        if (!await AllowedAsync(await ResolveRuleAsync(method, args, rule).ConfigureAwait(false)).ConfigureAwait(false))
            throw new UnauthorizedAccessException("ليس لديك صلاحية لهذه العملية. / Permission denied.");
        using var guardedContext = await ProtectSecurityEntitiesAsync().ConfigureAwait(false);
        WorkflowDepth.Value++;
        try { await ((Task)InvokeTarget(method, args)!).ConfigureAwait(false); }
        finally { WorkflowDepth.Value--; }
    }

    private async Task<T> RunResultAsync<T>(MethodInfo method, object?[] args, string rule)
    {
        if (!await AllowedAsync(await ResolveRuleAsync(method, args, rule).ConfigureAwait(false)).ConfigureAwait(false))
        {
            var fail = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(m => m.Name == "Fail" && m.GetParameters().FirstOrDefault()?.ParameterType == typeof(string));
            if (fail != null) return (T)fail.Invoke(null, fail.GetParameters().Length == 1 ? new object?[] { "ليس لديك صلاحية لهذه العملية. / Permission denied." } : new object?[] { "ليس لديك صلاحية لهذه العملية. / Permission denied.", null })!;
            throw new UnauthorizedAccessException("ليس لديك صلاحية لهذه العملية. / Permission denied.");
        }
        using var guardedContext = await ProtectSecurityEntitiesAsync().ConfigureAwait(false);
        WorkflowDepth.Value++;
        try
        {
            var result = await ((Task<T>)InvokeTarget(method, args)!).ConfigureAwait(false);
            if (_service == "UserService" && result is Result<List<UserReadDto>> users && users.Data != null)
            {
                if (!await _hasPermission("Users.View").ConfigureAwait(false))
                {
                    var customers = await AllowedAsync("Customers.View|POS.View|Vouchers.View").ConfigureAwait(false);
                    var suppliers = await AllowedAsync("Suppliers.View|PurchaseInvoice.View|PurchaseOrder.View|Vouchers.View").ConfigureAwait(false);
                    users.Data = users.Data.Where(u => (customers && u.Role == UserRole.Customer) || (suppliers && u.Role == UserRole.Supplier)).ToList();
                }
                if (!await _hasPermission("Users.ManageUsers").ConfigureAwait(false))
                    foreach (var user in users.Data) user.Password = string.Empty;
            }
            if (_service == "UserService" && result is Result<UserWriteDto> writable && writable.Data != null &&
                !await _hasPermission("Users.ManageUsers").ConfigureAwait(false)) writable.Data.Password = string.Empty;
            return result;
        }
        finally { WorkflowDepth.Value--; }
    }
}

public static class PermissionServiceRegistration
{
    public static void ProtectApplicationServices(this IServiceCollection services)
    {
        foreach (var descriptor in services.ToArray())
        {
            var type = descriptor.ServiceType;
            if (!type.IsInterface || type.Namespace?.Contains("ChatAssistant", StringComparison.Ordinal) == true) continue;
            if (type.Name is "IPermissionService" or "IReportPermissionService" or "IUserSession" or "IAuthService" or
                "INotificationService" or "IPosProductCatalogService" or "IPosProductCatalogRefreshQueue" or "IPayrollSensitiveDataProtector") continue;
            var businessContract = type.Namespace?.StartsWith("RaccoonWarehouse.Application.Service", StringComparison.Ordinal) == true;
            if (!businessContract && !ServicePermissionPolicy.IsProtectedContract(type.Name)) continue;
            var lifetime = descriptor.Lifetime;
            services.Remove(descriptor);
            services.Add(new ServiceDescriptor(type, provider =>
            {
                var target = descriptor.ImplementationInstance ?? descriptor.ImplementationFactory?.Invoke(provider)
                    ?? ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType!);
                return lifetime == ServiceLifetime.Singleton
                    ? ApplicationPermissionBoundary.WrapSingleton(type, target, provider.GetRequiredService<IServiceScopeFactory>())
                    : ApplicationPermissionBoundary.Wrap(type, target, provider.GetRequiredService<IPermissionService>(), provider.GetRequiredService<ApplicationDbContext>());
            }, lifetime));
        }
    }
}
