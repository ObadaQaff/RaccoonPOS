using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Warehouses.DTOs
{
    public class WarehouseWriteDto:IBaseDto
    {
      
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public int? PhoneNumber { get; set; }
        public WarehouseStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
