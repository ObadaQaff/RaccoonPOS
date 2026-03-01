using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Core.Interface;
using RaccoonWarehouse.Data.Configurations;
using RaccoonWarehouse.Domain.Base;
using RaccoonWarehouse.Domain.Categories;
using RaccoonWarehouse.Domain.EntityAndDtoStructure;
using RaccoonWarehouse.Domain.InvoiceLines;
using RaccoonWarehouse.Domain.Products;
using RaccoonWarehouse.Domain.ProductUnits;
using RaccoonWarehouse.Domain.Relations;
using RaccoonWarehouse.Domain.Users;
using System.Linq.Expressions;
using System.Reflection;

namespace RaccoonWarehouse.Data
{
    public class ApplicationDbContext : DbContext
    {   
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

     

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(DatabaseConnectionStringProvider.GetConnectionString());
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new CategoryConfiguration());
            var assembly = typeof(BaseEntity).Assembly;
            var entityTypes = assembly?.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(BaseEntity)));
            if (entityTypes == null) return;
            foreach (var type in entityTypes)
            {
                modelBuilder.Entity(type);
            }
            // ✅ Disable all cascade delete paths to avoid multiple cascade errors
            modelBuilder.Entity<InvoiceLine>()
                .HasOne(il => il.Invoice)
                .WithMany(i => i.InvoiceLines)
                .HasForeignKey(il => il.InvoiceId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InvoiceLine>()
                .HasOne(il => il.Product)
                .WithMany()
                .HasForeignKey(il => il.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InvoiceLine>()
                .HasOne(il => il.ProductUnit)
                .WithMany()
                .HasForeignKey(il => il.ProductUnitId)
                .OnDelete(DeleteBehavior.NoAction);

            // ✅ Disable cascade between Product and ProductUnit
            modelBuilder.Entity<ProductUnit>()
                .HasOne(pu => pu.Product)
                .WithMany(p => p.ProductUnits)
                .HasForeignKey(pu => pu.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
          


            // ✅ Disable cascade between ProductUnit and Unit
            modelBuilder.Entity<ProductUnit>()
                .HasOne(pu => pu.Unit)
                .WithMany()
                .HasForeignKey(pu => pu.UnitId)
                .OnDelete(DeleteBehavior.NoAction);

            // ✅ Configure SubCategoryBrand relations
            modelBuilder.Entity<SubCategoryBrand>()
                .HasOne(sb => sb.SubCategory)
                .WithMany(s => s.SubCategoryBrands)
                .HasForeignKey(sb => sb.SubCategoryId);

            modelBuilder.Entity<SubCategoryBrand>()
                .HasOne(sb => sb.Brand)
                .WithMany(b => b.SubCategoryBrands)
                .HasForeignKey(sb => sb.BrandId);


            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .HasQueryFilter(GetIsDeletedRestriction(entityType.ClrType));
                }
            }

          
    }
        private static LambdaExpression GetIsDeletedRestriction(Type type)
        {
            var param = Expression.Parameter(type, "e");
            var prop = Expression.Property(param, nameof(ISoftDelete.IsDeleted));
            var condition = Expression.Equal(prop, Expression.Constant(false));
            var lambda = Expression.Lambda(condition, param);
            return lambda;
        }

    }
}
