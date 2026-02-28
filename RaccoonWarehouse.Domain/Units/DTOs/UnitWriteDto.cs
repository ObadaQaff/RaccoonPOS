using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Units.DTOs
{
    public class UnitWriteDto: IBaseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<ProductUnitWriteDto>? ProductUnits { get; set; } = new List<ProductUnitWriteDto>();
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
