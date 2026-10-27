using QRCoder;
using RaccoonWarehouse.Domain.Invoices.DTOs;

namespace RaccoonWarehouse.Helpers.Pdf;

public sealed record ReceiptQrData(string? Payload,byte[]? Image,bool IsOfficial);
public interface IReceiptQrProvider { ReceiptQrData Get(InvoiceReadDto invoice); }
public sealed class TestReceiptQrProvider : IReceiptQrProvider
{
    public ReceiptQrData Get(InvoiceReadDto invoice)
    {
        if (!string.IsNullOrWhiteSpace(invoice.JordanEInvoiceQr) &&
            invoice.JordanEInvoiceStatus == RaccoonWarehouse.Domain.Invoices.JordanEInvoiceStatus.Submitted)
            return new ReceiptQrData(invoice.JordanEInvoiceQr, null, true);

        return new ReceiptQrData($"TEST|INVOICE={invoice.InvoiceNumber}|DATE={invoice.CreatedDate:yyyy-MM-dd}|TOTAL={invoice.TotalAmount.ToString("0.000",System.Globalization.CultureInfo.InvariantCulture)}", null, false);
    }
}
public static class ReceiptQrService
{
    public static byte[] Image(ReceiptQrData qr)
    {
        if(qr.Image is {Length:>0})return qr.Image;
        if(string.IsNullOrWhiteSpace(qr.Payload))throw new ArgumentException("QR payload is empty.");
        using var data=QRCodeGenerator.GenerateQrCode(qr.Payload,QRCodeGenerator.ECCLevel.M);
        using var renderer=new PngByteQRCode(data);
        return renderer.GetGraphic(6); // Includes the four-module white quiet zone.
    }
}
