using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Core.Interface
{
	    public interface IGenericService<T, TWriteDto, TReadDto>:IGenericRepository<T>
		    where T : BaseEntity
		    where TWriteDto : IBaseDto
		    where TReadDto : IBaseDto
		
	    {
        Task<Result<List<TReadDto>>> GetAllWithAdvancedIncludeAsync(
            Func<IQueryable<T>, IQueryable<T>> includeFunc,
            Expression<Func<T, bool>> predicate = null);

        Task<Result<TReadDto>> GetByIdAsync(int id);
		Task<Result<TWriteDto>> GetWriteDtoByIdAsync(int id);
		Task<Result<List<TReadDto>>> GetAllAsync();
        Task<Result<List<TReadDto>>>GetAllWithFilteringAndIncludeAsync(Expression<Func<T, bool>>
            predicate,params Expression<Func<T, object>>[] includes );
        Task<Result<List<TWriteDto>>>GetAllWriteDtoWithFilteringAndIncludeAsync(Expression<Func<T, bool>>
            predicate,params Expression<Func<T, object>>[] includes );
        Task<Result<bool>> SoftDeleteAsync(int id);

        Task<Result<TWriteDto>> CreateAsync(TWriteDto dto);
		Task<Result<TWriteDto>> UpdateAsync(TWriteDto dto);
		Task<Result<bool>> DeleteAsync(int id);
		Task<Result<List<TReadDto>>> GetAllWithIncludeAsync(
			params Expression<Func<T, object>>[] includes);
		Task<Result<TReadDto>> GetByIdWithIncludeAsync(
			Expression<Func<T, bool>> predicate,
			params Expression<Func<T, object>>[] includes);
        Task<Result<TWriteDto>> GetWriteDtoByIdWithIncludeAsync(
            Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includes);
        Task<IPagedResult<T>> GetPagedListAsync(
           int pageNumber,
           int pageSize,
           Expression<Func<T, bool>> filter = null,
           Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
           params Expression<Func<T, object>>[] includes);
        Task<IPagedResult<TReadDto>> GetReadDtoPagedListAsync(
           int pageNumber,
           int pageSize,
           Expression<Func<T, bool>> filter = null,
           Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
           params Expression<Func<T, object>>[] includes);
    }

}