using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Delegates;
using RaccoonWarehouse.Domain.Delegates.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Reports.Delegates.Dtos;
using RaccoonWarehouse.Domain.Reports.Delegates.Filters;
using DelegateEntity = RaccoonWarehouse.Domain.Delegates.Delegate;

namespace RaccoonWarehouse.Application.Service.Delegates
{
    public interface IDelegateService
    {
        Task<Result<DelegateCreateDto>> CreateAsync(DelegateCreateDto dto);
        Task<Result<DelegateUpdateDto>> UpdateAsync(DelegateUpdateDto dto);
        Task<Result<bool>> SetStatusAsync(int delegateId, DelegateStatus status);
        Task<Result<List<DelegateReadDto>>> GetListAsync(DelegateListFilterDto? filter = null);
        Task<Result<DelegateReadDto>> GetByIdAsync(int id);
        Task<Result<List<DelegateReadDto>>> GetActiveDelegatesAsync();
        Task<Result<bool>> LinkUserAsync(int delegateId, int userId);
        Task<Result<bool>> UnlinkUserAsync(int delegateId);
        Task<Result<DelegateAnalyticsDto>> GetAnalyticsAsync(int delegateId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<Result<List<DelegateReportRowDto>>> GetReportAsync(DelegateReportFilterDto filter);
        Task<Result<bool>> SoftDeleteAsync(int delegateId);
    }

    public class DelegateService : IDelegateService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        private readonly IDelegateFeatureService _featureService;

        public DelegateService(ApplicationDbContext dbContext, IUOW uow, IMapper mapper, IDelegateFeatureService featureService)
        {
            _dbContext = dbContext;
            _uow = uow;
            _mapper = mapper;
            _featureService = featureService;
        }

        public async Task<Result<DelegateCreateDto>> CreateAsync(DelegateCreateDto dto)
        {
            var validation = await ValidateAsync(dto.Id, dto.Code, dto.FullName, dto.PhoneNumber, dto.UserId);
            if (!validation.Success)
                return Result<DelegateCreateDto>.Fail(validation.Message);

            var entity = _mapper.Map<DelegateEntity>(dto);
            entity.CreatedDate = DateTime.Now;
            entity.UpdatedDate = DateTime.Now;

            await _uow.GetRepository<DelegateEntity>().AddAsync(entity);
            await _uow.CommitAsync();

            dto.Id = entity.Id;
            return Result<DelegateCreateDto>.Ok(dto, "Delegate created successfully.");
        }

        public async Task<Result<DelegateUpdateDto>> UpdateAsync(DelegateUpdateDto dto)
        {
            var entity = await _dbContext.Set<DelegateEntity>().FirstOrDefaultAsync(x => x.Id == dto.Id);
            if (entity == null)
                return Result<DelegateUpdateDto>.Fail("Delegate not found.");

            var validation = await ValidateAsync(dto.Id, dto.Code, dto.FullName, dto.PhoneNumber, dto.UserId);
            if (!validation.Success)
                return Result<DelegateUpdateDto>.Fail(validation.Message);

            var createdDate = entity.CreatedDate;
            _mapper.Map(dto, entity);
            entity.CreatedDate = createdDate;
            entity.UpdatedDate = DateTime.Now;

            await _uow.GetRepository<DelegateEntity>().UpdateAsync(entity);
            await _uow.CommitAsync();
            return Result<DelegateUpdateDto>.Ok(dto, "Delegate updated successfully.");
        }

        public async Task<Result<bool>> SetStatusAsync(int delegateId, DelegateStatus status)
        {
            var entity = await _dbContext.Set<DelegateEntity>().FirstOrDefaultAsync(x => x.Id == delegateId);
            if (entity == null)
                return Result<bool>.Fail("Delegate not found.");

            entity.Status = status;
            entity.UpdatedDate = DateTime.Now;
            await _dbContext.SaveChangesAsync();
            return Result<bool>.Ok(true, "Delegate status updated.");
        }

