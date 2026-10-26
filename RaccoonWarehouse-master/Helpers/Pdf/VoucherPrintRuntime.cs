using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Vouchers;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
namespace RaccoonWarehouse.Helpers.Pdf;
public static class VoucherPrintRuntime
{
    public static async Task<VoucherPrintData> LoadAsync(int id,VoucherType type)
    {
        using var scope=((App)System.Windows.Application.Current).ServiceProvider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<VoucherPrintDataService>().LoadAsync(id,type);
    }
    public static VoucherPrintData Load(int id,VoucherType type)
    {
        var provider=((App)System.Windows.Application.Current).ServiceProvider;
        return Task.Run(async()=>{using var scope=provider.CreateScope();return await scope.ServiceProvider.GetRequiredService<VoucherPrintDataService>().LoadAsync(id,type);}).GetAwaiter().GetResult();
    }
}
