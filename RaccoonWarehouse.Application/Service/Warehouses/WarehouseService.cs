using AutoMapper;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.SubCategories;
using RaccoonWarehouse.Domain.SubCategories.DTOs;
using RaccoonWarehouse.Domain.Warehouses;
using RaccoonWarehouse.Domain.Warehouses.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.Warehouses
{
    public class WarehouseService : GenericService<Warehouse, WarehouseWriteDto, WarehouseReadDto>, IWarehouseService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        public WarehouseService(ApplicationDbContext context, IUOW uow, IMapper mapper) : base(context, uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task EnsureDefaultWarehousesAsync()
        {
            var existing = await _uow.GetRepository<Warehouse>().GetAllAsync();
            if (existing.Any())
                return;

            var now = DateTime.Now;
            await _uow.GetRepository<Warehouse>().AddAsync(new Warehouse
            {
                Code = "WH-001",
                Name = "المستودع الرئيسي",
                Location = null,
                Description = "Default main warehouse",
                Status = Domain.Enums.WarehouseStatus.active,
                CreatedDate = now,
                UpdatedDate = now
            });

            await _uow.CommitAsync();
        }
    }
    public interface IWarehouseService : IGenericService<Warehouse, WarehouseWriteDto, WarehouseReadDto>
    {
        Task EnsureDefaultWarehousesAsync();
    }
}
