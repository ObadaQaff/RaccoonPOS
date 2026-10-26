using QuestPDF.Infrastructure;
using RaccoonWarehouse.Domain.Invoices.DTOs;
namespace RaccoonWarehouse.Helpers.Pdf;
public sealed class PayInvoiceReport : IReportDocument
{
    private readonly PurchasingDocument _document;
    public PayInvoiceReport(InvoiceReadDto invoice)=>_document=PurchasingDocument.Invoice(invoice,DocumentPrintRuntime.Branding(invoiceId:invoice.Id));
    public string FileName=>_document.FileName;
    public DocumentMetadata GetMetadata()=>_document.GetMetadata();
    public void Compose(IDocumentContainer container)=>_document.Compose(container);
}
