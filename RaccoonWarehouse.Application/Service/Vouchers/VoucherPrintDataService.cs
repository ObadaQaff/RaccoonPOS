using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Vouchers;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
using RaccoonWarehouse.Domain.Checks.DTOs;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Warehouses;
using RaccoonWarehouse.Domain.Currencies;
using RaccoonWarehouse.Domain.Enums;
namespace RaccoonWarehouse.Application.Service.Vouchers;

// Read-only projection for printing. No financial calculation or posting.
public sealed class VoucherPrintDataService
{
    private readonly ApplicationDbContext _db;
    private readonly DocumentBrandingService _branding;
    private readonly IPermissionService _permissions;
    public VoucherPrintDataService(ApplicationDbContext db,DocumentBrandingService branding,IPermissionService permissions)
        =>(_db,_branding,_permissions)=(db,branding,permissions);
    public async Task<VoucherPrintData> LoadAsync(int id,VoucherType expectedType)
    {
        if(!await _permissions.HasCurrentUserPermissionAsync("Vouchers.Print"))throw new UnauthorizedAccessException("ليس لديك صلاحية الطباعة. / Print permission required.");
        if(id<=0)throw new ArgumentException("احفظ السند قبل الطباعة. / Save the voucher before printing.");
        var v=await _db.Set<Voucher>().AsNoTracking().Include(x=>x.Checks).Include(x=>x.CashierSession).SingleOrDefaultAsync(x=>x.Id==id)
            ??throw new InvalidOperationException("السند غير موجود. / Voucher not found.");
        if(v.VoucherType!=expectedType)throw new InvalidOperationException("نوع السند لا يطابق المستند. / Voucher type mismatch.");
        var warehouse=v.WarehouseId.HasValue?await _db.Set<Warehouse>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==v.WarehouseId):null;
        var partyId=v.VoucherType==VoucherType.Receipt?v.CustomerId??v.SupplierId:v.SupplierId??v.CustomerId;
        var party=partyId.HasValue?await _db.Set<User>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==partyId):null;
        var currency=v.CurrencyId.HasValue?await _db.Set<Currency>().AsNoTracking().Where(x=>x.Id==v.CurrencyId).Select(x=>x.Code).SingleOrDefaultAsync():null;
        return new VoucherPrintData{
            Voucher=new VoucherWriteDto{Id=v.Id,VoucherNumber=v.VoucherNumber,VoucherType=v.VoucherType,Amount=v.Amount,PaymentType=v.PaymentType,WarehouseId=v.WarehouseId,CustomerId=v.CustomerId,SupplierId=v.SupplierId,Notes=v.Notes,CreatedDate=v.CreatedDate,UpdatedDate=v.UpdatedDate,
                Checks=v.Checks.Select(c=>new CheckWriteDto{Id=c.Id,CheckNumber=c.CheckNumber,BankName=c.BankName,Amount=c.Amount,DueDate=c.DueDate,Notes=c.Notes,Status=c.Status}).ToList()},
            Branding=await _branding.GetAsync(v.BranchId??warehouse?.BranchId??v.CashierSession?.BranchId),
            PartyName=party?.Name??partyId?.ToString()??"—",PartyPhone=party?.PhoneNumber,WarehouseName=warehouse?.Name,
            PostingStatus=v.PostingStatus,CurrencyCode=currency,ReferenceNumber=v.ReferenceNumber,DocumentDate=v.VoucherDate??v.CreatedDate};
    }
}
