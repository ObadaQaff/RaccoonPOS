using AutoMapper;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.Stock.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.Stocks
{
    public class StockService : GenericService<Stock, StockWriteDto, StockReadDto>, IStockService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        public StockService(ApplicationDbContext context, IUOW uow, IMapper mapper) : base(context, uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }
        public override async Task<Result<StockWriteDto>> UpdateAsync(StockWriteDto dto)
        {
            // Load the entity from DB
            var entity = await _uow.GetRepository<Stock>().GetByIdAsync(dto.Id);

            if (entity == null)
                return Result<StockWriteDto>.Fail("Stock not found.");

            // Update ONLY allowed fields (safe!)
            entity.Quantity = dto.Quantity;
            entity.ProductUnitId = dto.ProductUnitId;
            entity.ProductId = dto.ProductId;

            // Update date
            var jordanTime = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
            entity.UpdatedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, jordanTime);

            // DO NOT TOUCH:
            // entity.CreatedDate
            // entity.Product
            // entity.ProductUnit
            // entity.Id

            await _uow.GetRepository<Stock>().UpdateAsync(entity);
            await _uow.CommitAsync();

            return Result<StockWriteDto>.Ok(dto, "Stock updated successfully.");
        }

    }
    public interface IStockService : IGenericService<Stock, StockWriteDto, StockReadDto>
    {
       
          


    }
}
