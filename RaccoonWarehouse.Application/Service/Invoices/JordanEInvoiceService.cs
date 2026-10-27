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

sealed record ParsedJordanEInvoiceResponse(
    string? Status,
    string? InvoiceNumber,
    string? InvoiceUuid,
    string? QrPayload,
    string? Message,
    IReadOnlyList<string> Errors);

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
    private readonly IJordanEInvoiceSettingsService _settings;
    private readonly HttpClient _http;

    public JordanEInvoiceService(
        ApplicationDbContext db,
        DocumentBrandingService branding,
        IJordanEInvoiceSettingsService settings,
        HttpClient http)
    {
        _db = db;
        _branding = branding;
        _settings = settings;
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

        var settings = await _settings.GetAsync(cancellationToken);
        var clientId = settings.ClientId.Trim();
        var secretKey = settings.SecretKey.Trim();
        var enabled = settings.Enabled ? "true" : "false";

        if (!IsTrue(enabled))
            return Result<JordanEInvoiceSubmissionResult>.Fail("JoFotara integration is disabled. Set ROCCOPOS_JOFOTARA_ENABLED=true after configuring the credentials / تكامل جوافوترة غير مفعل. فعّل ROCCOPOS_JOFOTARA_ENABLED=true بعد إعداد بيانات الربط.");

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(secretKey))
            return Result<JordanEInvoiceSubmissionResult>.Fail("JoFotara Client-Id and Secret-Key are not configured / لم يتم إعداد Client-Id و Secret-Key الخاصين بجوافوترة.");

        var validation = ValidateInvoice(invoice, customerName);
        if (validation != null)
            return Result<JordanEInvoiceSubmissionResult>.Fail(validation);

        var branding = await _branding.GetAsync(invoice.BranchId);
        var configurationValidation = ValidateJoFotaraConfiguration(invoice, branding, settings);
        if (configurationValidation != null)
            return Result<JordanEInvoiceSubmissionResult>.Fail(configurationValidation);

        try
        {
            invoice.JordanEInvoiceUuid ??= Guid.NewGuid().ToString();
            var xml = BuildUblInvoice(invoice, branding, settings.IncomeSourceSequence, customerName, settings.TaxRegistrationType);
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
            invoice.JordanEInvoiceReference = parsed.InvoiceNumber;
            if (!string.IsNullOrWhiteSpace(parsed.InvoiceUuid))
                invoice.JordanEInvoiceUuid = parsed.InvoiceUuid;
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

            if (!accepted)
                message = BuildRejectedMessage(parsed, response.StatusCode);

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

    private static string? ValidateJoFotaraConfiguration(
        Invoice invoice,
        DocumentBranding branding,
        JordanEInvoiceSettingsDto settings)
    {
        var taxNumber = branding.TaxNumber?.Trim() ?? string.Empty;
        if (!IsAsciiDigits(taxNumber) || taxNumber.Length is < 4 or > 15)
            return "Seller tax number must contain ASCII digits only and be between 4 and 15 characters. Configure it in Document Branding / يجب أن يحتوي الرقم الضريبي للبائع على أرقام فقط وأن يكون بين 4 و15 خانة. أدخله من إعدادات هوية المنشأة.";

        var incomeSourceSequence = settings.IncomeSourceSequence?.Trim() ?? string.Empty;
        if (!IsAsciiDigits(incomeSourceSequence) || incomeSourceSequence.Length is < 4 or > 15)
            return "Income source sequence must contain ASCII digits only and be between 4 and 15 characters. Use the sequence linked to these JoFotara credentials / يجب أن يحتوي تسلسل مصدر الدخل على أرقام فقط وأن يكون بين 4 و15 خانة، ويجب أن يكون مرتبطاً ببيانات الربط الحالية.";

        var taxRegistrationType = settings.TaxRegistrationType?.Trim() ?? "2";
        if (taxRegistrationType is not ("1" or "2" or "3"))
            return "Tax registration type must be 1, 2, or 3 / نوع التسجيل الضريبي يجب أن يكون 1 أو 2 أو 3.";

        if (taxRegistrationType == "3")
            return "Special sales tax invoices require a separate special-tax amount per line, which is not stored by the current POS invoice model. Use tax registration type 2 for general sales tax invoices or add special-tax data before submitting type 3 / فواتير الضريبة الخاصة تحتاج قيمة ضريبة خاصة مستقلة لكل سطر.";

        var lines = invoice.InvoiceLines ?? Array.Empty<InvoiceLine>();
        if (taxRegistrationType == "1" && lines.Any(line => !line.TaxExempt && line.TaxRate > 0m))
            return "Income invoices cannot contain general sales tax lines. Use tax registration type 2 for a sales-tax invoice / لا يمكن لفاتورة الدخل أن تحتوي على أسطر ضريبة مبيعات عامة. استخدم النوع 2 لفاتورة ضريبة المبيعات.";

        var supportedRates = new[] { 0m, 1m, 2m, 3m, 4m, 5m, 7m, 8m, 10m, 16m };
        var unsupportedRate = lines
            .Where(line => !line.TaxExempt)
            .Select(line => (decimal?)line.TaxRate)
            .FirstOrDefault(rate => rate.HasValue && !supportedRates.Contains(rate.Value));
        if (unsupportedRate.HasValue)
            return $"Tax rate {unsupportedRate.Value.ToString("0.###", CultureInfo.InvariantCulture)} is not supported by JoFotara. Use one of 0, 1, 2, 3, 4, 5, 7, 8, 10, or 16 percent / نسبة الضريبة غير مدعومة من جوفوتارا.";

        return null;
    }

    private static XElement BuildUblInvoice(
        Invoice invoice,
        DocumentBranding branding,
        string? incomeSourceSequence,
        string? customerName,
        string? taxRegistrationType)
    {
        XNamespace ubl = UblNamespace;
        XNamespace cbc = CbcNamespace;
        XNamespace cac = CacNamespace;
        var currency = "JOD";
        var taxNumber = branding.TaxNumber?.Trim();
        var sellerName = string.IsNullOrWhiteSpace(branding.DisplayName) ? "ROCCOPOS" : branding.DisplayName.Trim();
        var buyerName = customerName?.Trim();
        var lineAmounts = BuildLineAmounts(invoice);
        var totalDiscount = Math.Round(lineAmounts.Sum(line => line.DiscountAmount), 3, MidpointRounding.AwayFromZero);
        var taxTotal = Math.Round(lineAmounts.Sum(line => line.TaxAmount), 3, MidpointRounding.AwayFromZero);
        var taxExclusiveBeforeDiscount = Math.Round(lineAmounts.Sum(line => line.BaseAmountBeforeDiscount), 3, MidpointRounding.AwayFromZero);
        var taxInclusiveTotal = Math.Round(lineAmounts.Sum(line => line.TaxInclusiveAmount), 3, MidpointRounding.AwayFromZero);
        var taxRegistration = taxRegistrationType?.Trim() ?? "2";

        var root = new XElement(ubl + "Invoice",
            new XAttribute(XNamespace.Xmlns + "cbc", cbc),
            new XAttribute(XNamespace.Xmlns + "cac", cac),
            new XElement(cbc + "UBLVersionID", "2.1"),
            new XElement(cbc + "ProfileID", "reporting:1.0"),
            new XElement(cbc + "ID", invoice.InvoiceNumber),
            new XElement(cbc + "UUID", invoice.JordanEInvoiceUuid),
            new XElement(cbc + "IssueDate", (invoice.DocumentDate ?? invoice.CreatedDate).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            new XElement(cbc + "InvoiceTypeCode",
                new XAttribute("name", BuildInvoiceTypeName(invoice, taxRegistrationType)),
                "388"),
            new XElement(cbc + "DocumentCurrencyCode", currency),
            new XElement(cbc + "TaxCurrencyCode", currency),
            new XElement(cac + "AdditionalDocumentReference",
                new XElement(cbc + "ID", "ICV"),
                new XElement(cbc + "UUID", invoice.Id.ToString(CultureInfo.InvariantCulture))),
            SupplierParty(cac, cbc, sellerName, branding.Address, branding.Phone, taxNumber),
            CustomerParty(cac, cbc, buyerName),
            SellerSupplierParty(cac, cbc, incomeSourceSequence),
            new XElement(cac + "AllowanceCharge",
                new XElement(cbc + "ChargeIndicator", "false"),
                new XElement(cbc + "AllowanceChargeReason", "discount"),
                new XElement(cbc + "Amount", new XAttribute("currencyID", currency), Money(totalDiscount))),
            taxRegistration == "2"
                ? new XElement(cac + "TaxTotal",
                    new XElement(cbc + "TaxAmount", new XAttribute("currencyID", currency), Money(taxTotal)))
                : null,
            new XElement(cac + "LegalMonetaryTotal",
                new XElement(cbc + "LineExtensionAmount", new XAttribute("currencyID", currency), Money(lineAmounts.Sum(line => line.LineExtensionAmount))),
                new XElement(cbc + "TaxExclusiveAmount", new XAttribute("currencyID", currency), Money(taxExclusiveBeforeDiscount)),
                new XElement(cbc + "TaxInclusiveAmount", new XAttribute("currencyID", currency), Money(taxInclusiveTotal)),
                new XElement(cbc + "AllowanceTotalAmount", new XAttribute("currencyID", currency), Money(totalDiscount)),
                new XElement(cbc + "PayableAmount", new XAttribute("currencyID", currency), Money(taxInclusiveTotal))),
            lineAmounts.Select((line, index) => InvoiceLine(cac, cbc, currency, line, index + 1, taxRegistration)));

        return root;
    }

    private static IReadOnlyList<JordanEInvoiceLineAmounts> BuildLineAmounts(Invoice invoice)
    {
        var sourceLines = (invoice.InvoiceLines ?? Array.Empty<InvoiceLine>()).ToList();
        var grossAfterLineDiscounts = sourceLines
            .Select(line => Math.Max(0m, Math.Abs(line.Quantity * line.UnitPrice) - Math.Max(0m, line.LineDiscountAmount)))
            .ToList();
        var invoiceDiscount = Math.Max(0m, invoice.DiscountAmount ?? 0m);
        var distributableGross = grossAfterLineDiscounts.Sum();
        var allocatedInvoiceDiscount = 0m;
        var result = new List<JordanEInvoiceLineAmounts>(sourceLines.Count);

        for (var index = 0; index < sourceLines.Count; index++)
        {
            var line = sourceLines[index];
            var quantity = Math.Abs(line.Quantity);
            var rate = line.TaxExempt ? 0m : line.TaxRate;
            var divisor = line.TaxExempt || rate <= 0m ? 1m : 1m + rate / 100m;
            var grossBeforeDiscount = Math.Round(Math.Abs(line.Quantity * line.UnitPrice), 3, MidpointRounding.AwayFromZero);
            var lineDiscount = Math.Min(grossBeforeDiscount, Math.Max(0m, line.LineDiscountAmount));
            var invoiceDiscountShare = index == sourceLines.Count - 1
                ? Math.Max(0m, invoiceDiscount - allocatedInvoiceDiscount)
                : distributableGross <= 0m
                    ? 0m
                    : Math.Round(invoiceDiscount * grossAfterLineDiscounts[index] / distributableGross, 3, MidpointRounding.AwayFromZero);
            allocatedInvoiceDiscount += invoiceDiscountShare;
            var totalDiscount = Math.Min(grossBeforeDiscount, lineDiscount + invoiceDiscountShare);
            var baseBeforeDiscount = Math.Round(grossBeforeDiscount / divisor, 3, MidpointRounding.AwayFromZero);
            var discountAmount = Math.Round(totalDiscount / divisor, 3, MidpointRounding.AwayFromZero);
            var lineExtensionAmount = Math.Max(0m, Math.Round(baseBeforeDiscount - discountAmount, 3, MidpointRounding.AwayFromZero));
            var taxAmount = line.TaxExempt || rate <= 0m
                ? 0m
                : Math.Round(lineExtensionAmount * rate / 100m, 3, MidpointRounding.AwayFromZero);
            var taxInclusiveAmount = Math.Round(lineExtensionAmount + taxAmount, 3, MidpointRounding.AwayFromZero);
            var unitPriceBeforeTax = quantity <= 0m
                ? 0m
                : Math.Round(baseBeforeDiscount / quantity, 3, MidpointRounding.AwayFromZero);

            result.Add(new JordanEInvoiceLineAmounts(
                line,
                quantity,
                rate,
                unitPriceBeforeTax,
                baseBeforeDiscount,
                discountAmount,
                lineExtensionAmount,
                taxAmount,
                taxInclusiveAmount,
                line.TaxExempt ? "Z" : rate == 0m ? "O" : "S"));
        }

        return result;
    }

    private static string BuildInvoiceTypeName(Invoice invoice, string? taxRegistrationType)
    {
        var paymentCode = invoice.PaymentType is PaymentType.Credit or PaymentType.InsuranceReceivable
            ? "2"
            : "1";
        var registrationCode = string.IsNullOrWhiteSpace(taxRegistrationType)
            ? "2"
            : taxRegistrationType.Trim();
        return $"0{paymentCode}{registrationCode}";
    }

    private static XElement SupplierParty(
        XNamespace cac,
        XNamespace cbc,
        string name,
        string address,
        string phone,
        string? taxNumber)
    {
        return new XElement(cac + "AccountingSupplierParty",
            new XElement(cac + "Party",
                new XElement(cac + "PartyName", new XElement(cbc + "Name", name)),
                new XElement(cac + "PostalAddress",
                    new XElement(cbc + "StreetName", address ?? string.Empty),
                    new XElement(cac + "Country", new XElement(cbc + "IdentificationCode", "JO"))),
                new XElement(cac + "PartyTaxScheme",
                    new XElement(cbc + "CompanyID", taxNumber ?? string.Empty),
                    TaxScheme(cac, cbc)),
                new XElement(cac + "PartyLegalEntity", new XElement(cbc + "RegistrationName", name)),
                new XElement(cac + "Contact", new XElement(cbc + "Telephone", phone ?? string.Empty))));
    }

    private static XElement SellerSupplierParty(XNamespace cac, XNamespace cbc, string? incomeSourceSequence) =>
        new XElement(cac + "SellerSupplierParty",
            new XElement(cac + "Party",
                new XElement(cac + "PartyIdentification",
                    new XElement(cbc + "ID", incomeSourceSequence?.Trim() ?? string.Empty))));

    private static XElement CustomerParty(XNamespace cac, XNamespace cbc, string? name)
    {
        return new XElement(cac + "AccountingCustomerParty",
            new XElement(cac + "Party",
                new XElement(cac + "PartyName", new XElement(cbc + "Name", string.IsNullOrWhiteSpace(name) ? "General Customer" : name)),
                new XElement(cac + "PartyLegalEntity", new XElement(cbc + "RegistrationName", string.IsNullOrWhiteSpace(name) ? "General Customer" : name))));
    }

    private static XElement InvoiceLine(
        XNamespace cac,
        XNamespace cbc,
        string currency,
        JordanEInvoiceLineAmounts amounts,
        int sequence,
        string taxRegistrationType)
    {
        var line = amounts.Source;
        var itemName = string.IsNullOrWhiteSpace(line.Product?.Name) ? $"Item {line.ProductId}" : line.Product!.Name!;

        return new XElement(cac + "InvoiceLine",
            new XElement(cbc + "ID", sequence.ToString(CultureInfo.InvariantCulture)),
            new XElement(cbc + "InvoicedQuantity", new XAttribute("unitCode", "EA"), amounts.Quantity.ToString("0.###", CultureInfo.InvariantCulture)),
            new XElement(cbc + "LineExtensionAmount", new XAttribute("currencyID", currency), Money(amounts.LineExtensionAmount)),
            amounts.DiscountAmount > 0m
                ? new XElement(cac + "AllowanceCharge",
                    new XElement(cbc + "ChargeIndicator", "false"),
                    new XElement(cbc + "AllowanceChargeReason", "discount"),
                    new XElement(cbc + "Amount", new XAttribute("currencyID", currency), Money(amounts.DiscountAmount)))
                : null,
            taxRegistrationType == "2"
                ? new XElement(cac + "TaxTotal",
                    new XElement(cbc + "TaxAmount", new XAttribute("currencyID", currency), Money(amounts.TaxAmount)),
                    new XElement(cbc + "RoundingAmount", new XAttribute("currencyID", currency), Money(amounts.TaxInclusiveAmount)),
                    new XElement(cac + "TaxSubtotal",
                        new XElement(cbc + "TaxAmount", new XAttribute("currencyID", currency), Money(amounts.TaxAmount)),
                        new XElement(cac + "TaxCategory",
                            new XElement(cbc + "ID", amounts.TaxCategory),
                            new XElement(cbc + "Percent", amounts.TaxRate.ToString("0.###", CultureInfo.InvariantCulture)),
                            TaxScheme(cac, cbc))))
                : null,
            new XElement(cac + "Item",
                new XElement(cbc + "Name", itemName),
                taxRegistrationType == "2"
                    ? new XElement(cac + "ClassifiedTaxCategory",
                        new XElement(cbc + "ID", amounts.TaxCategory),
                        new XElement(cbc + "Percent", amounts.TaxRate.ToString("0.###", CultureInfo.InvariantCulture)),
                        TaxScheme(cac, cbc))
                    : null),
            new XElement(cac + "Price",
                new XElement(cbc + "PriceAmount", new XAttribute("currencyID", currency), Money(amounts.UnitPriceBeforeTax))));
    }

    private static bool IsAsciiDigits(string value) =>
        value.Length > 0 && value.All(character => character is >= '0' and <= '9');

    private sealed record JordanEInvoiceLineAmounts(
        InvoiceLine Source,
        decimal Quantity,
        decimal TaxRate,
        decimal UnitPriceBeforeTax,
        decimal BaseAmountBeforeDiscount,
        decimal DiscountAmount,
        decimal LineExtensionAmount,
        decimal TaxAmount,
        decimal TaxInclusiveAmount,
        string TaxCategory);

    private static XElement TaxScheme(XNamespace cac, XNamespace cbc) =>
        new XElement(cac + "TaxScheme", new XElement(cbc + "ID", "VAT"));

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

    private static ParsedJordanEInvoiceResponse ParseResponse(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return new ParsedJordanEInvoiceResponse(null, null, null, null, null, Array.Empty<string>());

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var status = GetDirectString(root, "EINV_STATUS") ?? FindValue(root, "EINV_STATUS");
            var invoiceNumber = GetDirectString(root, "EINV_NUM") ?? FindValue(root, "EINV_NUM");
            var invoiceUuid = GetDirectString(root, "EINV_INV_UUID", "EINV_UUID")
                ?? FindValue(root, "EINV_INV_UUID", "EINV_UUID");
            var qrPayload = GetDirectString(root, "EINV_QR") ?? FindValue(root, "EINV_QR");
            var message = GetDirectString(root, "EINV_MESSAGE") ?? FindValue(root, "EINV_MESSAGE");
            var errors = ExtractErrorMessages(root);

            return new ParsedJordanEInvoiceResponse(
                status,
                invoiceNumber,
                invoiceUuid,
                qrPayload,
                message,
                errors);
        }
        catch (JsonException)
        {
            return new ParsedJordanEInvoiceResponse(null, null, null, null, null, Array.Empty<string>());
        }
    }

    private static string BuildRejectedMessage(ParsedJordanEInvoiceResponse parsed, System.Net.HttpStatusCode statusCode)
    {
        var details = parsed.Errors.Count > 0
            ? string.Join(" | ", parsed.Errors)
            : parsed.Message;
        var suffix = string.IsNullOrWhiteSpace(details) ? string.Empty : $" Details: {details}";
        return $"JoFotara rejected or did not accept the invoice. Status: {parsed.Status ?? statusCode.ToString()}.{suffix} / لم تقبل جوافوترة الفاتورة.";
    }

    private static List<string> ExtractErrorMessages(JsonElement root)
    {
        var errors = new List<string>();
        var errorsNode = FindProperty(root, "ERRORS");
        if (errorsNode is not JsonElement node)
            return errors;

        CollectNamedValues(node, errors, "EINV_MESSAGE", "message");
        return errors
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void CollectNamedValues(JsonElement element, ICollection<string> values, params string[] names)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (names.Any(name => string.Equals(name, property.Name, StringComparison.OrdinalIgnoreCase)) &&
                    property.Value.ValueKind == JsonValueKind.String)
                    values.Add(property.Value.GetString() ?? string.Empty);

                CollectNamedValues(property.Value, values, names);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
                CollectNamedValues(child, values, names);
        }
    }

    private static JsonElement? FindProperty(JsonElement element, string name)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    return property.Value;

                var nested = FindProperty(property.Value, name);
                if (nested.HasValue)
                    return nested;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
            {
                var nested = FindProperty(child, name);
                if (nested.HasValue)
                    return nested;
            }
        }

        return null;
    }

    private static string? GetDirectString(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var property in element.EnumerateObject())
        {
            if (names.Any(name => string.Equals(name, property.Name, StringComparison.OrdinalIgnoreCase)) &&
                property.Value.ValueKind == JsonValueKind.String)
                return property.Value.GetString();
        }

        return null;
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
