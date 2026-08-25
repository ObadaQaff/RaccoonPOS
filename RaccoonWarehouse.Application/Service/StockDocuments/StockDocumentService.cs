using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.StockDocuments;
using RaccoonWarehouse.Domain.StockDocuments.DTOs;
using RaccoonWarehouse.Domain.StockItems;

namespace RaccoonWarehouse.Application.Service.StockDocuments
{
    public class StockDocumentService : GenericService<StockDocument, StockDocumentWriteDto, StockDocumentReadDto>, IStockDocumentService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        private readonly IAccountingService? _accountingService;

        public StockDocumentService(ApplicationDbContext context, IUOW uow, IMapper mapper) : this(context, uow, mapper, null)
        {
        }

        public StockDocumentService(ApplicationDbContext context, IUOW uow, IMapper mapper, IAccountingService? accountingService) : base(context, uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
            _accountingService = accountingService;
        }

        public override async Task<Result<StockDocumentWriteDto>> CreateAsync(StockDocumentWriteDto dto)
        {
            try
            {
                if (dto.Items == null || dto.Items.Count == 0)
                    return Result<StockDocumentWriteDto>.Fail("Stock document must contain at least one item.");
                if (dto.Type == StockVoucherType.In && dto.Items.Any(item => !item.ExpiryDate.HasValue))
                    return Result<StockDocumentWriteDto>.Fail("Expiry date is required for every stock-in item.");
                if (dto.Type == StockVoucherType.In && dto.PaymentType == PaymentType.Credit && !dto.SupplierId.HasValue)
                    return Result<StockDocumentWriteDto>.Fail("A user/account is required for credit stock-in / الذمة تتطلب اختيار حساب.");
                if (dto.Type == StockVoucherType.In && dto.PaymentType == PaymentType.Check)
                {
                    var gross = dto.Items.Sum(item => Math.Max(0m, item.Quantity * item.PurchasePrice - item.LineDiscountAmount));
                    var expected = Math.Round(gross - Math.Clamp(dto.DiscountAmount ?? 0m, 0m, gross), 3);
                    var checksTotal = Math.Round((dto.Checks ?? new List<RaccoonWarehouse.Domain.Checks.DTOs.CheckWriteDto>()).Sum(check => check.Amount), 3);
                    if (checksTotal != expected)
                        return Result<StockDocumentWriteDto>.Fail($"Check total ({checksTotal:0.###}) must equal stock-in total ({expected:0.###}) / مجموع الشيكات يجب أن يساوي إجمالي سند الإدخال.");
                }

                var now = GetJordanNow();
                dto.CreatedDate = dto.CreatedDate == default ? now : dto.CreatedDate;
                dto.UpdatedDate = now;

                var document = _mapper.Map<StockDocument>(dto);
                document.CreatedDate = dto.CreatedDate;
                document.UpdatedDate = dto.UpdatedDate;
                document.Items = new List<StockItem>();

                await _uow.StockDocuments.AddAsync(document);
                await _uow.CommitAsync();

                dto.Id = document.Id;

                var lineRepo = _uow.GetRepository<StockItem>();
                var checkRepo = _uow.GetRepository<RaccoonWarehouse.Domain.Checks.Check>();
                for (var i = 0; i < dto.Items.Count; i++)
                {
                    var itemDto = dto.Items[i];
                    NormalizeItem(itemDto, dto.CreatedDate, dto.UpdatedDate, i + 1);

                    var item = _mapper.Map<StockItem>(itemDto);
                    item.StockDocumentId = document.Id;
                    item.StockDocument = null;
                    await lineRepo.AddAsync(item);
                }

                foreach (var checkDto in dto.Checks ?? Enumerable.Empty<RaccoonWarehouse.Domain.Checks.DTOs.CheckWriteDto>())
                {
                    var check = _mapper.Map<RaccoonWarehouse.Domain.Checks.Check>(checkDto);
                    check.Id = 0;
                    check.StockDocumentId = document.Id;
                    check.StockDocument = null;
                    await checkRepo.AddAsync(check);
                }

                await _uow.CommitAsync();

                if (_accountingService != null)
                {
                    var journalResult = await _accountingService.PostStockDocumentEntryAsync(dto);
                    if (!journalResult.Success)
                    {
                        _context.Set<StockItem>().RemoveRange(_context.Set<StockItem>().Where(x => x.StockDocumentId == document.Id));
                        _context.Set<RaccoonWarehouse.Domain.Checks.Check>().RemoveRange(_context.Set<RaccoonWarehouse.Domain.Checks.Check>().Where(x => x.StockDocumentId == document.Id));
                        _context.Set<StockDocument>().Remove(document);
                        await _uow.CommitAsync();
                        return Result<StockDocumentWriteDto>.Fail($"Stock document creation was rolled back because accounting posting failed: {journalResult.Message}");
                    }

                    document.PostingStatus = journalResult.Data?.Id > 0
                        ? AccountingPostingStatus.Posted
                        : AccountingPostingStatus.NotPosted;
                    await _uow.StockDocuments.UpdateAsync(document);
                    await _uow.CommitAsync();
                }

                return Result<StockDocumentWriteDto>.Ok(dto);
            }
            catch (Exception ex)
            {
                return Result<StockDocumentWriteDto>.Fail("Error creating stock document: " + ex.Message);
            }
        }

