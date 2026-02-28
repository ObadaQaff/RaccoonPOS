    using RaccoonWarehouse.Core.EntityAndDtoStructure;
    using RaccoonWarehouse.Domain.Base;
    using RaccoonWarehouse.Domain.Enums;
    using RaccoonWarehouse.Domain.StockItems;
    using RaccoonWarehouse.Domain.StockItems.DTOs;
using RaccoonWarehouse.Domain.Users;
using RaccoonWarehouse.Domain.Users.DTOs;
using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace RaccoonWarehouse.Domain.StockDocuments.DTOs
    {
         public class StockDocumentWriteDto:IBaseDto
         {
        
            public int Id { get; set; }
            public string DocumentNumber { get; set; } = string.Empty;
            public StockVoucherType Type { get; set; }
            public int? SupplierId { get; set; }
            public UserWriteDto? Supplier { get; set; }
            public string? Notes { get; set; }

            public List<StockItemWriteDto> Items { get; set; }
            public DateTime CreatedDate { get; set; }
            public DateTime UpdatedDate { get; set; }
         }
    }
