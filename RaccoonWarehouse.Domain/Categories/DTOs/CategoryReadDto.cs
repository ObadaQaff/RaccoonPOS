using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.SubCategories;
using RaccoonWarehouse.Domain.SubCategories.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Categories.DTOs
{
    public class CategoryReadDto : IBaseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public ICollection<SubCategoryReadDto>? SubCategories { get; set; } = new List<SubCategoryReadDto>();
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
