using AutoMapper;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.StockTransactions;
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

        public async Task<Result> PostMovementAsync(StockMovementPostDto dto)
        {
            return await PostMovementsAsync(new[] { dto });
        }

        public async Task<Result> PostMovementsAsync(IEnumerable<StockMovementPostDto> dtos)
        {
            var items = dtos?.ToList() ?? new List<StockMovementPostDto>();
            if (items.Count == 0)
                return Result.Ok("No stock movements to post.");

            var errors = new List<string>();
            foreach (var dto in items)
            {
                if (dto.ProductId <= 0)
                    errors.Add("ProductId is required.");
                if (dto.ProductUnitId <= 0)
                    errors.Add("ProductUnitId is required.");
                if (dto.Quantity == 0)
                    errors.Add("Quantity cannot be zero.");
                if (dto.QuantityPerUnitSnapshot <= 0)
                    errors.Add("QuantityPerUnitSnapshot must be greater than zero.");
                if (dto.BaseQuantity == 0)
                    errors.Add("BaseQuantity cannot be zero.");
            }

            if (errors.Count > 0)
                return Result.Fail("Invalid stock movement.", errors.Distinct().ToList());

            var stockRepo = _uow.GetRepository<Stock>();
            var transactionRepo = _uow.GetRepository<StockTransaction>();
            var jordanTime = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, jordanTime);

            foreach (var dto in items)
            {
                var stock = stockRepo.GetAllAsQueryable()
                    .FirstOrDefault(s => s.ProductId == dto.ProductId && s.ProductUnitId == dto.ProductUnitId);

                if (stock == null)
                {
                    if (dto.Quantity < 0)
                        return Result.Fail($"Stock is not available for product {dto.ProductId} / unit {dto.ProductUnitId}.");

                    stock = new Stock
                    {
                        ProductId = dto.ProductId,
                        ProductUnitId = dto.ProductUnitId,
                        Quantity = dto.Quantity,
                        CreatedDate = now,
                        UpdatedDate = now
                    };

                    await stockRepo.AddAsync(stock);
                }
                else
                {
                    var newQuantity = stock.Quantity + dto.Quantity;
                    if (newQuantity < 0)
                        return Result.Fail($"Insufficient stock for product {dto.ProductId} / unit {dto.ProductUnitId}.");

                    stock.Quantity = newQuantity;
                    stock.UpdatedDate = now;
                    await stockRepo.UpdateAsync(stock);
                }

                var transaction = new StockTransaction
                {
                    ProductId = dto.ProductId,
                    ProductUnitId = dto.ProductUnitId,
                    Stock = stock,
                    Quantity = dto.Quantity,
                    QuantityPerUnitSnapshot = dto.QuantityPerUnitSnapshot,
                    BaseQuantity = dto.BaseQuantity,
                    UnitPrice = dto.UnitPrice,
                    TransactionType = dto.TransactionType,
                    InvoiceId = dto.InvoiceId,
                    VoucherId = dto.VoucherId,
                    CasherId = dto.CasherId,
                    CashierSessionId = dto.CashierSessionId,
                    CustomerId = dto.CustomerId,
                    TransactionDate = dto.TransactionDate == default ? now : dto.TransactionDate,
                    Notes = dto.Notes,
                    CreatedDate = now,
                    UpdatedDate = now
                };

                await transactionRepo.AddAsync(transaction);
            }

            await _uow.CommitAsync();
            return Result.Ok("Stock movement posted successfully.");
        }

    }
    public interface IStockService : IGenericService<Stock, StockWriteDto, StockReadDto>
    {
        Task<Result> PostMovementAsync(StockMovementPostDto dto);
        Task<Result> PostMovementsAsync(IEnumerable<StockMovementPostDto> dtos);
    }

    public class StockMovementPostDto
    {
        public int ProductId { get; set; }
        public int ProductUnitId { get; set; }
        public decimal Quantity { get; set; }
        public decimal QuantityPerUnitSnapshot { get; set; }
        public decimal BaseQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public Domain.Enums.TransactionType TransactionType { get; set; }
        public int? InvoiceId { get; set; }
        public int? VoucherId { get; set; }
        public int? CasherId { get; set; }
        public int? CashierSessionId { get; set; }
        public int? CustomerId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Notes { get; set; }
    }
}
