using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Accounting;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Accounting.Enums;
using RaccoonWarehouse.Domain.Checks;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Vouchers;
using RaccoonWarehouse.Domain.Vouchers.DTOs;

namespace RaccoonWarehouse.Application.Service.Vouchers
{
    public class VoucherService : GenericService<Voucher, VoucherWriteDto, VoucherReadDto>, IVoucherService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        private readonly IAccountingService? _accountingService;

        public VoucherService(ApplicationDbContext context, IUOW uow, IMapper mapper) : this(context, uow, mapper, null)
        {
        }

        public VoucherService(ApplicationDbContext context, IUOW uow, IMapper mapper, IAccountingService? accountingService) : base(context, uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
            _accountingService = accountingService;
        }

        public override async Task<Result<VoucherWriteDto>> CreateAsync(VoucherWriteDto dto)
        {
            try
            {
                var now = GetJordanNow();
                dto.CreatedDate = dto.CreatedDate == default ? now : dto.CreatedDate;
                dto.UpdatedDate = now;

                var voucher = _mapper.Map<Voucher>(dto);
                voucher.CreatedDate = dto.CreatedDate;
                voucher.UpdatedDate = dto.UpdatedDate;
                voucher.Checks = BuildChecks(dto, voucher, now);

                await _uow.Vouchers.AddAsync(voucher);
                await _uow.CommitAsync();

                dto.Id = voucher.Id;

                if (_accountingService != null)
                {
                    var journalResult = await _accountingService.PostVoucherEntryAsync(dto);
                    if (!journalResult.Success)
                    {
                        await _uow.Vouchers.DeleteAsync(voucher.Id);
                        await _uow.CommitAsync();
                        return Result<VoucherWriteDto>.Fail($"Voucher creation was rolled back because accounting posting failed: {journalResult.Message}");
                    }

                    voucher.PostingStatus = journalResult.Data?.Id > 0
                        ? AccountingPostingStatus.Posted
                        : AccountingPostingStatus.NotPosted;
                    await _uow.Vouchers.UpdateAsync(voucher);
                    await _uow.CommitAsync();
                }

                return Result<VoucherWriteDto>.Ok(dto);
            }
            catch (Exception ex)
            {
                return Result<VoucherWriteDto>.Fail("خطأ أثناء إضافة السند: " + ex.Message);
            }
        }

        public override async Task<Result<VoucherWriteDto>> UpdateAsync(VoucherWriteDto dto)
        {
            try
            {
                if (dto.Id <= 0)
                    return Result<VoucherWriteDto>.Fail("Voucher id is required.");

                var voucher = await _uow.Vouchers.GetAllAsQueryable()
                    .Include(x => x.Checks)
                    .FirstOrDefaultAsync(x => x.Id == dto.Id);

                if (voucher == null)
                    return Result<VoucherWriteDto>.Fail("Voucher not found.");

                var now = GetJordanNow();
                var hadPostedAccounting = voucher.PostingStatus == AccountingPostingStatus.Posted;

                dto.CreatedDate = voucher.CreatedDate;
                dto.UpdatedDate = now;

                _mapper.Map(dto, voucher);
                voucher.CreatedDate = dto.CreatedDate;
                voucher.UpdatedDate = dto.UpdatedDate;

                if (voucher.Checks != null && voucher.Checks.Count > 0)
                    _context.Set<Check>().RemoveRange(voucher.Checks);

                voucher.Checks = BuildChecks(dto, voucher, now);

                await _uow.Vouchers.UpdateAsync(voucher);
                await _uow.CommitAsync();

                if (_accountingService != null)
                {
                    if (hadPostedAccounting)
                    {
                        var reverseResult = await _accountingService.ReverseJournalByReferenceAsync(
                            "Voucher",
                            voucher.Id,
                            $"Repost voucher #{voucher.VoucherNumber ?? voucher.Id.ToString()} after update");

                        if (!reverseResult.Success)
                            return Result<VoucherWriteDto>.Fail($"Voucher update was blocked because accounting reversal failed: {reverseResult.Message}");
                    }

                    var repostResult = await _accountingService.PostVoucherEntryAsync(dto);
                    if (!repostResult.Success)
                        return Result<VoucherWriteDto>.Fail($"Voucher data was updated but accounting repost failed: {repostResult.Message}");

                    voucher.PostingStatus = repostResult.Data?.Id > 0
                        ? AccountingPostingStatus.Posted
                        : AccountingPostingStatus.NotPosted;
                    await _uow.Vouchers.UpdateAsync(voucher);
                    await _uow.CommitAsync();
                }

                return Result<VoucherWriteDto>.Ok(dto, "Voucher updated successfully.");
            }
            catch (Exception ex)
            {
                return Result<VoucherWriteDto>.Fail("خطأ أثناء تعديل السند: " + ex.Message);
            }
        }

        public async Task<List<VoucherReadDto>> SearchVouchersAsync(
            string? voucherNumber,
            string? customerName,
            DateTime? dateFrom,
            DateTime? dateTo,
            PaymentType? paymentType,
            VoucherType? type)
        {
            var query = _uow.Vouchers.GetAllAsQueryable()
                .Include(v => v.Checks)
                .AsNoTracking();

            if (type.HasValue)
                query = query.Where(d => d.VoucherType == type.Value);

            if (paymentType.HasValue)
                query = query.Where(d => d.PaymentType == paymentType.Value);

            if (!string.IsNullOrWhiteSpace(voucherNumber))
                query = query.Where(d => d.VoucherNumber == voucherNumber);

            if (!string.IsNullOrWhiteSpace(customerName))
                query = query.Where(d =>
                    (d.VoucherNumber != null && d.VoucherNumber.Contains(customerName)) ||
                    (d.ReferenceNumber != null && d.ReferenceNumber.Contains(customerName)) ||
                    (d.Notes != null && d.Notes.Contains(customerName)));

            if (dateFrom.HasValue)
                query = query.Where(d => d.CreatedDate >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(d => d.CreatedDate <= dateTo.Value);

            var result = await query.ToListAsync();
            return _mapper.Map<List<VoucherReadDto>>(result);
        }

        private static List<Check> BuildChecks(VoucherWriteDto dto, Voucher voucher, DateTime now)
        {
            if (dto.Checks == null || dto.Checks.Count == 0)
                return new List<Check>();

            return dto.Checks.Select(c => new Check
            {
                CheckNumber = c.CheckNumber,
                BankName = c.BankName,
                DueDate = c.DueDate,
                Amount = c.Amount,
                Notes = c.Notes,
                Voucher = voucher,
                CreatedDate = now,
                UpdatedDate = now
            }).ToList();
        }

        private static DateTime GetJordanNow()
        {
            var jordanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, jordanTimeZone);
        }
    }

    public interface IVoucherService : IGenericService<Voucher, VoucherWriteDto, VoucherReadDto>
    {
        Task<List<VoucherReadDto>> SearchVouchersAsync(
            string? voucherNumber,
            string? customerName,
            DateTime? dateFrom,
            DateTime? dateTo,
            PaymentType? paymentType,
            VoucherType? type);
    }
}
