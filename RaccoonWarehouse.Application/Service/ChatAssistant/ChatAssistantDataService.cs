using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Invoices;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Products;
using RaccoonWarehouse.Application.Service.Stocks;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Core.ChatAssistant;
using RaccoonWarehouse.Domain.ChatAssistant.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Reports.Financial.Filters;
using System.Text.Json;

namespace RaccoonWarehouse.Application.Service.ChatAssistant;

public sealed class ChatAssistantDataService : IChatAssistantDataService
{
    private readonly IProductService _products;
    private readonly IStockReportService _stockReports;
    private readonly IUserService _users;
    private readonly IInvoiceService _invoices;
    private readonly UserStatementService _statements;
    private readonly IPermissionService _permissions;
    private readonly IUserSession _userSession;

    public ChatAssistantDataService(
        IProductService products,
        IStockReportService stockReports,
        IUserService users,
        IInvoiceService invoices,
        UserStatementService statements,
        IPermissionService permissions,
        IUserSession userSession)
    {
        _products = products;
        _stockReports = stockReports;
        _users = users;
        _invoices = invoices;
        _statements = statements;
        _permissions = permissions;
        _userSession = userSession;
    }

    public async Task<ChatAssistantDataResultDto?> FindDataAsync(string question, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = question.Trim().ToLowerInvariant();

        if (IsBalanceQuestion(normalized)) return await FindBalanceAsync(question, cancellationToken);
        if (IsSalesSummaryQuestion(normalized)) return await FindSalesSummaryAsync(question, cancellationToken);
        if (IsInvoiceLookupQuestion(normalized)) return await FindInvoicesAsync(question, cancellationToken);
        if (IsStockQuestion(normalized)) return await FindStockAsync(question, cancellationToken);
        if (IsProductLookupQuestion(normalized)) return await FindProductsAsync(question, cancellationToken);
        return null;
    }

    private async Task<ChatAssistantDataResultDto?> FindBalanceAsync(string question, CancellationToken cancellationToken)
    {
        var role = _userSession.CurrentRole ?? UserRole.Customer;
        var isOwnBalance = role == UserRole.Customer;
        if (!isOwnBalance && !await _permissions.HasPermissionAsync(role, "Customers.List")) return null;

        var userId = _userSession.CurrentUserId;
        if (!isOwnBalance)
        {
            var name = ExtractSearchTerm(question);
            var users = await _users.GetAllAsync();
            var customer = users.Data?.FirstOrDefault(x => x.Role == UserRole.Customer &&
                (!string.IsNullOrWhiteSpace(name) && (x.Name?.Contains(name, StringComparison.OrdinalIgnoreCase) ?? false)));
            userId = customer?.Id;
        }

        if (userId is not int id || id <= 0) return Empty("customer balance", ExtractSearchTerm(question), "Customers.List", "Open customers", "فتح العملاء");
        var balance = await _statements.GetCurrentBalanceAsync(id);
        var user = await _users.GetByIdAsync(id);
        var data = new { Customer = user.Data?.Name, Balance = balance, UserId = id };
        return Result("customer balance", ExtractSearchTerm(question), data, "Customers.List", "Open customers", "فتح العملاء");
    }

    private async Task<ChatAssistantDataResultDto?> FindInvoicesAsync(string question, CancellationToken cancellationToken)
    {
        var role = _userSession.CurrentRole ?? UserRole.Customer;
        var isOwn = role == UserRole.Customer;
        if (!isOwn && !await _permissions.HasPermissionAsync(role, "Invoices.Sales")) return null;

        var term = ExtractSearchTerm(question);
        var invoiceNumber = term.All(char.IsDigit) ? term : null;
        var customerName = isOwn ? _userSession.CurrentUser?.Name : invoiceNumber == null ? term : null;
        var result = await _invoices.SearchSalesInvoicesAsync(invoiceNumber, customerName, null, null, true, null, null);
        var rows = result.Data?.Take(20).Select(x => new
        {
            x.Id,
            x.InvoiceNumber,
            Customer = x.Customer?.Name,
            x.TotalAmount,
            x.PaymentType,
            x.Status,
            x.CreatedDate
        }).Cast<object>().ToList() ?? new List<object>();
        return Result("sales invoices", term, rows, "invoice-search", "Open invoice search", "فتح بحث الفواتير");
    }

    private async Task<ChatAssistantDataResultDto?> FindSalesSummaryAsync(string question, CancellationToken cancellationToken)
    {
        var role = _userSession.CurrentRole ?? UserRole.Customer;
        if (!await _permissions.HasPermissionAsync(role, "Reports.Sales")) return null;
        var (from, to) = GetDateRange(question);
        var result = await _invoices.GetSalesReportAsync(new FinancialSummaryFilterDto { From = from, To = to, IncludeReturns = true });
        if (!result.Success) return Empty("sales summary", question, "sales-report", "Open sales report", "فتح تقرير المبيعات");
        var data = new { From = from, To = to, result.Data.summary, InvoiceCount = result.Data.rows.Count };
        return Result("sales summary", question, data, "sales-report", "Open sales report", "فتح تقرير المبيعات");
    }

