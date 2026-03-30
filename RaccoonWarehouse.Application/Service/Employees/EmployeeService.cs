using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Employees.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.Reports.Employees.Dtos;
using RaccoonWarehouse.Domain.Reports.Employees.Filters;
using EmployeeEntity = RaccoonWarehouse.Domain.Employees.Employee;

namespace RaccoonWarehouse.Application.Service.Employees
{
    public interface IEmployeeService
    {
        Task<Result<EmployeeCreateDto>> CreateAsync(EmployeeCreateDto dto);
        Task<Result<EmployeeUpdateDto>> UpdateAsync(EmployeeUpdateDto dto);
        Task<Result<bool>> SetStatusAsync(int employeeId, EmployeeStatus status);
        Task<Result<List<EmployeeReadDto>>> GetListAsync(EmployeeListFilterDto? filter = null);
        Task<Result<EmployeeReadDto>> GetByIdAsync(int id);
        Task<Result<List<EmployeeReadDto>>> GetActiveEmployeesAsync();
        Task<Result<bool>> LinkUserAsync(int employeeId, int userId);
        Task<Result<bool>> UnlinkUserAsync(int employeeId);
        Task<Result<EmployeeAnalyticsDto>> GetAnalyticsAsync();
        Task<Result<List<EmployeeReportRowDto>>> GetReportAsync(EmployeeReportFilterDto filter);
        Task<Result<bool>> SoftDeleteAsync(int employeeId);
    }

    public class EmployeeService : IEmployeeService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUOW _uow;
        private readonly IMapper _mapper;
        private readonly IEmployeeFeatureService _featureService;

        public EmployeeService(ApplicationDbContext dbContext, IUOW uow, IMapper mapper, IEmployeeFeatureService featureService)
        {
            _dbContext = dbContext;
            _uow = uow;
            _mapper = mapper;
            _featureService = featureService;
        }

        public async Task<Result<EmployeeCreateDto>> CreateAsync(EmployeeCreateDto dto)
        {
            var validation = await ValidateAsync(dto.Id, dto.Code, dto.FullName, dto.PhoneNumber, dto.Email, dto.NationalId, dto.UserId);
            if (!validation.Success)
                return Result<EmployeeCreateDto>.Fail(validation.Message);

            var entity = _mapper.Map<EmployeeEntity>(dto);
            entity.CreatedDate = DateTime.Now;
            entity.UpdatedDate = DateTime.Now;

            await _uow.GetRepository<EmployeeEntity>().AddAsync(entity);
            await _uow.CommitAsync();

            dto.Id = entity.Id;
            return Result<EmployeeCreateDto>.Ok(dto, "Employee created successfully.");
        }

        public async Task<Result<EmployeeUpdateDto>> UpdateAsync(EmployeeUpdateDto dto)
        {
            var entity = await _dbContext.Set<EmployeeEntity>().FirstOrDefaultAsync(x => x.Id == dto.Id);
            if (entity == null)
                return Result<EmployeeUpdateDto>.Fail("Employee not found.");

            var validation = await ValidateAsync(dto.Id, dto.Code, dto.FullName, dto.PhoneNumber, dto.Email, dto.NationalId, dto.UserId);
            if (!validation.Success)
                return Result<EmployeeUpdateDto>.Fail(validation.Message);

            var createdDate = entity.CreatedDate;
            _mapper.Map(dto, entity);
            entity.CreatedDate = createdDate;
            entity.UpdatedDate = DateTime.Now;

            await _uow.GetRepository<EmployeeEntity>().UpdateAsync(entity);
            await _uow.CommitAsync();
            return Result<EmployeeUpdateDto>.Ok(dto, "Employee updated successfully.");
        }

        public async Task<Result<bool>> SetStatusAsync(int employeeId, EmployeeStatus status)
        {
            var entity = await _dbContext.Set<EmployeeEntity>().FirstOrDefaultAsync(x => x.Id == employeeId);
            if (entity == null)
                return Result<bool>.Fail("Employee not found.");

            entity.Status = status;
            entity.UpdatedDate = DateTime.Now;
            await _dbContext.SaveChangesAsync();
            return Result<bool>.Ok(true, "Employee status updated.");
        }

