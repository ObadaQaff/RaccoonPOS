using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.Relations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Brands
{
    public class Brand : BaseEntity
    {
        public string Name { get; set; }
        public string? ImageUrl { get; set; }
        public ICollection<SubCategoryBrand>? SubCategoryBrands { get; set; } = new List<SubCategoryBrand>();
        public ICollection<Product>? Products { get; set; } = new List<Product>();
    }
}
