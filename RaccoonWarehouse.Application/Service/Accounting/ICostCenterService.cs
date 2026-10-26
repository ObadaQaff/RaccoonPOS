using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.CostCenters;
using RaccoonWarehouse.Domain.CostCenters.DTOs;
namespace RaccoonWarehouse.Application.Service.Accounting;
public interface ICostCenterService
{
    Task<CostCenter> CreateAsync(string code, string name, int? parentId = null);
    Task UpdateAsync(int id, string code, string name, int? parentId, bool isActive);
    Task DeactivateAsync(int id);
    Task<List<CostCenterNodeDto>> GetTreeAsync();
}
