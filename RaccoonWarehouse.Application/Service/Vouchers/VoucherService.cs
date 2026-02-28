using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Checks;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Invoices;
using RaccoonWarehouse.Domain.Invoices.DTOs;
using RaccoonWarehouse.Domain.Stock;
using RaccoonWarehouse.Domain.StockDocuments.DTOs;
using RaccoonWarehouse.Domain.Vouchers;
using RaccoonWarehouse.Domain.Vouchers.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.Vouchers
{
    public class VoucherService : GenericService<Voucher, VoucherWriteDto, VoucherReadDto>, IVoucherService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        public VoucherService(ApplicationDbContext context, IUOW uow, IMapper mapper) : base(context, uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }
        public override async Task<Result<VoucherWriteDto>> CreateAsync(VoucherWriteDto dto)
        {
            try
            {
                // 1. Map DTO → Entity
                var voucher = _mapper.Map<Voucher>(dto);

                // 2. Attach checks if any
                if (dto.Checks != null && dto.Checks.Count > 0)
                {
                    voucher.Checks = dto.Checks.Select(c => new Check
                    {
                        CheckNumber = c.CheckNumber,
                        BankName = c.BankName,
                        DueDate = c.DueDate,
                        Amount = c.Amount,
                        Notes = c.Notes,

                        // Parent will be assigned automatically
                        Voucher = voucher,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    }).ToList();
                }

                // 3. Save voucher + checks in one transaction
                await _uow.Vouchers.AddAsync(voucher);
                await _context.SaveChangesAsync();

                // 4. Map back to DTO
                var resultDto = _mapper.Map<VoucherWriteDto>(voucher);

                return Result<VoucherWriteDto>.Ok(resultDto);
            }
            catch (Exception ex)
            {
                return Result<VoucherWriteDto>.Fail("خطأ أثناء إضافة السند: " + ex.Message);
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
                    .Include(v => v.User)
                    .AsNoTracking();
            if (type.HasValue)
            {
                query = query.Where(d => d.VoucherType == type.Value);


            }
            if (paymentType.HasValue)
            {
                query = query.Where(d => d.PaymentType == paymentType.Value);
            }

            if (!string.IsNullOrWhiteSpace(voucherNumber))
            query = query.Where(d => d.VoucherNumber == (voucherNumber));

            if (!string.IsNullOrWhiteSpace(customerName))
                query = query.Where(d => d.User.Name.Contains(customerName));

            if (dateFrom.HasValue)
                query = query.Where(d => d.CreatedDate >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(d => d.CreatedDate <= dateTo.Value);

            var result = await query.AsNoTracking().ToListAsync();
            return _mapper.Map<List<VoucherReadDto>>(result);

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
