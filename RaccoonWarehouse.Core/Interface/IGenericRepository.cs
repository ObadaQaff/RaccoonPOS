using RaccoonWarehouse.Core.Common;
using RaccoonWarehouse.Domain.Base;
using System.Linq.Expressions;

namespace RaccoonWarehouse.Core.Interface
{
    public interface IGenericRepository<T> where T : BaseEntity
	{
		Task<T>? AddAsync(T entity);
		Task<IEnumerable<T>> GetAllAsync();  

		Task<T> GetByIdAsync (int id);
		T GetById(int id);
        Task SoftDeleteAsync_repo(T entity);

        Task<T> GetByIdAsyncForUpdate(int id);
		Task<T> UpdateAsync(T entity);
		bool Any(Expression<Func<T, bool>> predicate);
		Task<bool> AnyAsync(Expression<Func<T, bool>> condition);
		Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
		Task<bool> DeleteAsync(int id);
		IQueryable<T> GetAllAsQueryable();
		Task<IEnumerable<T>> GetAllWithIncludeAsync(
			params Expression<Func<T, object>>[] includes);
		Task<T> GetByIdWithIncludeAsync(
			Expression<Func<T, bool>> predicate,
			params Expression<Func<T, object>>[]? includes);

        Task<bool> DeleteWithIncludeAsync(
               Expression<Func<T, bool>> predicate,
               params Expression<Func<T, object>>[] includes);


        Task<IEnumerable<T>> GetAllWithFilteringAndInclude(
            Expression<Func<T, bool>> filter = null,
            params Expression<Func<T, object>>[] includes);
        IQueryable<T> AsQueryable();
        
        
        
        Task<IPagedResult<T>> GetPagedListAsync(
           int pageNumber,
           int pageSize,
           Expression<Func<T, bool>> filter = null,
           Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
           params Expression<Func<T, object>>[] includes);


        public interface ISoftDelete
        {
            bool IsDeleted { get; set; }
        }


        /*  Task<T> GetAllAsyncWithFiltering<T>(
          FilterOptions<T>? filterOptions = null,
          IncludeOptions<T>? includeOptions = null,
          PaginationParams? paginationParams = null
      );*/

    }
}
