using RaccoonWarehouse.Domain.Invoices.DTOs;

namespace RaccoonWarehouse.Helpers.Pdf;

public sealed class SalesInvoiceReport : IReportDocument
{
    private readonly PurchasingDocument _document;

    public SalesInvoiceReport(InvoiceReadDto invoice)
        => _document = PurchasingDocument.SalesInvoice(invoice, DocumentPrintRuntime.Branding(invoiceId: invoice.Id));

    public string FileName => _document.FileName;
    public QuestPDF.Infrastructure.DocumentMetadata GetMetadata() => _document.GetMetadata();
    public void Compose(QuestPDF.Infrastructure.IDocumentContainer container) => _document.Compose(container);
}
