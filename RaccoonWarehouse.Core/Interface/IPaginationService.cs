using System.Linq.Expressions;

namespace RaccoonWarehouse.Core.Interface
{
    public interface IPaginationService<T>
    {
        Task<IPagedResult<T>> GetPagedListAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            params Expression<Func<T, object>>[] includes);
    }
}
