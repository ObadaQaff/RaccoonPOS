using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.Relations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Brands.DTOs
{
    public class BrandReadDto : IBaseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? ImageUrl { get; set; }
        public ICollection<SubCategoryBrand>? SubCategoryBrands { get; set; } = new List<SubCategoryBrand>();
        public ICollection<ProductReadDto>? Products { get; set; } = new List<ProductReadDto>();
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; } 


    }
}
