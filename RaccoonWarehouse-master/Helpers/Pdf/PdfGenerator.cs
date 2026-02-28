using QuestPDF.Fluent;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.StockDocuments.DTOs;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
using RaccoonWarehouse.Helpers.Pdf;
using RaccoonWarehouse.Helpers.Pdf.Reports;

public static class PdfGenerator
{
    public static string StockOut(StockDocumentReadDto doc, string savePath)
    {
        var pdf = new StockOutReportDocument(doc);
        pdf.GeneratePdf(savePath);
        return savePath;
    }

    public static string StockIn(StockDocumentReadDto doc, string savePath)
    {
        var pdf = new StockInReportDocument(doc);
        pdf.GeneratePdf(savePath);
        return savePath;
    }
    public static string GenerateVoucherPdf(VoucherWriteDto dto, string savePath)
    {
        var doc = new VoucherReportDocument(dto);
        doc.GeneratePdf(savePath);
        return savePath;
    }

    public static string GeneratePaymentVoucherPdf(VoucherWriteDto dto, string savePath)
    {
        var doc = new PaymentVoucherReportDocument(dto);
        doc.GeneratePdf(savePath);
        return savePath;
    }
    public static void SalesInvoice(InvoiceReadDto invoice, string filePath)
    {
        var doc = new SalesInvoiceReport(invoice);
        doc.GeneratePdf(filePath);
    }
    public static void PurchaseInvoice(InvoiceReadDto invoice, string path)
    {
        var doc = new PayInvoiceReport(invoice);
        doc.GeneratePdf(path);
    }



}
