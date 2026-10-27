using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Settings;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Application.Service.Settings;

namespace RaccoonWarehouse.Application.Service.Invoices;

public sealed record JordanEInvoiceSubmissionResult(
    JordanEInvoiceStatus Status,
    string? Reference,
    string? QrPayload,
    string Message);

public interface IJordanEInvoiceService
{
    Task<Result<JordanEInvoiceSubmissionResult>> SubmitAsync(int invoiceId, CancellationToken cancellationToken = default);
}

public sealed class JordanEInvoiceService : IJordanEInvoiceService
{
    private const string Endpoint = "https://backend.jofotara.gov.jo/core/invoices/";
    private const string UblNamespace = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private const string CbcNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private const string CacNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

    private readonly ApplicationDbContext _db;
    private readonly DocumentBrandingService _branding;
    private readonly HttpClient _http;

    public JordanEInvoiceService(ApplicationDbContext db, DocumentBrandingService branding, HttpClient http)
    {
        _db = db;
        _branding = branding;
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(45);
    }

    public async Task<Result<JordanEInvoiceSubmissionResult>> SubmitAsync(
        int invoiceId,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _db.Set<Invoice>()
            .Include(x => x.InvoiceLines!).ThenInclude(x => x.Product)
            .Include(x => x.InvoiceLines!).ThenInclude(x => x.ProductUnit)
            .SingleOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);

        var customerName = invoice?.CustomerId is int customerId
            ? await _db.Set<User>()
                .Where(x => x.Id == customerId)
                .Select(x => x.Name)
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        if (invoice == null)
            return Result<JordanEInvoiceSubmissionResult>.Fail("Invoice not found / الفاتورة غير موجودة.");

        if (invoice.InvoiceType != InvoiceType.Sale)
            return Result<JordanEInvoiceSubmissionResult>.Fail("Only completed sales invoices can be submitted to JoFotara / يمكن إرسال فواتير البيع المكتملة فقط إلى جوافوترة.");

        if (invoice.JordanEInvoiceStatus == JordanEInvoiceStatus.Submitted &&
            !string.IsNullOrWhiteSpace(invoice.JordanEInvoiceQr))
        {
            return Result<JordanEInvoiceSubmissionResult>.Ok(
                new JordanEInvoiceSubmissionResult(
                    invoice.JordanEInvoiceStatus,
                    invoice.JordanEInvoiceReference,
                    invoice.JordanEInvoiceQr,
                    "Invoice was already submitted to JoFotara / تم إرسال الفاتورة إلى جوافوترة مسبقاً."),
                "Invoice already submitted / الفاتورة مرسلة مسبقاً.");
        }

        var clientId = Environment.GetEnvironmentVariable("ROCCOPOS_JOFOTARA_CLIENT_ID")?.Trim();
        var secretKey = Environment.GetEnvironmentVariable("ROCCOPOS_JOFOTARA_SECRET_KEY")?.Trim();
        var enabled = Environment.GetEnvironmentVariable("ROCCOPOS_JOFOTARA_ENABLED");

