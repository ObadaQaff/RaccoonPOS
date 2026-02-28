using RaccoonWarehouse.Core.Interface;

namespace RaccoonWarehouse.Core.Common
{
    public class PagedResult<T> : IPagedResult<T>
    {
        public int PageNumber { get; private set; }
        public int PageSize { get; private set; }
        public int TotalCount { get; private set; }
        public int TotalPages { get; private set; }
        public IEnumerable<T> Items { get; private set; }

        public PagedResult(IEnumerable<T> Data, int totalCount, int pageNumber, int pageSize)
        {
            Items = Data;
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        }
    }
}