        public async Task<Result<List<EmployeeReadDto>>> GetListAsync(EmployeeListFilterDto? filter = null)
        {
            var query = _dbContext.Set<EmployeeEntity>()
                .Include(x => x.User)
                .Include(x => x.Manager)
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
                        (x.PhoneNumber != null && x.PhoneNumber.Contains(search)) ||
                        (x.Email != null && x.Email.Contains(search)));
                }

                if (filter.Status.HasValue)
                    query = query.Where(x => x.Status == filter.Status.Value);

                if (filter.BranchId.HasValue)
                    query = query.Where(x => x.BranchId == filter.BranchId.Value);

                if (filter.DepartmentId.HasValue)
                    query = query.Where(x => x.DepartmentId == filter.DepartmentId.Value);

                if (!string.IsNullOrWhiteSpace(filter.JobTitle))
                    query = query.Where(x => x.JobTitle != null && x.JobTitle.Contains(filter.JobTitle));

                if (filter.OnlyActive)
                    query = query.Where(x => x.Status == EmployeeStatus.Active);
            }

            var employees = await query.OrderBy(x => x.FullName).ToListAsync();
            return Result<List<EmployeeReadDto>>.Ok(_mapper.Map<List<EmployeeReadDto>>(employees));
        }

        public async Task<Result<EmployeeReadDto>> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Set<EmployeeEntity>()
                .Include(x => x.User)
                .Include(x => x.Manager)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return Result<EmployeeReadDto>.Fail("Employee not found.");

            return Result<EmployeeReadDto>.Ok(_mapper.Map<EmployeeReadDto>(entity));
        }

        public async Task<Result<List<EmployeeReadDto>>> GetActiveEmployeesAsync()
        {
            if (!await _featureService.IsEnabledAsync())
                return Result<List<EmployeeReadDto>>.Ok(new List<EmployeeReadDto>());

            return await GetListAsync(new EmployeeListFilterDto { OnlyActive = true });
        }

        public async Task<Result<bool>> LinkUserAsync(int employeeId, int userId)
        {
            var entity = await _dbContext.Set<EmployeeEntity>().FirstOrDefaultAsync(x => x.Id == employeeId);
            if (entity == null)
                return Result<bool>.Fail("Employee not found.");

            var conflict = await _dbContext.Set<EmployeeEntity>()
                .AnyAsync(x => x.Id != employeeId && x.UserId == userId);

            if (conflict)
                return Result<bool>.Fail("This user is already linked to another employee.");

            entity.UserId = userId;
            entity.UpdatedDate = DateTime.Now;
            await _dbContext.SaveChangesAsync();
            return Result<bool>.Ok(true, "User linked successfully.");
        }

        public async Task<Result<bool>> UnlinkUserAsync(int employeeId)
        {
            var entity = await _dbContext.Set<EmployeeEntity>().FirstOrDefaultAsync(x => x.Id == employeeId);
            if (entity == null)
                return Result<bool>.Fail("Employee not found.");

            entity.UserId = null;
            entity.UpdatedDate = DateTime.Now;
            await _dbContext.SaveChangesAsync();
            return Result<bool>.Ok(true, "User unlinked successfully.");
        }

        public async Task<Result<EmployeeAnalyticsDto>> GetAnalyticsAsync()
        {
            var employees = await _dbContext.Set<EmployeeEntity>().AsNoTracking().ToListAsync();

            var dto = new EmployeeAnalyticsDto
            {
                TotalEmployees = employees.Count,
                ActiveEmployees = employees.Count(x => x.Status == EmployeeStatus.Active),
                InactiveEmployees = employees.Count(x => x.Status == EmployeeStatus.Inactive),
                SuspendedEmployees = employees.Count(x => x.Status == EmployeeStatus.Suspended),
                TerminatedEmployees = employees.Count(x => x.Status == EmployeeStatus.Terminated),
                CountByBranch = employees.Where(x => x.BranchId.HasValue).GroupBy(x => x.BranchId!.Value).ToDictionary(x => x.Key, x => x.Count()),
                CountByDepartment = employees.Where(x => x.DepartmentId.HasValue).GroupBy(x => x.DepartmentId!.Value).ToDictionary(x => x.Key, x => x.Count()),
                CountByJobTitle = employees.Where(x => !string.IsNullOrWhiteSpace(x.JobTitle)).GroupBy(x => x.JobTitle!).ToDictionary(x => x.Key, x => x.Count()),
                RecentlyHiredEmployees = employees.Where(x => x.HireDate.HasValue).OrderByDescending(x => x.HireDate).Take(5).Select(x => x.FullName).ToList(),
                LastActivityDate = employees.OrderByDescending(x => x.UpdatedDate).Select(x => (DateTime?)x.UpdatedDate).FirstOrDefault()
            };

            return Result<EmployeeAnalyticsDto>.Ok(dto);
        }

        public async Task<Result<List<EmployeeReportRowDto>>> GetReportAsync(EmployeeReportFilterDto filter)
        {
            var query = _dbContext.Set<EmployeeEntity>()
                .Include(x => x.User)
                .AsNoTracking()
                .AsQueryable();

            if (filter.EmployeeId.HasValue)
                query = query.Where(x => x.Id == filter.EmployeeId.Value);

            if (filter.BranchId.HasValue)
                query = query.Where(x => x.BranchId == filter.BranchId.Value);

            if (filter.DepartmentId.HasValue)
                query = query.Where(x => x.DepartmentId == filter.DepartmentId.Value);

            if (filter.Status.HasValue)
                query = query.Where(x => x.Status == filter.Status.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(x => x.HireDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(x => x.HireDate <= filter.ToDate.Value);

            var rows = await query
                .OrderBy(x => x.FullName)
                .Select(x => new EmployeeReportRowDto
                {
                    EmployeeId = x.Id,
                    EmployeeName = x.FullName,
                    EmployeeCode = x.Code,
                    Status = x.Status,
                    BranchId = x.BranchId,
                    DepartmentId = x.DepartmentId,
                    JobTitle = x.JobTitle,
                    HireDate = x.HireDate,
                    LinkedUserName = x.User != null ? x.User.Name : null
                })
                .ToListAsync();

            return Result<List<EmployeeReportRowDto>>.Ok(rows);
        }

        public async Task<Result<bool>> SoftDeleteAsync(int employeeId)
        {
            var entity = await _dbContext.Set<EmployeeEntity>().FirstOrDefaultAsync(x => x.Id == employeeId);
            if (entity == null)
                return Result<bool>.Fail("Employee not found.");

            entity.IsDeleted = true;
            entity.Status = entity.Status == EmployeeStatus.Terminated ? EmployeeStatus.Terminated : EmployeeStatus.Inactive;
            entity.UpdatedDate = DateTime.Now;
            await _dbContext.SaveChangesAsync();
            return Result<bool>.Ok(true, "Employee deleted successfully.");
        }

        private async Task<Result<bool>> ValidateAsync(int id, string code, string fullName, string? phoneNumber, string? email, string? nationalId, int? userId)
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

            if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@'))
                return Result<bool>.Fail("Email format is not valid.");

            var duplicateCode = await _dbContext.Set<EmployeeEntity>().AnyAsync(x => x.Id != id && x.Code == code);
            if (duplicateCode)
                return Result<bool>.Fail("Employee code must be unique.");

            if (!string.IsNullOrWhiteSpace(nationalId))
            {
                var duplicateNationalId = await _dbContext.Set<EmployeeEntity>().AnyAsync(x => x.Id != id && x.NationalId == nationalId);
                if (duplicateNationalId)
                    return Result<bool>.Fail("Identity number must be unique.");
            }

            if (userId.HasValue)
            {
                var duplicateUser = await _dbContext.Set<EmployeeEntity>().AnyAsync(x => x.Id != id && x.UserId == userId.Value);
                if (duplicateUser)
                    return Result<bool>.Fail("The selected user is already linked to another employee.");
            }

            return Result<bool>.Ok(true);
        }
    }
}
