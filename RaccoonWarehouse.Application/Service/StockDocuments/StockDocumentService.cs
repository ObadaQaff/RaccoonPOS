using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.Stock.DTOs;
using RaccoonWarehouse.Domain.StockDocuments;
using RaccoonWarehouse.Domain.StockDocuments.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.StockDocuments
{
    public class StockDocumentService : GenericService<StockDocument, StockDocumentWriteDto, StockDocumentReadDto>, IStockDocumentService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        public StockDocumentService(ApplicationDbContext context, IUOW uow, IMapper mapper) : base(context, uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }
        public async Task<List<StockDocumentReadDto>> GetDocumentWithItemsAsync(string docNumber)
        {
            var data = await _uow.StockDocuments.GetAllAsQueryable()
                .Where(d => d.DocumentNumber == docNumber)
                .Include(d => d.Items)
                    .ThenInclude(i => i.Product)
                .Include(d => d.Items)
                    .ThenInclude(i => i.ProductUnit)
                        .ThenInclude(u => u.Unit)
                .AsNoTracking()
                .ToListAsync();

            return _mapper.Map<List<StockDocumentReadDto>>(data);
        }
        public async Task<List<StockDocumentReadDto>> SearchDocumentsAsync(
                string? docNumber,
                string? supplierName,
                DateTime? dateFrom,
                DateTime? dateTo,
                bool stockIn)

        {
            var query = _uow.StockDocuments.GetAllAsQueryable()
                .Include(d => d.Items)
                    .ThenInclude(i => i.Product)
                .Include(d => d.Items)
                    .ThenInclude(i => i.ProductUnit)
                        .ThenInclude(u => u.Unit)
                .Include(d => d.Supplier)
                .AsQueryable();
            if (stockIn)
            {
                query = query.Where(d => d.Type.ToString() =="In");
            }
            else{
                query = query.Where(d => d.Type.ToString() == "Out");
            }
            if (!string.IsNullOrWhiteSpace(docNumber))
                query = query.Where(d => d.DocumentNumber.Contains(docNumber));

            if (!string.IsNullOrWhiteSpace(supplierName))
                query = query.Where(d => d.Supplier.Name.Contains(supplierName));

            if (dateFrom.HasValue)
                query = query.Where(d => d.CreatedDate >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(d => d.CreatedDate <= dateTo.Value);

            var result = await query.AsNoTracking().ToListAsync();
            return _mapper.Map<List<StockDocumentReadDto>>(result);
        }

        public async Task<StockDocumentReadDto?> GetFullDocumentByIdAsync(int id)
        {
            var query = _uow.StockDocuments.GetAllAsQueryable()
                .Where(d => d.Id == id)
                .Include(d => d.Items)
                    .ThenInclude(i => i.Product)
                .Include(d => d.Items)
                    .ThenInclude(i => i.ProductUnit)
                        .ThenInclude(u => u.Unit)
                .Include(d => d.Supplier)
                .AsNoTracking();

            var doc = await query.FirstOrDefaultAsync();
            return _mapper.Map<StockDocumentReadDto>(doc);
        }



    }
    public interface IStockDocumentService : IGenericService<StockDocument, StockDocumentWriteDto, StockDocumentReadDto>
    {
        Task<List<StockDocumentReadDto>> GetDocumentWithItemsAsync(string docNumber);
        Task<List<StockDocumentReadDto>> SearchDocumentsAsync(
                string? docNumber,
                string? supplierName,
                DateTime? dateFrom,
                DateTime? dateTo,
                bool stockIn    
            );

        Task<StockDocumentReadDto?> GetFullDocumentByIdAsync(int id);

    }
}