        public async Task<Result<List<DelegateReadDto>>> GetListAsync(DelegateListFilterDto? filter = null)
        {
            var query = _dbContext.Set<DelegateEntity>()
                .Include(x => x.User)
                .Include(x => x.Invoices)
                .AsNoTracking()
                .AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.SearchText))
                {
                    var search = filter.SearchText.Trim();
                    query = query.Where(x =>
                        x.FullName.Contains(search) ||
                        x.Code.Contains(search) ||
                        (x.PhoneNumber != null && x.PhoneNumber.Contains(search)));
                }

                if (filter.Status.HasValue)
                    query = query.Where(x => x.Status == filter.Status.Value);

                if (filter.DelegateType.HasValue)
                    query = query.Where(x => x.DelegateType == filter.DelegateType.Value);

                if (filter.RegionId.HasValue)
                    query = query.Where(x => x.RegionId == filter.RegionId.Value);

                if (filter.OnlyActive)
                    query = query.Where(x => x.Status == DelegateStatus.Active);
            }

            var delegates = await query.OrderBy(x => x.FullName).ToListAsync();
            return Result<List<DelegateReadDto>>.Ok(_mapper.Map<List<DelegateReadDto>>(delegates));
        }

        public async Task<Result<DelegateReadDto>> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Set<DelegateEntity>()
                .Include(x => x.User)
                .Include(x => x.Invoices)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return Result<DelegateReadDto>.Fail("Delegate not found.");

            return Result<DelegateReadDto>.Ok(_mapper.Map<DelegateReadDto>(entity));
        }

        public async Task<Result<List<DelegateReadDto>>> GetActiveDelegatesAsync()
        {
            if (!await _featureService.IsEnabledAsync())
                return Result<List<DelegateReadDto>>.Ok(new List<DelegateReadDto>());

            return await GetListAsync(new DelegateListFilterDto { OnlyActive = true });
        }

        public async Task<Result<bool>> LinkUserAsync(int delegateId, int userId)
        {
            var entity = await _dbContext.Set<DelegateEntity>().FirstOrDefaultAsync(x => x.Id == delegateId);
            if (entity == null)
                return Result<bool>.Fail("Delegate not found.");

            var conflict = await _dbContext.Set<DelegateEntity>()
                .AnyAsync(x => x.Id != delegateId && x.UserId == userId);

            if (conflict)
                return Result<bool>.Fail("This user is already linked to another delegate.");

            entity.UserId = userId;
            entity.UpdatedDate = DateTime.Now;
            await _dbContext.SaveChangesAsync();
            return Result<bool>.Ok(true, "User linked successfully.");
        }

        public async Task<Result<bool>> UnlinkUserAsync(int delegateId)
        {
            var entity = await _dbContext.Set<DelegateEntity>().FirstOrDefaultAsync(x => x.Id == delegateId);
            if (entity == null)
                return Result<bool>.Fail("Delegate not found.");

            entity.UserId = null;
            entity.UpdatedDate = DateTime.Now;
            await _dbContext.SaveChangesAsync();
            return Result<bool>.Ok(true, "User unlinked successfully.");
        }

        public async Task<Result<DelegateAnalyticsDto>> GetAnalyticsAsync(int delegateId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var entity = await _dbContext.Set<DelegateEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == delegateId);

            if (entity == null)
                return Result<DelegateAnalyticsDto>.Fail("Delegate not found.");

            var invoiceQuery = _dbContext.Set<Invoice>()
                .Where(x => x.DelegateId == delegateId);

            if (fromDate.HasValue)
                invoiceQuery = invoiceQuery.Where(x => x.CreatedDate >= fromDate.Value);

            if (toDate.HasValue)
                invoiceQuery = invoiceQuery.Where(x => x.CreatedDate <= toDate.Value);

            var invoices = await invoiceQuery.AsNoTracking().ToListAsync();

            var dto = new DelegateAnalyticsDto
            {
                DelegateId = entity.Id,
                DelegateName = entity.FullName,
                TotalInvoices = invoices.Count,
                TotalSalesAmount = invoices.Sum(x => x.TotalAmount),
                InvoicesInRange = invoices.Count,
                UniqueCustomersServed = invoices.Where(x => x.CustomerId.HasValue).Select(x => x.CustomerId!.Value).Distinct().Count(),
                LastInvoiceDate = invoices.OrderByDescending(x => x.CreatedDate).Select(x => (DateTime?)x.CreatedDate).FirstOrDefault(),
                OpenInvoicesCount = invoices.Count(x => x.Status == InvoiceStatus.Draft || x.Status == InvoiceStatus.OnHold)
            };

            return Result<DelegateAnalyticsDto>.Ok(dto);
        }

        public async Task<Result<List<DelegateReportRowDto>>> GetReportAsync(DelegateReportFilterDto filter)
        {
            var query = _dbContext.Set<DelegateEntity>()
                .Include(x => x.Invoices)
                .AsNoTracking()
                .AsQueryable();

            if (filter.DelegateId.HasValue)
                query = query.Where(x => x.Id == filter.DelegateId.Value);

            if (filter.RegionId.HasValue)
                query = query.Where(x => x.RegionId == filter.RegionId.Value);

            var delegates = await query.ToListAsync();

            var rows = delegates.Select(delegateItem =>
            {
                var invoices = delegateItem.Invoices.AsEnumerable();

                if (filter.FromDate.HasValue)
                    invoices = invoices.Where(x => x.CreatedDate >= filter.FromDate.Value);

                if (filter.ToDate.HasValue)
                    invoices = invoices.Where(x => x.CreatedDate <= filter.ToDate.Value);

                var invoiceList = invoices.ToList();
                var invoiceCount = invoiceList.Count;
                var totalSales = invoiceList.Sum(x => x.TotalAmount);

                return new DelegateReportRowDto
                {
                    DelegateId = delegateItem.Id,
                    DelegateName = delegateItem.FullName,
                    InvoiceCount = invoiceCount,
                    TotalSales = totalSales,
                    AverageInvoiceValue = invoiceCount == 0 ? 0m : Math.Round(totalSales / invoiceCount, 2),
                    LastActivityDate = invoiceList.OrderByDescending(x => x.CreatedDate).Select(x => (DateTime?)x.CreatedDate).FirstOrDefault()
                };
            }).OrderBy(x => x.DelegateName).ToList();

            return Result<List<DelegateReportRowDto>>.Ok(rows);
        }

        public async Task<Result<bool>> SoftDeleteAsync(int delegateId)
        {
            var entity = await _dbContext.Set<DelegateEntity>()
                .Include(x => x.Invoices)
                .FirstOrDefaultAsync(x => x.Id == delegateId);

            if (entity == null)
                return Result<bool>.Fail("Delegate not found.");

            if (entity.Invoices.Any())
            {
                entity.Status = DelegateStatus.Inactive;
                entity.IsDeleted = true;
                entity.UpdatedDate = DateTime.Now;
                await _dbContext.SaveChangesAsync();
                return Result<bool>.Ok(true, "Delegate was deactivated and soft-deleted because invoices exist.");
            }

            entity.IsDeleted = true;
            entity.UpdatedDate = DateTime.Now;
            await _dbContext.SaveChangesAsync();
            return Result<bool>.Ok(true, "Delegate deleted successfully.");
        }

        private async Task<Result<bool>> ValidateAsync(int id, string code, string fullName, string? phoneNumber, int? userId)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return Result<bool>.Fail("Full name is required.");

            if (string.IsNullOrWhiteSpace(code))
                return Result<bool>.Fail("Code is required.");

            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
                if (digits.Length < 7)
                    return Result<bool>.Fail("Phone number format is not valid.");
            }

            var duplicateCode = await _dbContext.Set<DelegateEntity>()
                .AnyAsync(x => x.Id != id && x.Code == code);

            if (duplicateCode)
                return Result<bool>.Fail("Delegate code must be unique.");

            if (userId.HasValue)
            {
                var duplicateUser = await _dbContext.Set<DelegateEntity>()
                    .AnyAsync(x => x.Id != id && x.UserId == userId.Value);

                if (duplicateUser)
                    return Result<bool>.Fail("The selected user is already linked to another delegate.");
            }

            return Result<bool>.Ok(true);
        }
    }
}
