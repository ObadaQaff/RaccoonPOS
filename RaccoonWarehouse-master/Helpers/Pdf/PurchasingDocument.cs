using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RaccoonWarehouse.Domain.Settings;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.PurchaseOrders.DTOs;
using RaccoonWarehouse.Domain.PurchaseRequests.DTOs;
using RaccoonWarehouse.Helpers.Localization;
using System.Globalization;

namespace RaccoonWarehouse.Helpers.Pdf;

// Presentation-only: all financial values come from the existing document DTO.
public sealed class PurchasingDocument : IReportDocument
{
    public string FileName {get;init;}="Purchase";
    public string Title {get;init;}="";
    public string Number {get;init;}="";
    public DocumentBranding Branding {get;init;}=new();
    public List<string> Information {get;init;}=new();
    public string[] Headers {get;init;}=Array.Empty<string>();
    public float[] Widths {get;init;}=Array.Empty<float>();
    public List<string[]> Rows {get;init;}=new();
    public List<string> Totals {get;init;}=new();
    public string? Notes {get;init;}
    public string? ClosingText {get;init;}
    private static string T(string ar,string en)=>UiText.T(ar,en);
    private static string Status(string? value)=>value switch {
        "Draft"=>T("مسودة","Draft"),"Submitted"=>T("مقدم","Submitted"),"Approved"=>T("معتمد","Approved"),"Rejected"=>T("مرفوض","Rejected"),"Cancelled"=>T("ملغي","Cancelled"),"Converted"=>T("محول","Converted"),"Posted"=>T("مرحل","Posted"),"Returned"=>T("مرتجع","Returned"),"OnHold"=>T("معلق","On hold"),"Unknown"=>T("وارد","Received"),"InProcess"=>T("قيد التجهيز","In process"),"Completed"=>T("مكتمل","Completed"),"Open"=>T("مفتوح","Open"),"Held"=>T("معلق","Held"),"Paid"=>T("مدفوع","Paid"),"Unpaid"=>T("غير مدفوع","Unpaid"),"Pending"=>T("قيد المراجعة","Pending"),"Cash"=>T("نقدي","Cash"),"Credit"=>T("آجل","Credit"),"Debit"=>T("شبكة","Debit"),"Check"=>T("شيك","Check"),"MobilePayment"=>T("دفع إلكتروني","Mobile payment"),_=>value??"—"};
    private static string N(decimal value)=>value.ToString("0.000",CultureInfo.InvariantCulture);
    public DocumentMetadata GetMetadata()=>new(){Title=Title+" "+Number};
    public void Compose(IDocumentContainer container)
    {
        container.Page(page=>{
            page.Size(PageSizes.A4);page.Margin(24);
            page.DefaultTextStyle(x=>x.FontFamily("Arial").FontSize(8.5f));
            if(!UiText.IsEnglish)page.ContentFromRightToLeft();
            page.Header().PaddingBottom(8).Column(c=>{
                if(!string.IsNullOrWhiteSpace(Branding.LogoBase64))
                    try{c.Item().AlignCenter().Height(42).Image(Convert.FromBase64String(Branding.LogoBase64)).FitArea();}catch(FormatException){}
                c.Item().AlignCenter().Text(Branding.DisplayName).FontSize(15).Bold();
                foreach(var v in new[]{Branding.Address,Branding.Phone,Branding.AdditionalInformation})
                    if(!string.IsNullOrWhiteSpace(v))c.Item().AlignCenter().Text(v).FontSize(8);
                if(!string.IsNullOrWhiteSpace(Branding.TaxNumber))c.Item().AlignCenter().Text(T("الرقم الضريبي: ","Tax number: ")+Branding.TaxNumber);
                c.Item().AlignCenter().Text(Title+" — "+Number).FontSize(12).Bold();
            });
            page.Content().Column(c=>{
                c.Spacing(5);
                foreach(var value in Information.Where(x=>!string.IsNullOrWhiteSpace(x)))c.Item().Text(value);
                if(Headers.Length>0)c.Item().Table(table=>{
                    table.ColumnsDefinition(cols=>{foreach(var width in Widths)cols.RelativeColumn(width);});
                    table.Header(h=>{foreach(var name in Headers)h.Cell().Background("#E8EEF1").BorderBottom(1).Padding(3).Text(name).Bold();});
                    foreach(var row in Rows)
                        for(int i=0;i<Headers.Length;i++)
                        {
                            var cell=table.Cell().PreventPageBreak().BorderBottom(0.3f).BorderColor("#CCCCCC").Padding(3);
                            if(i==0)cell.Text(row[i]);else cell.AlignCenter().Text(row[i]);
                        }
                });
                c.Item().EnsureSpace(65).Column(total=>{foreach(var value in Totals)total.Item().Text(value).Bold();});
                if(!string.IsNullOrWhiteSpace(Notes))c.Item().Text(T("ملاحظات: ","Notes: ")+Notes);
                if(!string.IsNullOrWhiteSpace(ClosingText))c.Item().PaddingTop(12).Text(ClosingText);
            });
            page.Footer().PaddingTop(5).Column(c=>{
                if(!string.IsNullOrWhiteSpace(Branding.Footer))c.Item().AlignCenter().Text(Branding.Footer).FontSize(8);
                c.Item().AlignCenter().Text(text=>{text.Span(T("صفحة ","Page "));text.CurrentPageNumber();text.Span(" / ");text.TotalPages();});
            });
        });
    }
    public static PurchasingDocument Invoice(InvoiceReadDto i,DocumentBranding branding)=>new(){
        FileName="PurchaseInvoice",Title=T("فاتورة مشتريات","Purchase Invoice"),Number=i.InvoiceNumber,Branding=branding,
        Information=new(){T("التاريخ: ","Date: ")+i.CreatedDate.ToString("yyyy/MM/dd HH:mm"),T("المورد: ","Supplier: ")+(i.Supplier?.Name??i.SupplierId?.ToString()??"—"),T("هاتف المورد: ","Supplier phone: ")+i.Supplier?.PhoneNumber,T("الدفع: ","Payment: ")+Status(i.PaymentType?.ToString()),T("الحالة: ","Status: ")+Status(i.Status?.ToString()),i.CostPriceIncludesTax?T("السعر يشمل الضريبة","Price includes tax"):T("السعر لا يشمل الضريبة","Price excludes tax")},
        Headers=new[]{T("الصنف / الباركود","Item / Barcode"),T("الوحدة","Unit"),T("مدفوع","Paid qty"),T("مجاني / عرض","Free / Offer"),T("السعر","Price"),T("الخصم","Discount"),T("الضريبة","Tax"),i.CostPriceIncludesTax?T("الإجمالي شامل*","Total incl.*"):T("الإجمالي قبل*","Total excl.*"),T("الانتهاء","Expiry")},Widths=new float[]{4,1,1,1.5f,1.3f,1.3f,1.3f,1.5f,1.6f},
        Rows=(i.InvoiceLines??new List<RaccoonWarehouse.Domain.InvoiceLines.DTOs.InvoiceLineReadDto>()).Select(l=>new[]{(string.IsNullOrWhiteSpace(l.ProductName)?l.Product?.Name:l.ProductName)+"\n"+(l.ProductUnit?.AlternateBarcode??l.Product?.ITEMCODE?.ToString()??""),l.ProductUnit?.Unit?.Name??"—",N(l.Quantity),N(l.FreeQuantity)+(string.IsNullOrWhiteSpace(l.BonusRatio)?"":"\n"+l.BonusRatio),N(l.UnitPrice),N(l.LineDiscountAmount),N(l.TaxAmount),N(l.LineTotal),l.ExpiryDate==default?"—":l.ExpiryDate.ToString("yyyy/MM/dd")}).ToList(),
        Totals=new(){T("قبل الضريبة: ","Subtotal: ")+N(i.SubTotal),T("خصم الفاتورة: ","Invoice discount: ")+N(i.DiscountAmount??0),T("الضريبة: ","Tax: ")+N(i.TotalTax),T("الإجمالي المستحق (د.أ): ","Amount due (JOD): ")+N(i.TotalAmount),T("* إجمالي السطر كما يظهر في الفاتورة؛ المجاني ليس كمية مدفوعة.","* Line total as shown on the source invoice; free units are not paid units.")},Notes=i.Notes};
    public static PurchasingDocument SalesInvoice(InvoiceReadDto i,DocumentBranding branding)=>new(){
        FileName="SalesInvoice",Title=T("فاتورة مبيعات","Sales Invoice"),Number=i.InvoiceNumber,Branding=branding,
        Information=new(){
            T("التاريخ: ","Date: ")+i.CreatedDate.ToString("yyyy/MM/dd HH:mm"),
            T("العميل: ","Customer: ")+(i.Customer?.Name??i.User?.Name??"—"),
            T("هاتف العميل: ","Customer phone: ")+(i.Customer?.PhoneNumber??i.User?.PhoneNumber??"—"),
            T("الدفع: ","Payment: ")+Status(i.PaymentType?.ToString()),
            T("الحالة: ","Status: ")+Status(i.Status?.ToString())},
        Headers=new[]{T("الصنف / الباركود","Item / Barcode"),T("الوحدة","Unit"),T("الكمية","Qty"),T("السعر","Price"),T("الخصم","Discount"),T("الضريبة","Tax"),T("الإجمالي","Total"),T("الانتهاء","Expiry")},
        Widths=new float[]{4,1.2f,1,1.4f,1.3f,1.3f,1.5f,1.6f},
        Rows=(i.InvoiceLines??new List<RaccoonWarehouse.Domain.InvoiceLines.DTOs.InvoiceLineReadDto>()).Select(l=>new[]{
            (string.IsNullOrWhiteSpace(l.ProductName)?l.Product?.Name:l.ProductName)+"\n"+(l.ProductUnit?.AlternateBarcode??l.Product?.ITEMCODE?.ToString()??""),
            l.ProductUnit?.Unit?.Name??"—",N(l.Quantity),N(l.UnitPrice),N(l.LineDiscountAmount),N(l.TaxAmount),N(l.LineTotal),
            l.ExpiryDate==default?"—":l.ExpiryDate.ToString("yyyy/MM/dd")}).ToList(),
        Totals=new(){
            T("قبل الضريبة: ","Subtotal: ")+N(i.SubTotal),
            T("خصم الفاتورة: ","Invoice discount: ")+N(i.DiscountAmount??0),
            T("الضريبة: ","Tax: ")+N(i.TotalTax),
            T("الإجمالي المستحق (د.أ): ","Amount due (JOD): ")+N(i.TotalAmount)},
        Notes=i.Notes};

