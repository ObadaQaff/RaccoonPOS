using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;


namespace RaccoonWarehouse.Domain.Units.DTOs
{
    public class UnitReadDto: IBaseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<ProductUnitReadDto>? ProductUnits { get; set; } = new List<ProductUnitReadDto>();
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