    private async Task<ChatAssistantDataResultDto?> FindProductsAsync(string question, CancellationToken cancellationToken)
    {
        var role = _userSession.CurrentRole ?? UserRole.Customer;
        if (!await _permissions.HasPermissionAsync(role, "Products.List")) return null;
        var result = await _products.GetAllAsync();
        var search = ExtractSearchTerm(question);
        var products = result.Data?.Where(x => !x.IsDeleted && (string.IsNullOrWhiteSpace(search) ||
                (x.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                x.ITEMCODE?.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) == true))
            .Take(15).Select(x => new { x.Id, x.Name, Barcode = x.ITEMCODE, SalePrice = x.DefaultSalePrice, PurchasePrice = x.DefaultPurchasePrice, x.Description }).Cast<object>().ToList() ?? new List<object>();
        return Result("products", search, products, "Products.List", "Open products", "فتح المنتجات");
    }

    private async Task<ChatAssistantDataResultDto?> FindStockAsync(string question, CancellationToken cancellationToken)
    {
        var role = _userSession.CurrentRole ?? UserRole.Customer;
        if (!await _permissions.HasPermissionAsync(role, "current-stock")) return null;
        var search = ExtractSearchTerm(question);
        var rows = await _stockReports.GetCurrentStockAsync(string.IsNullOrWhiteSpace(search) ? null : search);
        var stock = rows.Take(25).Select(x => new { x.ProductId, x.ProductName, x.ITEMCODE, x.UnitName, x.Quantity, x.MinimumQuantity, x.SalePrice, x.IsLowStock, x.NearestExpiryDate }).Cast<object>().ToList();
        return Result("stock", search, stock, "current-stock", "Open current stock", "فتح المخزون الحالي");
    }

    private ChatAssistantDataResultDto Empty(string type, string query, string actionKey, string actionEn, string actionAr) => Result(type, query, Array.Empty<object>(), actionKey, actionEn, actionAr);

    private static ChatAssistantDataResultDto Result(string type, string query, object data, string actionKey, string actionEn, string actionAr) => new()
    {
        Type = type, Query = query, DataJson = JsonSerializer.Serialize(data), ActionKey = actionKey, ActionLabelEn = actionEn, ActionLabelAr = actionAr
    };

    private static bool IsBalanceQuestion(string text) => ContainsAny(text, "balance", "debt", "credit", "رصيد", "حساب", "مديون");
    private static bool IsSalesSummaryQuestion(string text) => ContainsAny(text, "sales today", "sales this month", "sales report", "how much did we sell", "مبيعات اليوم", "مبيعات الشهر", "تقرير المبيعات", "كم بعنا");
    private static bool IsInvoiceLookupQuestion(string text) =>
        ContainsAny(text, "invoice", "invoices", "receipt", "فاتورة", "فواتير") &&
        (ContainsAny(text, "search", "find", "show", "details", "number", "بحث", "ابحث", "تفاصيل", "رقم") || text.Any(char.IsDigit));
    private static bool IsStockQuestion(string text) => ContainsAny(text, "stock", "inventory", "quantity", "available", "low stock", "مخزون", "المخزون", "كمية", "متوفر", "الحد الأدنى");
    private static bool IsProductLookupQuestion(string text) =>
        ContainsAny(text, "product", "products", "item", "price", "barcode", "صنف", "منتج", "سعر", "باركود", "منتجات") &&
        !ContainsAny(text, "add", "new", "create", "اضافة", "إضافة", "جديد", "إنشاء") &&
        ContainsAny(text, "find", "search", "show", "details", "price", "barcode", "available", "how many", "what", "ابحث", "بحث", "اعرض", "تفاصيل", "سعر", "باركود", "متوفر", "كم");
    private static bool ContainsAny(string text, params string[] values) => values.Any(text.Contains);

    private static (DateTime From, DateTime To) GetDateRange(string question)
    {
        var today = DateTime.Today;
        return question.Contains("today", StringComparison.OrdinalIgnoreCase) || question.Contains("اليوم", StringComparison.Ordinal)
            ? (today, today.AddDays(1).AddTicks(-1))
            : question.Contains("month", StringComparison.OrdinalIgnoreCase) || question.Contains("الشهر", StringComparison.Ordinal)
                ? (new DateTime(today.Year, today.Month, 1), today.AddDays(1).AddTicks(-1))
                : (today.AddDays(-30), today.AddDays(1).AddTicks(-1));
    }

    private static string ExtractSearchTerm(string question)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "what", "is", "the", "of", "for", "show", "find", "search", "me", "please", "how", "many", "do", "we", "have", "my", "customer",
            "stock", "inventory", "quantity", "available", "product", "products", "item", "items", "price", "barcode", "low", "in", "invoice", "invoices", "sales", "report", "today", "this", "month", "balance", "debt", "credit",
            "ما", "هو", "هي", "كم", "من", "عن", "اعرض", "أظهر", "ابحث", "لي", "الرجاء", "هل", "يوجد", "مخزون", "المخزون", "كمية", "متوفر", "منتج", "المنتج", "صنف", "الصنف", "سعر", "باركود", "فاتورة", "فواتير", "مبيعات", "تقرير", "اليوم", "الشهر", "رصيد", "حساب", "مديون"
        };
        return string.Join(' ', question.Split(new[] { ' ', '\t', '\r', '\n', '?', '؟', ':', '-' }, StringSplitOptions.RemoveEmptyEntries).Where(x => !stopWords.Contains(x))).Trim();
    }
}