        if (!IsTrue(enabled))
            return Result<JordanEInvoiceSubmissionResult>.Fail("JoFotara integration is disabled. Set ROCCOPOS_JOFOTARA_ENABLED=true after configuring the credentials / تكامل جوافوترة غير مفعل. فعّل ROCCOPOS_JOFOTARA_ENABLED=true بعد إعداد بيانات الربط.");

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(secretKey))
            return Result<JordanEInvoiceSubmissionResult>.Fail("JoFotara Client-Id and Secret-Key are not configured / لم يتم إعداد Client-Id و Secret-Key الخاصين بجوافوترة.");

        var validation = ValidateInvoice(invoice, customerName);
        if (validation != null)
            return Result<JordanEInvoiceSubmissionResult>.Fail(validation);

        try
        {
            var branding = await _branding.GetAsync(invoice.BranchId);
            var xml = BuildUblInvoice(invoice, branding, Environment.GetEnvironmentVariable("ROCCOPOS_JOFOTARA_INCOME_SOURCE_SEQUENCE"), customerName);
            var encodedInvoice = Convert.ToBase64String(Encoding.UTF8.GetBytes(xml.ToString(SaveOptions.DisableFormatting)));

            invoice.JordanEInvoiceLastAttemptAtUtc = DateTime.UtcNow;
            invoice.JordanEInvoiceAttemptCount++;
            invoice.JordanEInvoiceStatus = JordanEInvoiceStatus.Failed;
            await _db.SaveChangesAsync(cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            request.Headers.Add("Client-Id", clientId);
            request.Headers.Add("Secret-Key", secretKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { invoice = encodedInvoice }),
                Encoding.UTF8,
                "application/json");

            using var response = await _http.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = ParseResponse(responseBody);
            var accepted = response.IsSuccessStatusCode && IsAccepted(parsed.Status);

            invoice.JordanEInvoiceResponse = responseBody;
            invoice.JordanEInvoiceReference = parsed.Reference;
            invoice.JordanEInvoiceQr = parsed.QrPayload;
            invoice.JordanEInvoiceStatus = accepted
                ? JordanEInvoiceStatus.Submitted
                : parsed.Status is "REJECTED" or "NOT_SUBMITTED"
                    ? JordanEInvoiceStatus.Rejected
                    : JordanEInvoiceStatus.Failed;
            invoice.JordanEInvoiceSubmittedAtUtc = accepted ? DateTime.UtcNow : null;
            await _db.SaveChangesAsync(cancellationToken);

            var message = accepted
                ? "Invoice submitted to JoFotara successfully / تم إرسال الفاتورة إلى جوافوترة بنجاح."
                : $"JoFotara rejected or did not accept the invoice: {parsed.Status ?? response.StatusCode.ToString()} / لم تقبل جوافوترة الفاتورة.";

            var result = new JordanEInvoiceSubmissionResult(
                invoice.JordanEInvoiceStatus,
                invoice.JordanEInvoiceReference,
                invoice.JordanEInvoiceQr,
                message);

            return accepted
                ? Result<JordanEInvoiceSubmissionResult>.Ok(result, message)
                : Result<JordanEInvoiceSubmissionResult>.Fail(message);
        }
        catch (Exception ex)
        {
            invoice.JordanEInvoiceStatus = JordanEInvoiceStatus.Failed;
            invoice.JordanEInvoiceResponse = ex.Message;
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                // Preserve the original integration failure for the UI.
            }

            return Result<JordanEInvoiceSubmissionResult>.Fail(
                $"JoFotara submission failed: {ex.Message} / فشل إرسال الفاتورة إلى جوافوترة: {ex.Message}");
        }
    }

    private static string? ValidateInvoice(Invoice invoice, string? customerName)
    {
        if (invoice.InvoiceLines == null || invoice.InvoiceLines.Count == 0)
            return "The invoice has no lines / الفاتورة لا تحتوي على بنود.";

        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
            return "Invoice number is required / رقم الفاتورة مطلوب.";

        if (invoice.PaymentType == PaymentType.Credit && invoice.CustomerId == null)
            return "A customer is required for a credit invoice / يجب اختيار عميل للفواتير الآجلة.";

        if (invoice.TotalAmount >= 10000m && invoice.CustomerId == null)
            return "A customer is required for sales of 10,000 JOD or more / يجب اختيار عميل للمبيعات التي تساوي أو تتجاوز 10000 دينار.";

        if (invoice.CustomerId.HasValue && string.IsNullOrWhiteSpace(customerName))
            return "The selected customer could not be found / لم يتم العثور على العميل المحدد.";

        return null;
    }

    private static XElement BuildUblInvoice(Invoice invoice, DocumentBranding branding, string? incomeSourceSequence, string? customerName)
    {
        XNamespace ubl = UblNamespace;
        XNamespace cbc = CbcNamespace;
        XNamespace cac = CacNamespace;
        var currency = "JOD";
        var taxNumber = branding.TaxNumber?.Trim();
        var sellerName = string.IsNullOrWhiteSpace(branding.DisplayName) ? "ROCCOPOS" : branding.DisplayName.Trim();
        var buyerName = customerName?.Trim();
        var discount = Math.Max(0m, invoice.DiscountAmount ?? 0m);
        var taxGroups = (invoice.InvoiceLines ?? Array.Empty<InvoiceLine>())
            .GroupBy(x => x.TaxExempt ? 0m : x.TaxRate)
            .Select(group => new
            {
                Rate = group.Key,
                Amount = Math.Round(group.Sum(x => x.TaxAmount), 3, MidpointRounding.AwayFromZero)
            })
            .ToList();

        var taxSubtotals = taxGroups.Select(group => new XElement(cac + "TaxSubtotal",
            new XElement(cbc + "TaxableAmount", new XAttribute("currencyID", currency), Money(group.Rate == 0m ? invoice.SubTotal : invoice.SubTotal - discount)),
            new XElement(cbc + "TaxAmount", new XAttribute("currencyID", currency), Money(group.Amount)),
            new XElement(cac + "TaxCategory",
                new XElement(cbc + "ID", group.Rate == 0m ? "Z" : "S"),
                new XElement(cbc + "Percent", group.Rate.ToString("0.###", CultureInfo.InvariantCulture)))))
            .ToList();

        var root = new XElement(ubl + "Invoice",
            new XAttribute(XNamespace.Xmlns + "cbc", cbc),
            new XAttribute(XNamespace.Xmlns + "cac", cac),
            new XElement(cbc + "UBLVersionID", "2.1"),
            new XElement(cbc + "ID", invoice.InvoiceNumber),
            new XElement(cbc + "IssueDate", (invoice.DocumentDate ?? invoice.CreatedDate).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            new XElement(cbc + "InvoiceTypeCode", "388"),
            new XElement(cbc + "DocumentCurrencyCode", currency),
            SupplierParty(cac, cbc, sellerName, branding.Address, branding.Phone, taxNumber, incomeSourceSequence),
            CustomerParty(cac, cbc, buyerName),
            new XElement(cac + "TaxTotal",
                new XElement(cbc + "TaxAmount", new XAttribute("currencyID", currency), Money(invoice.TotalTax)),
                taxSubtotals),
            new XElement(cac + "LegalMonetaryTotal",
                new XElement(cbc + "LineExtensionAmount", new XAttribute("currencyID", currency), Money(invoice.SubTotal)),
                new XElement(cbc + "AllowanceTotalAmount", new XAttribute("currencyID", currency), Money(discount)),
                new XElement(cbc + "TaxExclusiveAmount", new XAttribute("currencyID", currency), Money(invoice.SubTotal - discount)),
                new XElement(cbc + "TaxInclusiveAmount", new XAttribute("currencyID", currency), Money(invoice.TotalAmount)),
                new XElement(cbc + "PayableAmount", new XAttribute("currencyID", currency), Money(invoice.TotalAmount))),
            (invoice.InvoiceLines ?? Array.Empty<InvoiceLine>())
                .Select((line, index) => InvoiceLine(cac, cbc, currency, line, index + 1)));

        return root;
    }

    private static XElement SupplierParty(
        XNamespace cac,
        XNamespace cbc,
        string name,
        string address,
        string phone,
        string? taxNumber,
        string? incomeSourceSequence)
    {
        var ids = new List<XElement>();
        if (!string.IsNullOrWhiteSpace(taxNumber))
            ids.Add(new XElement(cac + "PartyIdentification", new XElement(cbc + "ID", new XAttribute("schemeID", "TIN"), taxNumber)));
        if (!string.IsNullOrWhiteSpace(incomeSourceSequence))
            ids.Add(new XElement(cac + "PartyIdentification", new XElement(cbc + "ID", new XAttribute("schemeID", "INCOME_SOURCE"), incomeSourceSequence.Trim())));

        return new XElement(cac + "AccountingSupplierParty",
            new XElement(cac + "Party",
                ids,
                new XElement(cac + "PartyName", new XElement(cbc + "Name", name)),
                new XElement(cac + "PostalAddress", new XElement(cbc + "StreetName", address ?? string.Empty)),
                new XElement(cac + "Contact", new XElement(cbc + "Telephone", phone ?? string.Empty))));
    }

    private static XElement CustomerParty(XNamespace cac, XNamespace cbc, string? name)
    {
        return new XElement(cac + "AccountingCustomerParty",
            new XElement(cac + "Party",
                new XElement(cac + "PartyName", new XElement(cbc + "Name", string.IsNullOrWhiteSpace(name) ? "General Customer" : name))));
    }

    private static XElement InvoiceLine(XNamespace cac, XNamespace cbc, string currency, InvoiceLine line, int sequence)
    {
        var quantity = Math.Abs(line.Quantity);
        var lineAmount = Math.Abs(line.LineSubTotal);
        var unitPrice = quantity <= 0m ? 0m : lineAmount / quantity;
        var taxRate = line.TaxExempt ? 0m : line.TaxRate;
        var itemName = string.IsNullOrWhiteSpace(line.Product?.Name) ? $"Item {line.ProductId}" : line.Product!.Name!;

        return new XElement(cac + "InvoiceLine",
            new XElement(cbc + "ID", sequence.ToString(CultureInfo.InvariantCulture)),
            new XElement(cbc + "InvoicedQuantity", new XAttribute("unitCode", "EA"), quantity.ToString("0.###", CultureInfo.InvariantCulture)),
            new XElement(cbc + "LineExtensionAmount", new XAttribute("currencyID", currency), Money(lineAmount)),
            new XElement(cac + "Item",
                new XElement(cbc + "Name", itemName),
                new XElement(cac + "ClassifiedTaxCategory",
                    new XElement(cbc + "ID", taxRate == 0m ? "Z" : "S"),
                    new XElement(cbc + "Percent", taxRate.ToString("0.###", CultureInfo.InvariantCulture)))),
            new XElement(cac + "Price",
                new XElement(cbc + "PriceAmount", new XAttribute("currencyID", currency), Money(unitPrice))));
    }

    private static string Money(decimal value) => Math.Round(value, 3, MidpointRounding.AwayFromZero).ToString("0.000", CultureInfo.InvariantCulture);

    private static bool IsTrue(string? value) =>
        string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);

    private static bool IsAccepted(string? status) =>
        string.Equals(status, "SUBMITTED", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "ALREADY_SUBMITTED", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "ACCEPTED", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "APPROVED", StringComparison.OrdinalIgnoreCase);

    private static (string? Status, string? Reference, string? QrPayload) ParseResponse(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return (null, null, null);

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            return (
                FindValue(root, "EINV_STATUS", "status", "state"),
                FindValue(root, "EINV_NUM", "EINV_ID", "uuid", "id", "reference"),
                FindValue(root, "EINV_QR", "qr", "qrCode", "qrPayload"));
        }
        catch (JsonException)
        {
            return (null, null, null);
        }
    }

    private static string? FindValue(JsonElement element, params string[] names)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (names.Any(name => string.Equals(name, property.Name, StringComparison.OrdinalIgnoreCase)) &&
                    property.Value.ValueKind == JsonValueKind.String)
                    return property.Value.GetString();

                var nested = FindValue(property.Value, names);
                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
            {
                var nested = FindValue(child, names);
                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }
        }

        return null;
    }
}
