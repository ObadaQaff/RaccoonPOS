using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.EntityAndDtoStructure
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
    }

}
