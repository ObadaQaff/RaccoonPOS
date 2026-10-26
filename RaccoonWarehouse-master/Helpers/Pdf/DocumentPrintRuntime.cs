using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Domain.Settings;
using System.IO;
using System.Diagnostics;
using System.Windows;

namespace RaccoonWarehouse.Helpers.Pdf;

public static class DocumentPrintRuntime
{
    public static async Task RequirePrintAsync(string permission)
    {
        using var scope=((App)System.Windows.Application.Current).ServiceProvider.CreateScope();
        if(!await scope.ServiceProvider.GetRequiredService<RaccoonWarehouse.Application.Service.Permissions.IPermissionService>().HasCurrentUserPermissionAsync(permission))
            throw new UnauthorizedAccessException(RaccoonWarehouse.Helpers.Localization.UiText.T("ليس لديك صلاحية الطباعة.","You do not have print permission."));
    }
    public static async Task<RaccoonWarehouse.Domain.PurchaseOrders.DTOs.PurchaseOrderWriteDto> SavedOrderAsync(int id)
    {
        using var scope=((App)System.Windows.Application.Current).ServiceProvider.CreateScope();
        var result=await scope.ServiceProvider.GetRequiredService<RaccoonWarehouse.Application.Service.PurchaseOrders.IPurchaseOrderService>().GetByIdAsync(id);
        if(!result.Success || result.Data==null)throw new InvalidOperationException(result.Message);
        return result.Data;
    }
    public static DocumentBranding Branding(int? invoiceId=null,int? warehouseId=null,int? branchId=null)
    {
        var provider=((App)System.Windows.Application.Current).ServiceProvider;
        return Task.Run(async()=>{
            using var scope=provider.CreateScope();
            var service=scope.ServiceProvider.GetRequiredService<DocumentBrandingService>();
            var branch=branchId??(invoiceId.HasValue?await service.InvoiceBranchAsync(invoiceId.Value):await service.OrderBranchAsync(warehouseId));
            return await service.GetAsync(branch);
        }).GetAwaiter().GetResult();
    }
    public static void Preview(IReportDocument report,Window owner)
    {
        var path=Path.Combine(Path.GetTempPath(),$"{report.FileName}_{Guid.NewGuid():N}.pdf");
        QuestPDF.Fluent.GenerateExtensions.GeneratePdf(report,path);
        Process.Start(new ProcessStartInfo(path){UseShellExecute=true});
    }
}
