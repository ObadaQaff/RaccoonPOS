using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Globalization;
namespace RaccoonWarehouse.Helpers.Pdf;
public static class VoucherPrintDocument
{
    private static string T(string ar,string en)=>UiText.T(ar,en);
    private static string Ltr(string? value)=>"\u2066"+(value??"—")+"\u2069";
    private static string N(decimal value)=>value.ToString("0.000",CultureInfo.InvariantCulture);
    private static string Payment(PaymentType value)=>value switch{PaymentType.Cash=>T("نقدي","Cash"),PaymentType.Credit=>T("آجل","Credit"),PaymentType.Check=>T("شيك","Check"),PaymentType.Debit=>T("شبكة","Debit"),PaymentType.MobilePayment=>T("دفع إلكتروني","Mobile payment"),_=>value.ToString()};
    private static string Posting(RaccoonWarehouse.Domain.Accounting.Enums.AccountingPostingStatus value)=>value switch {
        RaccoonWarehouse.Domain.Accounting.Enums.AccountingPostingStatus.NotPosted=>T("غير مرحل","Not posted"),
        RaccoonWarehouse.Domain.Accounting.Enums.AccountingPostingStatus.Pending=>T("بانتظار الترحيل","Pending posting"),
        RaccoonWarehouse.Domain.Accounting.Enums.AccountingPostingStatus.Posted=>T("مرحل","Posted"),
        RaccoonWarehouse.Domain.Accounting.Enums.AccountingPostingStatus.Reversed=>T("معكوس محاسبياً","Reversed"),_=>"—"};
    public static PurchasingDocument Create(VoucherPrintData data)
    {
        var v=data.Voucher;var receipt=v.VoucherType==VoucherType.Receipt;
        var hasChecks=v.Checks?.Count>0;
        return new PurchasingDocument{
            FileName=receipt?"ReceiptVoucher":"PaymentVoucher",Title=receipt?T("سند قبض","Receipt Voucher"):T("سند صرف / دفع","Payment Voucher"),Number=v.VoucherNumber??"—",Branding=data.Branding,
            Information=new(){T("التاريخ: ","Date: ")+Ltr(data.DocumentDate.ToString("yyyy/MM/dd")),
                (receipt?T("استلمنا من: ","Received from: "):T("يصرف إلى: ","Paid to: "))+data.PartyName,
                T("هاتف الجهة: ","Party phone: ")+Ltr(data.PartyPhone),T("المستودع: ","Warehouse: ")+(data.WarehouseName??"—"),
                T("طريقة الدفع: ","Payment method: ")+Payment(v.PaymentType),T("رقم المرجع: ","Reference: ")+Ltr(data.ReferenceNumber),T("حالة الترحيل: ","Posting status: ")+Posting(data.PostingStatus),
                T("المبلغ: ","Amount: ")+N(v.Amount)+(string.IsNullOrWhiteSpace(data.CurrencyCode)?"":" "+data.CurrencyCode),
                hasChecks?T("تفاصيل الشيكات","Check details"):T("لا توجد شيكات مرتبطة بالسند.","No checks are attached to this voucher.")},
            Headers=hasChecks?new[]{T("رقم الشيك","Check number"),T("البنك","Bank"),T("المبلغ","Amount"),T("الاستحقاق","Due date"),T("ملاحظات","Notes")}:Array.Empty<string>(),
            Widths=hasChecks?new float[]{2,2.5f,1.5f,1.8f,4}:Array.Empty<float>(),
            Rows=v.Checks?.Select(c=>new[]{Ltr(c.CheckNumber),c.BankName??"—",N(c.Amount),Ltr(c.DueDate.ToString("yyyy/MM/dd")),c.Notes??""}).ToList()??new(),
            Totals=new(){T("قيمة السند: ","Voucher amount: ")+N(v.Amount)+(string.IsNullOrWhiteSpace(data.CurrencyCode)?"":" "+data.CurrencyCode)},
            ClosingText=T("توقيع المستلم: __________________    توقيع الموظف: __________________","Recipient signature: __________________    Employee signature: __________________"),Notes=v.Notes};
    }
}
