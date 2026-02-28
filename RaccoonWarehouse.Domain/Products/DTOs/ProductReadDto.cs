using RaccoonWarehouse.Core.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.Brands.DTOs;
using RaccoonWarehouse.Domain.Enums;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.ProductUnits.DTOs;
using RaccoonWarehouse.Domain.SubCategories.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Products.DTOs
{
    public class ProductReadDto : IBaseDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public long? ITEMCODE { get; set; } // باركود
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public ProductStatus Status { get; set; }
        public bool? TaxExempt { get; set; } = false;
        public decimal? TaxRate { get; set; } = 16; // percentage (e.g. Jordan VAT)
        public decimal? MiniQuantity { get; set; }
        public SubCategoryReadDto? SubCategory {  get; set; }
        public bool IsDeleted { get; set; } = false;

        public int SubCategoryId { get; set; }
        public BrandReadDto? Brand { get; set; }
        public int? BrandId { get; set; }
        public ICollection<ProductUnitReadDto>? ProductUnits { get; set; } = new List<ProductUnitReadDto>();
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; } 

    }
}
