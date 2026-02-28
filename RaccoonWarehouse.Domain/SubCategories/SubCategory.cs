using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Categories;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.Relations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.SubCategories
{
    public class SubCategory : BaseEntity
    {
        public string Name { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public ICollection<Product>? Products { get; set; } = new List<Product>();
        public ICollection<SubCategoryBrand> SubCategoryBrands { get; set; } = new List<SubCategoryBrand>();
        public Category ParentCategory { get; set; }
        public int ParentCategoryId { get; set; }

    }
}
