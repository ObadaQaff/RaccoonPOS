using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.CostCenters;
using RaccoonWarehouse.Domain.CostCenters.DTOs;

namespace RaccoonWarehouse.Application.Service.Accounting
{
    public class CostCenterService
    {
        private readonly ApplicationDbContext _dbContext;

        public CostCenterService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<CostCenter> CreateAsync(string code, string name, int? parentId = null)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new InvalidOperationException("Cost center code is required.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Cost center name is required.");
            }

            var normalizedCode = code.Trim();
            var exists = await _dbContext.CostCenters.AnyAsync(x => x.Code == normalizedCode);
            if (exists)
            {
                throw new InvalidOperationException("Cost center code must be unique.");
            }

            CostCenter? parent = null;
            if (parentId.HasValue)
            {
                parent = await _dbContext.CostCenters.FirstOrDefaultAsync(x => x.Id == parentId.Value);
                if (parent == null)
                {
                    throw new InvalidOperationException("Parent cost center was not found.");
                }
            }

            var entity = new CostCenter
            {
                Code = normalizedCode,
                Name = name.Trim(),
                ParentCostCenterId = parentId,
                Level = (parent?.Level ?? 0) + 1,
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            _dbContext.CostCenters.Add(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(int id, string code, string name, int? parentId, bool isActive)
        {
            var entity = await _dbContext.CostCenters.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                throw new InvalidOperationException("Cost center was not found.");
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                throw new InvalidOperationException("Cost center code is required.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Cost center name is required.");
            }

            var normalizedCode = code.Trim();
            var duplicateCode = await _dbContext.CostCenters.AnyAsync(x => x.Id != id && x.Code == normalizedCode);
            if (duplicateCode)
            {
                throw new InvalidOperationException("Cost center code must be unique.");
            }

            if (parentId == id)
            {
                throw new InvalidOperationException("Cost center cannot be its own parent.");
            }

            CostCenter? parent = null;
            if (parentId.HasValue)
            {
                parent = await _dbContext.CostCenters.FirstOrDefaultAsync(x => x.Id == parentId.Value);
                if (parent == null)
                {
                    throw new InvalidOperationException("Parent cost center was not found.");
                }
            }

            entity.Code = normalizedCode;
            entity.Name = name.Trim();
            entity.ParentCostCenterId = parentId;
            entity.Level = (parent?.Level ?? 0) + 1;
            entity.IsActive = isActive;
            entity.UpdatedDate = DateTime.Now;

            await _dbContext.SaveChangesAsync();
        }

        public async Task DeactivateAsync(int id)
        {
            var entity = await _dbContext.CostCenters.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                throw new InvalidOperationException("Cost center was not found.");
            }

            var hasActiveChildren = await _dbContext.CostCenters.AnyAsync(x => x.ParentCostCenterId == id && x.IsActive);
            if (hasActiveChildren)
            {
                throw new InvalidOperationException("Cannot deactivate cost center that has active children.");
            }

            entity.IsActive = false;
            entity.UpdatedDate = DateTime.Now;
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<CostCenterNodeDto>> GetTreeAsync()
        {
            var list = await _dbContext.CostCenters
                .AsNoTracking()
                .OrderBy(x => x.Code)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var map = list.ToDictionary(
                x => x.Id,
                x => new CostCenterNodeDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    ParentId = x.ParentCostCenterId,
                    IsActive = x.IsActive
                });

            var roots = new List<CostCenterNodeDto>();
            foreach (var item in list)
            {
                var node = map[item.Id];
                if (item.ParentCostCenterId.HasValue && map.TryGetValue(item.ParentCostCenterId.Value, out var parent))
                {
                    parent.Children.Add(node);
                }
                else
                {
                    roots.Add(node);
                }
            }

            return roots;
        }
    }
}
