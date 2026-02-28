using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Brands;
using RaccoonWarehouse.Domain.SubCategories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Relations
{
    public class SubCategoryBrand : BaseEntity
    {
        public int SubCategoryId { get; set; }
        [JsonIgnore]
        public SubCategory? SubCategory { get; set; }

        public int BrandId { get; set; }
        [JsonIgnore]
        public Brand? Brand { get; set; }
    }
}
