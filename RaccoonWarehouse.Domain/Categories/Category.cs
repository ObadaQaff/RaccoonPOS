using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.SubCategories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonWarehouse.Domain.Categories
{
    public class Category :BaseEntity
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public ICollection<SubCategory>? SubCategories { get; set; } =new List<SubCategory>();

    }
}
