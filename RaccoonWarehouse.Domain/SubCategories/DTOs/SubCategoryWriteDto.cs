using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Categories;
using RaccoonWarehouse.Domain.Categories.DTOs;
using RaccoonWarehouse.Domain.Products.DTOs;
using RaccoonWarehouse.Domain.Relations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.SubCategories.DTOs
{
    public class SubCategoryWriteDto : IBaseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public ICollection<ProductWriteDto>? Products { get; set; } = new List<ProductWriteDto>();
        public ICollection<SubCategoryBrand> SubCategoryBrands { get; set; } = new List<SubCategoryBrand>();
        public int ParentCategoryId { get; set; }
        public Category ParentCategory { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
