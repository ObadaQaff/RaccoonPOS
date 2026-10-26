using QuestPDF.Infrastructure;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
namespace RaccoonWarehouse.Helpers.Pdf.Reports;
public sealed class PaymentVoucherReportDocument : IReportDocument
{
    private readonly PurchasingDocument _document;
    public PaymentVoucherReportDocument(VoucherWriteDto voucher):this(VoucherPrintRuntime.Load(voucher.Id,VoucherType.Payment)){}
    public PaymentVoucherReportDocument(VoucherPrintData data)=>_document=VoucherPrintDocument.Create(data);
    public string FileName=>_document.FileName;
    public DocumentMetadata GetMetadata()=>_document.GetMetadata();
    public void Compose(IDocumentContainer container)=>_document.Compose(container);
}
