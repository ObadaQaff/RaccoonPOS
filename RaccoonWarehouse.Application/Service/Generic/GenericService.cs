using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Data.Repository;
using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Products.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Application.Service.Generic
{
    public class GenericService<T, TWriteDto, TReadDto> : GenericService<T>, IGenericService<T, TWriteDto, TReadDto>
    where T : BaseEntity
    where TWriteDto : IBaseDto
    where TReadDto : IBaseDto
    {
        private readonly IGenericRepository<T> _repository;
        private readonly IMapper _mapper;
        private readonly IUOW _uow;

        public GenericService(ApplicationDbContext context, IUOW uow, IMapper mapper
            ) : base(context, mapper)

        {
            _uow = uow;
            _repository = _uow.GetRepository<T>();
            _mapper = mapper;
        }

        public async Task<Result<TReadDto>> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<TReadDto>.Fail("Entity not found.");

            var dto = _mapper.Map<TReadDto>(entity);
            return Result<TReadDto>.Ok(dto);
        }

        public virtual async Task<Result<List<TReadDto>>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            var dtos = _mapper.Map<List<TReadDto>>(entities);
            return Result<List<TReadDto>>.Ok(dtos);
        }

        public virtual async Task<Result<TWriteDto>> CreateAsync(TWriteDto dto)
        {
            var entity = _mapper.Map<T>(dto);
            var jordanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
            entity.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, jordanTimeZone);
            await _repository.AddAsync(entity);
            await _uow.CommitAsync();

            var createdDto = _mapper.Map<TWriteDto>(entity);
            return Result<TWriteDto>.Ok(createdDto, "Entity created successfully.");
        }

        public virtual async Task<Result<TWriteDto>> UpdateAsync(TWriteDto dto)
        {
            var entity = await _repository.GetByIdAsync(dto.Id);
            var jordanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");
            entity.UpdatedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, jordanTimeZone);
            dto.CreatedDate = entity.CreatedDate;
            if (entity == null)
                return Result<TWriteDto>.Fail("Entity not found.");

            _mapper.Map(dto, entity);
            await _repository.UpdateAsync(entity);
            await _uow.CommitAsync();

            return Result<TWriteDto>.Ok(dto, "Entity updated successfully.");
        }

        public virtual async Task<Result<bool>> DeleteAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<bool>.Fail("Entity not found.");

            await _repository.DeleteAsync(id);

            await _uow.CommitAsync();

            return Result<bool>.Ok(true, "Entity deleted successfully.");
        }
        public virtual async Task<Result<bool>> SoftDeleteAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<bool>.Fail("Entity not found.");

            await _repository.SoftDeleteAsync_repo(entity);

            await _uow.CommitAsync();

            return Result<bool>.Ok(true, "Entity deleted successfully.");
        }
        public virtual async Task<Result<List<TReadDto>>> GetAllWithIncludeAsync(
          params Expression<Func<T, object>>[] includes)
        {
            var entities = await _repository.GetAllWithIncludeAsync(includes);
            var dtos = _mapper.Map<List<TReadDto>>(entities);
            return Result<List<TReadDto>>.Ok(dtos);
        }
        public virtual async Task<Result<TReadDto>> GetByIdWithIncludeAsync(
            Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[]? includes)
        {

            var entity = _repository.GetByIdWithIncludeAsync(predicate, includes).Result;

            if (entity == null)
            {
                return Result<TReadDto>.Fail("Entity not found");
            }

            var dto = _mapper.Map<TReadDto>(entity);
            return Result<TReadDto>.Ok(dto);
        }




        public async Task<Result<TWriteDto>> GetWriteDtoByIdWithIncludeAsync(
            Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includes)
        {


            var entity = _repository.GetByIdWithIncludeAsync(predicate, includes).Result;

            if (entity == null)
            {
                return Result<TWriteDto>.Fail("Entity not found");
            }

            var dto = _mapper.Map<TWriteDto>(entity);
            return Result<TWriteDto>.Ok(dto);


        }
        public async Task<IPagedResult<T>> GetPagedListAsync(
           int pageNumber,
           int pageSize,
           Expression<Func<T, bool>> filter = null,
           Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
           params Expression<Func<T, object>>[] includes)
        {
            return await _repository.GetPagedListAsync(pageNumber, pageSize, filter, orderBy, includes);
        }

        public async Task<Result<TWriteDto>> GetWriteDtoByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            var dto = _mapper.Map<TWriteDto>(entity);
            return Result<TWriteDto>.Ok(dto);

        }

        public async Task<Result<List<TReadDto>>> GetAllWithFilteringAndIncludeAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
        {

            var result = await _repository.GetAllWithFilteringAndInclude(predicate, includes);
            var dtos = _mapper.Map<List<TReadDto>>(result);
            return Result<List<TReadDto>>.Ok(dtos);
        }
        public async Task<Result<List<TWriteDto>>> GetAllWriteDtoWithFilteringAndIncludeAsync(Expression<Func<T, bool>>
            predicate, params Expression<Func<T, object>>[] includes)
        {

            var result = await _repository.GetAllWithFilteringAndInclude(predicate, includes);
            var dtos = _mapper.Map<List<TWriteDto>>(result);
            return Result<List<TWriteDto>>.Ok(dtos);
        }

        /// <summary>
        /// Get list of TReadDto with advanced includes (ThenInclude, nested, filters inside include)
        /// without touching existing methods
        /// </summary>
        /// <param name="includeFunc">Function to customize IQueryable includes</param>
        /// <param name="predicate">Optional filter</param>
        public async Task<Result<List<TReadDto>>> GetAllWithAdvancedIncludeAsync(
            Func<IQueryable<T>, IQueryable<T>> includeFunc,
            Expression<Func<T, bool>> predicate = null)
        {
            IQueryable<T> query = _context.Set<T>().AsNoTracking();

            if (includeFunc != null)
                query = includeFunc(query);

            if (predicate != null)
                query = query.Where(predicate);

            var entities = await query.ToListAsync();
            var dtos = _mapper.Map<List<TReadDto>>(entities);
            return Result<List<TReadDto>>.Ok(dtos);
        }


        public async Task<IPagedResult<TReadDto>> GetReadDtoPagedListAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            params Expression<Func<T, object>>[] includes)
        {
            var entities = await _repository.GetPagedListAsync(pageNumber, pageSize, filter, orderBy, includes);

            if (entities == null)
                return new PagedResult<TReadDto>(new List<TReadDto>(), 0, pageNumber, pageSize);

            // Map entity items to DTOs
            var dtoItems = _mapper.Map<List<TReadDto>>(entities.Items);

            // Return a PagedResult of DTOs
            var result = new PagedResult<TReadDto>(
                dtoItems,
                entities.TotalCount,
                entities.PageNumber,
                entities.PageSize
            );

            return result;
        }

        
    }
}