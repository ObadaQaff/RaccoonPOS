using AutoMapper;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.StockTransactions;
using RaccoonWarehouse.Domain.StockTransactions.DTOs;

namespace RaccoonWarehouse.Application.Service.StockTransactions
{
    public class StockTransactionService : GenericService<StockTransaction, StockTransactionWriteDto, StockTransactionReadDto>,
                                          IStockTransactionService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;

        public StockTransactionService(ApplicationDbContext context, IUOW uow, IMapper mapper)
            : base(context, uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<Result<StockTransactionReadDto>> PostAsync(StockTransactionWriteDto dto)
        {
            var validation = Validate(dto);
            if (!validation.Success)
                return Result<StockTransactionReadDto>.Fail(validation.Message, validation.Errors);

            var entity = _mapper.Map<StockTransaction>(dto);
            ApplyAuditDefaults(entity);

            var repo = _uow.GetRepository<StockTransaction>();
            await repo.AddAsync(entity);
            await _uow.CommitAsync();

            return Result<StockTransactionReadDto>.Ok(_mapper.Map<StockTransactionReadDto>(entity), "Stock transaction posted successfully.");
        }

        public async Task<Result<List<StockTransactionReadDto>>> PostRangeAsync(IEnumerable<StockTransactionWriteDto> dtos)
        {
            var items = dtos?.ToList() ?? new List<StockTransactionWriteDto>();
            if (items.Count == 0)
                return Result<List<StockTransactionReadDto>>.Ok(new List<StockTransactionReadDto>(), "No stock transactions to post.");

            var repo = _uow.GetRepository<StockTransaction>();
            var entities = new List<StockTransaction>();

            foreach (var dto in items)
            {
                var validation = Validate(dto);
                if (!validation.Success)
                    return Result<List<StockTransactionReadDto>>.Fail(validation.Message, validation.Errors);

                var entity = _mapper.Map<StockTransaction>(dto);
                ApplyAuditDefaults(entity);
                entities.Add(entity);
                await repo.AddAsync(entity);
            }

            await _uow.CommitAsync();
            return Result<List<StockTransactionReadDto>>.Ok(_mapper.Map<List<StockTransactionReadDto>>(entities), "Stock transactions posted successfully.");
        }

        private static Result Validate(StockTransactionWriteDto dto)
        {
            var errors = new List<string>();

            if (dto.ProductId <= 0)
                errors.Add("ProductId is required.");

            if (dto.ProductUnitId <= 0)
                errors.Add("ProductUnitId is required.");

            if (dto.QuantityPerUnitSnapshot <= 0)
                errors.Add("QuantityPerUnitSnapshot must be greater than zero.");

            if (dto.Quantity == 0)
                errors.Add("Quantity cannot be zero.");

            if (dto.BaseQuantity == 0)
                errors.Add("BaseQuantity cannot be zero.");

            return errors.Count == 0
                ? Result.Ok()
                : Result.Fail("Invalid stock transaction.", errors);
        }

        private static void ApplyAuditDefaults(StockTransaction entity)
        {
            var jordanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, jordanTimeZone);

            if (entity.TransactionDate == default)
                entity.TransactionDate = now;

            entity.CreatedDate = now;
            entity.UpdatedDate = now;
        }
    }

    public interface IStockTransactionService : IGenericService<StockTransaction, StockTransactionWriteDto, StockTransactionReadDto>
    {
        Task<Result<StockTransactionReadDto>> PostAsync(StockTransactionWriteDto dto);
        Task<Result<List<StockTransactionReadDto>>> PostRangeAsync(IEnumerable<StockTransactionWriteDto> dtos);
    }
}
