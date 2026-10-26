using QuestPDF.Infrastructure;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
namespace RaccoonWarehouse.Helpers.Pdf.Reports;
public sealed class VoucherReportDocument : IReportDocument
{
    private readonly PurchasingDocument _document;
    public VoucherReportDocument(VoucherWriteDto voucher):this(VoucherPrintRuntime.Load(voucher.Id,VoucherType.Receipt)){}
    public VoucherReportDocument(VoucherPrintData data)=>_document=VoucherPrintDocument.Create(data);
    public string FileName=>_document.FileName;
    public DocumentMetadata GetMetadata()=>_document.GetMetadata();
    public void Compose(IDocumentContainer container)=>_document.Compose(container);
}
