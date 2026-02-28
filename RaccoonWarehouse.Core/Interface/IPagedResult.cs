using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Core.Interface
{
    public interface IPagedResult<T>
    {
        int PageNumber { get; }
        int PageSize { get; }
        int TotalCount { get; }
        int TotalPages { get; }
        IEnumerable<T> Items { get; }
    }
}