    public static PurchasingDocument Order(PurchaseOrderWriteDto o,DocumentBranding branding)=>new(){
        FileName="PurchaseOrder",Title=T("أمر شراء — BO / PO","Purchase Order — BO / PO"),Number=o.OrderNumber,Branding=branding,
        Information=new(){T("التاريخ: ","Date: ")+o.DocumentDate?.ToString("yyyy/MM/dd"),T("المورد: ","Supplier: ")+(o.SupplierName??o.SupplierId?.ToString()??"—"),T("الحالة: ","Status: ")+Status(o.Status.ToString()),T("حالة الدفع: ","Payment status: ")+Status(o.PaymentStatus.ToString()),T("طريقة الدفع: ","Payment: ")+Status(o.PaymentType?.ToString()),T("تاريخ الدفع: ","Paid date: ")+o.PaidDate?.ToString("yyyy/MM/dd"),T("تاريخ الاستحقاق: ","Due date: ")+o.DueDate?.ToString("yyyy/MM/dd"),T("رقم الفاتورة المرتبطة: ","Linked invoice ID: ")+o.ConvertedInvoiceId,o.CostPriceIncludesTax?T("التكلفة تشمل الضريبة","Costs include tax"):T("التكلفة لا تشمل الضريبة","Costs exclude tax")},
        Headers=new[]{T("الصنف","Item"),T("الوحدة","Unit"),T("الكمية","Qty"),T("مجاني","Free"),T("التكلفة","Cost"),T("الخصم","Discount"),T("الضريبة","Tax"),T("الإجمالي","Total"),T("الانتهاء","Expiry")},Widths=new float[]{4,1,1,1,1.4f,1.4f,1.4f,1.5f,1.6f},
        Rows=o.Lines.Select(l=>new[]{l.ProductName??l.ProductId.ToString(),l.UnitName??l.ProductUnitId.ToString(),N(l.Quantity),N(l.FreeQuantity),N(l.UnitCost),N(l.LineDiscountAmount),N(l.TaxAmount),N(l.LineTotal),l.ExpiryDate?.ToString("yyyy/MM/dd")??"—"}).ToList(),
        Totals=new(){T("قبل الضريبة: ","Subtotal: ")+N(o.SubTotal),T("الخصم: ","Discount: ")+N(o.DiscountAmount??0),T("الضريبة: ","Tax: ")+N(o.TotalTax),T("الإجمالي (د.أ): ","Total (JOD): ")+N(o.TotalAmount)},Notes=string.Join("\n",new[]{o.Notes,o.PaymentNote,o.RejectionReason}.Where(x=>!string.IsNullOrWhiteSpace(x)))};
    public static PurchasingDocument Request(PurchaseRequestReadDto r,DocumentBranding branding)=>new(){
        FileName="PurchaseRequest",Title=T("طلب شراء — BR / PR","Purchase Request — BR / PR"),Number=r.RequestNumber,Branding=branding,
        Information=new(){T("التاريخ: ","Date: ")+r.RequestDate.ToString("yyyy/MM/dd"),T("مقدم الطلب: ","Requested by: ")+r.RequesterName,T("القسم: ","Department: ")+r.Department,T("الفرع: ","Branch: ")+r.BranchId,T("الحالة: ","Status: ")+Status(r.Status.ToString()),T("الأولوية: ","Priority: ")+r.Priority,T("التقديم: ","Submitted by: ")+r.SubmittedByName+" "+r.SubmittedDate?.ToString("yyyy/MM/dd"),T("الاعتماد: ","Approved by: ")+r.ApprovedByName+" "+r.ApprovedDate?.ToString("yyyy/MM/dd"),T("الرفض: ","Rejected by: ")+r.RejectedByName+" "+r.RejectedDate?.ToString("yyyy/MM/dd")},
        Headers=new[]{T("الصنف / الباركود","Item / Barcode"),T("الوحدة","Unit"),T("المطلوب","Requested"),T("المعتمد","Approved"),T("السعر","Price"),T("السعر المعتمد","Approved price"),T("المورد","Supplier"),T("القرار / ملاحظات","Decision / Notes")},Widths=new float[]{4,1,1.3f,1.3f,1.4f,1.4f,2,2.5f},
        Rows=r.Lines.Select(l=>new[]{l.ItemName+"\n"+l.Barcode,l.UnitName??"—",N(l.RequestedQuantity),N(l.ApprovedQuantity),N(l.UnitPrice),N(l.ApprovedUnitPrice),l.SupplierName??"—",Status(l.Decision.ToString())+"\n"+l.Description+"\n"+l.Notes+"\n"+l.ApprovalNotes}).ToList(),
        Totals=new(){T("إجمالي الطلب (د.أ): ","Requested total (JOD): ")+N(r.TotalAmount),T("الإجمالي المعتمد (د.أ): ","Approved total (JOD): ")+N(r.ApprovedTotalAmount)},Notes=string.Join("\n",new[]{r.Notes,r.Justification,r.ReviewNotes,r.RejectionReason}.Where(x=>!string.IsNullOrWhiteSpace(x)))};
}
