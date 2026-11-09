using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Settings;

namespace RaccoonWarehouse.Application.Service.Settings;

public sealed class DocumentBrandingService
{
    public const string Key = "Printing.Branding.v1";
    private readonly ApplicationDbContext _db;
    private readonly IPermissionService _permissions;
    public DocumentBrandingService(ApplicationDbContext db, IPermissionService permissions) => (_db,_permissions)=(db,permissions);
    public static string SettingKey(int? branchId) => branchId is > 0 ? $"{Key}.Branch.{branchId}" : Key;
    public async Task<DocumentBranding> GetAsync(int? branchId = null)
    {
        var key=SettingKey(branchId);
        var json=await _db.AppSettings.AsNoTracking().Where(x=>x.Key==key).Select(x=>x.Value).SingleOrDefaultAsync();
        if(!string.IsNullOrWhiteSpace(json)) return JsonSerializer.Deserialize<DocumentBranding>(json) ?? new();
        // A known branch never inherits another business identity by accident.
        if(branchId is > 0)
        {
            var b=await _db.Branches.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==branchId);
            return new DocumentBranding { DisplayName=b?.ArabicName??b?.Name??"",Address=b?.Address??"",Phone=b?.PhoneNumber??"" };
        }
        return new();
    }
    public async Task SaveAsync(DocumentBranding value,int? branchId=null)
    {
        if(!await _permissions.HasCurrentUserPermissionAsync("Settings.ManageSettings")) throw new UnauthorizedAccessException("ليس لديك صلاحية تعديل الإعدادات. / Settings permission required.");
        Validate(value);
        if(branchId is > 0 && !await _db.Branches.AnyAsync(x=>x.Id==branchId)) throw new ArgumentException("الفرع غير موجود. / Branch not found.");
        var key=SettingKey(branchId);var row=await _db.AppSettings.SingleOrDefaultAsync(x=>x.Key==key);
        if(row==null){row=new AppSetting{Key=key,Description="Central document branding",CreatedDate=DateTime.Now};_db.AppSettings.Add(row);}
        row.Value=JsonSerializer.Serialize(value);row.UpdatedDate=DateTime.Now;await _db.SaveChangesAsync();
    }
    public static void Validate(DocumentBranding value)
    {
        if(value.ThermalPaperWidthMm is not (58 or 80))throw new ArgumentException("عرض الورق يجب أن يكون 58 أو 80 مم. / Paper width must be 58 or 80 mm.");
        foreach(var text in new[]{value.DisplayName,value.Address,value.Phone,value.TaxNumber,value.AdditionalInformation,value.Footer})
            if(text?.Length>1000)throw new ArgumentException("النص أطول من المسموح. / Text is too long.");
        if(!string.IsNullOrEmpty(value.LogoBase64))
        {
            byte[] bytes;try{bytes=Convert.FromBase64String(value.LogoBase64);}catch(FormatException){throw new ArgumentException("صورة غير صالحة. / Invalid image.");}
            if(bytes.Length>1024*1024 || bytes.Length<8 || !(bytes[0]==137&&bytes[1]==80&&bytes[2]==78&&bytes[3]==71 || bytes[0]==255&&bytes[1]==216))
                throw new ArgumentException("استخدم صورة PNG أو JPEG لا تتجاوز 1 ميجابايت. / Use PNG or JPEG up to 1 MB.");
        }
    }
    public async Task<int?> InvoiceBranchAsync(int invoiceId)=>await _db.Set<RaccoonWarehouse.Domain.Invoices.Invoice>().AsNoTracking().Where(x=>x.Id==invoiceId).Select(x=>x.BranchId).SingleOrDefaultAsync();
    public async Task<int?> OrderBranchAsync(int? warehouseId)=>warehouseId==null?null:await _db.Set<RaccoonWarehouse.Domain.Warehouses.Warehouse>().AsNoTracking().Where(x=>x.Id==warehouseId).Select(x=>x.BranchId).SingleOrDefaultAsync();
}
