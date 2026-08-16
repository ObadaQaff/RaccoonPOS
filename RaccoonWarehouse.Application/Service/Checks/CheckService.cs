using AutoMapper;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Application.Service.Generic;
using RaccoonWarehouse.Domain.Checks;
using RaccoonWarehouse.Domain.Checks.DTOs;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.Checks
{
    public class CheckService : GenericService<Check, CheckWriteDto, CheckReadDto>, ICheckService
    {
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        public CheckService(ApplicationDbContext context, IUOW uow, IMapper mapper) : base(context, uow, mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<Result<CheckWriteDto>> UpdateStatusAsync(int checkId, CheckStatus status)
        {
            if (checkId <= 0)
                return Result<CheckWriteDto>.Fail("Check id is required.");

            var check = await _context.Set<Check>().FirstOrDefaultAsync(x => x.Id == checkId);
            if (check == null)
                return Result<CheckWriteDto>.Fail("Check not found.");

            check.Status = status;
            check.UpdatedDate = GetJordanNow();
            await _context.SaveChangesAsync();

            return Result<CheckWriteDto>.Ok(_mapper.Map<CheckWriteDto>(check), "Check status updated successfully.");
        }

        private static DateTime GetJordanNow()
        {
            var jordanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, jordanTimeZone);
        }
    }
    public interface ICheckService : IGenericService<Check, CheckWriteDto, CheckReadDto>
    {
        Task<Result<CheckWriteDto>> UpdateStatusAsync(int checkId, CheckStatus status);
    }
}
