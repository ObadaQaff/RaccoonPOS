using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Brands;
using RaccoonWarehouse.Domain.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.SubCategories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Products
{
    public class Product: BaseEntity, ISoftDelete
    {
        public string? Name { get; set; }
        public long? ITEMCODE { get; set; } // باركود
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public ProductStatus? Status { get; set; }
        public bool? TaxExempt { get; set; } = false;
        public decimal? TaxRate { get; set; } = 16;
        public decimal? MiniQuantity { get; set; } 
        public SubCategory SubCategory { get; set; }
        public int SubCategoryId { get; set; }
        public Brand? Brand { get; set; }
        public bool IsDeleted { get; set; } = false;
        public bool? IsSoldOut { get; set; } = false; //temp 

        public int? BrandId { get; set; }
       public ICollection<ProductUnit>? ProductUnits { get; set; } = new List<ProductUnit>();
        public DateTime? EndDate { get; set; }//temp


    }

}




