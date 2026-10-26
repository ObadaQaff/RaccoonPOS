using RaccoonWarehouse.Domain.Settings;
namespace RaccoonWarehouse.Domain.Vouchers.DTOs;
public sealed class VoucherPrintData
{
    public VoucherWriteDto Voucher {get;init;}=new();
    public DocumentBranding Branding {get;init;}=new();
    public string PartyName {get;init;}="—";
    public string? PartyPhone {get;init;}
    public string? WarehouseName {get;init;}
    public string? CurrencyCode {get;init;}
    public string? ReferenceNumber {get;init;}
    public RaccoonWarehouse.Domain.Accounting.Enums.AccountingPostingStatus PostingStatus {get;init;}
    public DateTime DocumentDate {get;init;}
}