        public override async Task<Result<StockDocumentWriteDto>> UpdateAsync(StockDocumentWriteDto dto)
        {
            try
            {
                if (dto.Id <= 0)
                    return Result<StockDocumentWriteDto>.Fail("Stock document id is required.");

                if (dto.Items == null || dto.Items.Count == 0)
                    return Result<StockDocumentWriteDto>.Fail("Stock document must contain at least one item.");
                if (dto.Type == StockVoucherType.In && dto.Items.Any(item => !item.ExpiryDate.HasValue))
                    return Result<StockDocumentWriteDto>.Fail("Expiry date is required for every stock-in item.");

                var document = await _uow.StockDocuments.GetAllAsQueryable()
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.Id == dto.Id);

                if (document == null)
                    return Result<StockDocumentWriteDto>.Fail("Stock document not found.");

                var now = GetJordanNow();
                var hadPostedAccounting = document.PostingStatus == AccountingPostingStatus.Posted;

                dto.CreatedDate = document.CreatedDate;
                dto.UpdatedDate = now;

                _mapper.Map(dto, document);
                document.CreatedDate = dto.CreatedDate;
                document.UpdatedDate = dto.UpdatedDate;

                if (document.Items != null && document.Items.Count > 0)
                    _context.Set<StockItem>().RemoveRange(document.Items);

                document.Items = new List<StockItem>();

                await _uow.StockDocuments.UpdateAsync(document);
                await _uow.CommitAsync();

                var lineRepo = _uow.GetRepository<StockItem>();
                for (var i = 0; i < dto.Items.Count; i++)
                {
                    var itemDto = dto.Items[i];
                    NormalizeItem(itemDto, dto.CreatedDate, dto.UpdatedDate, i + 1);

                    var item = _mapper.Map<StockItem>(itemDto);
                    item.StockDocumentId = document.Id;
                    item.StockDocument = null;
                    await lineRepo.AddAsync(item);
                }

                await _uow.CommitAsync();

                if (_accountingService != null)
                {
                    if (hadPostedAccounting)
                    {
                        var reverseResult = await _accountingService.ReverseJournalByReferenceAsync(
                            "StockDocument",
                            document.Id,
                            $"Repost stock document #{document.DocumentNumber} after update");

                        if (!reverseResult.Success)
                            return Result<StockDocumentWriteDto>.Fail($"Stock document update was blocked because accounting reversal failed: {reverseResult.Message}");
                    }

                    var repostResult = await _accountingService.PostStockDocumentEntryAsync(dto);
                    if (!repostResult.Success)
                        return Result<StockDocumentWriteDto>.Fail($"Stock document data was updated but accounting repost failed: {repostResult.Message}");

                    document.PostingStatus = repostResult.Data?.Id > 0
                        ? AccountingPostingStatus.Posted
                        : AccountingPostingStatus.NotPosted;
                    await _uow.StockDocuments.UpdateAsync(document);
                    await _uow.CommitAsync();
                }

                return Result<StockDocumentWriteDto>.Ok(dto, "Stock document updated successfully.");
            }
            catch (Exception ex)
            {
                return Result<StockDocumentWriteDto>.Fail("Error updating stock document: " + ex.Message);
            }
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
                .Include(d => d.Checks)
                .AsQueryable();

            query = stockIn
                ? query.Where(d => d.Type == StockVoucherType.In)
                : query.Where(d => d.Type == StockVoucherType.Out);

            if (!string.IsNullOrWhiteSpace(docNumber))
                query = query.Where(d => d.DocumentNumber.Contains(docNumber));

            if (!string.IsNullOrWhiteSpace(supplierName))
                query = query.Where(d => d.Supplier != null && d.Supplier.Name.Contains(supplierName));

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

        private static void NormalizeItem(StockItemWriteDto item, DateTime createdDate, DateTime updatedDate, int lineNumber)
        {
            var factor = item.QuantityPerUnitSnapshot > 0 ? item.QuantityPerUnitSnapshot : 1m;
            item.QuantityPerUnitSnapshot = factor;
            item.BaseQuantity = item.BaseQuantity > 0 ? item.BaseQuantity : item.Quantity * factor;
            item.LineDiscountAmount = Math.Clamp(item.LineDiscountAmount, 0m, Math.Max(0m, item.Quantity * item.PurchasePrice));
            item.FreeQuantity = Math.Max(0m, item.FreeQuantity);
            item.CreatedDate = item.CreatedDate == default ? createdDate : item.CreatedDate;
            item.UpdatedDate = updatedDate;
            item.Id = 0;
        }

        private static DateTime GetJordanNow()
        {
            var jordanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, jordanTimeZone);
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
            bool stockIn);

        Task<StockDocumentReadDto?> GetFullDocumentByIdAsync(int id);
    }
}
